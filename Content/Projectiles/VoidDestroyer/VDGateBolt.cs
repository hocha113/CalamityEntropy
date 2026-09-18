using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 纵深环门的环成员:平面位置钉死(环心 + 角度 × 半径),只沿 Z 逼近平面,到达那几帧有判定。
    /// ai[0] 在环上的角度,ai[1] 环内序号(0 号负责画整环的连线与平面脚印),ai[2] 环半径。
    /// 深度由 <see cref="VDDepthSource"/> 给:整环同一 Z、同一 Z 速度,所以整环一起变大、一起到达
    /// </summary>
    public class VDGateBolt : VDDepthProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Projectiles/VoidDestroyer/VDVoidBolt";
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => 200;

        public float Angle => Projectile.ai[0];
        public int Index => (int)Projectile.ai[1];
        public float Radius => Projectile.ai[2];
        /// <summary>环心(平面坐标)</summary>
        public Vector2 RingCenter => Projectile.Center - Angle.ToRotationVector2() * Radius;

        public override float MarkerRadius => 22f;
        /// <summary>整环的脚印由 0 号成员画,其余成员不画各自的小标记(14 个小环叠在一起是噪声)</summary>
        public override bool WantsMarker => Index == 0 && base.WantsMarker;
        protected override float WhooshStrength => 0.25f;

        public override void SetExtraDefaults() {
            Projectile.width = 22;
            Projectile.height = 22;
        }

        protected override void DepthAI() {
            Projectile.velocity = Vector2.Zero;
            //环上的弹切向朝向
            Projectile.rotation = Angle + MathHelper.PiOver2 + MathHelper.PiOver2;
            if (Math.Abs(Z) < 0.5f) {
                Lighting.AddLight(Projectile.Center, VDVoidBolt.GlowColor.ToVector3() * 0.4f);
            }
            if (OutOfSight) {
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ || Math.Abs(Z) > 0.5f) {
                return;
            }
            for (int i = 0; i < 4; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
                VDVfx.Spark(Projectile.Center, v, VDVoidBolt.GlowColor, Main.rand.NextFloat(0.5f, 0.8f), 1f, 18, gravity: true);
            }
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = ProjectedCenter - Main.screenPosition;
            float scale = DrawScale;
            float depthAlpha = VDDepth.Alpha(Z);
            Color glow = VDDepth.Fog(VDVoidBolt.GlowColor, Z);

            spriteBatch.UseAdditive();
            if (Index == 0) {
                //整环的连线:环在当前深度下的投影圆,越近越亮
                Vector2 c = VDDepth.Project(RingCenter, Z);
                float r = Radius * VDDepth.Scale(Z);
                float near = MathHelper.Clamp(1f - Z / VDDirector.GateStartDepth, 0f, 1f);
                DrawRing(c, r, glow * (0.18f + 0.3f * near) * depthAlpha, 2f + 2f * near);
            }
            Texture2D glowTex = CEUtils.getExtraTex("Glow");
            spriteBatch.Draw(glowTex, drawPos, null, glow * (0.55f * depthAlpha), 0f, glowTex.Size() / 2f, 0.15f * scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            VDDepthDraw.Draw(tex, drawPos, null, Color.White, 1f, Projectile.rotation, origin, scale, SpriteEffects.None, Z);
        }

        /// <summary>平面脚印:环到达时会落在哪一圈,缺口一目了然(只画成员所在的弧,缺口处没有成员自然是空的)</summary>
        protected override void DrawMarker(SpriteBatch spriteBatch) {
            float p = MarkerProgress();
            if (p <= 0f) {
                return;
            }
            Color c = Color.Lerp(VDVfx.DepthMarker, Color.White, p * p) * (0.25f + 0.55f * p);
            spriteBatch.UseAdditive();
            //脚印按成员位置逐点画短弧:缺口自然空出
            foreach (Projectile other in Main.ActiveProjectiles) {
                if (other.type != Type || other.ModProjectile is not VDGateBolt gb || gb.RingCenter.DistanceSQ(RingCenter) > 64f) {
                    continue;
                }
                Vector2 tangent = (gb.Angle + MathHelper.PiOver2).ToRotationVector2();
                float half = Radius * MathHelper.TwoPi / VDDirector.GateBolts * 0.42f;
                CEUtils.drawLine(other.Center - tangent * half, other.Center + tangent * half, c, 3f + 3f * p);
            }
            CEUtils.ReSetToEndShader();
        }

        private static void DrawRing(Vector2 center, float radius, Color color, float width) {
            const int segments = 36;
            Vector2 prev = center + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++) {
                float a = MathHelper.TwoPi * i / segments;
                Vector2 next = center + a.ToRotationVector2() * radius;
                CEUtils.drawLine(prev, next, color, width, 1);
                prev = next;
            }
        }
    }
}
