using CalamityEntropy.Core.Weapons;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class LurkersCharm : ModItem
    {
        public static float damage = 0.12f;
        public static float MoveSpeed = 0.12f;
        public static float jumpSpeed = 0.12f;
        // 大招充能速度 +10%
        public static float chargeRate = 0.10f;
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.defense = 4;
            Item.height = 42;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[A]", damage.ToPercent());
            tooltips.Replace("[B]", MoveSpeed.ToPercent());
            tooltips.Replace("[D]", jumpSpeed.ToPercent());
            tooltips.Replace("[C]", chargeRate.ToPercent());

        }
        public static string ID = "LurkersCharm";

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 新效果:盗贼伤害转全伤害,潜行回复转大招充能速度(潜行体系退役)
            player.GetDamage(DamageClass.Generic) += damage;
            player.Entropy().moveSpeed += MoveSpeed;
            player.jumpSpeedBoost += Player.jumpSpeed * jumpSpeed;
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += chargeRate;
            player.Entropy().addEquip(ID, !hideVisual);
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_RogueEmblem))
            {
                CreateRecipe().AddIngredient(ItemID.Magiluminescence)
                .AddIngredient(CEID.Item_RogueEmblem, 1)
                .AddIngredient(ItemID.SoulofNight, 4)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Magiluminescence)
                .AddIngredient(ItemID.AvengerEmblem)
                .AddIngredient(ItemID.Ectoplasm, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
