using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Dash;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class AzafureDriverCore : ModItem, IAzafureEnhancable
    {
        public const int DashDamage = 80;
        public const float DashKnockback = 6f;
        public const int DashImmuneFrames = 12;
        public const int DashDuration = 20;
        public const float DashDistance = 20 * 16f;
        public const int DashCooldown = 30;
        public float charge = 0;
        public float maxCharge = 5f;
        public static int RechargeTime = 20 * 60;
        public static int MaxShield = 40;
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 54;
            Item.value = Item.buyPrice(gold: 20);
            Item.defense = 6;
            Item.accessory = true;
            Item.rare = ModContent.RarityType<AzafureOrange>();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (charge < maxCharge)
            {
                charge += 1f / 300f;
            }
            player.Entropy().DriverShieldVisual = !hideVisual;
            player.Entropy().AzafureDriverShieldItem = Item;
            player.GetModPlayer<CEDashPlayer>().Offer(CEDashRegistry.Get<AzafureDriverDash>());
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().DriverShieldVisual = true;
        }
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (charge < maxCharge)
            {
                CEUtils.DrawChargeBar(scale * 1.2f, position + new Vector2(0, 18) * scale, ((float)charge / maxCharge), (charge < 1) ? Color.DarkOrange : Color.Orange);
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[S]", MaxShield.ToString());
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_RoverDrive, CEID.Item_AshesofCalamity))
            {
                CreateRecipe()
                .AddIngredient<AzafureChargeShield>()
                .AddIngredient(CEID.Item_RoverDrive)
                .AddIngredient(CEID.Item_AshesofCalamity, 6)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<AzafureChargeShield>()
                .AddIngredient(ItemID.MartianConduitPlating, 100)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class AzafureDriverDash : AzafureDashBase
    {
        public override string ID => "AzafureDriverDash";
        public override int Priority => 11;
        public override float Distance => AzafureDriverCore.DashDistance;
        public override int Duration => AzafureDriverCore.DashDuration;
        public override int Cooldown => AzafureDriverCore.DashCooldown;
        protected override int HitDamage => AzafureDriverCore.DashDamage;
        protected override float HitKnockback => AzafureDriverCore.DashKnockback;
        protected override int HitImmuneFrames => AzafureDriverCore.DashImmuneFrames;

        protected override bool TryGetCharge(Player player, out float charge)
        {
            charge = 0f;
            if (player.Entropy().AzafureDriverShieldItem?.ModItem is not AzafureDriverCore core)
                return false;
            charge = core.charge;
            return true;
        }

        protected override void ConsumeCharge(Player player, float cost)
        {
            if (player.Entropy().AzafureDriverShieldItem?.ModItem is AzafureDriverCore core)
                core.charge = Math.Max(0f, core.charge - cost);
        }
    }
}
