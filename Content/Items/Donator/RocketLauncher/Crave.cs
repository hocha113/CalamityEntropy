using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher
{
    public class Crave : ModItem
    {
        public static int MaxStick => 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxStick);
        public static int ExplodeRadius => 120;
        public override void SetDefaults() {
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<CharredMissileProj>(), BaseMissileProj.AmmoType, singleShotTime: 58, shotVelocity: 40f, hasAutoReuse: true);
            Item.width = 90;
            Item.height = 42;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 80;
            Item.knockBack = 3f;
            var snd = CEUtils.GetSound("break");
            snd.PitchRange = (-0.4f, 0.1f);
            Item.UseSound = snd;
            Item.value = Item.buyPrice(gold: 7);
            Item.rare = ItemRarityID.Pink;
            Item.Entropy().tooltipStyle = 8;
            Item.Entropy().strokeColor = new Color(180, 20, 180);
            Item.Entropy().NameColor = Color.DarkBlue;
            Item.Entropy().NameLightColor = Color.Purple * 0.4f;
            Item.ArmorPenetration = 10;
        }

        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 76f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = -HoldoutOffset().Value;
            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (-0.2f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
        public override Vector2? HoldoutOffset() {
            return new Vector2(-30f, -8f);
        }
        public override bool RangedPrefix() {
            return true;
        }
        #endregion

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<FrailMissile>()
                .AddIngredient<OsseousRemains>(20)
                .AddIngredient(ItemID.HallowedBar, 12)
                .AddIngredient(ItemID.SoulofFright, 3)
                .AddIngredient(ItemID.SoulofMight, 3)
                .AddIngredient(ItemID.SoulofSight, 3)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            position += (new Vector2(54, -16) * new Vector2(1, player.direction)).RotatedBy(velocity.ToRotation());
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, MaxStick, ExplodeRadius);
            p.ToProj().Entropy().applyBuffs.Add(ModContent.BuffType<BrimstoneFlames>());
            return false;
        }
    }
}
