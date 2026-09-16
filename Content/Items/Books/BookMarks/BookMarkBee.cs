using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkBee : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 5);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Bee");
        public override EBookProjectileEffect getEffect() {
            return new BeeBMEffect();
        }

        public override Color tooltipColor => Color.Orange;
    }

    public class BeeBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            target.AddBuff(ModContent.BuffType<BonePiercingToxin>(), 8 * 60);
            for (int i = 0; i < 1; i++) {
                Vector2 shotDir = CEUtils.randomRot().ToRotationVector2();
                Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center + shotDir * 16, shotDir * 12, ModContent.ProjectileType<SmallBee>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 10, projectile.DamageType), projectile.knockBack / 3, projectile.owner).ToProj().DamageType = projectile.DamageType;
            }
        }
    }
}