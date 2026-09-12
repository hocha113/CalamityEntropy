using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Weapons;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    [AutoloadEquip(EquipType.Head)]
    public class VoidFaquirLurkerMask : ModItem
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
            Item.defense = 30;
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

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = VoidFaquirSet.BonusText(Mod, "helmvfl");
            player.GetArmorPenetration(DamageClass.Generic) += 20;
            // 潜行体系退役:原潜行条(上限1.35)按容量×10%换算为大招充能速度
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.135f;
            player.Entropy().VFSet = true;
            player.Entropy().VFHelmRogue = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().rogueVF = true;
            // 脱离灾厄:原盗贼伤害/暴击按「盗贼→全伤害」规则转通用
            player.GetDamage(DamageClass.Generic) += 0.25f;
            player.GetCritChance(DamageClass.Generic) += 25;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

        }

        public override void AddRecipes()
        {
            // 2026-08-31 平衡案:暂时删去盗贼头盔的获取方式(不可合成,物品保留)
        }
    }
}
