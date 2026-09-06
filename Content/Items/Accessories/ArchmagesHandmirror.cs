using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ArchmagesHandmirror : ModItem
    {
        public static int EnhancedManaFlat = 75;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 5));
        }


        public override void SetDefaults()
        {
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
            Item.accessory = true;
        }

        public override void UpdateEquip(Player player)
        {
            // 走强化魔力的固定点数通道,而非普通魔力上限:这 75 点要算进金色段并吃 0.15%/点的魔法伤害
            player.Entropy().enhancedManaFlat += EnhancedManaFlat;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[MANA]", EnhancedManaFlat);
        }
    }
}
