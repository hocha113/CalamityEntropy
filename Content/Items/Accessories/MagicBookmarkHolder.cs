using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class MagicBookmarkHolder : ModItem, IPriceFromRecipe
    {
        //额外书签槽外观贴图,加载期就位;仅在 !Main.dedServ 分支读取
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/Extra1")]
        internal static Texture2D SlotTex;
        public int AdditionalPrice => 200;
        public static float MAGEDAMAGE = 0.05f;
        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 30;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(gold: 2);
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().AdditionalBookmarkSlot += 1;
            player.GetDamage(DamageClass.Magic) += MAGEDAMAGE;
            if (!Main.dedServ)
                player.Entropy().BookmarkHolderSpecialTextures.Add(SlotTex);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[D]", MAGEDAMAGE.ToPercent().ToString());
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.ManaCrystal, 2)
                .AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.RichMahogany, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
