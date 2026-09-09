using CalamityEntropy.Common;
using CalamityEntropy.Core.CalamityRef;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class Fruitcake : ModItem, IDonatorItem
    {
        public static Dictionary<int, List<int>> ammoList = new();
        public string DonatorName => "永霞伊";
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().fruitCake = true;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_OverloadedSludge, CEID.Item_PurifiedGel))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_OverloadedSludge)
                .AddIngredient(ItemID.WoodenArrow)
                .AddIngredient(ItemID.SlimeCrown)
                .AddIngredient(CEID.Item_PurifiedGel, 8)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Bone, 20)
                .AddIngredient(ItemID.QueenSlimeCrystal)
                .AddIngredient(ItemID.PinkGel, 10)
                .AddIngredient(ItemID.WoodenArrow)
                .AddTile(TileID.Anvils)
                .Register();
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                if (tooltips[i].Mod == "Terraria" && tooltips[i].Text.StartsWith("#"))
                {
                    bool hide = true;
                    if (int.TryParse(tooltips[i].Text[1].ToString(), out int n))
                    {
                        if (Level() >= n)
                        { hide = false; }
                    }
                    tooltips[i].Text = tooltips[i].Text.Substring(2);
                    if (hide)
                    {
                        tooltips.RemoveAt(i);
                    }
                }
            }
        }
        public static int Level()
        {
            // 成长阶梯按 progression-map.md 重排：原版节点 + 自有 Boss 线
            int l = 0;
            if (NPC.downedSlimeKing || NPC.downedBoss1 || NPC.downedBoss2 || CECal.DownedDesertScourge)
            {
                l = 1;
            }
            if (NPC.downedBoss2)
            {
                l = 2;
            }
            if (CECal.DownedSlimeGod)
            {
                l = 3;
            }
            if (CECal.DownedCryogen || CECal.DownedBrimstoneElemental)
            {
                l = 4;
            }
            if (EDownedBosses.downedProphet)
            {
                l = 5;
            }
            if (NPC.downedMoonlord)
            {
                l = 6;
            }
            if (CECal.DownedPolterghast)
            {
                l = 7;
            }
            return l;
        }
    }
}
