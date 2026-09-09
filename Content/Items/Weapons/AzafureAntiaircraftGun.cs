using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureAntiaircraftGun : ModItem, IAzafureEnhancable
    {
        public override void SetStaticDefaults()
        {
            AmmoID.Sets.SpecificLauncherAmmoProjectileFallback[Type] = ItemID.RocketLauncher;
        }
        public override void SetDefaults()
        {
            Item.damage = 1250;
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
            Item.shoot = ModContent.ProjectileType<AzAAGunHoldout>();
            Item.shootSpeed = 12;
            Item.useAmmo = AmmoID.Rocket;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI, type);
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DubiousPlating, CEID.Item_ScoriaBar))
            {
                CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(4)
                .AddIngredient(CEID.Item_DubiousPlating, 10)
                .AddIngredient(CEID.Item_ScoriaBar, 6)
                .AddIngredient(ItemID.HellstoneBar, 18)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.RocketLauncher)
                .AddIngredient<HellIndustrialComponents>(10)
                .AddIngredient(ItemID.ShroomiteBar, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    public class AzAAGunHoldout : ModProjectile
    {
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
        public override void AI()
        {
            Projectile.timeLeft = 3;
            Player player = Projectile.GetOwner();
            int MaxTime = player.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / MaxTime;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (counter == 0 && player.AzafureEnhance())
            {
                player.itemTimeMax = (int)(player.itemTimeMax * 0.7f);
            }
            if (progress < 0.2f)
            {
                BarrelOffset = CEUtils.Parabola(progress / 0.2f, 60);
            }
            else
            {
                BarrelOffset = 0;
            }
            if (progress > 0.6f && slSound)
            {
                slSound = false;
                CEUtils.PlaySound("shellLand", 1, Projectile.Center);
            }
            if (progress > 0.2f)
            {
                if (steamSound)
                {
                    steamSound = false;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center - Projectile.velocity.normalize() * 120, Projectile.velocity.RotatedBy(-2 * player.direction).normalize() * 16, ModContent.ProjectileType<AntiaircraftShell>(), 0, 0, Projectile.owner);
                    }
                    for (int i = 0; i < 14; i++)
                    {
                        Color smokeColor = CEUtils.MulticolorLerp(Main.rand.NextFloat(), new Color[3] { Color.White, Color.Gray, Color.LightGray });
                        smokeColor = Color.Lerp(smokeColor, Color.Gray, 0.6f) * 0.65f;
                        //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                        PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center - Projectile.velocity.normalize() * 120, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(1.2f) * -1 * Main.rand.NextFloat(16, 24), smokeColor, 1f).Configure(1f, 40, 0.03f, true, 0.075f);
                    }
                    CEUtils.PlaySound("SteamAAG", 1, Projectile.Center);
                    CEUtils.PlaySound("AAGLB", 1, Projectile.Center);
                }
            }
            if (shoot && Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 130, Projectile.rotation.ToRotationVector2() * 42, ModContent.ProjectileType<AzAGShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.ai[0]);
            }
            if (shoot)
            {
                player.velocity -= Projectile.velocity.normalize() * 5 * player.Entropy().GetPressure();
                PRTLoader.NewParticle<PRT_ImpactParticle>(Projectile.Center + Projectile.velocity.normalize() * 150, Vector2.Zero, Color.LightGoldenrodYellow, 0.12f)
                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation);

                CEUtils.PlaySound("AAGShot", 1, Projectile.Center);
                CEUtils.SetShake(Projectile.Center - Projectile.rotation.ToRotationVector2() * 16, 36);
                for (int i = 0; i < 16; i++)
                {
                    Vector2 top = Projectile.Center + Projectile.velocity.normalize() * 130;
                    Vector2 sparkVelocity2 = Projectile.rotation.ToRotationVector2().RotateRandom(0.3f) * Main.rand.NextFloat(16f, 36f);
                    int sparkLifetime2 = Main.rand.Next(6, 10);
                    float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                    var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                    PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                }
            }
            shoot = false;
            if (progress < 1)
            {
                player.itemAnimation = player.itemTime = 3;
                Projectile.Center = player.MountedCenter + player.gfxOffY * Vector2.UnitY + Projectile.rotation.ToRotationVector2() * 40 + new Vector2(0, -20);
                player.Entropy().MouseWorldListener = true;
                float targetRot = (player.Entropy().MouseWorld - player.MountedCenter).ToRotation();
                Projectile.velocity = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), targetRot, 4f.ToRadians(), true).ToRotationVector2() * player.HeldItem.shootSpeed;
                player.direction = Math.Sign(Projectile.velocity.X);
            }
            else
            {
                player.itemTime = player.itemAnimation = 0;
                Projectile.Kill();
            }
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
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);

            return false;
        }
    }
    public class AzAGShot : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 4;
            Projectile.light = 0.4f;
            Projectile.scale = 2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MechanicalTrauma>(260);
            target.AddBuff<ArmorCrunch>(300);
            CEUtils.PlaySound("ystn_hit", 0.8f, Projectile.Center);
            for (int i = 0; i < 16; i++)
            {
                Vector2 top = target.Center;
                Vector2 sparkVelocity2 = Projectile.rotation.ToRotationVector2().RotateRandom(0.3f) * Main.rand.NextFloat(16f, 36f);
                int sparkLifetime2 = Main.rand.Next(16, 26);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            var p = new Projectile();
            p.SetDefaults((int)Projectile.ai[0]);
            p.whoAmI = 0;
            dmgMult *= 0.85f;
            //ProjectileLoader.OnHitNPC(p, target, hit, damageDone);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= dmgMult;
        }
        public float dmgMult = 1;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            for (float i = 0; i < 1; i += 0.2f)
            {
                //旧对象初始化器拆成字段直赋+Configure,顺序别反
                var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + CEUtils.randomPointInCircle(4) - Projectile.velocity * i, Projectile.velocity * 0.6f + CEUtils.randomPointInCircle(0.4f), Color.OrangeRed, Main.rand.NextFloat(0.04f, 0.06f));
                p.timeleftmax = 16;
                p.Lifetime = 16;
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
            }
            PRTLoader.NewParticle<PRT_EMediumSmoke>(Projectile.Center, CEUtils.randomPointInCircle(4), Color.LightGoldenrodYellow, Main.rand.NextFloat(0.4f, 0.8f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());
        }
    }
}
