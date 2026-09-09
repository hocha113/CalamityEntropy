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
    public class Struggle : ModItem
    {
        public static int MaxStick => 4;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxStick);
        public static int ExplodeRadius => 120;
        public override void SetDefaults()
        {
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<CharredMissileProj>(), BaseMissileProj.AmmoType, singleShotTime: 50, shotVelocity: 40f, hasAutoReuse: true);
            Item.width = 90;
            Item.height = 42;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 88;
            Item.knockBack = 4f;
            Item.UseSound = CEUtils.GetSound("cannon", 1.3f);
            Item.value = Item.buyPrice(gold: 16);
            Item.rare = ItemRarityID.Pink;
            Item.Entropy().tooltipStyle = 8;
            Item.Entropy().strokeColor = new Color(180, 20, 180);
            Item.Entropy().NameColor = Color.DarkBlue;
            Item.Entropy().NameLightColor = Color.Purple * 0.4f;
            Item.ArmorPenetration = 20;
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
            return new Vector2(-27f, -4f);
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        #endregion

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_CoreofCalamity, CEID.Item_UnholyCore))
            {
                CreateRecipe()
                .AddIngredient<Crave>()
                .AddIngredient<OsseousRemains>(20)
                .AddIngredient(CEID.Item_CoreofCalamity, 3)
                .AddIngredient(CEID.Item_UnholyCore, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Crave>()
                .AddIngredient<OsseousRemains>(20)
                .AddIngredient(ItemID.ShroomiteBar, 20)
                .AddIngredient(ItemID.HellstoneBar, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public static void struggleProjKilled(Projectile proj)
        {
            if (Main.myPlayer == proj.owner)
            {
                int type = ModContent.ProjectileType<BrimHomingBullet>();
                for (float i = 0; i < 360; i += 60)
                {
                    Projectile.NewProjectile(proj.GetSource_FromAI(), proj.Center, i.ToRadians().ToRotationVector2() * 16, type, proj.damage / 6, 4, proj.owner);
                }
            }
            //PRT_ShineParticle FollowOwner等字段spawn后赋,Configure只管Additive
            PRTLoader.NewParticle<PRT_ShineParticle>(proj.Center, Vector2.Zero, Color.Red, proj.scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            PRTLoader.NewParticle<PRT_ShineParticle>(proj.Center, Vector2.Zero, Color.White, proj.scale * 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            position += (new Vector2(54, -16) * new Vector2(1, player.direction)).RotatedBy(velocity.ToRotation());
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, MaxStick, ExplodeRadius);
            p.ToProj().Entropy().applyBuffs.Add(ModContent.BuffType<BrimstoneFlames>());
            p.ToProj().Entropy().OnKillActions += struggleProjKilled;
            return false;
        }
    }
    public class BrimHomingBullet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return Projectile.localAI[0] > 15 ? null : false;
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
            if (Projectile.localAI[0]++ > 16)
            {
                Projectile.HomingToNPCNearby(4, 0.93f);
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.1f);
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame > 3)
                    Projectile.frame = 0;
            }
            if (Projectile.Distance(Main.LocalPlayer.Center) < 3200)
            {
                for (int i = 0; i < 18; i++)
                {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * i / 18f, CEUtils.randomPointInCircle(0.5f), Color.Red, Main.rand.NextFloat(0.008f, 0.01f));
                    p.timeleftmax = 9;
                    p.Lifetime = 9;
                    p.Configure(0.7f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 9);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = 255;
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
}
