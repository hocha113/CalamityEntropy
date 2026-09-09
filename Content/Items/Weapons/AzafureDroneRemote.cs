using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureDroneRemote : ModItem, IAzafureEnhancable
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.IntegrateHotkey(CEKeybinds.CommandMinions);
        }
        public override void SetDefaults()
        {
            Item.damage = 20;
            Item.DamageType = DamageClass.Summon;
            Item.width = 22;
            Item.height = 30;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<AzafureDroneMinion>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(0, 5);
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.noMelee = true;
            Item.mana = 10;
            Item.buffType = ModContent.BuffType<AzafureDrone>();
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
            if (CECal.CalChainReady(CEID.Item_MysteriousCircuitry, CEID.Item_AerialiteBar, CEID.Item_EnergyCore))
            {
                CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient(CEID.Item_MysteriousCircuitry)
                .AddIngredient(CEID.Item_AerialiteBar, 8)
                .AddIngredient(CEID.Item_EnergyCore)
                .AddIngredient(ItemID.Dynamite, 1)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(5)
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddIngredient(ItemID.Dynamite, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class AzafureDrone : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<AzafureDroneMinion>();
    }
    public class AzafureDroneMinion : ModProjectile
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
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
            Projectile.ArmorPenetration = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 80;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        //待发射弹体贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/AzafureDroneBullet")]
        internal static Asset<Texture2D> DroneBulletTex;
        public int ReadyToFire = -1;
        public int FireCooldown = 60;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ReadyToFire);
            writer.Write(FireCooldown);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ReadyToFire = reader.ReadInt32();
            FireCooldown = reader.ReadInt32();
        }
        public int dir = 1;
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            Projectile.MinionCheck<AzafureDrone>();
            if (Projectile.localAI[0] == 0)
            {
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.TwoPi * 0.05f)
                {
                    var dust = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.RedTorch);
                    dust.position = Projectile.Center;
                    dust.velocity = i.ToRotationVector2() * 9;
                    dust.scale = 1.4f;
                    dust.noGravity = true;
                }
            }
            if (Projectile.localAI[0]++ < 3)
            {
                Projectile.timeLeft++;
                return;
            }
            if (Projectile.Distance(player.Center) > 4200)
            {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(128);
            }
            if (Projectile.velocity.X > 0.1f)
            {
                dir = 1;
            }
            if (Projectile.velocity.X < 0.1f)
            {
                dir = -1;
            }
            NPC target = Projectile.FindMinionTarget(1500, true);
            if (target != null)
                dir = -Math.Sign(Projectile.Center.X - target.Center.X);
            if (FireCooldown > 0)
            {
                if (target != null || FireCooldown > 1)
                {
                    FireCooldown--;
                }
                if (FireCooldown <= 0)
                {
                    ReadyToFire = 30;
                }
                Vector2 targetPos = target == null ? (player.Center + new Vector2(0, -100)) : (target.Center + new Vector2((Math.Sign(Projectile.Center.X - target.Center.X) * (190 + target.width)), -80 - target.height));
                if (CEUtils.getDistance(Projectile.Center, targetPos) > 20 || player.velocity.Length() > 1.5f)
                {
                    Projectile.velocity += (targetPos - Projectile.Center).normalize() * 0.6f;
                    Projectile.velocity *= target == null ? 0.99f : 0.98f;
                }
                else
                {
                    Projectile.velocity *= 0.94f;
                }
                Projectile.pushByOther(0.2f);
            }
            else
            {
                Projectile.velocity *= 0.94f;
                if (ReadyToFire > 0)
                {
                    ReadyToFire--;
                    if (ReadyToFire <= 0)
                    {
                        ReadyToFire = -1;
                        FireCooldown = player.AzafureEnhance() ? 45 : 60;
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(0, 6), ModContent.ProjectileType<AzafureDroneBullet>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                        }
                        for (int i = 0; i < 12; i++)
                        {
                            var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Firework_Yellow);
                            d.scale = 0.8f;
                            d.velocity = new Vector2(0, Main.rand.NextFloat(4, 8)).RotatedByRandom(0.8f);
                            d.position += d.velocity * 4;
                        }
                        CEUtils.PlaySound("mediumCannon", 1, Projectile.Center);
                        Projectile.velocity += new Vector2(0, -4);
                    }
                }
            }
            Projectile.rotation = Projectile.velocity.X * 0.02f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D bullet = DroneBulletTex.Value;
            float offset = (10 - FireCooldown) / 10f;
            if (offset > 1)
                offset = 1;
            if (FireCooldown > 10)
            {
                offset = 0;
            }
            if (FireCooldown <= 0)
            {
                offset = 1;
            }
            Main.EntitySpriteDraw(bullet, Projectile.Center + new Vector2(0, offset * 24 + 4).RotatedBy(Projectile.rotation) - Main.screenPosition, null, lightColor * offset, Projectile.rotation, bullet.Size() / 2f, Projectile.scale, SpriteEffects.None);
            Rectangle frame = CEUtils.GetCutTexRect(tex, 8, ((int)Main.GameUpdateCount) % 8, false);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, new Vector2(dir > 0 ? 37 : (tex.Width - 37), 21), Projectile.scale, (dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            return false;
        }
    }
    public class AzafureDroneBullet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, true, 1);
            Projectile.width = Projectile.height = 12;
            Projectile.MaxUpdates = 4;
        }
        public override void AI()
        {
            NPC target = Projectile.FindMinionTarget();
            int counter = (int)(Projectile.ai[0]++);
            if (target != null)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (counter < 64)
                {
                    Projectile.velocity *= 0.97f;
                    Projectile.velocity = Projectile.velocity.RotatedBy(CEUtils.getRotateAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), 1.6f));

                }
                else
                {
                    if (counter == 64)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                            PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Projectile.rotation.ToRotationVector2().RotatedByRandom(1) * -8, Color.White, 0.3f).Configure(0.3f, 16, Main.rand.NextFloat(-0.006f, 0.006f), true);
                        }
                    }
                    Projectile.velocity += (target.Center - Projectile.Center).normalize() * 1;
                    Projectile.velocity *= 0.86f;
                    for (int i = 0; i < 2; i++)
                    {
                        var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * 3, CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.02f, 0.04f));
                        p.timeleftmax = 26;
                        p.Lifetime = 26;
                        p.Configure(0.5f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 26);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                    var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * 3, CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.02f, 0.04f));
                    p.timeleftmax = 26;
                    p.Lifetime = 26;
                    p.Configure(0.5f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 26);
                }
                Projectile.ai[0] = 50;
                Projectile.velocity += new Vector2(0, 0.1f);
                Projectile.velocity *= 0.996f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
        }
        public override void OnKill(int timeLeft)
        {
            CEUtils.PlaySound("pulseBlast", 0.8f, Projectile.Center, 6, 0.55f);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(1.5f, 8);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick, 3.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 2.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            if (Projectile.owner == Main.myPlayer)
            {
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage, 160, Projectile.DamageType);
            }
            for (int i = 0; i < 32; i++)
            {
                var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Firework_Yellow);
                d.scale = 0.8f;
                d.velocity = CEUtils.randomPointInCircle(14);
                d.position += d.velocity * 4;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation - MathHelper.PiOver2, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
