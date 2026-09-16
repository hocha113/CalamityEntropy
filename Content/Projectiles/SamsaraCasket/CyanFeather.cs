using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class CyanFeather : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1; Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }
        public override void AI() {
            Projectile.ArmorPenetration = HorizonssKey.getArmorPen();
            noHit--;
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 3) {
                Projectile.PrepareBombToBlow();
            }
            else {
                if (Projectile.timeLeft > 3) {
                    Projectile.velocity.Y += 0.8f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.velocity *= 0f;
            Projectile.timeLeft = 3;
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            PrepareBombToBlow();
            if (HorizonssKey.getVoidTouchLevel() > 0) {
                EGlobalNPC.AddVoidTouch(target, 80, HorizonssKey.getVoidTouchLevel(), 800, 16);
            }

        }
        public override void PrepareBombToBlow() {
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
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

        }
        int noHit = 20;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (noHit <= 0) {
                return null;
            }
            return false;
        }
    }
}