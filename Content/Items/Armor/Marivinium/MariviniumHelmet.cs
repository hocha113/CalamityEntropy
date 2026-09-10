using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.Marivinium
{
    [AutoloadEquip(EquipType.Head)]
    public class MariviniumHelmet : ModItem
    {
        public static int ShieldCd = 36 * 60;
        public static int MaxShield = 2;
        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.value = Item.buyPrice(platinum: 2, gold: 80);
            Item.defense = 50;
            Item.rare = ModContent.RarityType<AbyssalBlue>();
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MariviniumBodyArmor>() && legs.type == ModContent.ItemType<MariviniumLeggings>();
        }


        // 2026-08-31 平衡案:套装奖励重做。水中畅行+无限飞行走 MariviniumSet 的 WaterCollision 钩子,
        // 渊海护盾(两层/36s/第二层减半/破盾给深渊狂怒)走 EModPlayer 的既有护盾计时。
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Mod.GetLocalization("MariviniumSet").Value;
            player.Entropy().MariviniumSet = true;
            // 降低20%敌怪接触伤害
            player.Entropy().meleeDamageReduce += 0.20f;
            // 大幅提升自然生命再生(4hp/s)
            player.lifeRegen += 8;
            // +3仆从栏与+20%近战攻速
            player.maxMinions += 3;
            player.GetAttackSpeed(DamageClass.Melee) += 0.20f;
        }
        // 2026-08-31 平衡案把 ApplyBuffImmune 的调用连同 +10 仆从栏、+75% 召唤伤害、
        // 静止回血与真近战加成一起从套装奖励里删掉,同笔提交还把三件的防御、暴击、
        // 生命上限成套下调,是有意削弱而非漏改,故那 30 行减益免疫表在此一并移除。
        // 套装文案仍写着"免疫绝大多数减益",需由本地化批次删除该承诺。
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.2f;
            player.GetCritChance(DamageClass.Generic) += 10;
            player.statLifeMax2 += 100;
            player.statManaMax2 += 100;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_OmegaBlueHelmet, ItemID.HallowedMask)
                .AddIngredient<WyrmTooth>(4)
                .AddIngredient<FadingRunestone>()
                .AddTile<AbyssalAltarTile>()
                .Register();
        }
    }
}
