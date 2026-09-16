using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkFlesh : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 20);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Flesh");
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.lifeSteal += 0.25f;
        }
        public override Color tooltipColor => Color.Red;
        public override EBookProjectileEffect getEffect() {
            return new FleshBMEffect();
        }
    }
    public class FleshBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            if (((EBookBaseProjectile)projectile.ModProjectile).hitCount == 1) {
                for (int i = 0; i < 4; i++) {
                    // 原灾厄 BloodBeam 改用自有 BloodSpray
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, CEUtils.randomRot().ToRotationVector2() * 4, ModContent.ProjectileType<BloodSpray>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 10, projectile.DamageType), projectile.knockBack / 3, projectile.owner);
                }
            }
        }
    }
}