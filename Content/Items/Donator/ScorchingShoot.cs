using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using CalamityEntropy.Core.CalamityRef;
namespace CalamityEntropy.Content.Items.Donator
{
    public class ScorchingShoot : ModItem, IDonatorItem
    {
        public string DonatorName => "Romi";
        public static int HitCount = 0;
        public override bool RangedPrefix()
        {
            return true;
        }
        public override void SetDefaults()
        {
            Item.width = 136;
            Item.height = 52;
            Item.damage = 2200;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 46;
            Item.useAnimation = 46;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.UseSound = CEUtils.GetSound("gunshot_large");
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 32f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 4;
            Item.ArmorPenetration = 200;
        }
        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit += 38;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-10, -12);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            HitCount++;
            var p = Projectile.NewProjectile(source, position, velocity, type, damage * (HitCount > 6 ? 4 : 1), knockback, player.whoAmI).ToProj();
            CEUtils.SetShake(position - velocity, 6);
            p.GetGlobalProjectile<ScorchingGProj>().Active = true;
            for (int i = 0; i < (HitCount > 6 ? 36 : 16); i++)
            {
                Vector2 top = position + velocity * 2;
                Vector2 sparkVelocity2 = velocity.normalize().RotateRandom(0.1f) * Main.rand.NextFloat(16f, 36f);
                int sparkLifetime2 = Main.rand.Next(6, 10);
                float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                //PRT_LineCal CalamityPorts,Configure签名对齐Calamity原构造
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            if (HitCount > 6)
            {
                HitCount = 0;
                p.GetGlobalProjectile<ScorchingGProj>().Enhanced = true;
                Projectile.NewProjectile(source, position, velocity.normalize().RotatedBy(player.direction > 0 ? -1.9f : 1.9f) * 18, ModContent.ProjectileType<ScorchingShell>(), 0, 0, player.whoAmI);
                CEUtils.PlaySound("sniper_rifle", 1, position);
            }
            CEUtils.SyncProj(p.whoAmI);
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
            Vector2 itemOrigin = new Vector2(-10, 18);

            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (-0.22f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
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
            if (CECal.CalChainReady(CEID.Item_AngelicShotgun, CEID.Item_Auralis, CEID.Item_AuricBar, CEID.Item_DarksunFragment, CEID.Tile_CosmicAnvil))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_AngelicShotgun)
                .AddIngredient(CEID.Item_Auralis)
                .AddIngredient(CEID.Item_AuricBar, 5)
                .AddIngredient(CEID.Item_DarksunFragment, 20)
                .AddTile(CEID.Tile_CosmicAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<BuriedSun>()
                .AddIngredient<ChaoticPiece>(5)
                .AddIngredient(ItemID.HallowedBar, 25)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    public class ScorchingGProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool Active = false;
        public bool Enhanced = false;
        public PRT_TrailGunShot trail = null;
        public bool flag = true;
        public List<int> NPCHited = new List<int>();
        public override void AI(Projectile projectile)
        {
            if (Active)
            {
                if (flag)
                {
                    flag = false;
                    if (projectile.penetrate > 0)
                        projectile.penetrate = Enhanced ? -1 : projectile.penetrate + 6;
                    if (Enhanced)
                        projectile.MaxUpdates *= 2;

                }
            }
            if (Active)
            {
                if (trail == null)
                {
                    trail = PRTLoader.NewParticle<PRT_TrailGunShot>(projectile.Center, Vector2.Zero, Color.White, 1f);
                    trail.Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, -1);
                }
                else
                {
                    trail.Position = projectile.Center + projectile.velocity * projectile.MaxUpdates;
                    trail.Lifetime = 60;
                }
                if (Enhanced)
                {
                    Vector2 position = projectile.Center + CEUtils.randomPointInCircle(6);
                    Vector2 velocity = projectile.velocity * 0.2f;
                    Vector2 top = position;
                    Vector2 sparkVelocity2 = velocity.normalize().RotateRandom(0.02f) * Main.rand.NextFloat(16f, 36f);
                    int sparkLifetime2 = Main.rand.Next(6, 10);
                    float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                    var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                    PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                }
            }
        }
        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (!Active)
                return;
            if (trail != null)
            {
                trail.Lifetime = 0;
            }
        }
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Active)
            {
                return;
            }
            if (Enhanced)
            {
                target.AddBuff<MarkedforDeath>(600);
                target.AddBuff(ModContent.BuffType<Dragonfire>(), 400);
            }
            NPCHited.Add(target.whoAmI);
        }

        public override bool? CanHitNPC(Projectile projectile, NPC target)
        {
            if (!Active)
                return null;
            if (NPCHited.Contains(target.whoAmI))
                return false;
            return null;
        }
        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(Active);
            binaryWriter.Write(Enhanced);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            Active = binaryReader.ReadBoolean();
            Enhanced = binaryReader.ReadBoolean();
        }
    }
}
