using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Items.Weapons.AzafureMissileLauncher;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.PeaceKey
{
    public class AzafurePeaceKey : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 20;
            Item.damage = 1700;
            Item.mana = 0;
            Item.useTime = Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = SoundID.DD2_DefenseTowerSpawn;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<NucLauncher>();
            Item.shootSpeed = 10f;
            Item.DamageType = DamageClass.Summon;
            Item.sentry = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.type == type && proj.owner == player.whoAmI)
                {
                    proj.timeLeft = 1;
                    proj.netUpdate = true;
                }
            }
            player.FindSentryRestingSpot(type, out int XPosition, out int YPosition, out int YOffset);
            YOffset -= 18;
            position = new Vector2((float)XPosition, (float)(YPosition - YOffset));
            int p = Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, 20f);
            if (Main.projectile.IndexInRange(p))
                Main.projectile[p].originalDamage = Item.damage;
            player.UpdateMaxTurrets();
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ScoriaBar))
            {
                CreateRecipe()
                .AddIngredient<AzafureProtectiveCannon>()
                .AddIngredient<AzafureTacticalRadio>()
                .AddIngredient(CEID.Item_ScoriaBar, 6)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<AzafureProtectiveCannon>()
                .AddIngredient<AzafureTacticalRadio>()
                .AddIngredient(ItemID.LunarBar, 6)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    public class NucLauncher : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 32;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.sentry = true;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
        }
        public int ShootDelay = 300;
        public int ShootCount = 0;
        public int ShootCooldown = 0;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (Projectile.frame > 0)
            {
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 2)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                    if (Projectile.frame > 6)
                        Projectile.frame = 0;
                }
            }
            var target_ = Projectile.FindMinionTarget(10000);
            if (target_ != null && target != target_)
            {
                Num = 0;
            }

            target = target_;
            if (target != null)
            {
                Num += Num < 1 ? 0.05f : 0;
                if (ShootCount <= 0)
                {
                    ShootDelay--;
                    if (ShootDelay == 40)
                    {
                        CEUtils.PlaySound("Alarm", 1, Projectile.Center);
                    }
                }
                else
                {
                    ShootCooldown--;
                    if (ShootCooldown <= 0)
                    {
                        Projectile.frame = 1;
                        ShootCooldown = 12;
                        ShootCount--;
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(0, -46), Vector2.UnitY * -78, ModContent.ProjectileType<PeaceJR>(), Projectile.damage, 2, Projectile.owner, target.Center.X, target.Center.Y);
                        }
                    }
                }
                targetPos = target.Center;
                if (ShootDelay <= 0)
                {
                    ShootDelay = 300;
                    ShootCount = player.AzafureEnhance() ? 2 : 1;
                }
            }
            else
            {
                Num -= Num > 0 ? 0.05f : 0;
                ShootCount = 0;
                ShootCooldown = 0;
                if (ShootDelay > 0)
                    ShootDelay--;
            }


        }
        public float Num = 0;

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return false;
        }
        public NPC target = null;
        public Vector2 targetPos = Vector2.Zero;
        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            return true;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
    }
    public class PeaceJR : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.timeLeft = 49;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 100000;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public NPC target = null;
        public Vector2 targetPos => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        public PRT_APRCAlarm particle = null;
        public override void AI()
        {
            if (target != null && !target.active)
                target = null;
            if (Projectile.ai[2] == 0)
            {
                target = CEUtils.FindTarget_HomingProj(Projectile, targetPos, 10000);
                if (Projectile.GetOwner().MinionAttackTargetNPC >= 0)
                {
                    var npc = Projectile.GetOwner().MinionAttackTargetNPC.ToNPC();
                    if (npc.active)
                    {
                        target = npc;
                    }
                }
                CEUtils.PlaySound("shootMissileLarge", 1, Projectile.GetOwner().Center);
                //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                particle = PRTLoader.NewParticle<PRT_APRCAlarm>(new Vector2(Projectile.ai[0], Projectile.ai[1]), Vector2.Zero, Color.White, 1f);
                particle.stick = target;
                particle.Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, Projectile.timeLeft / Projectile.MaxUpdates);

                PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center - Projectile.velocity * 0.5f, Vector2.Zero, Color.OrangeRed, 0.1f).Configure(new Vector2(1, 0.16f), 0, 2, 18);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 center = Projectile.Center - new Vector2(0, Projectile.velocity.Y / 2f);
                    var la = PRTLoader.NewParticle<PRT_LightAlt>(center, Vector2.Zero, Color.OrangeRed, 0.36f);
                    la.ScaleAdd = new Vector2(1f, 0);
                    la.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 18);
                    la = PRTLoader.NewParticle<PRT_LightAlt>(center, Vector2.Zero, Color.White, 0.25f);
                    la.ScaleAdd = new Vector2(1f, 0);
                    la.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 18);
                }
                for (float i = 0.5f; i < 1; i += 0.025f)
                {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * (1 - i), CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.05f, 0.07f));
                    p.timeleftmax = 18;
                    p.Lifetime = 18;
                    p.Configure(0.5f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 18);
                }
            }
            else
            {
                for (float i = 0; i < 1; i += 0.1f)
                {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * i, CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.05f, 0.07f));
                    p.timeleftmax = 18;
                    p.Lifetime = 18;
                    p.Configure(0.5f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 18);
                }
                for (float i = 0; i < 1; i += 0.1f)
                {
                    //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                    var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * i, CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.05f, 0.07f));
                    p.timeleftmax = 18;
                    p.Lifetime = 18;
                    p.Configure(0.5f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 18);
                }
            }
            Projectile.ai[2]++;
            if (Projectile.ai[2] == 25)
            {
                Vector2 pos = targetPos;
                if (target != null)
                {
                    pos = target.Center + target.velocity * 24;
                }
                Projectile.Center = pos + Projectile.velocity * 25;
                Projectile.velocity *= -1;
            }
            if (Projectile.ai[2] > 25)
            {
                if (target != null)
                {
                    if (Projectile.Center.Y < target.Center.Y - Projectile.velocity.Y)
                        Projectile.timeLeft = 2;
                }
                if (target != null)
                    Projectile.velocity.X = (target.Center.X - Projectile.Center.X);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override void OnKill(int timeLeft)
        {
            if (particle != null)
                particle.Kill();
            CEUtils.PlaySound("explosionbig", 0.9f, Projectile.Center, 4, 1.4f);
            CEUtils.PlaySound("blackholeEnd", 1.5f, Projectile.Center, 4, 1.4f);
            CEUtils.PlaySound("pulseBlast", 0.3f, Projectile.Center, 4, 1.4f);
            int lifetime = 20;
            Vector2 center = Projectile.Center;
            //旧对象初始化器拆成字段直赋+Configure,顺序别反
            var ring = PRTLoader.NewParticle<PRT_ERing>(center, Vector2.Zero, new Color(255, 30, 30), 300f);
            ring.LineWidth = 120;
            ring.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            for (int i = 0; i < 2; i++)
            {
                var la = PRTLoader.NewParticle<PRT_LightAlt>(center, Vector2.Zero, Color.OrangeRed, 0.6f);
                la.ScaleAdd = new Vector2(1, 0);
                la.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0, lifetime);
                la = PRTLoader.NewParticle<PRT_LightAlt>(center, Vector2.Zero, Color.White, 0.2f);
                la.ScaleAdd = new Vector2(1, 0);
                la.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0, lifetime);
                la = PRTLoader.NewParticle<PRT_LightAlt>(center, Vector2.Zero, Color.OrangeRed, 0.6f);
                la.ScaleAdd = new Vector2(0.4f, 0);
                la.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, MathHelper.PiOver2, lifetime);
                la = PRTLoader.NewParticle<PRT_LightAlt>(center, Vector2.Zero, Color.White, 0.2f);
                la.ScaleAdd = new Vector2(0.4f, 0);
                la.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, MathHelper.PiOver2, lifetime);
            }
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(center, Vector2.Zero, Color.OrangeRed, 0.1f).Configure(new Vector2(1, 0.16f), 0, 6, 36);
            ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.UnitY * -4, 4), Projectile.Center);
            for (int i = 0; i < 96; i++)
            {
                Vector2 top = Projectile.Center;
                Vector2 velocity = CEUtils.randomPointInCircle(26);
                int sparkLifetime2 = Main.rand.Next(20, 24);
                float sparkScale2 = Main.rand.NextFloat(1.2f, 1.6f);
                var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                PRTLoader.NewParticle<PRT_LineCal>(top, velocity, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            void onhit(NPC target, NPC.HitInfo info, int damage)
            {
                target.AddBuff<MechanicalTrauma>(300);
            }
            if (Main.myPlayer == Projectile.owner)
            {
                var mp = CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, 360, Projectile.DamageType).ModProjectile;
                if (mp is CommonExplotionFriendly cef)
                    cef.onHitAction = onhit;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
}
