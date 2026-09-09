using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator
{
    public class SmartArc : ModItem, IDonatorItem
    {
        public string DonatorName => "Votum Stellarum";

        public override bool RangedPrefix()
        {
            return true;
        }
        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 28;
            Item.damage = 2;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 16f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 18;
        }
        public int mode = 0;
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-4, -6);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            velocity = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.Zero) * Item.shootSpeed;
            position += velocity.normalize() * 40;
            CEUtils.SetShake(position, 0.5f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = velocity.RotateRandom(0.12f) * Main.rand.NextFloat(0.4f, 1.4f);
                int p = Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI);
                p.ToProj().Entropy().SmartArcEffect = true;
                CEUtils.SyncProj(p);
            }
            CEUtils.PlaySound("gunshot_large", 1, player.Center, volume: 0.66f);
            return false;
        }
        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 18f + new Vector2(0, 14);
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-10, 8);

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

            if (animProgress > 0.5f)
            {
                float backArmRotation = rotation + 0.52f * player.direction;

                Player.CompositeArmStretchAmount stretch = ((float)Math.Sin(MathHelper.Pi * (animProgress - 0.5f) / 0.36f)).ToStretchAmount();
                player.SetCompositeArmBack(true, stretch, backArmRotation);
            }

        }
        #endregion

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_SeaPrism, CEID.Item_PrismShard))
            {
                CreateRecipe()
                .AddIngredient(ItemID.IllegalGunParts, 2)
                .AddIngredient(CEID.Item_SeaPrism, 6)
                .AddIngredient(CEID.Item_PrismShard, 4)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.IllegalGunParts)
                .AddIngredient(ItemID.IceBlock, 10)
                .AddIngredient(ItemID.Diamond, 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
