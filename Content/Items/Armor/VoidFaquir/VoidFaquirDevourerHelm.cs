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
    public class VoidFaquirDevourerHelm : ModItem
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
            Item.defense = 50;
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

        // 2026-08-31 平衡案:职业专属奖励=+20%近战攻速、接触伤害降低15%
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.20f;
            player.Entropy().meleeDamageReduce += 0.15f;
            player.Entropy().VFSet = true;
            player.Entropy().VFHelmMelee = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().meleeVF = true;
            player.GetDamage(DamageClass.Melee) += 0.20f;
            player.GetCritChance(DamageClass.Melee) += 12;
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
