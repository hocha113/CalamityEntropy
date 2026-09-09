using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafurePioneerCannon : ModItem, IAzafureEnhancable
    {
        public override void SetStaticDefaults()
        {
            AmmoID.Sets.SpecificLauncherAmmoProjectileFallback[Type] = ItemID.RocketLauncher;
        }
        public override void SetDefaults()
        {
            Item.damage = 1400;
            Item.crit = 10;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 80;
            Item.height = 28;
            Item.useTime = 90;
            Item.useAnimation = 90;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 16;
            Item.value = Item.buyPrice(1, 0);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<ApcHoldout>();
            Item.shootSpeed = 12;
            Item.useAmmo = AmmoID.Rocket;
            Item.channel = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI, type);
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DivineGeode))
            {
                CreateRecipe()
                .AddIngredient<AzafureAntiaircraftGun>()
                .AddIngredient<HellIndustrialComponents>(4)
                .AddIngredient(CEID.Item_DivineGeode, 6)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<AzafureAntiaircraftGun>()
                .AddIngredient<HellIndustrialComponents>(10)
                .AddIngredient(ItemID.LunarBar, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    public class ApcHoldout : ModProjectile
    {
        //飞轮贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/Flywheel")]
        internal static Asset<Texture2D> FlywheelTex;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 100;
            Projectile.penetrate = -1;
        }
        public float counter = 0;
        public float BarrelOffset = 0;
        public bool shoot = true;
        public bool steamSound = true;
        public bool slSound = true;
        public int LoadedAmmo = 0;
        public int MaxAmmo => Main.zenithWorld ? 591 : 5 + (Projectile.GetOwner().AzafureEnhance() ? 3 : 0);
        public int FrameCount = 0;
        public float LoadCounter = 0;
        public float FlywheelRot = 0;
        public float FlywheelAddRot = 0;
        public bool PlayLoadSound = true;
        public float BarrelVelocity = 0;
        public bool Charging = true;
        public bool flag = true;
        public override void AI()
        {
            Projectile.timeLeft = 3;
            Player player = Projectile.GetOwner();
            if (player.channel || Charging)
            {
                if (LoadedAmmo < MaxAmmo)
                {
                    if (PlayLoadSound)
                    {
                        PlayLoadSound = false;
                        CEUtils.PlaySound("Reload", 1, Projectile.Center);
                    }
                    LoadCounter += player.GetTotalAttackSpeed(DamageClass.Ranged) / Projectile.MaxUpdates / 24f;
                    FlywheelAddRot = CEUtils.GetRepeatedCosFromZeroToOne(LoadCounter, 1) * MathHelper.PiOver2;
                    if (LoadCounter > 1)
                    {
                        LoadedAmmo++;
                        LoadCounter -= 1;
                        FlywheelAddRot = 0;
                        FlywheelRot += MathHelper.PiOver2;
                        PlayLoadSound = true;
                        if (LoadedAmmo == MaxAmmo)
                        {
                            Charging = false;
                            LoadCounter = 0;
                        }
                        if (!player.channel)
                        {
                            Charging = false;
                        }
                    }
                }
            }
            else
            {
                bool f = true;
                if (LoadCounter <= 0)
                {
                    FlywheelAddRot = 0;
                    if (!flag)
                    {
                        FlywheelRot -= MathHelper.PiOver2;
                    }
                    flag = false;
                    LoadedAmmo--;
                    if (LoadedAmmo < 0)
                    {
                        if (Main.myPlayer == Projectile.owner)
                        {
                            if (Main.mouseLeft && !Main.LocalPlayer.mouseInterface)
                            {
                                Main.LocalPlayer.channel = true;
                                counter = 0;
                                shoot = true;
                                steamSound = true;
                                slSound = true;
                                LoadedAmmo = 0;
                                LoadCounter = 0;
                                PlayLoadSound = true;
                                Charging = true;
                                flag = true;
                                f = false;
                            }
                            else
                            {
                                Projectile.Kill();
                                player.itemAnimation = player.itemTime = 0;
                                return;
                            }
                        }
                    }
                    if (f)
                    {
                        LoadCounter = 12;
                        BarrelVelocity += 16;
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center - Projectile.velocity.normalize() * 100, Projectile.velocity.RotatedBy(-2 * player.direction).normalize() * 16, ModContent.ProjectileType<AntiaircraftShell>(), 0, 0, Projectile.owner, 1);
                        }
                        for (int i = 0; i < 14; i++)
                        {
                            Color smokeColor = CEUtils.MulticolorLerp(Main.rand.NextFloat(), new Color[3] { Color.White, Color.Gray, Color.LightGray });
                            smokeColor = Color.Lerp(smokeColor, Color.Gray, 0.6f) * 0.65f;
                            //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                            PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center - Projectile.velocity.normalize() * 100, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(1.2f) * -1 * Main.rand.NextFloat(16, 24), smokeColor, 1f).Configure(1f, 40, 0.03f, true, 0.075f);
                        }
                        CEUtils.PlaySound("SteamAAG", 1, Projectile.Center);
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 130, Projectile.rotation.ToRotationVector2() * 42, ModContent.ProjectileType<AzAGShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.ai[0]);
                        }
                        player.velocity -= Projectile.velocity.normalize() * 2 * player.Entropy().GetPressure();
                        PRTLoader.NewParticle<PRT_ImpactParticle>(Projectile.Center + Projectile.velocity.normalize() * 134, Vector2.Zero, Color.LightGoldenrodYellow, 0.12f)
                            .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation);

                        CEUtils.PlaySound("AAGShot", 1, Projectile.Center);
                        CEUtils.SetShake(Projectile.Center - Projectile.rotation.ToRotationVector2() * 16, 4);
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 top = Projectile.Center + Projectile.velocity.normalize() * 130;
                            Vector2 sparkVelocity2 = Projectile.rotation.ToRotationVector2().RotateRandom(0.3f) * Main.rand.NextFloat(16f, 36f);
                            int sparkLifetime2 = Main.rand.Next(6, 10);
                            float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                            var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                            //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                            PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                        }
                    }
                }
                else
                {
                    FrameCount = (int)(LoadCounter / 3f);
                    if (FrameCount > 3)
                        FrameCount = 0;
                    LoadCounter -= player.GetTotalAttackSpeed(DamageClass.Ranged) / Projectile.MaxUpdates;
                    FlywheelAddRot = -CEUtils.Parabola((1 - (LoadCounter / 12f)) * 0.5f, 1) * MathHelper.PiOver2;
                }
            }
            BarrelOffset += BarrelVelocity;
            BarrelVelocity *= 0.88f;
            BarrelOffset *= 0.8f;
            player.itemAnimation = player.itemTime = 3;
            Projectile.Center = player.MountedCenter + player.gfxOffY * Vector2.UnitY - Projectile.velocity.normalize() * 32 + new Vector2(0, -20);
            player.Entropy().MouseWorldListener = true;
            float targetRot = (player.Entropy().MouseWorld - player.MountedCenter).ToRotation();
            Projectile.velocity = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), targetRot, MathHelper.ToRadians(4), true).ToRotationVector2() * player.HeldItem.shootSpeed;
            player.direction = Math.Sign(Projectile.velocity.X);
            Projectile.rotation = Projectile.velocity.ToRotation();
            counter++;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D alt = this.getTextureAlt();

            Main.EntitySpriteDraw(alt, Projectile.Center - Projectile.rotation.ToRotationVector2() * BarrelOffset * Projectile.scale - Main.screenPosition, null, lightColor, Projectile.rotation, alt.Size() / 2f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            Rectangle frame = new Rectangle(0, tex.Height / 4 * FrameCount, tex.Width, tex.Height / 4);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, tex.Size() / new Vector2(2, 8), Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            Texture2D Flywheel = FlywheelTex.Value;
            Main.EntitySpriteDraw(Flywheel, Projectile.Center - Main.screenPosition - new Vector2(58, 0).RotatedBy(Projectile.rotation) * Projectile.scale, null, lightColor, Projectile.rotation - (FlywheelAddRot + FlywheelRot) * (Projectile.velocity.X > 0 ? 1 : -1), Flywheel.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
