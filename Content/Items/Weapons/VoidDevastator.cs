using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class VoidDevastator : ModItem
    {
        public override void SetStaticDefaults()
        {
            AmmoID.Sets.SpecificLauncherAmmoProjectileFallback[Type] = ItemID.RocketLauncher;

        }

        public override void SetDefaults()
        {
            Item.DefaultToRangedWeapon(ProjectileID.RocketI, AmmoID.Rocket, singleShotTime: 20, shotVelocity: 28, hasAutoReuse: true);
            Item.width = 152;
            Item.height = 48;
            Item.damage = 760;
            Item.knockBack = 4f;
            Item.crit = 5;
            Item.UseSound = SoundID.Item11;
            Item.value = Item.buyPrice(gold: 80);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-34f, 2f);
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ArmoredShell, CEID.Item_CoreofCalamity))
            {
                CreateRecipe()
                    .AddIngredient(ItemID.Celeb2, 1)
                    .AddIngredient(ModContent.ItemType<VoidBar>(), 5)
                    .AddIngredient(CEID.Item_ArmoredShell, 8)
                    .AddIngredient(CEID.Item_CoreofCalamity, 1)
                    .AddTile(ModContent.TileType<VoidWellTile>())
                    .Register();
                return;
            }
            CreateRecipe()
                    .AddIngredient(ItemID.Celeb2)
                    .AddIngredient<VoidBar>(5)
                    .AddTile(ModContent.TileType<VoidWellTile>())
                    .Register();
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int shootCount = 1;
            shootCount += Main.rand.Next(0, player.Entropy().WeaponBoost + 1);
            for (int i = 0; i < shootCount; i++)
            {
                int types = Main.rand.Next(0, 5);
                if (types == 0)
                {
                    Projectile p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI).ToProj();
                    Projectile p2 = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI).ToProj();

                    var modProj = p.Entropy();
                    var modProj2 = p2.Entropy();
                    p.hostile = false;
                    p2.hostile = false;
                    modProj.vdtype = types;
                    modProj2.vdtype = types;
                    modProj2.vddirection = -1;

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p.whoAmI);
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p2.whoAmI);
                    }
                }
                else if (types == 1)
                {
                    Projectile p = Projectile.NewProjectile(source, position, velocity.RotatedByRandom(MathHelper.ToRadians(4)), type, damage, knockback, player.whoAmI).ToProj();
                    Projectile p2 = Projectile.NewProjectile(source, position, velocity.RotatedByRandom(MathHelper.ToRadians(4)), type, damage, knockback, player.whoAmI).ToProj();
                    Projectile p3 = Projectile.NewProjectile(source, position, velocity.RotatedByRandom(MathHelper.ToRadians(4)), type, damage, knockback, player.whoAmI).ToProj();

                    var modProj = p.Entropy();
                    var modProj2 = p2.Entropy();
                    var modProj3 = p3.Entropy();
                    p.hostile = false;
                    p2.hostile = false;
                    p3.hostile = false;
                    modProj.vdtype = types;
                    modProj2.vdtype = types;
                    modProj3.vdtype = types;

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p.whoAmI);
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p2.whoAmI);
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p3.whoAmI);
                    }
                }
                else if (types == 2)
                {
                    Projectile p = Projectile.NewProjectile(source, position, velocity, type, (int)(damage * 1.6f), knockback, player.whoAmI).ToProj();

                    var modProj = p.Entropy();
                    p.hostile = false;
                    modProj.vdtype = types;

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p.whoAmI);
                    }
                }
                else if (types == 3)
                {
                    Projectile p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI).ToProj();

                    var modProj = p.Entropy();
                    p.hostile = false;
                    modProj.vdtype = types;

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p.whoAmI);
                    }
                }
                else if (types == 4)
                {
                    Projectile p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI).ToProj();
                    p.hostile = false;
                    var modProj = p.Entropy();

                    modProj.vdtype = types;

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p.whoAmI);
                    }
                }
            }
            return false;
        }
    }
}
