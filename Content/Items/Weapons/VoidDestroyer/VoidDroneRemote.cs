using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidDestroyer
{
    /// <summary>
    /// 虚空无人机遥控器:虚空驱逐舰掉落的召唤武器。占 <see cref="BaseSlots"/> 个召唤栏位,召唤一架悬在头顶的投影无人机;
    /// 无人机自己射击,还按固定节拍投影全息陆龟 / 红恶魔 / 白龙扑向目标。
    /// 无人机已在场时再次使用不会多召一架,而是再占 1 个栏位并让它的伤害提高 <see cref="StackBonus"/>
    /// </summary>
    public class VoidDroneRemote : ModItem
    {
        public const int BaseSlots = 3;
        public const float StackBonus = 0.33f;

        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = BaseSlots;
        }

        public override void SetDefaults() {
            Item.damage = 180;
            Item.DamageType = DamageClass.Summon;
            Item.width = 50;
            Item.height = 44;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.knockBack = 3f;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.shoot = ModContent.ProjectileType<ProjectionDrone>();
            Item.shootSpeed = 1f;
            Item.value = Item.buyPrice(platinum: 2, gold: 50);
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.noMelee = true;
            Item.mana = 10;
            Item.buffType = ModContent.BuffType<ProjectionDroneBuff>();
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        /// <summary>首次召唤要 3 个空栏位,叠层只要 1 个;不走原版献祭,栏位不够就用不了</summary>
        public override bool CanUseItem(Player player) {
            bool exists = player.ownedProjectileCounts[Item.shoot] > 0;
            int need = exists ? 1 : BaseSlots;
            return player.maxMinions - player.slotsMinions >= need - 0.001f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 3);
            Projectile existing = FindDrone(player);
            if (existing != null) {
                existing.ai[0] += 1f;
                existing.netUpdate = true;
                CEUtils.PlaySound("vbapear", 1.3f, existing.Center, 4, 0.7f);
                if (!Main.dedServ) {
                    VDVfx.HoloBurst(existing.Center, VDVfx.VoidPink);
                }
                return false;
            }
            Vector2 spawn = player.MountedCenter + new Vector2(0f, -100f);
            int p = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, Item.damage, knockback, player.whoAmI, 0f, Item.damage);
            if (p >= 0 && p < Main.maxProjectiles) {
                Main.projectile[p].originalDamage = Item.damage;
            }
            CEUtils.PlaySound("vmspawn", 1f, spawn, 4, 0.6f);
            return false;
        }

        private Projectile FindDrone(Player player) {
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == player.whoAmI && p.type == Item.shoot) {
                    return p;
                }
            }
            return null;
        }
    }

    public class ProjectionDroneBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<ProjectionDrone>();
    }

    /// <summary>
    /// 投影无人机:悬在玩家头顶,有目标时每 <see cref="ShotInterval"/> 帧射一发,并按节拍投影全息生物。
    /// ai[0] 叠层数(每层 +33% 伤害、+1 栏位),ai[1] 召唤时的物品基础伤害;两者都同步,所有端由它们推导栏位与 originalDamage。
    /// 投影 / 射击的生成只在拥有者端做,计时器不同步也无妨(其他端只负责画)
    /// </summary>
    public class ProjectionDrone : ModProjectile
    {
        public const int ShotInterval = 30;
        public const int TortoiseInterval = 30;
        public const int DevilInterval = 60;
        public const int WyvernInterval = 120;
        public const float TargetRange = 1400f;
        public const float TortoiseMult = 0.55f;
        public const float DevilMult = 0.6f;
        public const float WyvernMult = 0.8f;
        public const int TortoiseCap = 6;
        public const int DevilCap = 5;
        public const int WyvernCap = 3;
        public static readonly Color GlowColor = new Color(220, 120, 255);

        private int shotTimer = ShotInterval - 10;
        private int tortoiseTimer = TortoiseInterval - 5;
        private int devilTimer = DevilInterval - 20;
        private int wyvernTimer = WyvernInterval - 40;
        private float flash;
        private float bob;

        public int Stacks => (int)Projectile.ai[0];
        public int BaseDamage => (int)Projectile.ai[1];
        public float DamageMult => 1f + VoidDroneRemote.StackBonus * Stacks;

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 60;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 120;
            Projectile.minion = true;
            Projectile.minionSlots = VoidDroneRemote.BaseSlots;
            Projectile.aiStyle = -1;
        }

        public override bool? CanHitNPC(NPC target) => false;
        public override bool? CanCutTiles() => false;

        public override void AI() {
            Player player = Projectile.GetOwner();
            Projectile.MinionCheck<ProjectionDroneBuff>();
            Projectile.minionSlots = VoidDroneRemote.BaseSlots + Stacks;
            if (BaseDamage > 0) {
                Projectile.originalDamage = (int)(BaseDamage * DamageMult);
            }

            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (!Main.dedServ) {
                    VDVfx.HoloBurst(Projectile.Center, GlowColor);
                }
            }
            if (Projectile.Distance(player.Center) > 3000f) {
                Projectile.Center = player.Center + new Vector2(0f, -100f);
                Projectile.velocity = Vector2.Zero;
            }
            Projectile.pushByOther(0.3f);

            //悬停:头顶 100 像素,轻微浮动
            bob += 0.05f;
            Vector2 tpos = player.MountedCenter + new Vector2(0f, -100f + MathF.Sin(bob) * 8f);
            Vector2 to = tpos - Projectile.Center;
            float dist = to.Length();
            float pull = Utils.Remap(dist, 0f, 800f, 0f, 9f);
            if (dist > 80f) {
                Projectile.velocity *= 0.96f;
            }
            Projectile.velocity += to.SafeNormalize(Vector2.Zero) * pull;
            Projectile.velocity *= 0.9f;

            NPC target = Projectile.FindMinionTarget((int)TargetRange);
            if (target != null && Vector2.Distance(target.Center, Projectile.Center) > TargetRange) {
                target = null;
            }
            Projectile.spriteDirection = target != null ? Math.Sign(target.Center.X - Projectile.Center.X) : Math.Sign(player.Center.X - Projectile.Center.X);
            if (Projectile.spriteDirection == 0) {
                Projectile.spriteDirection = 1;
            }
            Projectile.rotation = Projectile.velocity.X * 0.02f;
            flash *= 0.85f;
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.35f);

            if (target == null) {
                return;
            }
            RunAttacks(player, target);
        }

        private void RunAttacks(Player player, NPC target) {
            shotTimer++;
            tortoiseTimer++;
            devilTimer++;
            wyvernTimer++;
            bool owner = Projectile.owner == Main.myPlayer;
            IEntitySource src = Projectile.GetSource_FromThis();

            if (shotTimer >= ShotInterval) {
                shotTimer = 0;
                flash = 1f;
                Vector2 muzzle = Projectile.Center + new Vector2(0f, 14f);
                if (owner) {
                    Vector2 vel = (target.Center + target.velocity * 6f - muzzle).SafeNormalize(Vector2.UnitY) * 16f;
                    Projectile.NewProjectile(src, muzzle, vel, ModContent.ProjectileType<ProjectionDroneShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                if (!Main.dedServ) {
                    CEUtils.PlaySound("lasershoot", Main.rand.NextFloat(1.2f, 1.4f), muzzle, 8, 0.3f);
                    VDVfx.Spark(muzzle, Vector2.UnitY * 2f, GlowColor, 0.6f, 1f, 10);
                }
            }

            if (tortoiseTimer >= TortoiseInterval) {
                tortoiseTimer = 0;
                if (owner && player.ownedProjectileCounts[ModContent.ProjectileType<ProjectionTortoise>()] < TortoiseCap) {
                    int dmg = Math.Max(1, (int)(Projectile.damage * TortoiseMult));
                    Projectile.NewProjectile(src, Projectile.Center + new Vector2(0f, 40f), Vector2.Zero, ModContent.ProjectileType<ProjectionTortoise>(), dmg, Projectile.knockBack, Projectile.owner, target.whoAmI);
                }
                flash = Math.Max(flash, 0.6f);
            }

            if (devilTimer >= DevilInterval) {
                devilTimer = 0;
                if (owner && player.ownedProjectileCounts[ModContent.ProjectileType<ProjectionRedDevil>()] < DevilCap) {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DevilMult));
                    float side = Projectile.Center.X < target.Center.X ? -1f : 1f;
                    Projectile.NewProjectile(src, Projectile.Center + new Vector2(0f, 40f), Vector2.Zero, ModContent.ProjectileType<ProjectionRedDevil>(), dmg, Projectile.knockBack, Projectile.owner, target.whoAmI, side);
                }
                flash = Math.Max(flash, 0.6f);
            }

            if (wyvernTimer >= WyvernInterval) {
                wyvernTimer = 0;
                if (owner && player.ownedProjectileCounts[ModContent.ProjectileType<ProjectionWyvern>()] < WyvernCap) {
                    int dmg = Math.Max(1, (int)(Projectile.damage * WyvernMult));
                    Projectile.NewProjectile(src, Projectile.Center + new Vector2(0f, 40f), Vector2.Zero, ModContent.ProjectileType<ProjectionWyvern>(), dmg, Projectile.knockBack, Projectile.owner, target.whoAmI);
                }
                flash = Math.Max(flash, 0.8f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = tex.Size() / 2f;
            SpriteEffects fx = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            //机腹投影灯:常亮,投影瞬间爆闪
            Main.spriteBatch.Draw(glow, drawPos + new Vector2(0f, 16f), null, GlowColor * (0.45f + 0.5f * flash), 0f, glow.Size() / 2f, 0.22f + 0.25f * flash, SpriteEffects.None, 0f);
            //叠层越多,机身晕光越亮
            float stackGlow = MathHelper.Clamp(Stacks * 0.12f, 0f, 0.6f);
            if (stackGlow > 0.01f) {
                Main.spriteBatch.Draw(tex, drawPos, null, GlowColor * stackGlow, Projectile.rotation, origin, Projectile.scale, fx, 0f);
            }
            CEUtils.ReSetToEndShader();

            Main.spriteBatch.Draw(tex, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, fx, 0f);
            if (flash > 0.05f) {
                Main.spriteBatch.UseAdditive();
                Main.spriteBatch.Draw(tex, drawPos, null, Color.White * (0.5f * flash), Projectile.rotation, origin, Projectile.scale, fx, 0f);
                CEUtils.ReSetToEndShader();
            }
            return false;
        }
    }

    /// <summary>无人机子弹:紫色短光矢,轻微追踪</summary>
    public class ProjectionDroneShot : ModProjectile
    {
        public static readonly Color GlowColor = new Color(230, 140, 255);

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = -1;
        }

        public override void AI() {
            CEUtils.HomeInOnNPC(Projectile, true, 700f, 16f, 30f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.25f);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 4; i++) {
                VDVfx.Spark(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 5f), GlowColor, Main.rand.NextFloat(0.3f, 0.6f), 1f, 14, gravity: true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Texture2D streak = CEUtils.getExtraTex("StreakSolid");
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.UseAdditive();
            float len = 34f;
            Vector2 mid = drawPos - dir * len * 0.5f;
            Main.spriteBatch.Draw(streak, mid, null, GlowColor * 0.8f, Projectile.rotation, streak.Size() / 2f, new Vector2(len / streak.Width, 8f / streak.Height), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(streak, mid, null, Color.White * 0.8f, Projectile.rotation, streak.Size() / 2f, new Vector2(len / streak.Width, 3f / streak.Height), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, drawPos, null, GlowColor * 0.6f, 0f, glow.Size() / 2f, 0.07f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }

    /// <summary>全息生物公共部分:ai[0] 为目标敌怪索引;目标失效即淡出。渐显 / 渐隐与全息配色</summary>
    public abstract class ProjectionHoloBase : ModProjectile
    {
        public const int FadeFrames = 14;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public abstract Color HoloColor { get; }

        public int Age => (int)Projectile.localAI[0];

        protected NPC Target {
            get {
                int idx = (int)Projectile.ai[0];
                if (idx < 0 || idx >= Main.maxNPCs) {
                    return null;
                }
                NPC npc = Main.npc[idx];
                return npc.active && !npc.friendly && npc.life > 0 ? npc : null;
            }
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        /// <summary>目标没了就把剩余寿命压到渐隐长度</summary>
        protected void FadeIfLost() {
            if (Target == null && Projectile.timeLeft > FadeFrames) {
                Projectile.timeLeft = FadeFrames;
            }
        }

        protected float HoloOpacity(float peak = 0.9f) {
            float fadeIn = MathHelper.Clamp(Age / 10f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeFrames, 0f, 1f);
            return peak * Math.Min(fadeIn, fadeOut);
        }

        public override void OnKill(int timeLeft) {
            if (!Main.dedServ) {
                VDVfx.HoloBurst(Projectile.Center, HoloColor);
            }
        }
    }

    /// <summary>
    /// 全息巨型陆龟:在无人机下方旋转着显形 <see cref="AppearFrames"/> 帧,随即闪现到目标头顶,
    /// 沿抛物线在目标头顶弹跳 <see cref="Bounces"/> 次(每次落到头上算一次命中)后消散
    /// </summary>
    public class ProjectionTortoise : ProjectionHoloBase
    {
        public const int AppearFrames = 14;
        public const int BounceFrames = 26;
        public const int Bounces = 5;
        public const float BounceHeight = 120f;
        public const float HitBand = 16f;

        public override Color HoloColor => VDHologramDraw.JungleGreen;

        private float spin;
        private float height;
        private bool landedThisBounce;

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = AppearFrames + BounceFrames / 2 + BounceFrames * Bounces + FadeFrames;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Projectile.localAI[0]++;
            FadeIfLost();
            NPC target = Target;
            if (target == null) {
                spin += 0.2f;
                Projectile.rotation = spin;
                return;
            }

            if (Age <= AppearFrames) {
                //显形:原地快转、由小变大
                spin += 0.5f;
                Projectile.rotation = spin;
                Projectile.scale = MathHelper.Clamp(Age / (float)AppearFrames, 0.1f, 1f);
                height = BounceHeight;
                if (Age == AppearFrames) {
                    Vector2 from = Projectile.Center;
                    Projectile.Center = HeadPos(target, BounceHeight);
                    if (!Main.dedServ) {
                        VDVfx.HoloBurst(from, HoloColor);
                        VDVfx.HoloBurst(Projectile.Center, HoloColor);
                        CEUtils.PlaySound("vbapear", 1.1f, Projectile.Center, 6, 0.6f);
                    }
                }
                return;
            }

            //弹跳:从最高点起落,f = 0.5 为最高点,f = 0 / 1 落在头上
            int t = Age - AppearFrames + BounceFrames / 2;
            int bounce = t / BounceFrames;
            if (bounce >= Bounces) {
                if (Projectile.timeLeft > FadeFrames) {
                    Projectile.timeLeft = FadeFrames;
                }
                height = 0f;
                Projectile.Center = HeadPos(target, 0f);
                return;
            }
            float f = (t % BounceFrames) / (float)BounceFrames;
            height = BounceHeight * (1f - 4f * (f - 0.5f) * (f - 0.5f));
            Projectile.Center = HeadPos(target, height);
            spin += 0.28f;
            Projectile.rotation = spin;
            Projectile.scale = 1f;

            bool landing = height < HitBand;
            if (landing && !landedThisBounce) {
                landedThisBounce = true;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("shellLand", Main.rand.NextFloat(0.9f, 1.1f), Projectile.Center, 6, 0.5f);
                    VDVfx.SparkBurst(Projectile.Bottom, HoloColor, 8, 2f, 6f, 16, 0.4f, 0.8f);
                }
            }
            else if (!landing) {
                landedThisBounce = false;
            }
            Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.4f);
        }

        /// <summary>目标头顶上方 h 像素处;h = 0 时机体下沿压进目标顶部 10 像素,保证落地那一帧有判定</summary>
        private Vector2 HeadPos(NPC target, float h) => new Vector2(target.Center.X, target.Top.Y - Projectile.height * 0.5f + 10f - h);

        public override bool? CanDamage() => Age > AppearFrames && height < HitBand ? null : false;

        public override bool PreDraw(ref Color lightColor) {
            Main.instance.LoadNPC(NPCID.GiantTortoise);
            Texture2D tex = TextureAssets.Npc[NPCID.GiantTortoise].Value;
            int frames = Math.Max(1, Main.npcFrameCount[NPCID.GiantTortoise]);
            int frameH = tex.Height / frames;
            Rectangle src = new Rectangle(0, 0, tex.Width, frameH);
            Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
            float scale = Projectile.width / (float)Math.Max(tex.Width, frameH) * 1.25f * Projectile.scale;
            float opacity = HoloOpacity();
            if (opacity <= 0.01f) {
                return false;
            }
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, HoloColor * (0.35f * opacity), 0f, glow.Size() / 2f, 0.4f * Projectile.scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            VDHologramDraw.Begin();
            if (height > HitBand) {
                for (int i = 1; i <= 3; i++) {
                    VDHologramDraw.DrawPart(tex, drawPos + new Vector2(0f, -i * 6f), src, HoloColor, opacity * (0.25f - i * 0.06f), Projectile.rotation - i * 0.15f, origin, scale, SpriteEffects.None);
                }
            }
            VDHologramDraw.DrawPart(tex, drawPos, src, HoloColor, opacity, Projectile.rotation, origin, scale, SpriteEffects.None);
            VDHologramDraw.End();
            return false;
        }
    }

    /// <summary>
    /// 全息红恶魔:快速飞到目标身侧(ai[1] 为侧向),每 <see cref="ThrowInterval"/> 帧掷一发极速全息三叉戟,<see cref="Life"/> 帧后消散。本体无判定
    /// </summary>
    public class ProjectionRedDevil : ProjectionHoloBase
    {
        public const int Life = 240;
        public const int ThrowInterval = 45;
        public const int FirstThrow = 18;
        public const float SideOffset = 180f;
        public const float TridentSpeed = 26f;

        public override Color HoloColor => VDHologramDraw.HellRed;

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 40;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Life;
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Projectile.localAI[0]++;
            FadeIfLost();
            NPC target = Target;
            if (target == null) {
                return;
            }
            float side = Projectile.ai[1] == 0f ? 1f : Projectile.ai[1];
            Vector2 desired = target.Center + new Vector2(side * SideOffset, -70f + MathF.Sin(Age * 0.08f) * 12f);
            Projectile.Center = Vector2.Lerp(Projectile.Center, desired, 0.2f);
            Projectile.direction = Projectile.spriteDirection = target.Center.X > Projectile.Center.X ? 1 : -1;
            Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.4f);

            if (Age >= FirstThrow && (Age - FirstThrow) % ThrowInterval == 0 && Projectile.timeLeft > FadeFrames) {
                Vector2 hand = Projectile.Center + new Vector2(Projectile.direction * 18f, -6f);
                if (Projectile.owner == Main.myPlayer) {
                    Vector2 vel = (target.Center + target.velocity * 4f - hand).SafeNormalize(Vector2.UnitX) * TridentSpeed;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), hand, vel, ModContent.ProjectileType<ProjectionTrident>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                if (!Main.dedServ) {
                    CEUtils.PlaySound("throw", Main.rand.NextFloat(0.8f, 1f), hand, 6, 0.5f);
                    VDVfx.SparkBurst(hand, HoloColor, 6, 2f, 5f, 14, 0.4f, 0.8f);
                }
            }
            if (!Main.dedServ && Main.rand.NextBool(4)) {
                VDVfx.Spark(Projectile.Center + CEUtils.randomPointInCircle(24f), CEUtils.randomPointInCircle(1f), HoloColor, Main.rand.NextFloat(0.3f, 0.6f), 0.8f, 14);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.instance.LoadNPC(NPCID.RedDevil);
            Texture2D tex = TextureAssets.Npc[NPCID.RedDevil].Value;
            int frames = Math.Max(1, Main.npcFrameCount[NPCID.RedDevil]);
            int frameH = tex.Height / frames;
            int frame = (int)(Main.GlobalTimeWrappedHourly * 8f) % frames;
            Rectangle src = new Rectangle(0, frame * frameH, tex.Width, frameH);
            Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
            SpriteEffects fx = Projectile.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float opacity = HoloOpacity(0.85f);
            if (opacity <= 0.01f) {
                return false;
            }
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, HoloColor * (0.3f * opacity), 0f, glow.Size() / 2f, 0.45f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            VDHologramDraw.Draw(tex, drawPos, src, HoloColor, opacity, 0f, origin, 0.9f, fx);
            return false;
        }
    }

    /// <summary>投影三叉戟(召唤):红恶魔掷出的极速全息三叉戟</summary>
    public class ProjectionTrident : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 90;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = -1;
        }

        public override void AI() {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, VDHologramDraw.HellRed.ToVector3() * 0.4f);
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                VDVfx.Spark(Projectile.Center, -Projectile.velocity * 0.05f + CEUtils.randomPointInCircle(1f), VDHologramDraw.HellRed, Main.rand.NextFloat(0.3f, 0.6f), 0.9f, 12);
            }
        }

        public override void OnKill(int timeLeft) {
            if (!Main.dedServ) {
                VDVfx.HoloBurst(Projectile.Center, VDHologramDraw.HellRed);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            HoloTridentDraw.Draw(Projectile, VDHologramDraw.HellRed, MathHelper.Clamp(Projectile.localAI[0] / 6f, 0f, 1f) * 0.9f);
            return false;
        }
    }

    /// <summary>
    /// 全息小白龙:<see cref="SegmentCount"/> 节蠕虫,以恒定速度对目标来回冲撞:朝目标转向冲过去,越过目标后直飞一小段再急转回头,
    /// <see cref="Life"/> 帧后消散。任一节碰到敌怪都算命中,每目标每 24 帧一次
    /// </summary>
    public class ProjectionWyvern : ProjectionHoloBase
    {
        public const int Life = 300;
        public const int SegmentCount = 14;
        public const float SegmentLength = 26f;
        public const float BodyScale = 0.9f;
        public const float Speed = 24f;
        public const float ChargeTurn = 0.05f;
        public const float ReturnTurn = 0.15f;
        public const int OvershootFrames = 16;

        public override Color HoloColor => VDHologramDraw.SkyBlue;

        private readonly Vector2[] segments = new Vector2[SegmentCount];
        private bool initialized;
        /// <summary>0 冲撞,1 越过后直飞,2 回头</summary>
        private int mode;
        private int modeTimer;

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Life;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 24;
        }

        public override void AI() {
            Projectile.localAI[0]++;
            FadeIfLost();
            NPC target = Target;
            if (!initialized) {
                initialized = true;
                for (int i = 0; i < SegmentCount; i++) {
                    segments[i] = Projectile.Center;
                }
                Vector2 dir = target != null ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) : Vector2.UnitX;
                Projectile.velocity = dir * Speed;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("CruiserDash", 0.9f, Projectile.Center, 4, 0.6f);
                }
            }

            if (target != null) {
                Steer(target);
            }
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Speed;
            Projectile.rotation = Projectile.velocity.ToRotation();

            //蠕虫跟随:每节钉在前一节后方定长处
            segments[0] = Projectile.Center;
            for (int i = 1; i < SegmentCount; i++) {
                Vector2 diff = segments[i] - segments[i - 1];
                if (diff.LengthSquared() < 0.01f) {
                    diff = -Projectile.velocity;
                }
                segments[i] = segments[i - 1] + diff.SafeNormalize(Vector2.UnitX) * SegmentLength;
            }
            Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.5f);
            if (!Main.dedServ && Main.rand.NextBool(2)) {
                int idx = Main.rand.Next(SegmentCount);
                VDVfx.Spark(segments[idx] + CEUtils.randomPointInCircle(10f), CEUtils.randomPointInCircle(1.2f), HoloColor, Main.rand.NextFloat(0.3f, 0.6f), 0.8f, 14);
            }
        }

        private void Steer(NPC target) {
            Vector2 toTarget = target.Center - Projectile.Center;
            float cur = Projectile.velocity.ToRotation();
            float want = toTarget.ToRotation();
            float diff = MathHelper.WrapAngle(want - cur);
            switch (mode) {
                case 0: {
                    //冲撞:小幅修正朝向,越过目标(目标已在身后且拉开一段)转入直飞
                    Projectile.velocity = (cur + MathHelper.Clamp(diff, -ChargeTurn, ChargeTurn)).ToRotationVector2() * Speed;
                    if (Vector2.Dot(toTarget, Projectile.velocity) < 0f && toTarget.Length() > 70f) {
                        mode = 1;
                        modeTimer = 0;
                    }
                    break;
                }
                case 1: {
                    modeTimer++;
                    if (modeTimer >= OvershootFrames) {
                        mode = 2;
                        if (!Main.dedServ) {
                            CEUtils.PlaySound("CruiserDash", 1.1f, Projectile.Center, 4, 0.5f);
                        }
                    }
                    break;
                }
                default: {
                    //回头:大幅转向,对准后再次冲撞
                    Projectile.velocity = (cur + MathHelper.Clamp(diff, -ReturnTurn, ReturnTurn)).ToRotationVector2() * Speed;
                    if (Math.Abs(diff) < 0.25f) {
                        mode = 0;
                    }
                    break;
                }
            }
        }

        public override bool? CanDamage() => Age > 6 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            int half = (int)(16f * BodyScale);
            for (int i = 0; i < SegmentCount; i++) {
                Rectangle seg = new Rectangle((int)segments[i].X - half, (int)segments[i].Y - half, half * 2, half * 2);
                if (seg.Intersects(targetHitbox)) {
                    return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            if (!initialized) {
                return false;
            }
            Main.instance.LoadNPC(NPCID.WyvernHead);
            Main.instance.LoadNPC(NPCID.WyvernLegs);
            Main.instance.LoadNPC(NPCID.WyvernBody);
            Main.instance.LoadNPC(NPCID.WyvernBody2);
            Main.instance.LoadNPC(NPCID.WyvernBody3);
            Main.instance.LoadNPC(NPCID.WyvernTail);
            float opacity = HoloOpacity(0.85f);
            if (opacity <= 0.01f) {
                return false;
            }

            VDHologramDraw.Begin();
            //尾先画,头压在上面
            for (int i = SegmentCount - 1; i >= 0; i--) {
                int type;
                if (i == 0) {
                    type = NPCID.WyvernHead;
                }
                else if (i == SegmentCount - 1) {
                    type = NPCID.WyvernTail;
                }
                else if (i % 5 == 2) {
                    type = NPCID.WyvernLegs;
                }
                else {
                    type = (i % 3) switch { 0 => NPCID.WyvernBody, 1 => NPCID.WyvernBody2, _ => NPCID.WyvernBody3 };
                }
                Texture2D tex = TextureAssets.Npc[type].Value;
                int frames = Math.Max(1, Main.npcFrameCount[type]);
                int frameH = tex.Height / frames;
                Rectangle src = new Rectangle(0, 0, tex.Width, frameH);
                Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
                Vector2 here = segments[i];
                Vector2 ahead = i == 0 ? here + Projectile.velocity : segments[i - 1];
                float rot = (ahead - here).ToRotation() + MathHelper.PiOver2;
                VDHologramDraw.DrawPart(tex, here - Main.screenPosition, src, HoloColor, opacity, rot, origin, BodyScale, SpriteEffects.None);
            }
            VDHologramDraw.End();
            return false;
        }
    }
}
