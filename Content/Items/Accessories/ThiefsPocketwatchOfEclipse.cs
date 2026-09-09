using CalamityEntropy.Content.Items;
using CalamityEntropy.Core.Weapons;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ThiefsPocketwatchOfEclipse : ModItem
    {
        public static float damage = 0.14f;
        public static float MoveSpeed = 0.14f;
        public static float jumpSpeed = 0.14f;
        // 大招充能速度 +12%
        public static float chargeRate = 0.12f;
        // 日蚀期间额外增伤
        public static float eclipseDamage = 0.10f;
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[A]", damage.ToPercent());
            tooltips.Replace("[B]", MoveSpeed.ToPercent());
            tooltips.Replace("[C]", chargeRate.ToPercent());
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 新效果:盗贼伤害转全伤害,潜行回复转大招充能速度,日蚀期间额外增伤(潜行体系退役)
            player.GetDamage(DamageClass.Generic) += damage;
            player.Entropy().moveSpeed += MoveSpeed;
            player.jumpSpeedBoost += Player.jumpSpeed * jumpSpeed;
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += chargeRate;
            // 2026-08-31 平衡案:死亡后复活并回复100生命(120秒冷却,结算在 EModPlayer.PreKill)
            player.Entropy().thiefWatch = true;
            if (Main.eclipse)
            {
                player.GetDamage(DamageClass.Generic) += eclipseDamage;
            }
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AscendantSpiritEssence, CEID.Item_DarksunFragment, CEID.Tile_CosmicAnvil))
            {
                CreateRecipe()
                .AddIngredient<LurkersCharm>(1)
                .AddIngredient(CEID.Item_AscendantSpiritEssence, 4)
                .AddIngredient(CEID.Item_DarksunFragment, 20)
                .AddIngredient(ItemID.GoldWatch)
                .AddTile(CEID.Tile_CosmicAnvil)
                .Register();

                CreateRecipe()
                .AddIngredient<LurkersCharm>(1)
                .AddIngredient(CEID.Item_AscendantSpiritEssence, 4)
                .AddIngredient(CEID.Item_DarksunFragment, 20)
                .AddIngredient(ItemID.PlatinumWatch)
                .AddTile(CEID.Tile_CosmicAnvil)
                .Register();
                return;
            }
            // 脱离灾厄:升华精魄→幽渊魂髓、暗日碎片→日耀碎片、宇宙铁砧→远古操纵机(material-map)
            CreateRecipe()
                .AddIngredient<LurkersCharm>()
                .AddIngredient<ChaoticPiece>(15)
                .AddIngredient(ItemID.FragmentSolar, 20)
                .AddIngredient(ItemID.GoldWatch)
                .AddTile(TileID.LunarCraftingStation)
                .Register();

            CreateRecipe()
                .AddIngredient<LurkersCharm>()
                .AddIngredient<ChaoticPiece>(15)
                .AddIngredient(ItemID.FragmentSolar, 20)
                .AddIngredient(ItemID.PlatinumWatch)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
