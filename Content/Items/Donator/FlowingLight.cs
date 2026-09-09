using CalamityEntropy.Common;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator
{
    [AutoloadEquip(EquipType.Wings)]
    public class FlowingLight : ModItem, IDonatorItem
    {
        public override string LocalizationCategory => "Items.Accessories.Wings";

        // 原灾厄 BaseWings 的五项飞行参数，改由本类 VerticalWingSpeeds 直接承接
        public float BonusAscentWhileFalling => 1f;
        public float BonusAscentWhileRising => 0.17f;
        public float RisingSpeedThreshold => 1.5f;
        public float MaxAscentSpeed => 4f;
        public float BaseAscent => 0.15f;

        public string DonatorName => "五彩斑斓的黑";

        public override void SetStaticDefaults() => ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(540, 12f, 3f);


        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 50;
            Item.value = Item.buyPrice(platinum: 2, gold: 80);
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.accessory = true;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = BonusAscentWhileFalling;
            ascentWhenRising = BonusAscentWhileRising;
            maxCanAscendMultiplier = RisingSpeedThreshold;
            maxAscentMultiplier = MaxAscentSpeed;
            constantAscend = BaseAscent;
        }
        public override void ModifyTooltips(List<TooltipLine> list)
        {
            if (!Config.Instance.TextEffects)
            {
                list.Replace("$", "");
            }
        }
        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Text.StartsWith("$"))
            {
                if (!Config.Instance.TextEffects)
                {
                    return true;
                }
                DrawableTooltipLine nLine = new DrawableTooltipLine(new(Mod, "-", line.Text.Replace("$", "")), line.Index, line.X, line.Y, line.Color);
                // 鎏金描字：借用自有 ShiningViolet 通用描绘，配 Golden 稀有度同源金色
                ShiningViolet.Draw(Item, nLine, new Color(246, 200, 0), new Color(255, 236, 130), new Color(255, 220, 80));
                return false;
            }
            return true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().addEquip("FlowingLightWing", !hideVisual);
            player.accRunSpeed = 9f;
            player.moveSpeed += 0.18f;
            player.iceSkate = true;
            player.waterWalk = true;
            player.fireWalk = true;
            player.lavaImmune = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.noFallDmg = true;

            if (player.controlJump && player.controlDown && player.wingTime > 0)
            {
                player.velocity.Y = BonusAscentWhileFalling + 0.142f;
            }
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().addEquipVisual("FlowingLightWing");
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_SeraphTracers, CEID.Item_WingsofRebirth))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_SeraphTracers)
                .AddIngredient(CEID.Item_WingsofRebirth)
                .AddIngredient<FadingRunestone>(2)
                .AddTile<VoidWellTile>()
                .DisableDecraft()
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.TerrasparkBoots)
                .AddIngredient(ItemID.LongRainbowTrailWings)
                .AddIngredient<FadingRunestone>()
                .AddTile<VoidWellTile>()
                .DisableDecraft()
                .Register();
        }
    }
}
