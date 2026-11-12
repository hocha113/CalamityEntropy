using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.ElegyOfSouls
{
    public class ElegyOfSouls : ModItem
    {
        public const int TriggerHits = 3;
        public const int TriggerWindow = 60;           //帧
        public const float DomainDamageMult = 0.2f;
        public const int DomainHitCooldown = 10;       //每秒 6 跳
        public const float DomainRadius = 576f;        //36 格
        public const int DomainLinger = 180;           //最后一次命中后维持的帧数
        public const int DomainFade = 30;
        public const float DomainSlow = 0.2f;
        public const int DomainLifeRegen = 4;          //lifeRegen 2 点合 1 生命/秒
        private const int BaseDamage = 180;
        private const int UseFrames = 20;
        private const float BaseKnockback = 6f;
        private const int SpriteWidth = 96;
        private const int SpriteHeight = 92;
        private const int SellGold = 25;
        private const int EssenceCost = 12;

        public override void SetDefaults() {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = BaseKnockback;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<ElegyOfSoulsSlash>();
            Item.shootSpeed = 1f;
            Item.shootsEveryUse = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, player.MountedCenter, new Vector2(player.direction, 0f), type, damage, knockback, player.whoAmI,
                player.direction * player.gravDir, player.itemAnimationMax, player.GetAdjustedItemScale(Item));
            return false;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
            player.GetModPlayer<ElegyOfSoulsPlayer>().RegisterSwordHit(Item);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<WraithSoulEssence>(EssenceCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class ElegyOfSoulsPlayer : ModPlayer
    {
        private const int RegenRefresh = 2;            //ResetEffects 先减一

        private int regenTimer;
        private readonly uint[] hitTicks = new uint[ElegyOfSouls.TriggerHits];   //新到旧
        private int hitCount;

        public void RefreshRegen() => regenTimer = RegenRefresh;

        public void RegisterSwordHit(Item item) {
            if (Player.whoAmI != Main.myPlayer || item.ModItem is not ElegyOfSouls)
                return;
            int panel = Player.GetWeaponDamage(item);
            ElegyOfSoulsDomain domain = FindDomain();
            if (domain != null) {
                domain.Refresh(panel);
                return;
            }
            uint now = Main.GameUpdateCount;
            for (int i = hitTicks.Length - 1; i > 0; i--)
                hitTicks[i] = hitTicks[i - 1];
            hitTicks[0] = now;
            if (hitCount < hitTicks.Length)
                hitCount++;
            if (hitCount < hitTicks.Length || now - hitTicks[^1] > ElegyOfSouls.TriggerWindow)
                return;
            hitCount = 0;
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.Center, Vector2.Zero, ModContent.ProjectileType<ElegyOfSoulsDomain>(),
                (int)(panel * ElegyOfSouls.DomainDamageMult), 0f, Player.whoAmI);
        }

        private ElegyOfSoulsDomain FindDomain() {
            int type = ModContent.ProjectileType<ElegyOfSoulsDomain>();
            foreach (Projectile proj in Main.ActiveProjectiles) {
                if (proj.type == type && proj.owner == Player.whoAmI && proj.ModProjectile is ElegyOfSoulsDomain domain)
                    return domain;
            }
            return null;
        }

        public override void ResetEffects() {
            if (regenTimer > 0)
                regenTimer--;
        }

        public override void UpdateLifeRegen() {
            if (regenTimer > 0)
                Player.lifeRegen += ElegyOfSouls.DomainLifeRegen;
        }
    }

    public class ElegyOfSoulsSlash : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.NightsEdge;

        private const int SheetFrames = 4;             //末帧是高光
        private const float SlashScale = 1.3f;         //剑图 96×92,比永夜刃长
        private const float ScaleGrowth = 0.2f;        //到 BackConeTo 照原版 972
        private const float ConeLength = 94f;
        private const float ConeHalfAngle = MathHelper.PiOver4;
        private const float ConeLead = MathHelper.TwoPi / 25f;
        private const float BackConeFrom = 0.3f;
        private const float BackConeTo = 0.5f;
        private const float CutReach = 60f;
        private const float OwnerHitCheckDistance = 300f;
        private const float DrawScale = 1.1f;
        private const float InnerScale = 0.975f;
        private const float MistScale = 0.8f;
        private const float PeakAt = 0.6f;
        private const float ShineLead = 0.01f;
        private const float SparkleInset = 4f;
        private const float BodyAlpha = 0.75f;
        private const float MistAlpha = 0.3f;
        private const float EdgeAlpha = 0.7f;
        private const float ShineAlpha = 0.5f;
        private const float SparkleScale = 0.1f;       //StarTexture_White 边长 326
        private const float SparkleGlowScale = 0.16f;
        private const float DustRadius = 84f;
        private const float DustSpread = 0.7f;
        private const int DustOdds = 3;
        private const float DustScale = 1.1f;
        private const float DustDrift = 1.5f;
        private const float DustInherit = 0.2f;
        private const int HitSparks = 6;
        private const float SparkSpread = 0.4f;
        private const float SparkSpeedMin = 3f;
        private const float SparkSpeedMax = 9f;
        private const float SparkScale = 0.035f;
        private const int SparkLife = 12;
        private static readonly Vector2 SparkSquash = new(0.7f, 1f);
        private static readonly Color BodyColor = new(118, 132, 168);
        private static readonly Color MistColor = new(150, 190, 255);
        private static readonly Color EdgeColor = new(214, 232, 255);
        private static readonly Color ShineColor = new(255, 255, 255, 0);
        private static readonly Color SparkleTint = new(150, 200, 255, 0);
        private static readonly Color SparkColor = new(220, 236, 255);

        private float Dir => Projectile.ai[0];                       //朝向乘重力方向
        private float Duration => Math.Max(Projectile.ai[1], 1f);    //挥剑帧数
        private float Progress => Projectile.localAI[0] / Duration;
        private readonly bool[] fxShown = new bool[Main.maxNPCs];

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.ownerHitCheckDistance = OwnerHitCheckDistance;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Projectile.localAI[0]++;
            Player owner = Main.player[Projectile.owner];
            float t = Progress;
            Projectile.rotation = MathHelper.Pi * Dir * t + Projectile.velocity.ToRotation() + Dir * MathHelper.Pi + owner.fullRotation;
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter) - Projectile.velocity;
            Projectile.scale = (1f + t * ScaleGrowth) * Projectile.ai[2] * SlashScale;
            if (!Main.dedServ && Main.rand.NextBool(DustOdds))
                Mist(owner);
            if (Projectile.owner != Main.myPlayer)
                RemoteHitFx();
            if (Projectile.localAI[0] >= Duration)
                Projectile.Kill();
        }

        private void Mist(Player owner) {
            float f = Projectile.rotation + Main.rand.NextFloatDirection() * MathHelper.PiOver2 * DustSpread;
            Vector2 vel = (f + Dir * MathHelper.PiOver2).ToRotationVector2() * DustDrift + owner.velocity * DustInherit;
            Dust dust = Dust.NewDustPerfect(Projectile.Center + f.ToRotationVector2() * DustRadius * Projectile.scale, DustID.DungeonSpirit, vel, 0, default, DustScale);
            dust.noGravity = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float length = ConeLength * Projectile.scale;
            float cone = Projectile.rotation + ConeLead * Dir;
            if (targetHitbox.IntersectsConeSlowMoreAccurate(Projectile.Center, length, cone, ConeHalfAngle))
                return true;
            float back = Utils.Remap(Projectile.localAI[0], Duration * BackConeFrom, Duration * BackConeTo, 1f, 0f);
            return back > 0f && targetHitbox.IntersectsConeSlowMoreAccurate(Projectile.Center, length, cone - MathHelper.PiOver4 * Dir * back, ConeHalfAngle);
        }

        public override void CutTiles() {
            float reach = CutReach * Projectile.scale;
            Vector2 from = Projectile.Center + (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * reach;
            Vector2 to = Projectile.Center + (Projectile.rotation + MathHelper.PiOver4).ToRotationVector2() * reach;
            Utils.PlotTileLine(from, to, reach, DelegateMethods.CutTiles);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Player owner = Main.player[Projectile.owner];
            owner.GetModPlayer<ElegyOfSoulsPlayer>().RegisterSwordHit(owner.HeldItem);
            HitFx(target);
        }

        private void RemoteHitFx() {
            if (Main.dedServ)
                return;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (fxShown[npc.whoAmI] || !npc.CanBeChasedBy() || Colliding(Projectile.Hitbox, npc.Hitbox) != true)
                    continue;
                fxShown[npc.whoAmI] = true;
                HitFx(npc);
            }
        }

        private void HitFx(NPC target) {
            if (Main.dedServ)
                return;
            Vector2 sweep = (Projectile.rotation + Dir * MathHelper.PiOver2).ToRotationVector2();
            for (int i = 0; i < HitSparks; i++) {
                Vector2 vel = sweep.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, SparkColor, SparkScale).Configure(false, SparkLife, SparkSquash);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle body = tex.Frame(1, SheetFrames);
            Rectangle shine = tex.Frame(1, SheetFrames, 0, SheetFrames - 1);
            Vector2 origin = body.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float t = Progress;
            float fade = Utils.Remap(t, 0f, PeakAt, 0f, 1f) * Utils.Remap(t, PeakAt, 1f, 1f, 0f);
            float scale = Projectile.scale * DrawScale;
            SpriteEffects flip = Dir >= 0f ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Main.EntitySpriteDraw(tex, pos, body, BodyColor * (fade * BodyAlpha), Projectile.rotation - Dir * MathHelper.PiOver4 * (1f - t), origin, scale * InnerScale, flip);
            Main.EntitySpriteDraw(tex, pos, body, MistColor * (fade * MistAlpha), Projectile.rotation, origin, scale * MistScale, flip);
            Main.EntitySpriteDraw(tex, pos, body, EdgeColor * (fade * EdgeAlpha), Projectile.rotation, origin, scale * InnerScale, flip);
            Main.EntitySpriteDraw(tex, pos, shine, ShineColor * (fade * ShineAlpha), Projectile.rotation + Dir * ShineLead, origin, scale, flip);
            float tipRot = Projectile.rotation + Utils.Remap(t, 0f, 1f, 0f, MathHelper.PiOver2) * Dir;
            DrawSparkle(pos + tipRot.ToRotationVector2() * (tex.Width * 0.5f - SparkleInset) * scale, fade);
            return false;
        }

        private static void DrawSparkle(Vector2 pos, float fade) {
            Texture2D star = CEExtraAssets.StarTexture_White;
            Vector2 origin = star.Size() * 0.5f;
            Main.EntitySpriteDraw(star, pos, null, SparkleTint * fade, 0f, origin, SparkleGlowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(star, pos, null, ShineColor * fade, 0f, origin, SparkleScale, SpriteEffects.None);
        }
    }

    public class ElegyOfSoulsDomain : ModProjectile
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.DungeonSpirit;

        private const int GhostFrames = 3;             //NPC_288 竖排 3 帧
        private const int GhostCount = 7;
        private const float GhostOrbitMin = 0.45f;     //占领域半径
        private const float GhostOrbitStep = 0.06f;
        private const float GhostSpeedBase = 0.011f;   //弧度/帧
        private const float GhostSpeedStep = 0.003f;
        private const int GhostSpeedVariants = 3;
        private const float GhostBob = 12f;
        private const float GhostBobSpeed = 0.05f;
        private const float GhostPhaseStep = 1.7f;
        private const int GhostFrameTicks = 6;
        private const float GhostAlpha = 0.42f;
        private const float GhostGlowAlpha = 0.25f;
        private const float GhostScale = 1.1f;
        private const float GhostGlowScale = 1.15f;
        private const float FlickerDepth = 0.15f;
        private const float FlickerSpeed = 0.13f;
        private const float RingAlpha = 0.22f;
        private const int FadeInFrames = 24;
        private const int SyncInterval = 15;           //刷新后最快隔多久同步
        private const int SlowRefresh = 1;             //NPC 先于弹幕更新,续 1 帧正好
        private const int DrainEvery = 8;
        private const int DrainMaxTargets = 8;
        private const float DrainSpeed = 3f;
        private const float DrainScale = 1.2f;
        private const float SpawnPitch = 1f;
        private const float SpawnVolume = 0.8f;
        private const float FadePitch = 1.2f;
        private const float FadeVolume = 0.5f;
        private const int SoundInstances = 2;
        private static readonly Color GhostColor = new(170, 210, 255);
        private static readonly Color GhostGlow = new(120, 170, 255, 0);
        private static readonly Color RingColor = new(140, 190, 255);

        private float SinceHit { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }    //距上次剑命中的帧数
        private float Age { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }
        private bool Active => SinceHit < ElegyOfSouls.DomainLinger;

        private bool syncPending;
        private int sinceSync;
        private readonly Dictionary<int, float> lastHitAge = new();   //按 realLife 合并,蠕虫整条共用冷却

        public void Refresh(int panelDamage) {
            SinceHit = 0f;
            Projectile.damage = (int)(panelDamage * ElegyOfSouls.DomainDamageMult);
            syncPending = true;
        }

        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = ElegyOfSouls.DomainHitCooldown;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            Projectile.Center = owner.MountedCenter;
            Projectile.timeLeft = 2;
            if (Age == 0f)
                CEUtils.PlaySound("SoulSpawn0", SpawnPitch, owner.Center, SoundInstances, CEUtils.WeapSound * SpawnVolume);
            Age++;
            SinceHit++;
            if (SinceHit == ElegyOfSouls.DomainLinger)
                CEUtils.PlaySound("soul", FadePitch, owner.Center, SoundInstances, CEUtils.WeapSound * FadeVolume);

            //消散与同步只听拥有者,远端等击杀包
            if (Projectile.owner == Main.myPlayer) {
                sinceSync++;
                if (syncPending && sinceSync >= SyncInterval) {
                    syncPending = false;
                    sinceSync = 0;
                    Projectile.netUpdate = true;
                }
                if (SinceHit >= ElegyOfSouls.DomainLinger + ElegyOfSouls.DomainFade) {
                    Projectile.Kill();
                    return;
                }
            }
            if (!Active)
                return;
            if (Projectile.owner == Main.myPlayer)
                owner.GetModPlayer<ElegyOfSoulsPlayer>().RefreshRegen();
            MarkSlow();
            if (!Main.dedServ && Age % DrainEvery == 0)
                Drain(owner);
        }

        private bool Inside(NPC npc) => Vector2.DistanceSquared(npc.Center, Projectile.Center) <= ElegyOfSouls.DomainRadius * ElegyOfSouls.DomainRadius;

        private void MarkSlow() {
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.friendly && !npc.CountsAsACritter && Inside(npc))
                    npc.GetGlobalNPC<ElegyOfSoulsSlowNPC>().SlowTimer = SlowRefresh;
            }
        }

        private void Drain(Player owner) {
            int shown = 0;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (shown >= DrainMaxTargets)
                    break;
                if (!npc.CanBeChasedBy() || !Inside(npc))
                    continue;
                shown++;
                Vector2 vel = (owner.Center - npc.Center).SafeNormalize(Vector2.Zero) * DrainSpeed;
                Vector2 at = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f);
                Dust dust = Dust.NewDustPerfect(at, DustID.DungeonSpirit, vel, 0, default, DrainScale);
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Active ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => new Circle(Projectile.Center, ElegyOfSouls.DomainRadius).Intersects(targetHitbox);

        private static int LifeKey(NPC npc) => npc.realLife >= 0 ? npc.realLife : npc.whoAmI;

        public override bool? CanHitNPC(NPC target) {
            if (lastHitAge.TryGetValue(LifeKey(target), out float last) && Age - last < ElegyOfSouls.DomainHitCooldown)
                return false;
            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            lastHitAge[LifeKey(target)] = Age;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor) {
            float grow = Utils.GetLerpValue(0f, FadeInFrames, Age, true);
            float alpha = grow * Utils.GetLerpValue(ElegyOfSouls.DomainLinger + ElegyOfSouls.DomainFade, ElegyOfSouls.DomainLinger, SinceHit, true);
            if (alpha <= 0f)
                return false;
            grow = 1f - (1f - grow) * (1f - grow);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D ring = CEExtraAssets.HollowCircleSoftEdge;
            Main.EntitySpriteDraw(ring, center, null, RingColor * (RingAlpha * alpha), 0f, ring.Size() * 0.5f, ElegyOfSouls.DomainRadius * 2f / ring.Width * grow, SpriteEffects.None);

            Texture2D ghost = TextureAssets.Projectile[Type].Value;
            int frameHeight = ghost.Height / GhostFrames;
            for (int i = 0; i < GhostCount; i++) {
                float angle = Age * (GhostSpeedBase + GhostSpeedStep * (i % GhostSpeedVariants)) + MathHelper.TwoPi * i / GhostCount;
                float orbit = ElegyOfSouls.DomainRadius * (GhostOrbitMin + GhostOrbitStep * i) * grow + MathF.Sin(Age * GhostBobSpeed + i * GhostPhaseStep) * GhostBob;
                Vector2 pos = center + angle.ToRotationVector2() * orbit;
                Rectangle frame = new(0, frameHeight * (((int)Age / GhostFrameTicks + i) % GhostFrames), ghost.Width, frameHeight);
                Vector2 origin = frame.Size() * 0.5f;
                float flicker = 1f - FlickerDepth + FlickerDepth * MathF.Sin(Age * FlickerSpeed + i);
                //脸朝切线,旋转即极角
                Main.EntitySpriteDraw(ghost, pos, frame, GhostColor * (GhostAlpha * alpha * flicker), angle, origin, GhostScale, SpriteEffects.None);
                Main.EntitySpriteDraw(ghost, pos, frame, GhostGlow * (GhostGlowAlpha * alpha * flicker), angle, origin, GhostScale * GhostGlowScale, SpriteEffects.None);
            }
            return false;
        }
    }

    public class ElegyOfSoulsSlowNPC : GlobalNPC
    {
        private const float MaxStep = 160f;            //单帧位移超过视为传送,不压

        public override bool InstancePerEntity => true;

        public int SlowTimer;

        public override bool PreAI(NPC npc) {
            if (SlowTimer <= 0)
                return true;
            SlowTimer--;
            //体节跟头走,只压头
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
                return true;
            Vector2 moved = npc.position - npc.oldPosition;
            if (moved.LengthSquared() > MaxStep * MaxStep)
                return true;
            //地面怪只压水平,竖直向回退会压进台阶
            if (!npc.noGravity && !npc.noTileCollide)
                moved.Y = 0f;
            npc.position -= moved * ElegyOfSouls.DomainSlow;
            return true;
        }
    }
}
