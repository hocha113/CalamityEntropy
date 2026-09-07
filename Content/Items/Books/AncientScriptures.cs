using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books
{
    public class AncientScriptures : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 20;
            Item.crit = 4;
            Item.mana = 5;
        }
        public override int HeldProjectileType => ModContent.ProjectileType<AncientScripturesHeld>();
        public override int SlotCount => 1;

        public override void AddRecipes()
        {
            // 原灾厄 LoreAwakening(新手袋赠品, 零门槛)原料删除, 不影响可达性
            CreateRecipe()
                .AddIngredient(ItemID.Silk, 10)
                .AddIngredient(ItemID.ManaCrystal, 3)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    public class AncientScripturesHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/AncientScriptures/AncientScripturesOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/AncientScriptures/AncientScripturesPage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/AncientScriptures/AncientScripturesUI";
    }

}
