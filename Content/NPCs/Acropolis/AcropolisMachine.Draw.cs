using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.Acropolis.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 卫城机器的表现层:整机集中绘制(腿的三段 IK、两条手臂、本体、肩甲)、焦痕着色器、受击与死亡粒子。
    /// 纯本地,只读 gameplay 状态,绝不回写
    /// </summary>
    public partial class AcropolisMachine
    {
        //harpoonOutlineTex 设为 internal 供同目录 Harpoon 复用
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/Leg1")]
        private static Asset<Texture2D> leg1Tex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/Leg2")]
        private static Asset<Texture2D> leg2Tex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/Foot")]
        private static Asset<Texture2D> footTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/CannonConnect")]
        private static Asset<Texture2D> cannonConnectTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/Cannon")]
        private static Asset<Texture2D> cannonTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/HarpoonArm")]
        private static Asset<Texture2D> harpoonArmTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/HarpoonLauncher")]
        private static Asset<Texture2D> harpoonLauncherTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/Harpoon")]
        private static Asset<Texture2D> harpoonTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/HarpoonOutline")]
        internal static Asset<Texture2D> harpoonOutlineTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/Shoulder")]
        private static Asset<Texture2D> shoulderTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/cloudNoise")]
        private static Asset<Texture2D> cloudNoiseTex;

        public static void PrepareCharredShader(Texture2D tex, Texture2D noise, float minAlpha, Vector2 noiseOffset, Color color)
        {
            Effect shader = CommonEffects.charred;
            if (minAlpha >= 1)
            {
                Main.spriteBatch.ExitShaderRegion();
            }
            else
            {
                Main.spriteBatch.End();
                shader.Parameters["minAlpha"].SetValue(minAlpha);
                shader.Parameters["cColor"].SetValue(color.ToVector4());
                shader.Parameters["noiseOffset"].SetValue(noiseOffset);
                shader.Parameters["texSize"].SetValue(tex.Size());
                shader.Parameters["noiseSize"].SetValue(noise.Size() * 2f);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = tex;
                gd.Textures[1] = noise;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            //焦痕的随机量按 whoAmI 播种,每台机器的斑驳是固定的
            UnifiedRandom random = new UnifiedRandom((NPC.type + NPC.whoAmI * 47));
            Texture2D noise = cloudNoiseTex.Value;
            float cAlpha = random.NextBool(6) ? random.NextFloat(0.6f, 1f) : random.NextFloat(0.8f, 1f);
            void prepareShader(Texture2D texture)
            {
                float al = float.Clamp(cAlpha + random.NextFloat(-0.1f, 0.1f), 0, 1);
                if (random.NextBool(5))
                    al = 0;
                Vector2 ofs = new Vector2(random.NextFloat(0, 0.5f), random.NextFloat(0, 0.5f)) * noise.Size();
                PrepareCharredShader(texture, noise, al, ofs, Color.Black * 0.4f);
            }
            if (Main.zenithWorld)
            {
                drawColor = Main.DiscoColor;
            }
            Texture2D body = NPC.getTexture();
            Texture2D t1 = leg1Tex.Value;
            Texture2D t2 = leg2Tex.Value;
            Texture2D t3 = footTex.Value;

            if (legs == null)
                return false;
            foreach (var leg in legs)
            {
                float l1 = 46 * NPC.scale * leg.Scale;
                float l2 = 70 * NPC.scale * leg.Scale;
                float l3 = 72 * NPC.scale * leg.Scale;
                List<Vector2> points = new List<Vector2>();
                points.Add(NPC.Center + (new Vector2(Math.Sign(leg.offset.X) * 20, 60) * NPC.scale).RotatedBy(dir > 0 ? NPC.rotation : -MathHelper.Pi + NPC.rotation));
                Vector2 e = CalculateLegJoints(points[0], leg.StandPoint, l1, l2, l3, out var p1, out var p2);
                points.Add(p1);
                points.Add(CEUtils.GetCircleIntersection(p1, l2, leg.StandPoint, l3));
                points.Add(points[points.Count - 1] + (leg.StandPoint - points[points.Count - 1]).normalize() * l3);

                prepareShader(t1);
                Main.EntitySpriteDraw(t1, points[0] - Main.screenPosition, null, drawColor, (points[1] - points[0]).ToRotation(), new Vector2(4, 13), NPC.scale * leg.Scale, SpriteEffects.None);
                prepareShader(t2);
                Main.EntitySpriteDraw(t2, points[1] - Main.screenPosition, null, drawColor, (points[2] - points[1]).ToRotation(), new Vector2(6, 9), NPC.scale * leg.Scale, SpriteEffects.None);
                prepareShader(t3);
                Main.EntitySpriteDraw(t3, points[2] - Main.screenPosition, null, drawColor, (points[3] - points[2]).ToRotation() + ((leg.offset.X > 0 ? 1 : -1) * MathHelper.ToRadians(24)), new Vector2(27, t3.Height / 2f), NPC.scale * leg.Scale, leg.offset.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            }
            NPC npc = NPC;
            Texture2D cannon1 = cannonConnectTex.Value;
            Texture2D cannon2 = cannonTex.Value;
            Texture2D harpoon1 = harpoonArmTex.Value;
            Texture2D harpoon2 = harpoonLauncherTex.Value;
            Texture2D harpoon3 = harpoonTex.Value;
            Texture2D harpoonOutline = harpoonOutlineTex.Value;

            Texture2D shoulder = shoulderTex.Value;
            float charge = Context?.HarpoonCharge ?? 0f;

            prepareShader(harpoon1);
            Main.EntitySpriteDraw(harpoon1, (harpoon.offset * new Vector2(dir, 1) * NPC.scale).RotatedBy(dir > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi)) + NPC.Center - Main.screenPosition, null, drawColor, harpoon.Seg1Rot, new Vector2(6, harpoon1.Height / 2f), NPC.scale, SpriteEffects.None);
            if (_harpoon < 0 || HarpoonOnLauncher)
            {
                Main.spriteBatch.ExitShaderRegion();
                for (float r = 0; r <= 360; r += 60)
                {
                    Main.EntitySpriteDraw(harpoonOutline, MathHelper.ToRadians(r).ToRotationVector2() * 2 + harpoon.seg1end + harpoon.Seg2Rot.ToRotationVector2() * AcropolisDirector.HarpoonMuzzleReach * NPC.scale + new Vector2(0, AcropolisDirector.HarpoonMuzzleSide * dir).RotatedBy(harpoon.Seg2Rot) * NPC.scale - Main.screenPosition, null, Color.OrangeRed * charge, harpoon.Seg2Rot, new Vector2(70, harpoon3.Height / 2f), NPC.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
                }
                prepareShader(harpoon3);
                Main.EntitySpriteDraw(harpoon3, harpoon.seg1end + harpoon.Seg2Rot.ToRotationVector2() * AcropolisDirector.HarpoonMuzzleReach * NPC.scale + new Vector2(0, AcropolisDirector.HarpoonMuzzleSide * dir).RotatedBy(harpoon.Seg2Rot) * NPC.scale - Main.screenPosition, null, drawColor, harpoon.Seg2Rot, new Vector2(70, harpoon3.Height / 2f), NPC.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            }
            else
            {
                //Don't mess up the random
                prepareShader(harpoon3);
            }
            prepareShader(harpoon2);
            Main.EntitySpriteDraw(harpoon2, harpoon.seg1end - Main.screenPosition, null, drawColor, harpoon.Seg2Rot, new Vector2(6, harpoon2.Height / 2f), NPC.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            prepareShader(body);
            Main.EntitySpriteDraw(body, NPC.Center - screenPos, null, drawColor, NPC.rotation, body.Size() / 2f, NPC.scale, dir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
            prepareShader(cannon1);
            Main.EntitySpriteDraw(cannon1, (cannon.offset * new Vector2(dir, 1) * NPC.scale).RotatedBy(dir > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi)) + NPC.Center - Main.screenPosition, null, drawColor, cannon.Seg1Rot, new Vector2(6, cannon1.Height / 2f), NPC.scale, SpriteEffects.None);
            prepareShader(cannon2);
            Main.EntitySpriteDraw(cannon2, cannon.seg1end - Main.screenPosition, null, drawColor, cannon.Seg2Rot, new Vector2(6, cannon2.Height / 2f), NPC.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);

            prepareShader(shoulder);
            Main.EntitySpriteDraw(shoulder, NPC.Center - screenPos, null, drawColor, NPC.rotation, shoulder.Size() / 2f, NPC.scale, dir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);

            return false;
        }

        /// <summary>三段腿的解析 IK:限制第一节偏摆,再按几何解出第二节与脚踝</summary>
        public Vector2 CalculateLegJoints(Vector2 Center, Vector2 legStandPoint, float l1, float l2, float l3, out Vector2 P1, out Vector2 P2)
        {
            P1 = Vector2.Zero;
            P2 = Vector2.Zero;

            if (l1 <= 0 || l2 <= 0 || l3 <= 0)
            {
                return Center;
            }

            Vector2 D = legStandPoint - Center;
            float dist = D.Length();

            Vector2 target = legStandPoint;
            if (dist > l1 + l2 + l3)
            {
                target = Center + Vector2.Normalize(D) * (l1 + l2 + l3);
            }

            Vector2 downDirection = new Vector2(0, 1);
            Vector2 targetDirection = D.Length() > 0 ? Vector2.Normalize(D) : downDirection;

            float maxDeflectionAngle = MathHelper.ToRadians(68);
            float angleToTarget = (float)Math.Atan2(targetDirection.Y, targetDirection.X) - (float)Math.PI / 2; // 相对于Y轴正方向
            float deflectionAngle = MathHelper.Clamp(angleToTarget, -maxDeflectionAngle, maxDeflectionAngle);

            float cosAngle = (float)Math.Cos(deflectionAngle);
            float sinAngle = (float)Math.Sin(deflectionAngle);
            Vector2 firstSegmentDirection = new Vector2(
                downDirection.X * cosAngle - downDirection.Y * sinAngle,
                downDirection.X * sinAngle + downDirection.Y * cosAngle
            );

            P1 = Center + l1 * firstSegmentDirection;

            float y2 = target.Y - l3;
            float deltaY = y2 - P1.Y;
            float deltaX;
            try
            {
                deltaX = (float)Math.Sqrt(l2 * l2 - deltaY * deltaY);
            }
            catch
            {
                deltaX = 0;
                y2 = P1.Y - l2;
            }

            float x2_positive = P1.X + deltaX;
            float x2_negative = P1.X - deltaX;
            float x2 = (Math.Abs(x2_positive - target.X) < Math.Abs(x2_negative - target.X)) ? x2_positive : x2_negative;

            P2 = new Vector2(x2, y2);

            float distP2ToTarget = Vector2.Distance(P2, target);
            if (Math.Abs(distP2ToTarget - l3) > 0.001f)
            {
                P2 = new Vector2(P1.X, P1.Y - l2);
                target = new Vector2(P2.X, P2.Y + l3);
            }

            return target;
        }

        /// <summary>受击与解体表现。全部在 <c>!dedServ</c> 内,纯本地</summary>
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (DeathCounter > 0)
            {
                return;
            }
            if (chargeSnd != null)
            {
                chargeSnd.timeleft = 0;
            }
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            if (Main.zenithWorld)
            {
                PRTLoader.NewParticle<PRT_RealisticExplosion>(NPC.Center, Vector2.Zero, Color.White, 18f * NPC.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, -1);
            }
            else
            {
                //正常死亡PulseRing+双Shine+40 EMediumSmoke,zenith改单RealisticExplosion
                PRTLoader.NewParticle<PRT_PulseRing>(NPC.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(7f, 8);
                PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.Firebrick, 14f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.White, 10f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.Zero, 100), CEUtils.getDistance(NPC.Center, Main.LocalPlayer.Center), 1200);

                for (int i = 0; i < 40; i++)
                {
                    //40颗EMediumSmoke随机喷出,跟PulseRing/Shine同帧,死亡密度最高的一段
                    PRTLoader.NewParticle<PRT_EMediumSmoke>(NPC.Center + CEUtils.randomPointInCircle(60 * NPC.scale), CEUtils.randomPointInCircle(32 * NPC.scale), Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()), Main.rand.NextFloat(1f, 4f) * NPC.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), 120);
                }
            }
            SpawnGore("AcrGore0", 1);
            SpawnGore("AcrGore1", 1);
            SpawnGore("AcrGore2", 1);
            SpawnGore("AcrGore3", 1);
            SpawnGore("AcrGore4", 4);
            SpawnGore("AcrGore5", 4);
            SpawnGore("AcrGore6", 1);
            SpawnGore("AcrGore7", 4);
            SpawnGore("AcrGore8", 1);
            SpawnGore("AcrGore9", 1);
        }

        private void SpawnGore(string name, int count)
        {
            int type = Mod.Find<ModGore>(name).Type;
            for (int i = 0; i < count; i++)
            {
                Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), type, NPC.scale);
            }
        }
    }
}
