using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    [AutoloadEquip(EquipType.Head)]
    public class VoidFaquirShadowHelm : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.defense = 42;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<VoidFaquirBodyArmor>() && legs.type == ModContent.ItemType<VoidFaquirCuises>();
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawOutlines = true;
        }

        // 2026-08-31 平衡案:职业专属奖励=远程伤害加成额外×1.15、+50%射弹速度
        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Ranged) *= 1.15f;
            player.Entropy().shootSpeed += 0.5f;
            player.Entropy().VFSet = true;
            player.Entropy().VFHelmRanged = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().rangerVF = true;
            player.GetDamage(DamageClass.Ranged) += 0.18f;
            player.GetCritChance(DamageClass.Ranged) += 25;
            player.statLifeMax2 += 80;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_TwistingNether))
            {
                CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 14)
                .AddIngredient(CEID.Item_TwistingNether, 4)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 12)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
