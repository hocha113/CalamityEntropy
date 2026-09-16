using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Lores
{
    public class LoreAcropolis : CELoreItem
    {
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.maxStack = 1;
        }
    }
}
