using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher
{
    public class Filthless : ModItem
    {
        public static int MaxStick => 5;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxStick);
        public static int ExplodeRadius => 120;
        public float Charge = 0;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<CharredMissileProj>(), BaseMissileProj.AmmoType, singleShotTime: 32, shotVelocity: 45f, hasAutoReuse: true);
            Item.width = 90;
            Item.height = 42;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 325;
            Item.knockBack = 4f;
            var snd = CEUtils.GetSound("howlingShoot");
            snd.PitchRange = (-0.5f, -0.4f);
            snd.Volume = 0.6f;
            Item.UseSound = snd;
            Item.value = Item.buyPrice(gold: 35);
            Item.rare = ItemRarityID.Pink;
            Item.Entropy().tooltipStyle = 8;
            Item.Entropy().strokeColor = Color.BlueViolet;
            Item.Entropy().NameColor = Color.Violet * 5;
            Item.Entropy().NameLightColor = Color.MediumVioletRed * 0.4f;
            Item.ArmorPenetration = 50;
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
            if (animProgress < 0.5)
                rotation += (-0.2f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-28f, -3f);
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        #endregion

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DivineGeode, CEID.Item_RuinousSoul))
            {
                CreateRecipe()
                .AddIngredient<Zeal>()
                .AddIngredient<OsseousRemains>(20)
                .AddIngredient(CEID.Item_DivineGeode, 20)
                .AddIngredient(CEID.Item_RuinousSoul, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<RustExpeditioner>()
                .AddIngredient(ItemID.LunarBar, 10)
                .AddIngredient(ItemID.SpectreBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<FilthlessShootAlt>();
                int m = 3;
                player.itemTime *= m;
                player.itemTimeMax *= m;
                player.itemAnimation *= m;
                player.itemAnimationMax *= m;
                damage = (int)(damage * 1.2f);
                velocity *= 0.5f;
            }
            position += (new Vector2(54, -16) * new Vector2(1, player.direction)).RotatedBy(velocity.ToRotation());
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, MaxStick, ExplodeRadius);
            List<int> buffs = new List<int>() { ModContent.BuffType<HolyFlames>(), ModContent.BuffType<BrimstoneFlames>(), BuffID.Daybreak, ModContent.BuffType<SoulDisorder>() };
            p.ToProj().Entropy().applyBuffs.Add(buffs[Main.rand.Next(buffs.Count)]);
            if (player.altFunctionUse == 2)
            {
                p.ToProj().Entropy().OnKillActions += OnKillAction;
            }
            return false;
        }
        public static void OnKillAction(Projectile Projectile)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                int type = ModContent.ProjectileType<FilthlessMissile>();
                for (float i = 0; i < 8; i += 1)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(10, 20), type, Projectile.damage / 2, 4, Projectile.owner);
                }
            }
            for (int i = 0; i < 128; i++)
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.PurpleTorch);
                if (d < 6000)
                {
                    Main.dust[d].velocity = CEUtils.randomPointInCircle(16);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].scale = 1.9f;
                }
            }
        }
    }
    public class FilthlessMissile : ModProjectile
    {
        public override bool? CanHitNPC(NPC target)
        {
            return Projectile.localAI[0] > 18 ? null : false;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, 1);
            Projectile.width = Projectile.height = 36;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.timeLeft = 5 * 60;
        }

        public override void AI()
        {
            if (Projectile.localAI[0]++ > 19)
            {
                Projectile.HomingToNPCNearby(4, 0.86f);
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.035f * (Projectile.whoAmI % 2 == 0 ? 1 : -1));
            }
            if (Projectile.Distance(Main.LocalPlayer.Center) < 3200)
            {
                for (int i = 0; i < 18; i++)
                {
                    //PRT_Smoke timeleftmax/vd字段spawn后直赋
                    var smoke = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * i / 18f, CEUtils.randomPointInCircle(0.5f), new Color(100, 10, 100), Main.rand.NextFloat(0.02f, 0.03f));
                    smoke.timeleftmax = 9;
                    smoke.Lifetime = 9;
                    smoke.scaleEnd = 0;
                    smoke.Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 9);
                    var smoke2 = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * i / 18f, CEUtils.randomPointInCircle(0.5f), Color.BlueViolet, Main.rand.NextFloat(0.01f, 0.018f));
                    smoke2.timeleftmax = 9;
                    smoke2.Lifetime = 9;
                    smoke2.scaleEnd = 0;
                    smoke2.Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 9);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = lightColor.B = 255;
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
    public class FilthlessShootAlt : ModProjectile
    {
        public override bool? CanHitNPC(NPC target)
        {
            return null;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, 1);
            Projectile.width = Projectile.height = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.timeLeft = 16 * 60;
            Projectile.scale = 2f;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            if (Projectile.Distance(Main.LocalPlayer.Center) < 3200)
            {
                for (int i = 0; i < 6; i++)
                {
                    var smoke = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * i / 6f, CEUtils.randomPointInCircle(0.5f), Color.Violet, Main.rand.NextFloat(0.04f, 0.06f));
                    smoke.timeleftmax = 16;
                    smoke.Lifetime = 16;
                    smoke.Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
                    var smoke2 = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * i / 6f, CEUtils.randomPointInCircle(0.5f), new Color(255, 120, 255), Main.rand.NextFloat(0.012f, 0.02f));
                    smoke2.timeleftmax = 16;
                    smoke2.Lifetime = 16;
                    smoke2.Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
                }
            }
            if (Projectile.velocity.Length() < 24)
                Projectile.velocity *= 1.06f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            CEUtils.AddLight(Projectile.Center, Color.Violet);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.G = 255;
            lightColor.R = 255;
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            Projectile.GetOwner().Heal(5);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Violet * 1.2f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShineExplosion2", Vector2.One, Main.rand.NextFloat(-10, 10), 0.005f, 0.12f * Projectile.scale, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Violet * 1.2f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShineExplosion1", Vector2.One, Main.rand.NextFloat(-10, 10), 0.005f, 0.12f * Projectile.scale, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Violet * 1.2f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.005f, 0.12f * Projectile.scale, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Violet * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.005f, 0.123f * Projectile.scale, 24);

            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage * 5, 250, Projectile.DamageType);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            CEUtils.PlaySound("explosion", 0.9f, Projectile.Center);
            CEUtils.PlaySound("explosionbig", 1.2f, Projectile.Center, 8, 0.5f);
            for (int i = 0; i < 300; i++)
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.BlueFlare);
                if (d < 6000)
                {
                    Main.dust[d].velocity = CEUtils.randomPointInCircle(36);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].scale = Main.rand.NextFloat(1, 2);
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            List<int> buffs = new List<int>() { ModContent.BuffType<HolyFlames>(), ModContent.BuffType<GodSlayerInferno>(), ModContent.BuffType<Dragonfire>(), ModContent.BuffType<Plague>(), BuffID.Daybreak, ModContent.BuffType<SoulDisorder>() };
            foreach (int i in buffs)
            {
                target.AddBuff(i, 5 * 60);
            }
        }
    }
}
