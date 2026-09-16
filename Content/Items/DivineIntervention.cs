using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Cooldowns;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class DivineIntervention : ModItem
    {
        public override void SetStaticDefaults() {
        }

        public override void SetDefaults() {
            Item.width = 26;
            Item.height = 26;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = -1;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<DivineShield>();
            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 5;

        }
        public override bool CanUseItem(Player player) {
            if (player.HasBuff(ModContent.BuffType<DivineShieldCooldown>()))
                return false;
            foreach (NPC n in Main.ActiveNPCs) {
                if (n.IsABoss())
                    return false;
            }
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(ModContent.BuffType<DivineShieldCooldown>(), 18000, true, false);
            player.AddCooldown(DivineCd.ID, 18000);
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.HallowedBar, 5)
                .AddIngredient(ItemID.Ruby, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

    }
}
