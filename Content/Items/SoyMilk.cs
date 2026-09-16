using CalamityEntropy.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items;

public class SoyMilk : ModItem
{
    public override void SetDefaults() {
        Item.width = 40;
        Item.height = 40;
        Item.useTurn = true;
        Item.maxStack = 30;
        Item.rare = ItemRarityID.Orange;
        Item.useAnimation = 17;
        Item.useTime = 17;
        Item.useStyle = ItemUseStyleID.DrinkLiquid;
        Item.UseSound = SoundID.Item3;
        Item.consumable = true;
        Item.buffType = ModContent.BuffType<SoyMilkBuff>();
        Item.buffTime = CEUtils.SecondsToFrames(300f);
        Item.value = Item.buyPrice(0, 5, 0, 0);
    }
}
