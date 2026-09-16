using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Vanity.Luminar
{
    [AutoloadEquip(EquipType.Body)]
    public class LuminarDress : ModItem, IDonatorItem
    {
        public string DonatorName => "玲瓏";
        public override void SetDefaults() {
            Item.width = 48;
            Item.height = 48;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ModContent.RarityType<Lunarblight>();
            Item.vanity = true;
        }
    }
}
