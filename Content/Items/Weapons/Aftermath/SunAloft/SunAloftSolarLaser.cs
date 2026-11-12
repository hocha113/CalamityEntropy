using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.SunAloft
{
    //Center 为命中点, velocity 为命中点指向太阳的偏移(不移动)
    public class SunAloftSolarLaser : ModProjectile
    {
        //时间单位帧, 距离单位 px
        public const int Lifetime = 16;
        public const int HitFrames = 4;
        public const float BeamWidth = 22f;
        public const float Overrun = 24f;

        public static readonly Color Core = new(255, 245, 200);
        public static readonly Color Outer = new(255, 150, 40);

        private int age;

        public override string Texture => CEUtils.WhiteTexPath;

        private Vector2 BeamOrigin => Projectile.Center + Projectile.velocity;
        private Vector2 BeamEnd => Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * Overrun;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI() {
            if (age == 0 && !Main.dedServ) {
                CEUtils.PlaySound("lasershoot", Main.rand.NextFloat(1.25f, 1.4f), Projectile.Center, 3, 0.4f);
                PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Core, 1.2f).Configure(Outer, 10, 0f, 2f);
                Vector2 back = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
                for (int i = 0; i < 6; i++)
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (-back).RotatedByRandom(0.7f) * Main.rand.NextFloat(8f, 16f), Outer, Main.rand.NextFloat(0.04f, 0.07f)).Configure(false, 10, new Vector2(0.3f, 1f), false, false);
            }
            age++;
            Lighting.AddLight(Projectile.Center, new Vector3(1.1f, 0.75f, 0.3f) * Power);
        }

        private float Power => Math.Max(0f, 1f - age / (float)Lifetime);

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => age <= HitFrames ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), BeamOrigin, BeamEnd, BeamWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<FlareBurn>(), SunAloftSwing.FlareBurnFrames);
        }

        public override bool PreDraw(ref Color lightColor) {
            float power = Power;
            Vector2 from = BeamOrigin;
            Vector2 delta = BeamEnd - from;
            float len = delta.Length();
            if (power <= 0f || len < 1f)
                return false;
            float rot = delta.ToRotation();
            Texture2D px = Projectile.GetTexture();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Texture2D star = CEUtils.getExtraTex("StarTexture");
            Vector2 mid = (from + BeamEnd) / 2f - Main.screenPosition;
            Main.EntitySpriteDraw(glow, mid, null, (Outer with { A = 0 }) * 0.75f * power, rot, glow.Size() / 2f, new Vector2(len * 1.1f / glow.Width, BeamWidth * 2.4f / glow.Height), SpriteEffects.None);
            Main.EntitySpriteDraw(px, from - Main.screenPosition, null, Core * power, rot, new Vector2(0f, 0.5f), new Vector2(len, BeamWidth * 0.35f * power), SpriteEffects.None);
            Main.EntitySpriteDraw(star, Projectile.Center - Main.screenPosition, null, (Outer with { A = 0 }) * power, rot, star.Size() / 2f, 0.3f * power, SpriteEffects.None);
            return false;
        }
    }
}
