using CalamityEntropy.Content.Tiles.Furniture;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture
{
    public enum CEFurnitureKind
    {
        Bathtub,
        Bed,
        Bookcase,
        Candelabra,
        Candle,
        Chair,
        Chandelier,
        Chest,
        Clock,
        Door,
        Dresser,
        Lamp,
        Lantern,
        Piano,
        Platform,
        Sink,
        Sofa,
        Table,
        WorkBench,
    }

    /// <summary>
    /// 家具物品基类,放置 TTile,配方由系列与种类推出
    /// </summary>
    public abstract class CEFurnitureItem<TTile> : ModItem where TTile : ModTile
    {
        protected abstract CEFurnitureSeries Series { get; }

        protected abstract CEFurnitureKind Kind { get; }

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = Kind == CEFurnitureKind.Platform ? 200 : 1;
        }

        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<TTile>());
            if (Kind == CEFurnitureKind.Platform) {
                Item.width = 8;
                Item.height = 10;
            }
        }

        /// <summary>
        /// 主材料用量 = 原版同类家具的木料数 / Series.Divisor 向上取整,其余原料照原版配方
        /// </summary>
        public override void AddRecipes() {
            (int wood, int torches) = Kind switch {
                CEFurnitureKind.Bathtub => (14, 0),
                CEFurnitureKind.Bed => (15, 0),
                CEFurnitureKind.Bookcase => (20, 0),
                CEFurnitureKind.Candelabra => (5, 3),
                CEFurnitureKind.Candle => (4, 1),
                CEFurnitureKind.Chair => (4, 0),
                CEFurnitureKind.Chandelier => (4, 4),
                CEFurnitureKind.Chest => (8, 0),
                CEFurnitureKind.Clock => (10, 0),
                CEFurnitureKind.Door => (6, 0),
                CEFurnitureKind.Dresser => (16, 0),
                CEFurnitureKind.Lamp => (3, 1),
                CEFurnitureKind.Lantern => (6, 1),
                CEFurnitureKind.Piano => (15, 0),
                CEFurnitureKind.Sink => (6, 0),
                CEFurnitureKind.Sofa => (5, 0),
                CEFurnitureKind.Table => (8, 0),
                CEFurnitureKind.WorkBench => (10, 0),
                _ => (1, 0),
            };

            bool platform = Kind == CEFurnitureKind.Platform;
            Recipe recipe = CreateRecipe(platform ? 2 * Series.Divisor : 1);
            recipe.AddIngredient(Series.Material(), platform ? 1 : Math.Max(1, (wood + Series.Divisor - 1) / Series.Divisor));
            if (torches > 0) {
                recipe.AddIngredient(Series.LightMaterial?.Invoke() ?? ItemID.Torch, torches);
            }

            switch (Kind) {
                case CEFurnitureKind.Bed:
                    recipe.AddIngredient(ItemID.Silk, 5);
                    break;
                case CEFurnitureKind.Sofa:
                    recipe.AddIngredient(ItemID.Silk, 2);
                    break;
                case CEFurnitureKind.Bookcase:
                    recipe.AddIngredient(ItemID.Book, 10);
                    break;
                case CEFurnitureKind.Piano:
                    recipe.AddIngredient(ItemID.Bone, 4).AddIngredient(ItemID.Book);
                    break;
                case CEFurnitureKind.Chandelier:
                    recipe.AddIngredient(ItemID.Chain);
                    break;
                case CEFurnitureKind.Chest:
                    recipe.AddRecipeGroup(RecipeGroupID.IronBar, 2);
                    break;
                case CEFurnitureKind.Clock:
                    recipe.AddRecipeGroup(RecipeGroupID.IronBar, 3).AddIngredient(ItemID.Glass, 6);
                    break;
                case CEFurnitureKind.Sink:
                    recipe.AddIngredient(Series.LavaSink ? ItemID.LavaBucket : ItemID.WaterBucket);
                    break;
            }

            recipe.AddTile(Series.Station()).Register();
        }
    }
}
