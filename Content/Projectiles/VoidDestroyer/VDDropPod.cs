using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 深空投送舱(前卫教徒的载具):在 Z <see cref="VDDirector.ReinforcePodDepth"/> 的高空出现,平面坐标就是地面门的落点,
    /// 只沿 Z 坠落(表观上从背景里的一点滑落到门上、越来越大),落地那一帧在半径 <see cref="VDDirector.ReinforcePodImpactRadius"/>
    /// 内有 6 帧接触判定,同时服务端在落点放出一名教徒,舱壳再 20 帧碎成虚空烟散掉。
    /// 舱体是一团虚空能量(Glow 核 + 环壳 + 拖焰),不用新贴图。ai[0] 目标玩家索引,ai[1] 本体 whoAmI
    /// </summary>
    public class VDDropPod : VDDepthProjectile
    {
        public const int ShatterFrames = 20;
        public static readonly Color ShellColor = new Color(150, 80, 230);

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => 300;
        public override float MarkerRadius => VDDirector.ReinforcePodImpactRadius;
        public override Color MarkerColor => VDDirector.RimSummonGold;
        protected override float WhooshStrength => 0f;

        /// <summary>落地帧龄(-1 未落地)</summary>
        private int landedAge = -1;
        private bool released;

        public bool Landed => landedAge >= 0;
        public bool Impacting => landedAge >= 0 && landedAge < VDDirector.ReinforcePodImpactFrames;

        public override void SetExtraDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
        }

        protected override void OnDepthSpawn(Terraria.DataStructures.IEntitySource source) {
            if (!Main.dedServ) {
                VDVfx.HoloBurst(ProjectedCenter, VDDirector.RimSummonGold);
            }
        }

        protected override void DepthAI() {
            Projectile.velocity = Vector2.Zero;
            if (!Landed) {
                //坠落:Z 由基类积分;越过平面(或 Z 速度被同步成 0 而 Z 已到底)即落地
                if (Z <= 0f || (!HasDepth)) {
                    Land();
                }
                else if (!Main.dedServ && Z < 1f && Main.rand.NextBool(2)) {
                    //贴近平面时拖出一点虚空烟(粒子系统不分层,远处不放)
                    float sc = VDDepth.Scale(Z);
                    VDVfx.VoidPuff(ProjectedCenter + CEUtils.randomPointInCircle(14f * sc), CEUtils.randomRot().ToRotationVector2() * 1.5f, 0.9f * sc, 0.5f);
                }
                Lighting.AddLight(Projectile.Center, VDDirector.RimSummonGold.ToVector3() * 0.3f * VDDepth.Scale(Math.Max(Z, 0f)));
                return;
            }
            landedAge++;
            Lighting.AddLight(Projectile.Center, VDDirector.RimSummonGold.ToVector3() * (1f - landedAge / (float)ShatterFrames));
            if (landedAge >= ShatterFrames) {
                Projectile.Kill();
            }
        }

        /// <summary>落地:钉回平面、开判定、放教徒、一记冲击</summary>
        private void Land() {
            landedAge = 0;
            Z = 0f;
            ZVel = 0f;
            ZAccel = 0f;
            if (!released) {
                released = true;
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    int type = ModContent.NPCType<VoidVanguardCultist>();
                    int n = NPC.NewNPC(Projectile.GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Center.Y, type);
                    if (n < Main.maxNPCs) {
                        int target = (int)Projectile.ai[0];
                        Main.npc[n].target = target >= 0 && target < Main.maxPlayers ? target : Main.npc[n].target;
                        if (Main.netMode == NetmodeID.Server) {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                        }
                    }
                }
            }
            if (!Main.dedServ) {
                CEUtils.PlaySound("VoidBomb", 1.1f, Projectile.Center, 4, 0.8f);
                CEUtils.SetShake(Projectile.Center, 5f, 1400f);
                VDVfx.Explosion(Projectile.Center, 0.55f, 20);
                VDVfx.SparkBurst(Projectile.Center, VDDirector.RimSummonGold, 18, 3f, 9f, 26, 0.6f, 1.1f);
                for (int i = 0; i < 10; i++) {
                    Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
                    VDVfx.VoidPuff(Projectile.Center + v * 3f, v, 1.2f, 0.6f);
                }
            }
        }

        protected override bool? CollidingOnPlane(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Impacting) {
                return false;
            }
            Vector2 nearest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return nearest.DistanceSQ(Projectile.Center) <= VDDirector.ReinforcePodImpactRadius * VDDirector.ReinforcePodImpactRadius;
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            Vector2 drawPos = ProjectedCenter - Main.screenPosition;
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            float scale = DrawScale;
            float depthAlpha = VDDepth.Alpha(Z);
            Color shell = VDDepth.Fog(ShellColor, Z);
            Color core = VDDepth.Fog(VDDirector.RimSummonGold, Z);

            spriteBatch.UseAdditive();
            if (Landed) {
                //落地碎裂:环壳撑开淡出,核心熄灭
                float p = landedAge / (float)ShatterFrames;
                spriteBatch.Draw(ring, drawPos, null, ShellColor * (0.8f * (1f - p)), landedAge * 0.1f, ring.Size() / 2f, 0.55f + 0.9f * p, SpriteEffects.None, 0f);
                spriteBatch.Draw(glow, drawPos, null, VDDirector.RimSummonGold * (0.9f * (1f - p)), 0f, glow.Size() / 2f, 0.5f * (1f - p * 0.5f), SpriteEffects.None, 0f);
                CEUtils.ReSetToEndShader();
                return;
            }
            //坠落中:拖焰朝消失点方向(表观运动的反向),舱壳一环一核,越近越亮
            float heat = MathHelper.Clamp(1f - Z / VDDirector.ReinforcePodDepth, 0f, 1f);
            Vector2 vanish = VDDepth.CameraCenter - Main.screenPosition;
            Vector2 back = (vanish - drawPos).SafeNormalize(-Vector2.UnitY);
            Texture2D streak = CEUtils.getExtraTex("StreakSolid");
            float len = (30f + 50f * heat) * Math.Max(scale, 0.3f);
            spriteBatch.Draw(streak, drawPos + back * len * 0.5f, null, core * (0.45f * depthAlpha), back.ToRotation(), streak.Size() / 2f, new Vector2(len / streak.Width, 10f * Math.Max(scale, 0.3f) / streak.Height), SpriteEffects.None, 0f);
            spriteBatch.Draw(glow, drawPos, null, shell * (0.7f * depthAlpha), 0f, glow.Size() / 2f, 0.55f * scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(ring, drawPos, null, shell * (0.85f * depthAlpha), Main.GlobalTimeWrappedHourly * 2f, ring.Size() / 2f, 0.42f * scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glow, drawPos, null, core * ((0.6f + 0.4f * heat) * depthAlpha), 0f, glow.Size() / 2f, 0.28f * scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glow, drawPos, null, Color.White * (0.5f * heat * depthAlpha), 0f, glow.Size() / 2f, 0.12f * scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
        }
    }
}
