using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FartCloud : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 256;
            Projectile.height = 256;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
        }
        public bool Exp = false;
        public override bool? CanCutTiles() {
            return false;
        }
        public override void AI() {
            for (int i = 0; i < 5; i++) {
                Vector2 smokeSpeed = Vector2.Zero;
                //MediumMistCal CalamityPorts,Configure是mist原构造
                PRTLoader.NewParticle<PRT_MediumMistCal>(Projectile.Center + new Vector2(Main.rand.Next(-120, 121)).RotatedBy(CEUtils.randomRot()), Vector2.Zero, Color.Green, Main.rand.NextFloat(0.8f, 1.2f)).Configure(Color.Green, 255f * 0.6f, Main.rand.NextFloat(-0.1f, 0.1f));  //MediumMistCal CalamityPorts,Configure是mist原构造
            }
            if (!Exp) {
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (p.Colliding(p.getRect(), Projectile.getRect())) {
                        if (p.ModProjectile is Flame || (p.ModProjectile is FartCloud fc && fc.Exp) || (p.ModProjectile is PoopBombProjectile pb && pb.Exp)) {
                            Exp = true;
                            break;
                        }
                    }
                }
                if (Exp) {
                    CEUtils.PlaySound("boss explosions 0", 1, Projectile.Center);
                    Projectile.timeLeft = 4;
                    Projectile.damage *= 14 * 6;
                    Projectile.timeLeft = 3;
                    Projectile.hostile = true;
                    for (int i = 0; i < Projectile.localNPCImmunity.Length; i++) {
                        Projectile.localNPCImmunity[i] = 0;
                    }
                    for (int i = 0; i < 30; i++) {
                        Dust smokeDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                        smokeDust.velocity *= 2.8f;
                    }

                    for (int j = 0; j < 20; j++) {
                        Dust fireDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 3.5f);
                        fireDust.noGravity = true;
                        fireDust.velocity *= 7f;
                        fireDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1.5f);
                        fireDust.velocity *= 6f;
                    }

                    for (int k = 0; k < 2; k++) {
                        float speedMulti = 0.4f;
                        if (k == 1) {
                            speedMulti = 0.8f;
                        }
                        speedMulti *= 2;
                        Gore smokeGore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, default, Main.rand.Next(GoreID.Smoke1, GoreID.Smoke3 + 1));
                        smokeGore.velocity *= speedMulti;
                        smokeGore.velocity += Vector2.One;
                        smokeGore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, default, Main.rand.Next(GoreID.Smoke1, GoreID.Smoke3 + 1));
                        smokeGore.velocity *= speedMulti;
                        smokeGore.velocity.X -= 1f;
                        smokeGore.velocity.Y += 1f;
                        smokeGore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, default, Main.rand.Next(GoreID.Smoke1, GoreID.Smoke3 + 1));
                        smokeGore.velocity *= speedMulti;
                        smokeGore.velocity.X += 1f;
                        smokeGore.velocity.Y -= 1f;
                        smokeGore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, default, Main.rand.Next(GoreID.Smoke1, GoreID.Smoke3 + 1));
                        smokeGore.velocity *= speedMulti;
                        smokeGore.velocity -= Vector2.One;
                    }
                }
            }
        }
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
            modifiers.FinalDamage *= 0.1f;
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }


}