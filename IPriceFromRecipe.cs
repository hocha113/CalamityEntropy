using InnoVault;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy
{
    public interface IPriceFromRecipe
    {
        public virtual int AdditionalPrice => 0;
    }
    public class PriceSetGItem : GlobalItem
    {
        public override void SetDefaults(Item entity) {
            if (PriceSetSys.Inited && entity.ModItem != null && entity.ModItem is IPriceFromRecipe pfr) {
                Recipe recipe = CEUtils.FindRecipe(entity.type);
                if (recipe != null) {
                    entity.value = entity.ModItem.GetPriceFromRecipe(recipe) + pfr.AdditionalPrice;
                }
            }
        }
    }
    public class PriceSetSys : ModSystem
    {
        public static bool Inited = false;
        public override void AddRecipes() {
        }
        public override void Load() {
            VaultLoad.EndLoadenEvent += endLoad;
        }

        public override void Unload() {
            VaultLoad.EndLoadenEvent -= endLoad;
        }

        private void endLoad() {
            Inited = true;
            for (int i = 0; i < ItemLoader.ItemCount; i++) {
                Item item = ContentSamples.ItemsByType[i];
                if (item.ModItem != null && item.ModItem is IPriceFromRecipe pfr) {
                    Recipe recipe = CEUtils.FindRecipe(item.type);
                    if (recipe == null) {
                        continue;
                    }
                    item.value = item.ModItem.GetPriceFromRecipe(recipe) + pfr.AdditionalPrice;
                }
            }
        }
    }
}
