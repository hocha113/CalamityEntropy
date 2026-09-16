using CalamityEntropy.Common;
using CalamityEntropy.Content.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkMechanical : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 20);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Mechanical");
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.Homing += 0.22f;
        }
        public override Color tooltipColor => Color.LightGray;
        public override EBookProjectileEffect getEffect() {
            return new MechanicalBMEffect();
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.HallowedBar, 4)
                .AddIngredient(ItemID.SoulofFright, 3)
                .AddIngredient(ItemID.SoulofMight, 3)
                .AddIngredient(ItemID.SoulofSight, 3)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    public class MechanicalBMEffect : EBookProjectileEffect
    {
        public override void OnProjectileSpawn(Projectile projectile, bool ownerClient) {
            if (ownerClient && (projectile.ModProjectile is EBookBaseProjectile eb && eb.mainProj) && Main.rand.NextBool(projectile.HasEBookEffect<APlusBMEffect>() ? 5 : 8) && CECooldowns.CheckCD("MechanicalBookmark", 30)) {
                Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, Vector2.UnitY * -8, ModContent.ProjectileType<Detector>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 20, projectile.DamageType), projectile.knockBack, projectile.owner);
            }
        }
    }
}
