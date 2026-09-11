using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.Oath
{
    public class OathBanner : ModItem
    {
        public static float MoveSpeedDecrease = 0.1f;
        public static int TeamDefense = 4;
        public static int TeamLifeRegenSec = 1;
        public static float BuffDamageAddition = 0.09f;
        public static int AggroBonus = 800;
        public static int TeamBuffRange = 3200;
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[MSD]", MoveSpeedDecrease.ToPercent());
            tooltips.Replace("[DR]", TeamDefense.ToString());
            tooltips.Replace("[REG]", TeamLifeRegenSec);
            tooltips.Replace("[DMG]", BuffDamageAddition.ToPercent());
        }
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 56;
            Item.accessory = true;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().oathBanner = true;
            player.Entropy().oathBannerVisual = !hideVisual;
            player.aggro += AggroBonus;
            player.statDefense += TeamDefense;
            player.lifeRegen += TeamLifeRegenSec * 2;
            player.GetDamage(DamageClass.Generic) += BuffDamageAddition;
            player.Entropy().moveSpeed -= MoveSpeedDecrease;
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().oathBannerVisual = true;
        }
        // 两条配方档位不对等:昆古尼尔本身要 18 神圣锭,钴蓝薙刀只要 10 钴锭。
        // 原版微光拆解取的是 CraftingRecipeIndices 里第一条可拆解的配方,也就是昆古尼尔那条,
        // 于是"薙刀合战旗 → 微光拆出昆古尼尔 → 微光拆出 18 神圣锭"成了刷锭循环。
        // 两条都禁用拆解即可,合成侧一字不动。
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Gungnir).
                AddIngredient(ItemID.SoulofNight, 8).
                AddIngredient(ItemID.Silk, 12).
                DisableDecraft().
                Register();
            CreateRecipe().
                AddIngredient(ItemID.CobaltNaginata).
                AddIngredient(ItemID.SoulofNight, 8).
                AddIngredient(ItemID.Silk, 12).
                DisableDecraft().
                Register();
        }
    }
    public class OathofCommand : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) += OathBanner.BuffDamageAddition;
            player.statDefense += OathBanner.TeamDefense;
            player.lifeRegen += OathBanner.TeamLifeRegenSec * 2;
        }
    }
}
