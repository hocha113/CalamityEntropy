using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 核心紫色激光束:高速短光束,由本体逐发对准玩家当前位置射出形成追随的激光流。命中 348 + 带电 3 秒。
    /// 碰撞按自身长度做线段判定
    /// </summary>
    public class VDCoreLaserBolt : VDHostileProjectile
    {
        public const float BeamLength = 70f;
        public static readonly Color GlowColor = new Color(200, 90, 255);

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => BuffID.Electrified;
        public override int DefaultTimeLeft => 90;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.4f);
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 v = CEUtils.randomPointInCircle(1.2f);
                var s = PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.3f, 0.6f))
                    .Configure(0.8f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 14);
                s.grav = false;
            }
        }

        /// <summary>FTW 种子下所有激光类加粗 50%</summary>
        public static float WidthMult => Main.getGoodWorld ? 1.5f : 1f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            return CEUtils.LineThroughRect(Projectile.Center - dir * BeamLength, Projectile.Center + dir * 6f, targetHitbox, (int)(10 * WidthMult));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D beam = CEUtils.getExtraTex("BasicTrail");
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = Projectile.Center - dir * BeamLength * 0.5f - Main.screenPosition;
            float rot = Projectile.rotation;
            float w = WidthMult;
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(beam, center, null, GlowColor * 0.9f, rot, beam.Size() / 2f, new Vector2(BeamLength / beam.Width, 22f * w / beam.Height), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(beam, center, null, Color.White * 0.9f, rot, beam.Size() / 2f, new Vector2(BeamLength / beam.Width, 8f * w / beam.Height), SpriteEffects.None, 0f);
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, GlowColor * 0.8f, 0f, glow.Size() / 2f, 0.16f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
