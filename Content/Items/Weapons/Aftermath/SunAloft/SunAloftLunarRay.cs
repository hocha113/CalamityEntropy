using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.SunAloft
{
    //Center 为落点, velocity 为落点指向光束起点的偏移(不移动), ai[0] 延迟帧数, ai[1] 同日耀爆炸的 ai[0]
    public class SunAloftLunarRay : ModProjectile
    {
        //时间单位帧, 距离单位 px; 原版 645 贴图为 7 帧 98x98
        public const int Frames = 7;
        public const int TicksPerFrame = 3;
        public const int FallFrames = 5;
        public const int HitFrames = 6;
        public const int BeamFadeFrames = 14;
        public const int Lifetime = FallFrames + Frames * TicksPerFrame + 2;
        public const float BeamWidth = 28f;
        public const float ImpactRadius = 40f;
        public const float BurstScale = 1.3f;

        public static readonly Color Core = new(225, 240, 255);
        public static readonly Color Outer = new(120, 170, 255);

        private int age;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.LunarFlare;

        private int Strike => age - 1 - (int)Projectile.ai[0];
        private Vector2 BeamStart => Projectile.Center + Projectile.velocity;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
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
            age++;
            if (age == 1) {
                Projectile.timeLeft = Lifetime + (int)Projectile.ai[0];
                if (Projectile.ai[1] >= SunAloftSwing.BladeHit && !Main.dedServ)
                    CEUtils.PlaySound(Main.rand.NextBool() ? "SwordHit0" : "SwordHit1", Main.rand.NextFloat(1.05f, 1.2f), Projectile.Center, 4, 0.6f * CEUtils.WeapSound);
                if (Projectile.ai[1] == SunAloftSwing.FirstBladeHit && !Main.dedServ)
                    CEUtils.SetShake(Projectile.Center, SunAloftSwing.HitShake, SunAloftSwing.ShakeRange);
            }
            int strike = Strike;
            if (strike == FallFrames)
                ImpactVfx();
            if (strike >= 0)
                Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.6f, 1.1f) * BeamPower(strike));
        }

        private float BeamPower(int strike) {
            if (strike < 0)
                return 0f;
            if (strike < FallFrames)
                return 0.6f + 0.4f * strike / FallFrames;
            return Math.Max(0f, 1f - (strike - FallFrames) / (float)BeamFadeFrames);
        }

        private void ImpactVfx() {
            if (Main.dedServ)
                return;
            Vector2 c = Projectile.Center;
            CEUtils.PlaySound("light_bolt", Main.rand.NextFloat(1.15f, 1.3f), c, 4, 0.5f);
            PRTLoader.NewParticle<PRT_PulseRing>(c, Vector2.Zero, Outer, 0.1f).Configure(1.2f, 14);
            PRTLoader.NewParticle<PRT_SparkleCal>(c, Vector2.Zero, Core, 1.3f).Configure(Outer, 12, 0f, 2.2f);
            Vector2 back = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            for (int i = 0; i < 8; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(c, back.RotatedByRandom(1.1f) * Main.rand.NextFloat(8f, 18f), Color.Lerp(Outer, Core, Main.rand.NextFloat()), Main.rand.NextFloat(0.05f, 0.08f)).Configure(false, 12, new Vector2(0.3f, 1f), false, false);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Strike >= FallFrames - 1 && Strike < FallFrames + HitFrames ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (targetHitbox.Distance(Projectile.Center) <= ImpactRadius)
                return true;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), BeamStart, Projectile.Center, BeamWidth, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor) {
            int strike = Strike;
            if (strike < 0)
                return false;
            float power = BeamPower(strike);
            if (power > 0f) {
                Vector2 head = Vector2.Lerp(BeamStart, Projectile.Center, Math.Min(1f, strike / (float)FallFrames));
                DrawBeam(BeamStart, head, power);
            }
            if (strike >= FallFrames) {
                Texture2D tex = Projectile.GetTexture();
                int t = strike - FallFrames;
                Rectangle frame = tex.Frame(1, Frames, 0, Math.Min(Frames - 1, t / TicksPerFrame));
                float fade = Math.Max(0f, 1f - t / (float)(Frames * TicksPerFrame));
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, frame, Color.White * (0.5f + 0.5f * fade), 0f, frame.Size() / 2f, BurstScale, SpriteEffects.None);
                Main.EntitySpriteDraw(tex, pos, frame, (Outer with { A = 0 }) * 0.6f * fade, 0f, frame.Size() / 2f, BurstScale * 1.15f, SpriteEffects.None);
            }
            return false;
        }

        private static void DrawBeam(Vector2 from, Vector2 to, float power) {
            Vector2 delta = to - from;
            float len = delta.Length();
            if (len < 1f)
                return;
            float rot = delta.ToRotation();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Texture2D px = CEUtils.getExtraTex("white");
            Vector2 mid = (from + to) / 2f - Main.screenPosition;
            Main.EntitySpriteDraw(glow, mid, null, (Outer with { A = 0 }) * 0.7f * power, rot, glow.Size() / 2f, new Vector2(len * 1.15f / glow.Width, BeamWidth * 2.4f / glow.Height), SpriteEffects.None);
            Main.EntitySpriteDraw(px, from - Main.screenPosition, null, Core * power, rot, new Vector2(0f, 0.5f), new Vector2(len, BeamWidth * 0.3f * power), SpriteEffects.None);
        }
    }
}
