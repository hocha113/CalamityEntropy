using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Donator;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ShadowRune : ModItem, IDonatorItem
    {
        public string DonatorName => "南巷";
        public static float SummonDmgToMinionSlot = 6.25f;
        public static float WhipAtkSpeedAddition = 0.8f;

        public override void SetDefaults() {
            Item.width = 46;
            Item.height = 46;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Purple;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            var mp = player.GetModPlayer<EModPlayer>();
            mp.shadowRune = true;
            player.GetDamage(DamageClass.Magic) *= 0.5f;
            player.GetDamage(DamageClass.Melee) *= 0.5f;
            // 脱离灾厄:盗贼职业并入原版,投掷减半承接原盗贼减半
            player.GetDamage(DamageClass.Throwing) *= 0.5f;
            player.GetDamage(DamageClass.Ranged) *= 0.5f;
            if (!hideVisual) {
                player.Entropy().addEquipVisual("ShadowRune");
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<ShadowRuneVanity>()] < 1) {
                Projectile.NewProjectile(player.GetSource_FromAI(), player.Center, Vector2.Zero, ModContent.ProjectileType<ShadowRuneVanity>(), 0, 0, player.whoAmI);
            }
            player.whipRangeMultiplier *= 2;
        }
        public override void UpdateVanity(Player player) {
            player.Entropy().addEquipVisual("ShadowRune");
            if (player.ownedProjectileCounts[ModContent.ProjectileType<ShadowRuneVanity>()] < 1) {
                Projectile.NewProjectile(player.GetSource_FromAI(), player.Center, Vector2.Zero, ModContent.ProjectileType<ShadowRuneVanity>(), 0, 0, player.whoAmI);
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[0]", (1f / SummonDmgToMinionSlot).ToPercent());
            tooltips.Replace("[1]", WhipAtkSpeedAddition.ToPercent());
        }

        public override void AddRecipes() {
            CreateRecipe().
                AddIngredient(ItemID.FallenStar, 2).
                AddIngredient(ItemID.DemoniteBar, 4).
                AddIngredient(ItemID.ThornWhip).
                AddTile(TileID.Anvils).
                Register();

            CreateRecipe().
                AddIngredient(ItemID.FallenStar, 2).
                AddIngredient(ItemID.CrimtaneBar, 4).
                AddIngredient(ItemID.ThornWhip).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
