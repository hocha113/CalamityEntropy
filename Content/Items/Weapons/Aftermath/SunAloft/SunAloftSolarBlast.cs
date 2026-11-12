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
    //ai[0] 1 刀身命中产生(首帧补播金属击中声), 2 为该次挥砍的首个命中(另加震屏); velocity 只给火花定向
    public class SunAloftSolarBlast : ModProjectile
    {
        //时间单位帧, 距离单位 px; 原版 612 贴图为 5 帧 52x52
        public const int Frames = 5;
        public const int TicksPerFrame = 3;
        public const int DamageFrames = 6;
        public const float HitRadius = 56f;
        public const float DrawScale = 1.9f;

        public static readonly Color Flame = new(255, 150, 50);
        public static readonly Color Spark = new(255, 220, 120);

        private int age;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SolarWhipSwordExplosion;

        private float Fade => Projectile.timeLeft / (float)(Frames * TicksPerFrame);

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
        }

        public override void SetDefaults() {
            Projectile.width = (int)(HitRadius * 2f);
            Projectile.height = (int)(HitRadius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Frames * TicksPerFrame;
        }

        public override void AI() {
            if (age == 0) {
                Projectile.rotation = Projectile.velocity.ToRotation();
                SpawnVfx();
            }
            age++;
            Projectile.frame = Math.Min(Frames - 1, age / TicksPerFrame);
            Lighting.AddLight(Projectile.Center, new Vector3(1.3f, 0.75f, 0.25f) * Fade);
        }

        private void SpawnVfx() {
            if (Main.dedServ)
                return;
            Vector2 c = Projectile.Center;
            CEUtils.PlaySound("cinderExplosion", Main.rand.NextFloat(1.15f, 1.3f), c, 3, 0.45f);
            if (Projectile.ai[0] >= SunAloftSwing.BladeHit)
                CEUtils.PlaySound(Main.rand.NextBool() ? "SwordHit0" : "SwordHit1", Main.rand.NextFloat(1f, 1.15f), c, 4, 0.6f * CEUtils.WeapSound);
            if (Projectile.ai[0] == SunAloftSwing.FirstBladeHit)
                CEUtils.SetShake(c, SunAloftSwing.HitShake, SunAloftSwing.ShakeRange);
            PRTLoader.NewParticle<PRT_PulseRing>(c, Vector2.Zero, Flame, 0.1f).Configure(1.3f, 14);
            PRTLoader.NewParticle<PRT_SparkleCal>(c, Vector2.Zero, new Color(255, 235, 170), 1.4f).Configure(Flame, 12, 0f, 2.2f);
            Vector2 sweep = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 8; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(c, sweep.RotatedByRandom(0.5f) * Main.rand.NextFloat(10f, 20f), Color.Lerp(Flame, Spark, Main.rand.NextFloat()), Main.rand.NextFloat(0.05f, 0.08f)).Configure(false, 12, new Vector2(0.3f, 1f), false, false);
            for (int i = 0; i < 6; i++)
                Dust.NewDustPerfect(c, DustID.SolarFlare, Main.rand.NextVector2Circular(6f, 6f), 0, default, Main.rand.NextFloat(1f, 1.5f)).noGravity = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => age <= DamageFrames ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => targetHitbox.Distance(Projectile.Center) <= HitRadius;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<FlareBurn>(), SunAloftSwing.FlareBurnFrames);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Rectangle frame = tex.Frame(1, Frames, 0, Projectile.frame);
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float fade = Fade;
            Main.EntitySpriteDraw(glow, pos, null, (Flame with { A = 0 }) * 0.55f * fade, 0f, glow.Size() / 2f, 0.9f, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, frame, Color.White * (0.6f + 0.4f * fade), Projectile.rotation, frame.Size() / 2f, DrawScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, frame, (Spark with { A = 0 }) * 0.5f * fade, Projectile.rotation, frame.Size() / 2f, DrawScale * 1.1f, SpriteEffects.None);
            return false;
        }
    }
}
