using CalamityEntropy.Content.Items.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class SacrificalDagger : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = NoneTypeDamageClass.Instance;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.ArmorPenetration = 100;
        }
        public float ofs = 0;
        public float ofsVel = 0;
        public Vector2 cPos = Vector2.Zero;
        public override void AI() {
            healCd--;
            Projectile.velocity *= 0;
            Player player = Projectile.owner.ToPlayer();
            if (player.Entropy().sacrMask && !player.dead) {
                Projectile.timeLeft = 3;
            }
            NPC n = Projectile.FindTargetWithinRange(300, false);
            if (n != null && CEUtils.getDistance(Projectile.Center, n.Center) > CEUtils.getDistance(n.Center, player.Center + (n.Center - player.Center).SafeNormalize(Vector2.One).RotatedBy(MathHelper.PiOver4) * 64)) {
                n = null;
            }
            if (n != null && healCd <= 0) {
                if (ofs <= 0) {
                    ofsVel = 16;
                    cPos = n.Center;
                }
            }
            ofs += ofsVel;
            if (ofs > 0) {
                ofs += ofsVel;
                ofsVel -= 1f;

            }
            if (ofs < 0) {
                ofs = 0;
                ofsVel = 0;
            }
            int index = 0;
            foreach (Projectile p in Main.projectile) {
                if (p.whoAmI == Projectile.whoAmI) {
                    break;
                }
                if (p.type == Projectile.type && p.active && p.owner == Projectile.owner) {
                    index++;
                }
            }
            Projectile.Center = player.Center + (player.Entropy().CasketSwordRot * 0.5f).ToRotationVector2().RotatedBy(MathHelper.ToRadians(index * 360 / 8)) * 64 + (cPos - (player.Center + (player.Entropy().CasketSwordRot * 0.5f).ToRotationVector2().RotatedBy(MathHelper.ToRadians(index * 360 / 8)) * 64)).SafeNormalize(Vector2.Zero) * ofs;
            if (n != null) {
                Projectile.rotation = (n.Center - Projectile.Center).ToRotation();
            }
            else if (ofs > 0) {
                Projectile.rotation = (Projectile.Center - player.Center).ToRotation();
            }
            else {
                Projectile.rotation = player.Entropy().CasketSwordRot * 0.5f + MathHelper.ToRadians(index * 360 / 8);
            }
            Projectile.ai[0]++;

        }
        public int healCd = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.owner.ToPlayer().statLife < Projectile.owner.ToPlayer().statLifeMax2 && healCd <= 0)
                Projectile.owner.ToPlayer().Entropy().TryHealMeWithCd(1, 1);
            healCd = 14;
        }
    }

}