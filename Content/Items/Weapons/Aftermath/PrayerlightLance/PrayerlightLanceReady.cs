using CalamityEntropy.Content.Items.Weapons.VoidDestroyer;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PrayerlightLance
{
    /// <summary>光之能量积满的闪光:拥有者生成,各端首帧放钟声与粒子,随后一圈白金光环扩散淡出</summary>
    public class PrayerlightLanceReady : ModProjectile
    {
        public const int LifeFrames = 30;
        //光环从起始半径扩到最终半径,像素
        public const float RingStart = 20f;
        public const float RingRadius = 90f;
        public const int SparkCount = 14;

        private int age;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetDefaults() {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifeFrames;
            Projectile.aiStyle = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active) {
                Projectile.Kill();
                return;
            }
            Projectile.Center = owner.MountedCenter;
            if (age == 0) {
                Flash();
            }
            age++;
            Lighting.AddLight(Projectile.Center, SunVfx.Core.ToVector3() * (1f - age / (float)LifeFrames));
        }

        private void Flash() {
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("bell", 1.15f, Projectile.Center, 2, 0.8f);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, SunVfx.Core, 0.1f).Configure(1.6f, 24);
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, SunVfx.Core, 2.4f).Configure(SunVfx.Gold, 22, 0f, 3f);
            for (int i = 0; i < SparkCount; i++) {
                Color c = Color.Lerp(SunVfx.Gold, SunVfx.Core, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_SparkCal>(Projectile.Center, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 11f), c, Main.rand.NextFloat(0.6f, 1f)).Configure(false, 26);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            float k = age / (float)LifeFrames;
            float ease = 1f - (1f - k) * (1f - k);
            VDWeaponFx.BeginAdditive();
            VDWeaponFx.Ring(Projectile.Center, Color.Lerp(SunVfx.Core, SunVfx.Gold, k) * (1f - k), MathHelper.Lerp(RingStart, RingRadius, ease));
            VDWeaponFx.Glow(Projectile.Center, SunVfx.Core * (0.55f * (1f - k)), 54f);
            VDWeaponFx.End();
            return false;
        }
    }
}
