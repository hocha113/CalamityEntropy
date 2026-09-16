using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Walls
{
    public class BeewaxWall : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 200;
        }

        // 灾厄巨型蜂巢墙随脱离灾厄移除，改放置原版安全蜂巢墙
        public override void SetDefaults() => Item.DefaultToPlaceableWall(WallID.Hive);

        public override void AddRecipes() {
            CreateRecipe(40).
            AddIngredient(ItemID.BeeWax).
            AddTile(TileID.HoneyDispenser).
            Register();
        }
    }
}
