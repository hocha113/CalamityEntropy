using CalamityEntropy.Content.Rarities;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class EventideSniper : ModItem
    {

        public float SniperDmgMult = 8f;
        public float SniperCritMult = Main.zenithWorld ? 7f : 1.35f;
        public float SniperVelocityMult = 2f;

        public override bool RangedPrefix() {
            return true;
        }
        public override void SetDefaults() {
            Item.width = 234;
            Item.height = 70;
            Item.damage = 680;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 42;
            Item.reuseDelay = 10;
            Item.useAnimation = 38;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.ArmorPenetration = 15;
            Item.noMelee = true;
            Item.knockBack = 8f;
            Item.value = Item.buyPrice(1, 50);
            Item.rare = ModContent.RarityType<NihilityBlue>();
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 6f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 8;
            Item.scale = 0.6f;

        }

        public override Vector2? HoldoutOffset() {
            return new Vector2(-24, 0);
        }


        public override float UseSpeedMultiplier(Player player) {
            return 1f;
        }



        #region Shooting
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int p = Projectile.NewProjectile(source, position + velocity.SafeNormalize(Vector2.Zero) * 96, velocity, type, damage, knockback, player.whoAmI);
            p.ToProj().Entropy().EventideShot = true;
            p.ToProj().usesLocalNPCImmunity = true;
            p.ToProj().localNPCHitCooldown = 60;
            CEUtils.PlaySound("evshot", 1, player.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p);
            }
            return false;
        }
        #endregion

        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 76f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-24, 0);



            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (-0.15f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
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
