using CalamityEntropy.Content.NPCs.VoidDestroyer;
using System;
using Terraria;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 红色地狱模式的红色射线:本体核心向下持续 ai[0] 帧的定长射线,velocity 只作朝向,ai[1] 为长度。
    /// 命中 396,无减益;FTW 加宽 50%
    /// </summary>
    public class VDRedRay : VDHostileProjectile
    {
        public static readonly Color RayColor = new Color(255, 60, 60);
        public const float BaseWidth = 40f;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DefaultTimeLeft => 45;

        public int Duration => (int)Math.Max(Projectile.ai[0], 10f);
        public float Length => Projectile.ai[1] > 0 ? Projectile.ai[1] : 3000f;
        public float Width => BaseWidth * (Main.getGoodWorld ? 1.5f : 1f);
        public Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitY);

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>宽度包络:前 6 帧张开,最后 10 帧收拢</summary>
        public float Envelope() {
            int age = Duration - Projectile.timeLeft;
            float open = MathHelper.Clamp(age / 6f, 0f, 1f);
            float close = MathHelper.Clamp(Projectile.timeLeft / 10f, 0f, 1f);
            return Math.Min(open, close);
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = Duration;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("void_laser", 0.7f, Projectile.Center, 3, 1f);
                    CEUtils.SetShake(Projectile.Center, 7f, 2400f);
                }
            }
            Projectile.rotation = Dir.ToRotation();
            Lighting.AddLight(Projectile.Center + Dir * 300f, RayColor.ToVector3() * 0.8f);
            if (!Main.dedServ && Main.rand.NextBool(2)) {
                Vector2 pos = Projectile.Center + Dir * Main.rand.NextFloat(0f, Length * 0.6f);
                Vector2 v = Dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-4f, 4f);
                VDVfx.Spark(pos, v, RayColor, Main.rand.NextFloat(0.4f, 0.8f), 0.9f, 16);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Envelope() < 0.5f) {
                return false;
            }
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Dir * Length, targetHitbox, (int)Width);
        }

        public override bool PreDraw(ref Color lightColor) {
            //射线主体走 VDVoidBeam 着色器(湍流 + 白热核心 + 边缘辉光),红色板
            float env = Envelope();
            VDBeamDraw.Draw(Projectile.Center, Dir, Length, Width * 1.6f, RayColor, new Color(255, 200, 160), env, 1f, Projectile.whoAmI * 0.37f);
            return false;
        }
    }
}
