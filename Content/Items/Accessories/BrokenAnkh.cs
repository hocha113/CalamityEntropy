using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Armor;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class BrokenAnkh : ModItem
    {
        public override void ModifyTooltips(List<TooltipLine> list) {
            if (!isaac) {
                list.Add(new TooltipLine(Mod, "isaac_lock", Mod.GetLocalization("IsaacLock").Value) { OverrideColor = Color.Red });
            }
            else {
                // 脱离灾厄:灾厄 IntegrateHotkey 扩展改自有键名提示
                list.Replace("[KEY]", CEKeybinds.ThrowPoopHotKey.TooltipKeyHint());
            }
        }
        public override void SetDefaults() {
            Item.width = 50;
            Item.height = 50;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().brokenAnkh = true;
        }
        public static bool isaac = false;
        public override void Unload() {
            isaac = false;
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded) {
            return isaac;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.PoopBlock, 8)
                .AddIngredient(ItemID.Wood, 12)
                .AddTile(TileID.WorkBenches)
                .AddCondition(new Condition(Mod.GetLocalization("DownloadBOI"), () => isaac))
                .Register();
        }

    }
}
