using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class VoidMonolith : ModItem
    {
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 28;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<VoidMonolithTile>();
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.accessory = true;
            Item.vanity = true;
        }

        public override void UpdateEquip(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.Entropy().crSky = 30;
            }
        }
        public override void UpdateVanity(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.Entropy().crSky = 30;
            }
        }
    }
}
