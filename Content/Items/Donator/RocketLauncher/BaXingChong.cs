using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher
{
    public class BaXingChong : ModItem, IDonatorItem
    {
        public static int MaxStick => 0;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxStick);
        public static int ExplodeRadius => 80;

        public string DonatorName => "Shadow Warrior";

        public override void SetDefaults()
        {
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<CharredMissileProj>(), BaseMissileProj.AmmoType, singleShotTime: 20, shotVelocity: 30f, hasAutoReuse: true);
            Item.width = 84;
            Item.height = 28;
            Item.useTime = 16;
            Item.useAnimation = 32;
            Item.reuseDelay = 60;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 380;
            Item.knockBack = 2f;
            Item.UseSound = null;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = CECal.RarityCalamityRed(ModContent.RarityType<VoidPurple>());
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
            if (animProgress < 0.5 && player.reuseDelay != 0)
                rotation += (-0.3f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
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
            if (CECal.CalChainReady(CEID.Item_Spyker, CEID.Item_UniversalGenesis, CEID.Item_MiracleMatter))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_Spyker)
                .AddIngredient(CEID.Item_UniversalGenesis)
                .AddIngredient(CEID.Item_MiracleMatter)
                .AddIngredient<FadingRunestone>()
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Redemption>()
                .AddIngredient<FadingRunestone>()
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }


        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            position += (new Vector2(54, -16) * new Vector2(1, player.direction)).RotatedBy(velocity.ToRotation());
            for (int i = 0; i < 8; i++)
            {
                var v = velocity + CEUtils.randomPointInCircle(4);
                int p = Projectile.NewProjectile(source, position + velocity * 1.5f, v, type, damage, knockback, player.whoAmI, MaxStick, ExplodeRadius);
                p.ToProj().Entropy().applyBuffs.Add(ModContent.BuffType<VoidTouch>());
                p.ToProj().Entropy().flameTrail = true;
                if (p.ToProj().ModProjectile is BaseMissileProj m)
                {
                    m.Homing = 6;
                    m.winding = Main.rand.NextFloat() * 0.7f;
                }
                CEUtils.SyncProj(p);
            }
            CEUtils.PlaySound("AcropolisShoot", 1, position);
            CEUtils.PlaySound("AcropolisShoot", 1, position);
            CEUtils.PlaySound("aprclaunch", Main.rand.NextFloat(1.7f, 1.8f), position, 8, 0.5f);
            return false;
        }
    }
}
