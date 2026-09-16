using CalamityEntropy.Content.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkIce : BookMark
    {
        public override Texture2D UITexture => BookMark.GetUITexture("Ice");
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 20);
        }
        public override Color tooltipColor => new Color(160, 250, 255);
        public override EBookProjectileEffect getEffect() {
            return new IceBMEffect();
        }
    }
    public class IceBMEffect : EBookProjectileEffect
    {
        public override void OnProjectileSpawn(Projectile projectile, bool ownerClient) {
            if (ownerClient && ((projectile.ModProjectile is EBookBaseProjectile eb && eb.mainProj) || Main.rand.NextBool(8))) {
                Vector2 pos = projectile.Center - projectile.velocity.normalize() * 168 + CEUtils.randomVec(128);
                int p = Projectile.NewProjectile(projectile.GetSource_FromThis(), pos, (Main.MouseWorld - pos).normalize() * 32, ModContent.ProjectileType<IceEdge2>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 50, projectile.DamageType), projectile.knockBack, projectile.owner);
                (p.ToProj().ModProjectile as EBookBaseProjectile).homing = (projectile.ModProjectile as EBookBaseProjectile).homing;
            }
        }
    }
}