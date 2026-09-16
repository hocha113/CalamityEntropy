using CalamityEntropy.Content.Projectiles;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class RuneMachineGun : ModItem
    {
        public new string LocalizationCategory => "Items.Weapons.Ranged";

        public override bool RangedPrefix() {
            return true;
        }
        public override void SetDefaults() {
            Item.width = 100;
            Item.height = 70;
            Item.damage = 16;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 7;
            Item.useAnimation = 7;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 9f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 8;

            Item.ArmorPenetration = 64;
        }
        public override Vector2? HoldoutOffset() {
            return new Vector2(-10, 0);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            CEUtils.PlaySound("gunshot_small" + Main.rand.Next(1, 4).ToString(), Main.rand.NextFloat(0.7f, 1.3f), position, 10, 0.4f);
            CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3).ToString(), Main.rand.NextFloat(0.7f, 1.3f), position, 10, 0.4f);
            Projectile.NewProjectile(source, position + new Vector2(8, velocity.X > 0 ? -7 : 7).RotatedBy(velocity.ToRotation()), velocity, type, damage, knockback, player.whoAmI);
            Projectile.NewProjectile(source, position + new Vector2(0, Main.rand.NextFloat(-10, 10)).RotatedBy(velocity.ToRotation()), velocity, ModContent.ProjectileType<RuneTorrentRanger>(), (int)(damage * 0.75f), 4, player.whoAmI);
            return false;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player) {
            return Main.rand.NextBool(6);
        }

        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 18f + new Vector2(0, 14);
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-10, 0);

            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (-0.02f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);

            if (animProgress > 0.5f) {
                float backArmRotation = rotation + 0.52f * player.direction;

                Player.CompositeArmStretchAmount stretch = ((float)Math.Sin(MathHelper.Pi * (animProgress - 0.5f) / 0.36f)).ToStretchAmount();
                player.SetCompositeArmBack(true, stretch, backArmRotation);
            }

        }
        #endregion
    }
}
