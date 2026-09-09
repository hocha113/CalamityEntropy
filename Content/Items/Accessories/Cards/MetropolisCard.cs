using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class MetropolisCard : ModItem
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
            // 原+10%潜行上限,潜行退役后按 Echo 前缀先例减半转通用伤害
            player.GetDamage(DamageClass.Generic) += 0.05f;
            player.GetModPlayer<EModPlayer>().metropolisCard = true;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AncientBoneDust, CEID.Item_EssenceofHavoc))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_AncientBoneDust, 5).
                AddIngredient(CEID.Item_EssenceofHavoc, 3).
                AddTile(TileID.Bookcases).
                Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.FossilOre, 30)
                .AddIngredient(ItemID.Amber, 2)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
