using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 透视点阵(相位激光的重拍):一整片无人机停在 Z <see cref="VDDirector.LatticeDepth"/> 的背景里,平面坐标是以 Center 为中心、
    /// LatticeRows × LatticeCols 的格点(ai[2] = 1 时整片错开半格);ai[0] 帧预警(每架从投影位置拉一条细线收敛到脚下格点,格点小环收紧变亮),
    /// 到时全部格点同帧打一记半径 LatticeSpotRadius 的 Z 射线(前 LatticeStrikeFrames 帧判定),再 14 帧收干。
    /// 一片格点是一个弹幕:一个生成包、每层一次批次切换画完 63 架;判定只查离玩家最近的 3 × 3 格点(格距 110 &gt; 2 × 半径 40,不会同时踩两个)。
    /// 无人机 + 预警线在远景层(墙后物块前),格点环 + 打击射线 + 落点光盘在平面标记层(实心物块之后、NPC 之前):射线是朝镜头打过来的,近端粗。
    /// ai[1] 为无人机深度。命中 372 + 带电 3 秒
    /// </summary>
    public class VDLatticeField : VDHostileProjectile, IVDDepthDrawable
    {
        public const int FadeFrames = 14;
        public const int AppearFrames = 8;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => BuffID.Electrified;
        public override int DefaultTimeLeft => 120;

        public int WarnTime => (int)Math.Max(Projectile.ai[0], 10f);
        public float SourceDepth => Projectile.ai[1] > 0f ? Projectile.ai[1] : VDDirector.LatticeDepth;
        public bool HalfShift => Projectile.ai[2] > 0.5f;
        public int Age => (int)Projectile.localAI[1];
        public bool Striking => Age >= WarnTime && Age < WarnTime + VDDirector.LatticeStrikeFrames;
        float IVDDepthDrawable.DepthZ => SourceDepth;

        /// <summary>本帧无人机是否画在远景层(地下 / 投影点落在实心物块里时退回弹幕层)</summary>
        private bool farLayer;

        public override void SetExtraDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>第 r 行 c 列格点(平面坐标)</summary>
        public Vector2 GridPoint(int r, int c) {
            float shift = HalfShift ? VDDirector.LatticeSpacing * 0.5f : 0f;
            return Projectile.Center + new Vector2(
                (c - (VDDirector.LatticeCols - 1) * 0.5f) * VDDirector.LatticeSpacing + shift,
                (r - (VDDirector.LatticeRows - 1) * 0.5f) * VDDirector.LatticeSpacing + shift);
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = WarnTime + VDDirector.LatticeStrikeFrames + FadeFrames;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("vbapear", 1.25f, Projectile.Center, 4, 0.6f);
                }
            }
            Projectile.localAI[1]++;
            Projectile.velocity = Vector2.Zero;

            if (Age == WarnTime && !Main.dedServ) {
                //整片同帧打下:一次音效 + 一次震屏,不按格点各放一遍
                CEUtils.PlaySound("void_laser", 1.35f, Projectile.Center, 4, 0.7f);
                CEUtils.SetShake(Projectile.Center, 4f, 1600f);
                for (int r = 0; r < VDDirector.LatticeRows; r++) {
                    for (int c = 0; c < VDDirector.LatticeCols; c++) {
                        if ((r + c) % 3 != 0) {
                            continue;
                        }
                        Vector2 p = GridPoint(r, c);
                        Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
                        VDVfx.Spark(p, v, VDSporeDrone.WarnColor, Main.rand.NextFloat(0.5f, 0.9f), 1f, 18, gravity: true);
                    }
                }
            }
            if (Striking) {
                Lighting.AddLight(Projectile.Center, VDSporeLaser.BeamColor.ToVector3() * 0.8f);
            }
        }

        #region 判定
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Striking) {
                return false;
            }
            float shift = HalfShift ? VDDirector.LatticeSpacing * 0.5f : 0f;
            Vector2 rel = targetHitbox.Center.ToVector2() - Projectile.Center - new Vector2(shift);
            int c0 = (int)Math.Round(rel.X / VDDirector.LatticeSpacing) + (VDDirector.LatticeCols - 1) / 2;
            int r0 = (int)Math.Round(rel.Y / VDDirector.LatticeSpacing) + (VDDirector.LatticeRows - 1) / 2;
            float radiusSq = VDDirector.LatticeSpotRadius * VDDirector.LatticeSpotRadius;
            for (int r = r0 - 1; r <= r0 + 1; r++) {
                for (int c = c0 - 1; c <= c0 + 1; c++) {
                    if (r < 0 || r >= VDDirector.LatticeRows || c < 0 || c >= VDDirector.LatticeCols) {
                        continue;
                    }
                    Vector2 p = GridPoint(r, c);
                    Vector2 nearest = new Vector2(
                        MathHelper.Clamp(p.X, targetHitbox.Left, targetHitbox.Right),
                        MathHelper.Clamp(p.Y, targetHitbox.Top, targetHitbox.Bottom));
                    if (nearest.DistanceSQ(p) <= radiusSq) {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region 分层与绘制
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            farLayer = VDDepth.FarLayerUsable(VDDepth.Project(Projectile.Center, SourceDepth));
            Projectile.hide = farLayer;
            if (farLayer) {
                VDDepthStage.RegisterFar(this);
            }
            VDDepthStage.RegisterMarker(this);
        }

        /// <summary>远景层不可用时的退路:无人机在弹幕层按同样的投影画</summary>
        public override bool PreDraw(ref Color lightColor) {
            DrawDrones(Main.spriteBatch);
            return false;
        }

        void IVDDepthDrawable.DrawDepthFar(SpriteBatch spriteBatch) => DrawDrones(spriteBatch);

        void IVDDepthDrawable.DrawDepthMarker(SpriteBatch spriteBatch) => DrawPlane(spriteBatch);

        /// <summary>出现 8 帧放大;打击后按剩余寿命收缩</summary>
        private float DroneScale() {
            if (Age <= WarnTime) {
                return MathHelper.Clamp(Age / (float)AppearFrames, 0f, 1f);
            }
            return MathHelper.Clamp(Projectile.timeLeft / (float)FadeFrames, 0f, 1f);
        }

        /// <summary>打击热度:判定帧满值,之后随剩余寿命收干</summary>
        private float StrikeHeat() {
            if (Age < WarnTime) {
                return 0f;
            }
            if (Striking) {
                return 1f;
            }
            return MathHelper.Clamp(Projectile.timeLeft / (float)FadeFrames, 0f, 1f);
        }

        /// <summary>背景里的一片无人机:预警线(投影位置 → 格点)与光点一个加法批次画完,机身一个雾化批次画完</summary>
        private void DrawDrones(SpriteBatch sb) {
            float droneScale = DroneScale();
            if (droneScale <= 0.01f) {
                return;
            }
            float z = SourceDepth;
            float depthScale = VDDepth.Scale(z);
            float depthAlpha = VDDepth.Alpha(z);
            float fog = VDDepth.FogAmount(z);
            Texture2D tex = TextureAssets.Projectile[ModContent.ProjectileType<VDSporeDrone>()].Value;
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Color warn = VDDepth.Fog(VDSporeDrone.WarnColor, z);
            bool warning = Age < WarnTime;
            float charge = MathHelper.Clamp(Age / (float)WarnTime, 0f, 1f);
            float lineAlpha = 0.2f + 0.5f * charge;
            if (warning && WarnTime - Age < 15) {
                lineAlpha += 0.3f * MathF.Sin(Age * 1.2f);
            }
            lineAlpha = MathHelper.Clamp(lineAlpha, 0f, 1f) * droneScale;
            float wobble = MathF.Sin(Age * 0.15f) * 0.08f;

            sb.UseAdditive();
            for (int r = 0; r < VDDirector.LatticeRows; r++) {
                for (int c = 0; c < VDDirector.LatticeCols; c++) {
                    Vector2 p = GridPoint(r, c);
                    Vector2 shown = VDDepth.Project(p, z);
                    if (warning) {
                        CEUtils.drawLine(shown, p, VDSporeDrone.WarnColor * lineAlpha, 1.5f + 2.5f * charge, 1);
                    }
                    sb.Draw(glow, shown - Main.screenPosition, null, warn * (0.5f * depthAlpha * droneScale), 0f, glow.Size() / 2f, 0.2f * depthScale * (0.6f + 0.4f * droneScale), SpriteEffects.None, 0f);
                }
            }
            CEUtils.ReSetToEndShader();

            Effect fogShader = VDDepthDraw.Fog;
            bool shaded = fog > 0.02f && VDDepthDraw.BeginFog();
            if (shaded) {
                VDDepthDraw.ApplyFog(fogShader, tex, fog);
            }
            Color body = shaded ? Color.White : VDDepth.Fog(Color.White, z);
            float bodyScale = depthScale * droneScale;
            for (int r = 0; r < VDDirector.LatticeRows; r++) {
                for (int c = 0; c < VDDirector.LatticeCols; c++) {
                    Vector2 shown = VDDepth.Project(GridPoint(r, c), z) - Main.screenPosition;
                    sb.Draw(tex, shown, null, body * depthAlpha, wobble + (r * 0.37f + c * 0.61f) % 0.2f - 0.1f, tex.Size() / 2f, bodyScale, SpriteEffects.None, 0f);
                }
            }
            if (shaded) {
                VDDepthDraw.EndFog();
            }
        }

        /// <summary>平面层:预警期每个格点一枚收紧的小环;打击期从背景里的无人机到格点的一束束近端粗的白热射线 + 落点光盘</summary>
        private void DrawPlane(SpriteBatch sb) {
            float z = SourceDepth;
            if (Age < WarnTime) {
                float charge = MathHelper.Clamp(Age / (float)WarnTime, 0f, 1f);
                float appear = DroneScale();
                sb.UseAdditive();
                for (int r = 0; r < VDDirector.LatticeRows; r++) {
                    for (int c = 0; c < VDDirector.LatticeCols; c++) {
                        VDVfx.DrawDepthMarker(GridPoint(r, c), charge, VDDirector.LatticeSpotRadius * 0.9f, VDSporeDrone.WarnColor, 0.6f * appear, ownBatch: false);
                    }
                }
                CEUtils.ReSetToEndShader();
                return;
            }

            float hot = StrikeHeat();
            if (hot <= 0.01f) {
                return;
            }
            float radius = VDDirector.LatticeSpotRadius;
            bool batched = VDBeamDraw.BeginTapered(VDSporeLaser.BeamColor, Color.White, hot, Projectile.whoAmI * 0.29f);
            for (int r = 0; r < VDDirector.LatticeRows; r++) {
                for (int c = 0; c < VDDirector.LatticeCols; c++) {
                    Vector2 p = GridPoint(r, c);
                    Vector2 from = VDDepth.Project(p, z);
                    //传无人机的 Z:噪声向背景那端压缩、远端吃雾,几十根一起读成一片从深处打下来的射线
                    if (batched) {
                        VDBeamDraw.TaperedQuad(from, p, 4f * hot, radius * 0.9f * hot, 1f, zStart: z, zEnd: 0f);
                    }
                    else {
                        VDBeamDraw.DrawTapered(from, p, 4f * hot, radius * 0.9f * hot, VDSporeLaser.BeamColor, Color.White, 1f, hot, Projectile.whoAmI * 0.29f, endGlow: false, zStart: z, zEnd: 0f);
                    }
                }
            }
            if (batched) {
                VDBeamDraw.EndTapered();
            }

            //落点光盘:整片一个加法批次
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            float ringScale = radius * 2f / ring.Width * (1f + 0.5f * (1f - hot));
            sb.UseAdditive();
            for (int r = 0; r < VDDirector.LatticeRows; r++) {
                for (int c = 0; c < VDDirector.LatticeCols; c++) {
                    Vector2 pos = GridPoint(r, c) - Main.screenPosition;
                    sb.Draw(glow, pos, null, VDSporeLaser.BeamColor * (0.9f * hot), 0f, glow.Size() / 2f, radius * 2.4f / glow.Width, SpriteEffects.None, 0f);
                    sb.Draw(glow, pos, null, Color.White * (0.7f * hot), 0f, glow.Size() / 2f, radius * 1.2f / glow.Width, SpriteEffects.None, 0f);
                    sb.Draw(ring, pos, null, VDSporeLaser.BeamColor * (0.8f * hot), Age * 0.1f, ring.Size() / 2f, ringScale, SpriteEffects.None, 0f);
                }
            }
            CEUtils.ReSetToEndShader();
        }
        #endregion
    }
}
