using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureTeslaGun : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            Item.damage = 25;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 194;
            Item.height = 42;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 16;
            Item.value = Item.buyPrice(1, 0);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = CEUtils.GetSound("ofshoot");
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<TeslaBall>();
            Item.shootSpeed = 6;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DubiousPlating, CEID.Item_AerialiteBar))
            {
                CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(4)
                .AddIngredient(CEID.Item_DubiousPlating, 8)
                .AddIngredient(CEID.Item_AerialiteBar, 8)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(5)
                .AddIngredient(ItemID.MeteoriteBar, 20)
                .AddTile(TileID.Anvils)
                .Register();
        }


        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 76f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(0 + ((player.itemAnimation >= (player.itemAnimationMax * 0.75f)) ? CEUtils.Parabola(4 * (player.itemAnimation - player.itemAnimationMax * 0.75f) / (float)player.itemAnimationMax, 24) : 0), 0);

            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }
        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
        #endregion
    }
}
