using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator
{
    public class Typhoon : ModItem, IDonatorItem
    {
        public string DonatorName => "a3a4";

        public override bool RangedPrefix()
        {
            return true;
        }
        public override void SetDefaults()
        {
            Item.width = 136;
            Item.height = 52;
            Item.damage = 13;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 1;
            Item.useAnimation = 1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 16f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 5;
            Item.ArmorPenetration = 10000;
        }
        public int mode = 0;
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-10, -16);
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (mode == 0)
            {
                Item.useAnimation = Item.useTime = 1;
            }
            else
            {
                Item.useAnimation = Item.useTime = 60;
            }
            if (player.altFunctionUse == 2)
            {
                mode = 1 - mode;
                CEUtils.PlaySound("Typ1", 1, player.Center);
                Item.useAnimation = Item.useTime = 30;
            }
            return base.CanUseItem(player);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            velocity = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.Zero) * Item.shootSpeed;
            position += velocity.normalize() * 80;
            if (player.altFunctionUse == 2)
                return false;
            CEUtils.SetShake(position, mode == 0 ? 0.2f : 8);
            for (int i = 0; i < (mode == 0 ? 2 : 36); i++)
            {
                Vector2 top = position;
                Vector2 sparkVelocity2 = velocity.normalize().RotateRandom(0.3f) * Main.rand.NextFloat(16f, 36f);
                int sparkLifetime2 = Main.rand.Next(6, 10);
                float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                //PRT_LineCal CalamityPorts,Configure签名对齐Calamity原构造
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            if (mode == 1)
            {
                for (int i = 0; i < 60; i++)
                {
                    Vector2 vel = velocity.RotateRandom(0.3f) * Main.rand.NextFloat(0.4f, 1.2f);
                    Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI);
                }
                CEUtils.PlaySound("typ2", 1, player.Center, volume: 0.9f);
                return false;
            }
            CEUtils.PlaySound("typ3", 1, player.Center, 1, 0.6f);
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0);

            return false;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return Main.rand.NextBool(20) || mode == 1;
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
                rotation += (mode == 1 ? -0.34f : 0) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
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
            if (CECal.CalChainReady(CEID.Item_P90))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_P90)
                .AddIngredient(ItemID.FragmentVortex, 6)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.ChainGun)
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
