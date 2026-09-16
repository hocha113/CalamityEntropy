using CalamityEntropy.Content.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkIntergelactic : BookMark
    {
        public override Texture2D UITexture => BookMark.GetUITexture("Intergelactic");
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(platinum: 1);
        }
        public override Color tooltipColor => Color.LightSkyBlue;
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Gel, 99)
                .AddIngredient(ItemID.FragmentNebula, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
        public override EBookProjectileEffect getEffect() {
            return new IGBMEffect();
        }
    }
    public class IGBMEffect : EBookProjectileEffect
    {
        public override void BookUpdate(Projectile projectile, bool s) {
            if (s && projectile.ModProjectile is EntropyBookHeldProjectile book) {
                if (Main.GameUpdateCount % 50 == 0) {
                    Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, projectile.velocity * 0.6f, ModContent.ProjectileType<NovaSlimerProj>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 320, projectile.DamageType), projectile.knockBack, projectile.owner);
                }
            }
        }
    }
}
