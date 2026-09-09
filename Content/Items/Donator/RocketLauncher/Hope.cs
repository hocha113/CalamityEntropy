using CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo;
using CalamityEntropy.Content.Items.Weapons;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher
{
    public class Hope : ModItem
    {
        public static int MaxStick => 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxStick);
        public static int ExplodeRadius => 60;
        public override void SetDefaults()
        {
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<CharredMissileProj>(), BaseMissileProj.AmmoType, singleShotTime: 52, shotVelocity: 30f, hasAutoReuse: true);
            Item.width = 90;
            Item.height = 42;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 28;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item61;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.Orange;
            Item.Entropy().tooltipStyle = 8;
            Item.Entropy().strokeColor = Color.DarkBlue;
            Item.Entropy().NameColor = new Color(80, 220, 255);
            Item.Entropy().NameLightColor = Color.Black;
            Item.ArmorPenetration = 8;
        }

        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 76f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = -HoldoutOffset().Value;
            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (-0.2f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-27f, -4f);
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        #endregion

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AerialiteBar))
            {
                CreateRecipe()
                .AddIngredient<RustExpeditioner>()
                .AddIngredient<OsseousRemains>(20)
                .AddIngredient(CEID.Item_AerialiteBar, 10)
                .AddIngredient(ItemID.SunplateBlock, 4)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Minishark)
                .AddIngredient(ItemID.Feather, 10)
                .AddIngredient(ItemID.SunplateBlock, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }


        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            position += (new Vector2(54, -16) * new Vector2(1, player.direction)).RotatedBy(velocity.ToRotation());
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, MaxStick, ExplodeRadius);
            p.ToProj().Entropy().applyBuffs.Add(BuffID.OnFire3);
            return false;
        }
    }
}
