using CalamityMod.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class WispLantern : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 54;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().visualWispLantern = !hideVisual;
            player.Entropy().accWispLantern = true;
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().visualWispLantern = true;
        }
        public override void AddRecipes()
        {
        }
    }
}
