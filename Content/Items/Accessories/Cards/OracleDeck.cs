using CalamityEntropy.Common;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class OracleDeck : ModItem, IDeck
    {

        // 2026-08-31 平衡案重做:5防,+5%伤害/暴击/减伤,+10%移速/近战攻速/魔力减耗,
        // +1召唤栏,100%光照,每10秒团队回20血,单次受伤≤最大生命66.7%(5秒CD)。
        // 减伤/移速/魔耗/光照/治疗/伤害上限分别挂在 EModPlayer 与 CalamityEntropy 的 oracleDeck 站点。
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.defense = 5;
            Item.height = 22;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;

        }
        public static int CRIT = 5;
        public static float DAMAGE = 0.05f;
        public static int MINIONADD = 1;
        public static float MELEEAS = 0.1f;
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().oracleDeckInInv = true;
            player.GetCritChance(DamageClass.Generic) += CRIT;
            player.GetDamage(DamageClass.Generic) += DAMAGE;
            player.maxMinions += MINIONADD;
            player.GetAttackSpeed(DamageClass.Melee) += MELEEAS;
            player.Entropy().moveSpeed += 0.10f;
            player.GetModPlayer<EModPlayer>().oracleDeck = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[CR]", CRIT);
            tooltips.Replace("[ST]", DAMAGE.ToPercent());
            tooltips.Replace("[MN]", MINIONADD);
            tooltips.Replace("[ATS]", MELEEAS.ToPercent());
        }
        public override void UpdateInventory(Player player)
        {
            player.Entropy().oracleDeckInInv = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return !(equippedItem.ModItem is IDeck && incomingItem.ModItem is IDeck);
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AuraCard>()
                .AddIngredient<BrillianceCard>()
                .AddIngredient<EntityCard>()
                .AddIngredient<InspirationCard>()
                .AddIngredient<MetropolisCard>()
                .AddIngredient<WisdomCard>()
                .AddIngredient<RadianceCard>()
                .AddIngredient<TemperanceCard>()
                .AddIngredient<EnduranceCard>()
                .AddIngredient<ThreadOfFate>()
                .AddCalOrOwn(CEID.Item_CoreofCalamity, 1, ItemID.LunarBar, 5)
                .AddTile(TileID.Bookcases)
                .Register();
        }
    }
}
