using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class RecoveryVineTop : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 400;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }
        public int uLeft = 5;
        public int uCount = 4;
        public override void AI() {
            Projectile.ArmorPenetration = HorizonssKey.getArmorPen();
            uCount--;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (uCount <= 0 && uLeft > 0) {
                if (Main.myPlayer == Projectile.owner) {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<RecoveryVine>(), Projectile.damage, Projectile.knockBack);
                }
                uCount = 4;
                uLeft--;
                Projectile.Center += Projectile.velocity;
            }
            if (uLeft <= 0) {
                Projectile.alpha += 255 / 60;
                if (Projectile.alpha >= 255) {
                    Projectile.Kill();
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (HorizonssKey.getVoidTouchLevel() > 0) {
                EGlobalNPC.AddVoidTouch(target, 80, HorizonssKey.getVoidTouchLevel(), 800, 16);
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

    }

}