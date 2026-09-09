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
    public class VoidFaquirEvokerHelm : ModItem
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
            Item.defense = 28;
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

        // 2026-08-31 平衡案:职业专属奖励=+32%召唤伤害、+4仆从栏、召唤迷你虚空吞噬者(VFHelmSummoner 驱动)
        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.32f;
            player.maxMinions += 4;
            player.Entropy().VFSet = true;
            player.Entropy().VFHelmSummoner = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().summonerVF = true;
            player.GetDamage(DamageClass.Summon) += 0.40f;
            player.maxMinions += 1;
            player.statLifeMax2 += 80;
            player.statManaMax2 += 100;
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
