using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories.EvilCards
{
    public class TaintedDeck : ModItem, IDeck
    {

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return !(equippedItem.ModItem is IDeck && incomingItem.ModItem is IDeck);
        }
        // 2026-08-31 平衡案重做:+30%伤害但受伤×1.25;每召唤栏+1.5%伤害(上限15%,走贪婪卡公式);
        // 投掷武器轻微追踪(EGlobalProjectile 的 EvilDeck 站点);攻击时追踪暗影火焰并附蒙蔽/虚空之触
        // (晦暗+迷惑+虚无三卡旗标);+12%近战攻速,-10%暴击,+25%魔耗;自然生命再生-100%(UpdateLifeRegen)。
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<EModPlayer>().EvilDeck = true;

            player.GetDamage(DamageClass.Generic) += 0.30f;
            player.Entropy().damageReduce -= 0.25f;

            player.GetModPlayer<EModPlayer>().GreedCard = true;

            player.GetModPlayer<EModPlayer>().TarnishCard = true;
            player.GetModPlayer<EModPlayer>().ConfuseCard = true;
            player.GetModPlayer<EModPlayer>().NothingCard = true;
            player.Entropy().AttackVoidTouch += 0.03f;

            player.GetAttackSpeed(DamageClass.Melee) += 0.12f;
            player.GetCritChance(DamageClass.Generic) -= 10;
            player.Entropy().ManaCost += 0.25f;

            player.Entropy().taintedDeckInInv = true;
            player.Entropy().addEquip("TaintedDeck", !hideVisual);
        }
        public override void UpdateInventory(Player player)
        {
            player.Entropy().taintedDeckInInv = true;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<GreedCard>()
                .AddIngredient<Frail>()
                .AddIngredient<Barren>()
                .AddIngredient<Tarnish>()
                .AddIngredient<Confuse>()
                .AddIngredient<Perplexed>()
                .AddIngredient<Sacrifice>()
                .AddIngredient<Nothing>()
                .AddIngredient<Fool>()
                .AddIngredient<ThreadOfAbyss>()
                .AddCalOrOwn(CEID.Item_CoreofCalamity, 1, ItemID.LunarBar, 5)
                .AddTile(TileID.Bookcases)
                .Register();
        }
    }
}
