using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    //脱离灾厄:灾厄AbyssBladeSplitProjectile同短名移植(渊刃大招追踪魂),运动/减益逐式对齐,烟雾改自有Ports粒子
    public class AbyssBladeSplitProjectile : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Ports/Invisible";

        public int Time = 0;
        public int randTimer;
        public int dustType1 = 104;
        public int dustType2 = 29;

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            Time++;
            if (Time == 1) {
                randTimer = Main.rand.Next(240, 301);
                Projectile.timeLeft = randTimer;
            }
            if (Time > 20 && Time < (randTimer - 70)) {
                //原灾厄HomeInOnNPC(350,1→10渐升,20),忽略视线
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 350f);
                if (target != null)
                    Projectile.HomingNPCBetter(target, 350f, MathHelper.Clamp(1f + Time * 0.075f, 1, 10), 20f, giveExtraUpdate: 1, ignoreDist: true);
            }
            else if (Time >= (randTimer - 70)) {
                if (Projectile.velocity.Y < 10)
                    Projectile.velocity.Y += 0.4f;
                Projectile.velocity.X *= 0.97f;
            }
            if (Time % 3 == 0) {
                //原灾厄HeavySmokeParticle(30寿命,0.3不透明度)
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Projectile.velocity * Main.rand.NextFloat(0.6f, 0.8f), Color.MediumBlue, Main.rand.NextFloat(0.35f, 0.5f)).Configure(0.3f, 30, Main.rand.NextFloat(-0.2f, 0.2f), false, 0f, true);
            }
            for (int i = 0; i < 2; i++) {
                Vector2 dustPos = Projectile.Center;
                int dustType = Main.rand.NextBool(3) ? dustType1 : dustType2;
                Dust dust = Dust.NewDustPerfect(dustPos, dustType);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.95f, 1.7f);
                dust.velocity = Projectile.velocity + new Vector2(0.5f, 0.5f).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1.1f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 120);

            SoundEngine.PlaySound(SoundID.ShimmerWeak1 with { Pitch = 0.35f }, Projectile.Center);
        }
    }
}
