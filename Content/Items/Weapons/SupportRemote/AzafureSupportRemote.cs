using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Buffs.PortsDoT;
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

namespace CalamityEntropy.Content.Items.Weapons.SupportRemote
{
    public class AzafureSupportRemote : ModItem, IAzafureEnhancable
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void SetDefaults()
        {
            Item.damage = 55;
            Item.DamageType = DamageClass.Summon;
            Item.width = 22;
            Item.height = 30;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 2;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<AzafureCombatDrone>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(gold: 5);
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.noMelee = true;
            Item.mana = 10;
            Item.buffType = ModContent.BuffType<CombatDrone>();
            Item.rare = ModContent.RarityType<AzafureOrange>();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;

            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_MysteriousCircuitry))
            {
                CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(10)
                .AddIngredient(CEID.Item_MysteriousCircuitry, 2)
                .AddRecipeGroup(CERecipeGroups.AnyOrichalcumBar, 8)
                .AddIngredient<AzafureDroneRemote>()
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<AzafureDroneRemote>()
                .AddIngredient(ItemID.HallowedBar, 10)
                .AddIngredient<HellIndustrialComponents>(6)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    public class CombatDrone : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<AzafureCombatDrone>();
    }
    public class AzafureCombatDrone : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
        }
        public override bool? CanDamage() => false;
        public int FireCooldown = 60;
        public int dir = 1;
        public override void AI()
        {
            if (Projectile.ai[1] > 0)
                Projectile.ai[1] -= 0.05f;
            if (Projectile.ai[1] < 0)
                Projectile.ai[1] = 0;
            Player player = Projectile.GetOwner();
            Projectile.MinionCheck<CombatDrone>();
            if (Projectile.Distance(player.Center) > 3200)
            {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(128);
            }
            int id = 0;
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.owner == Projectile.owner && proj.type == Projectile.type)
                {
                    if (proj.whoAmI == Projectile.whoAmI)
                        break;
                    id++;
                }
            }
            dir = Math.Sign(player.Center.X - Projectile.Center.X);
            NPC target = Projectile.FindMinionTarget(1600);
            if (target != null)
                dir = -Math.Sign(Projectile.Center.X - target.Center.X);
            if (FireCooldown > 0)
            {
                if (target != null || FireCooldown > 10)
                {
                    FireCooldown--;
                }
                if (target == null)
                {
                    if (FireCooldown < 10)
                        FireCooldown = 10;
                }
                Vector2 targetPos = target == null ? (player.Center + new Vector2(-(140 + id * 70) * player.direction, -60)) : (target.Center + new Vector2((target.width + (120 + id * 56) * (id % 2 == 0 ? 1 : -1)), -90 - target.height));
                var nearby = target == null ? new List<NPC>() : CEUtils.FindSomeNearEnemies(target.Center, 2, 1600, (npC) => npC.ToNPC().velocity.Length() > 0 || npC.ToNPC().realLife < 0);
                if (nearby.Count >= 2)
                {
                    targetPos = nearby[0].Center + (nearby[0].Center - nearby[1].Center).SafeNormalize(Vector2.UnitX) * (320 + 78 * id);
                }
                if (target == null)
                {
                    Projectile.velocity *= 0.94f;
                    Projectile.velocity += (targetPos - Projectile.Center) * 0.004f;
                }
                else
                {
                    Projectile.velocity *= Utils.Remap(target.velocity.Length(), 0, 12, 0.88f, 0.6f);
                    Projectile.velocity += (targetPos - Projectile.Center) * Utils.Remap(target.velocity.Length(), 0, 12, 0.005f, 0.06f);
                }
                Projectile.pushByOther(0.1f);
                if (target != null)
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 0.16f, false);
                else
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, dir > 0 ? 0 : MathHelper.Pi, 0.1f, false);
            }
            else
            {
                if (target == null)
                {
                    FireCooldown = 10;
                    return;
                }
                FireCooldown = (Projectile.GetOwner().AzafureEnhance() ? 60 : 48) + Main.rand.Next(20);
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 0.16f, false);
                Projectile.velocity *= 0;
                Projectile.ai[1] = 1;
                Vector2 shootVel = (target.Center + target.velocity * 4 - Projectile.Center).normalize() * 16;
                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, shootVel, ModContent.ProjectileType<CombatDroneBullet>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                //dedServ时PRTLoader.NewParticle给孤儿实例,Configure照常
                if (!Main.dedServ)
                    Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.rotation.ToRotationVector2().RotatedBy(dir * -2.5f) * 8 + CEUtils.randomPointInCircle(3), Mod.Find<ModGore>("ASRShell").Type);
                for (int i = 0; i < 16; i++)
                {
                    //dedServ时PRTLoader.NewParticle给孤儿实例,Configure照常
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, shootVel.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.1f, 1.6f), Color.OrangeRed, 0.04f).Configure(false, 20, new Vector2(0.16f, 1));
                }
                Projectile.velocity -= shootVel * 0.8f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D tex2 = this.getTextureAlt("2");
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, (dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically));
            Main.EntitySpriteDraw(tex2, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation - dir * CEUtils.Parabola(Projectile.ai[1], 0.8f), tex2.Size() * 0.5f, Projectile.scale, (dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically));

            return false;
        }
    }
    public class CombatDroneBullet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, 5);
            Projectile.width = Projectile.height = 16;
            Projectile.MaxUpdates = 6;
            Projectile.timeLeft = 300;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            float scale = 0.6f * dmgMult * dmgMult;
            //CustomPulse贴图路径Configure现传,Texture属性填白图应付框架
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 0.95f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.95f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.04f, 12);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 16);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.06f, 20);

            target.AddBuff<MechanicalTrauma>(260);
            target.AddBuff<ArmorCrunch>(300);
            CEUtils.PlaySound("ystn_hit", 0.36f, Projectile.Center, 8, 0.5f);
            for (int i = 0; i < 16; i++)
            {
                Vector2 top = target.Center;
                Vector2 sparkVelocity2 = Projectile.rotation.ToRotationVector2().RotateRandom(0.3f) * Main.rand.NextFloat(16f, 36f);
                int sparkLifetime2 = Main.rand.Next(16, 26);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            dmgMult *= 0.9f;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= dmgMult;
            modifiers.ArmorPenetration += 16;
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
            for (int i = 0; i < 6; i++)
            {
                var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * Main.rand.NextFloat(), Projectile.velocity * 0.6f, Color.OrangeRed, 0.02f);
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
            }
        }
    }
}
