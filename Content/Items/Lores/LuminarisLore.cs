using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Lores
{
    public class LuminarisLore : CELoreItem
    {
        public static float wingTimeAddition = 0.05f;

        public override void SetDefaults() {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ModContent.RarityType<Lunarblight>();
            Item.maxStack = 1;
        }
    }
}
