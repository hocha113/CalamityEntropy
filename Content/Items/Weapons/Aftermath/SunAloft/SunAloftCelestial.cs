using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.SunAloft
{
    //ai[0] 0 太阳 1 月亮; 自身不造成伤害, 只在主人端选目标开火, 子弹继承 Empowered 不回充
    public class SunAloftCelestial : ModProjectile
    {
        //时间单位帧, 距离单位 px(800 px = 50 格)
        public const int Lifetime = 600;
        public const int FireInterval = 30;
        public const int FirstShotDelay = 12;
        public const float SearchRange = 800f;
        public const float HoverHeight = 150f;
        public const float BobHeight = 6f;
        public const float FollowLerp = 0.18f;
        public const int FadeFrames = 20;
        public const float SunSpin = 0.01f;
        public const float LaserDamageMult = 0.6f;
        public const float RayDamageMult = 0.3f;
        public const int RayCount = 2;
        public const int RayStagger = 6;
        public const int FrameCount = 2;
        public const float BobSpeed = 0.05f;

        public static readonly Color SunGlow = new(255, 180, 70);
        public static readonly Color MoonGlow = new(130, 175, 255);

        private int age;
        private int fireTimer = FireInterval - FirstShotDelay;

        private bool Moon => Projectile.ai[0] == 1f;
        private Color GlowColor => Moon ? MoonGlow : SunGlow;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults() {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            Player owner = Projectile.GetOwner();
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            if (age == 0)
                SummonVfx();
            age++;
            Projectile.frame = Moon ? 1 : 0;
            Vector2 home = owner.MountedCenter + new Vector2(0f, -HoverHeight + (float)Math.Sin(age * BobSpeed) * BobHeight);
            Projectile.Center = Vector2.Lerp(Projectile.Center, home, FollowLerp);
            if (!Moon)
                Projectile.rotation += SunSpin;
            Projectile.Opacity = Math.Min(1f, Math.Min(age, Projectile.timeLeft) / (float)FadeFrames);
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.8f * Projectile.Opacity);

            if (Projectile.owner != Main.myPlayer || ++fireTimer < FireInterval)
                return;
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, owner.Center, SearchRange);
            if (target == null) {
                fireTimer = FireInterval;
                return;
            }
            fireTimer = 0;
            Fire(target);
        }

        private void Fire(NPC target) {
            IEntitySource source = Projectile.GetSource_FromThis();
            Vector2 offset = Projectile.Center - target.Center;
            if (!Moon) {
                Projectile.NewProjectile(source, target.Center, offset, ModContent.ProjectileType<SunAloftSolarLaser>(), (int)(Projectile.damage * LaserDamageMult), 0f, Projectile.owner);
                return;
            }
            for (int i = 0; i < RayCount; i++) {
                Vector2 jitter = new(Main.rand.NextFloat(-0.25f, 0.25f) * target.width, Main.rand.NextFloat(-0.25f, 0.25f) * target.height);
                Projectile.NewProjectile(source, target.Center + jitter, offset - jitter, ModContent.ProjectileType<SunAloftLunarRay>(), (int)(Projectile.damage * RayDamageMult), SunAloftSwing.RayKnockback, Projectile.owner, i * RayStagger);
            }
        }

        private void SummonVfx() {
            if (Main.dedServ)
                return;
            CEUtils.PlaySound("AscendantActivate", Moon ? 1.25f : 1.1f, Projectile.Center, 2, 0.6f);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, GlowColor, 0.1f).Configure(1.8f, 20);
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Color.White, 1.8f).Configure(GlowColor, 18, 0f, 2.6f);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, GlowColor, 0.1f).Configure(1.2f, 16);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Rectangle frame = tex.Frame(1, FrameCount, 0, Projectile.frame);
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float pulse = 0.85f + 0.15f * (float)Math.Sin(age * 0.1f);
            Main.EntitySpriteDraw(glow, pos, null, (GlowColor with { A = 0 }) * 0.55f * pulse * Projectile.Opacity, 0f, glow.Size() / 2f, 0.75f, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, frame, Color.White * Projectile.Opacity, Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, frame, (GlowColor with { A = 0 }) * 0.35f * pulse * Projectile.Opacity, Projectile.rotation, frame.Size() / 2f, Projectile.scale * 1.08f, SpriteEffects.None);
            return false;
        }
    }
}
