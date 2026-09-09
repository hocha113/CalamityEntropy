using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class AzafureDetectionEquipment : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 46;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.accessory = true;
            Item.defense = 2;
        }
        public static string ID = "AzafureDetectorEquipment";

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.rocketBoots += 90;
            player.noFallDmg = true;
            player.jumpSpeedBoost += player.AzafureEnhance() ? 1.6f : 0.8f;
            player.maxRunSpeed *= 1.12f;
            player.Entropy().addEquip(ID, !hideVisual);
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().addEquipVisual(ID);
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AerialiteBar))
            {
                CreateRecipe().
                AddIngredient<RustyDetectionEquipment>().
                AddIngredient<HellIndustrialComponents>(4).
                AddIngredient(CEID.Item_AerialiteBar, 8).
                AddTile(TileID.Anvils).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient<RustyDetectionEquipment>().
                AddIngredient<HellIndustrialComponents>(5).
                AddIngredient(ItemID.CobaltBar, 10).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
