using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories.EvilCards
{
    public class Sacrifice : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<EModPlayer>().SacrificeCard = true;
            player.GetDamage(DamageClass.Generic) += 0.1f;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_EssenceofHavoc, CEID.Item_PerennialBar))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_EssenceofHavoc, 6)
                .AddIngredient(CEID.Item_PerennialBar, 2)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.Ectoplasm, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
