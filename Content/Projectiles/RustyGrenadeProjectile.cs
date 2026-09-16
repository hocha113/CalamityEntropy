using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class RustyGrenadeProjectile : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.IsARocketThatDealsDoubleDamageToPrimaryEnemy[Type] = true; ProjectileID.Sets.PlayerHurtDamageIgnoresDifficultyScaling[Type] = true;
            ProjectileID.Sets.Explosive[Type] = true;

        }
        public override void SetDefaults() {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = -1; Projectile.DamageType = DamageClass.Ranged;

        }
        public override void AI() {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 3) {
                Projectile.PrepareBombToBlow();
            }
            else {
                if (Math.Abs(Projectile.velocity.X) >= 8f || Math.Abs(Projectile.velocity.Y) >= 8f) {
                    for (int i = 0; i < 2; i++) {
                        float posOffsetX = 0f;
                        float posOffsetY = 0f;
                        if (i == 1) {
                            posOffsetX = Projectile.velocity.X * 0.5f;
                            posOffsetY = Projectile.velocity.Y * 0.5f;
                        }

                        Dust fireDust = Dust.NewDustDirect(new Vector2(Projectile.position.X + 3f + posOffsetX, Projectile.position.Y + 3f + posOffsetY) - Projectile.velocity * 0.5f,
   Projectile.width - 8, Projectile.height - 8, DustID.Torch, 0f, 0f, 100);
                        fireDust.scale *= 2f + Main.rand.Next(10) * 0.1f;
                        fireDust.velocity *= 0.2f;
                        fireDust.noGravity = true;


                        Dust smokeDust = Dust.NewDustDirect(new Vector2(Projectile.position.X + 3f + posOffsetX, Projectile.position.Y + 3f + posOffsetY) - Projectile.velocity * 0.5f, Projectile.width - 8, Projectile.height - 8, DustID.Smoke, 0f, 0f, 100, default, 0.5f);
                        smokeDust.fadeIn = 1f + Main.rand.Next(5) * 0.1f;
                        smokeDust.velocity *= 0.05f;
                    }
                }

                if (Math.Abs(Projectile.velocity.X) <= 15f && Math.Abs(Projectile.velocity.Y) <= 15f) {
                    Projectile.velocity *= 1.1f;
                }
            }

            if (Projectile.velocity != Vector2.Zero) {
                Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + MathHelper.PiOver2;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.velocity *= 0f; Projectile.timeLeft = 3; return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 300);
        }
        public override void PrepareBombToBlow() {
            Projectile.tileCollide = false; Projectile.alpha = 255;
            Projectile.Resize(128, 128);
            Projectile.knockBack = 8f;
        }

        public override void OnKill(int timeLeft) {

            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            Projectile.Resize(22, 22);

            for (int i = 0; i < 30; i++) {
                Dust smokeDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                smokeDust.velocity *= 1.4f;
            }

            for (int j = 0; j < 20; j++) {
                Dust fireDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 3.5f);
                fireDust.noGravity = true;
                fireDust.velocity *= 7f;
                fireDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1.5f);
                fireDust.velocity *= 3f;
            }

            for (int k = 0; k < 2; k++) {
                float speedMulti = 0.4f;
                if (k == 1) {
                    speedMulti = 0.8f;
                }

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
            if (CalamityEntropy.AprilFool) {
                //EXPLOSION类名和贴图绑死了,迁移纪律不改名
                PRTLoader.NewParticle<PRT_EXPLOSION>(Projectile.Center + new Vector2(0, -26), Vector2.Zero, Color.White, 1).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);  //EXPLOSION类名和贴图绑死了,迁移纪律不改名
            }



        }
    }
}