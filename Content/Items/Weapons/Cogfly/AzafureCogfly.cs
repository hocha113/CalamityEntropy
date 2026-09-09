using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles.CalamityPorts;
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

namespace CalamityEntropy.Content.Items.Weapons.Cogfly
{
    public class AzafureCogfly : ModItem, IAzafureEnhancable
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void SetDefaults()
        {
            Item.damage = 28;
            Item.DamageType = DamageClass.Summon;
            Item.width = 42;
            Item.height = 42;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 7;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<AzafureCogflyMinion>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(0, 5);
            Item.autoReuse = true;
            Item.UseSound = CEUtils.GetSound("CogflyUse");
            Item.noMelee = true;
            Item.buffType = ModContent.BuffType<CogflyBuff>();
            Item.rare = ModContent.RarityType<AzafureOrange>();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, position, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;

            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_MysteriousCircuitry))
            {
                CreateRecipe().AddIngredient<HellIndustrialComponents>(4)
                .AddIngredient(CEID.Item_MysteriousCircuitry)
                .AddRecipeGroup(CERecipeGroups.IronBar, 4)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(5)
                .AddIngredient(ItemID.IronBar, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class CogflyBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<AzafureCogflyMinion>();
    }
    public class AzafureCogflyMinion : ModProjectile
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
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
            Projectile.ArmorPenetration = 32;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public int Counter = 0;
        public bool Attacking = false;
        public int AttackTimer = 120;
        public int DCounter = 0;
        public int DashToPlayer = -120;
        public List<Vector2> OldPos = new List<Vector2>();
        public override bool? CanHitNPC(NPC target)
        {
            if (!Attacking)
                return false;
            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MechanicalTrauma>(200);
            Player player = Projectile.GetOwner();
            for (int i = 0; i < 8; i++)
            {
                Vector2 top = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).normalize() * Main.rand.NextFloat(-12, 12);
                Vector2 sparkVelocity2 = Projectile.velocity.RotateRandom(0.4f) * 0.32f * Main.rand.NextFloat(0.3f, 1f);
                int sparkLifetime2 = Main.rand.Next(24, 28);
                float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                //dedServ时PRTLoader.NewParticle给孤儿实例,Configure照常
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 top = Projectile.Center;
                Vector2 sparkVelocity2 = Projectile.velocity.RotateRandom(0.6f) * 0.4f * Main.rand.NextFloat(0.4f, 1f);
                int sparkLifetime2 = Main.rand.Next(24, 28);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                Color sparkColor2 = Color.Lerp(Color.Red, Color.Firebrick, Main.rand.NextFloat(0, 1));
                //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                PRTLoader.NewParticle<PRT_AltSpark>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            CEUtils.PlaySound("ExoHit1", Main.rand.NextFloat(1.4f, 1.7f), target.Center, volume: 0.5f);
            AttackTimer = Projectile.GetOwner().AzafureEnhance() ? 60 : 80;
            Attacking = false;
            Projectile.velocity *= -0.5f;
            DCounter = 25;

        }
        public NPC target;
        public override void AI()
        {
            OldPos.Add(Projectile.Center);
            if (OldPos.Count > 6)
                OldPos.RemoveAt(0);
            Counter++;
            if (Counter == 1)
            {
                Projectile.velocity = new Vector2(0, -16);
            }
            if (Counter < 25)
            {
                Projectile.velocity += new Vector2(0, 0.8f);
                Projectile.rotation += 0.6f;
            }
            if (Counter == 25)
            {
                Projectile.rotation = 0;
                CEUtils.PlaySound("CogflyActive", 1, Projectile.Center);
                if (!Main.dedServ)
                {
                    Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomPointInCircle(12), Mod.Find<ModGore>("CogflyRing").Type);
                }
                Projectile.velocity *= 0;
            }
            Player player = Projectile.GetOwner();
            if (Counter > 25)
            {
                target = Projectile.FindMinionTarget();
                if (!Attacking)
                {
                    AttackTimer--;
                    Vector2 tpos = player.Center + new Vector2(-player.direction * 80, -100);
                    Projectile.pushByOther(0.06f);
                    if (DCounter <= 0)
                        Projectile.velocity += (tpos - Projectile.Center).normalize() * float.Min(0.6f, CEUtils.getDistance(Projectile.Center, tpos) / 400);
                    else
                        Projectile.velocity *= 0.97f;
                    Projectile.velocity *= 0.96f;
                    Projectile.rotation = Projectile.velocity.X * 0.024f;
                    if (AttackTimer < 0 && target != null)
                    {
                        if (Main.rand.NextBool(3))
                        {
                            AttackTimer = 300;
                            Attacking = true;
                            Projectile.ResetLocalNPCHitImmunity();
                            Projectile.velocity = (target.Center + target.velocity * CEUtils.getDistance(Projectile.Center, target.Center) / 30f - Projectile.Center).normalize() * 32;
                        }
                    }
                    DashToPlayer--;
                    if (!Attacking)
                    {
                        if (DashToPlayer < -40 && CEUtils.getDistance(Projectile.Center, player.Center) > 800)
                        {
                            DashToPlayer = 50;
                        }
                    }
                    if (DashToPlayer > 0)
                        Projectile.Center = Vector2.Lerp(Projectile.Center, player.Center, .08f);
                }
                else
                {
                    if (target == null || AttackTimer-- < 0 || !target.active || target.dontTakeDamage)
                    {
                        AttackTimer = player.AzafureEnhance() ? 50 : 80;
                        Attacking = false;
                    }
                    else
                    {
                        Projectile.velocity += (target.Center - Projectile.Center).normalize() * 1.6f;
                        Projectile.velocity *= 0.97f;
                        Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center + target.velocity * CEUtils.getDistance(Projectile.Center, target.Center) / 30f - Projectile.Center).ToRotation(), 0.034f));
                    }
                }
            }
            if (!Attacking)
                OldPos.Clear();
            DCounter--;
            Projectile.MinionCheck<CogflyBuff>();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D wingTex = this.getTextureGlow();
            Texture2D tex2 = this.getTextureAlt();
            Texture2D tex3 = this.getTextureAlt("Spawn");
            Rectangle? frame = CEUtils.GetCutTexRect(tex, 4, ((int)Main.GameUpdateCount / 4) % 4, false);
            Texture2D draw = Counter < 25 ? tex3 : (Attacking ? tex2 : tex);
            if (Counter < 25 || Attacking)
                frame = null;
            Projectile.spriteDirection = -1 * (target != null ? Math.Sign(target.Center.X - Projectile.Center.X) : Math.Sign(Projectile.GetOwner().Center.X - Projectile.Center.X));
            Rectangle frameWing = CEUtils.GetCutTexRect(tex, 4, ((int)Main.GameUpdateCount / 4) % 2, false);
            if (Counter > 25 && !Attacking)
                Main.EntitySpriteDraw(wingTex, Projectile.Center - Main.screenPosition, frameWing, Color.White, Projectile.rotation, new Vector2(wingTex.Width / 2f, wingTex.Height / 8f), Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));

            if (Attacking)
            {
                OldPos.Add(Projectile.Center);
                for (int i = 1; i < OldPos.Count; i++)
                {
                    for (float j = 0; j < 1; j += 0.1f)
                    {
                        Main.EntitySpriteDraw(draw, Vector2.Lerp(OldPos[i - 1], OldPos[i], j) - Main.screenPosition, frame, lightColor * 0.12f * ((float)i / OldPos.Count), Projectile.rotation, new Vector2(draw.Width / 2f, 24), Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
                    }
                }
                OldPos.RemoveAt(OldPos.Count - 1);
            }
            Main.EntitySpriteDraw(draw, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, new Vector2(draw.Width / 2f, 24), Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));

            return false;
        }
    }
}
