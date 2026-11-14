using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class CelestialRing : ModItem
    {
        public override void SetDefaults() {
            Item.width = 26;
            Item.defense = 6;
            Item.height = 26;
            Item.value = Item.buyPrice(gold: 36);
            Item.rare = ModContent.RarityType<NihilityBlue>();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.noKnockback = true;
            player.GetDamage(DamageClass.Generic) += 0.12f;
            player.GetCritChance(DamageClass.Generic) += 5;
            player.GetAttackSpeed(DamageClass.Melee) += 0.12f;
            player.lifeRegen += 3; 
            player.maxMinions += 1;
            player.Entropy().moveSpeed += 0.10f;
            player.Entropy().WingTimeAddTicks += 60;
            player.jumpSpeedBoost += Player.jumpSpeed * 0.10f;
            player.breathEffectiveness += 1f;  //呼吸时长翻倍,耗氧减半
        }

        public override void AddRecipes() {
            CreateRecipe().
                AddIngredient(ItemID.CelestialShell).
                AddIngredient(ItemID.PapyrusScarab).
                AddIngredient<BlazingSunlight>(15).
                AddIngredient<WraithSoulEssence>(15)
                .AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
