using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Rarities;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureStandardShotgun : ModItem, IAzafureEnhancable
    {
        public override bool RangedPrefix()
        {
            return true;
        }
        public override void SetDefaults()
        {
            Item.damage = 88;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 82;
            Item.height = 36;
            Item.useTime = 44;
            Item.useAnimation = 44;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 14;
            Item.value = Item.buyPrice(0, 5);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = CEUtils.GetSound("shotgun", 1.6f, 8, 0.6f);
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<ImperialGuardShot>();
            Item.shootSpeed = 38;
        }
        public static int BulletCount = 6;
        public static int BulletCountEnhanced = 9;
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-2, 0);
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_PerennialBar))
            {
                CreateRecipe()
                .AddIngredient(ItemID.Shotgun)
                .AddIngredient<HellIndustrialComponents>(8)
                .AddIngredient(CEID.Item_PerennialBar, 8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Shotgun)
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient(ItemID.ChlorophyteBar, 12)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
        public override float UseSpeedMultiplier(Player player)
        {
            return 1f;
        }

        #region Shooting
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for(int i = 0; i < (player.AzafureEnhance() ? BulletCountEnhanced : BulletCount); i++)
            {
                int p = Projectile.NewProjectile(source, position + velocity.SafeNormalize(Vector2.Zero) * 16, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 1.3f), type, damage, knockback, player.whoAmI);
            }
            return false;
        }
        #endregion

        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 20f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(0, 0);



            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (-0.32f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);

            if (animProgress > 0.5f)
            {
                float backArmRotation = rotation + 0.52f * player.direction;

                Player.CompositeArmStretchAmount stretch = ((float)Math.Sin(MathHelper.Pi * (animProgress - 0.5f) / 0.36f)).ToStretchAmount();
                player.SetCompositeArmBack(true, stretch, backArmRotation);
            }

        }
        #endregion
    }
}
