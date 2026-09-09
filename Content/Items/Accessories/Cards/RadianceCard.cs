using CalamityEntropy.Common;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class RadianceCard : ModItem
    {
        public static float LifeRegenMul = 0.2f; //+20%生命恢复

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
            player.lifeRegen = (int)(player.lifeRegen * (1 + LifeRegenMul));
            player.GetModPlayer<EModPlayer>().radianceCard = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[LR]", (int)(Math.Round(LifeRegenMul * 100)));
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_StarblightSoot, CEID.Item_EssenceofSunlight))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_StarblightSoot, 5)
                .AddIngredient(CEID.Item_EssenceofSunlight, 5)
                .AddIngredient(ItemID.SoulofLight, 3)
                .AddTile(TileID.CrystalBall)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.SoulofLight, 4)
                .AddIngredient(ItemID.PixieDust, 4)
                .AddIngredient(ItemID.FallenStar, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
