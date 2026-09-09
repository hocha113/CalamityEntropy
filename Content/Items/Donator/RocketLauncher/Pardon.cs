using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher
{
    public class Pardon : ModItem
    {
        public static int MaxStick => 5;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxStick);
        public static int ExplodeRadius => 120;
        public override void SetDefaults()
        {
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<CharredMissileProj>(), BaseMissileProj.AmmoType, singleShotTime: 60, shotVelocity: 40f, hasAutoReuse: true);
            Item.width = 90;
            Item.height = 42;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 48;
            Item.knockBack = 4f;
            var snd = CEUtils.GetSound("cannon");
            snd.PitchRange = (0.6f, 0.8f);
            Item.UseSound = snd;
            Item.value = Item.buyPrice(gold: 18);
            Item.rare = ItemRarityID.Pink;
            Item.Entropy().tooltipStyle = 8;
            Item.Entropy().strokeColor = Color.DarkGreen;
            Item.Entropy().NameColor = Color.Yellow;
            Item.Entropy().NameLightColor = Color.Yellow * 0.4f;
            Item.ArmorPenetration = 25;
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
                rotation += (-0.3f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-30f, -8f);
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        #endregion

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_LifeAlloy, CEID.Item_InfectedArmorPlating))
            {
                CreateRecipe()
                .AddIngredient<Struggle>()
                .AddIngredient<OsseousRemains>(20)
                .AddIngredient(CEID.Item_LifeAlloy, 5)
                .AddIngredient(CEID.Item_InfectedArmorPlating, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Struggle>()
                .AddIngredient<OsseousRemains>(20)
                .AddIngredient(ItemID.FragmentVortex, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
        public static void OnKillAction(Projectile Projectile)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                int type = ModContent.ProjectileType<PlagueMissile>();
                for (float i = 0; i < 360; i += 120)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, i.ToRadians().ToRotationVector2() * 18, type, Projectile.damage / 2, 4, Projectile.owner);
                }
            }
            for (int i = 0; i < 128; i++)
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.GreenBlood);
                if (d < 6000)
                {
                    Main.dust[d].velocity = CEUtils.randomPointInCircle(15);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].scale = 1.9f;
                }
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CEUtils.PlaySound("shotgun", Main.rand.NextFloat(1.6f, 2), position, 6, 0.6f);
            position += (new Vector2(54, -16) * new Vector2(1, player.direction)).RotatedBy(velocity.ToRotation());
            {
                int p = Projectile.NewProjectile(source, position, velocity * 0.3f, type, damage, knockback, player.whoAmI, MaxStick, ExplodeRadius);
                p.ToProj().Entropy().applyBuffs.Add(ModContent.BuffType<Crumbling>());
                if (p.ToProj().ModProjectile is BaseMissileProj bmp)
                {
                    bmp.winding += 0.6f;
                    bmp.Homing += 3.8f;
                    bmp.NoGrav = true;
                    bmp.MinVel = Item.shootSpeed;
                }
                p.ToProj().Entropy().OnKillActions += OnKillAction;
                CEUtils.SyncProj(p);
            }
            {
                int p = Projectile.NewProjectile(source, position, velocity * 0.1f, type, damage, knockback, player.whoAmI, MaxStick, ExplodeRadius);
                p.ToProj().Entropy().applyBuffs.Add(ModContent.BuffType<Crumbling>());
                if (p.ToProj().ModProjectile is BaseMissileProj bmp)
                {
                    bmp.winding += 0.6f;
                    bmp.Homing += 3.8f;
                    bmp.NoGrav = true;
                    bmp.MinVel = Item.shootSpeed;
                }
                p.ToProj().Entropy().OnKillActions += OnKillAction;
                CEUtils.SyncProj(p);
            }
            return false;
        }
    }
    public class PlagueMissile : ModProjectile
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
                Projectile.velocity = Projectile.velocity.RotatedBy(0.02f);
            }
            if (Projectile.Distance(Main.LocalPlayer.Center) < 3200)
            {
                for (int i = 0; i < 18; i++)
                {
                    //PRT_Smoke timeleftmax/vd字段spawn后直赋
                    var smoke = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * i / 18f, CEUtils.randomPointInCircle(0.5f), new Color(0, 120, 0), Main.rand.NextFloat(0.02f, 0.03f));
                    smoke.timeleftmax = 9;
                    smoke.Lifetime = 9;
                    smoke.Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 9);
                    var smoke2 = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * i / 18f, CEUtils.randomPointInCircle(0.5f), new Color(120, 255, 120), Main.rand.NextFloat(0.01f, 0.018f));
                    smoke2.timeleftmax = 9;
                    smoke2.Lifetime = 9;
                    smoke2.Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 9);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.G = 255;
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
}
