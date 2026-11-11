using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Sharkbite
{
    /// <summary>
    /// 远古鲨噬法杖,开火、耗魔、连发走原版流程,持杖体只管表现
    /// </summary>
    public class AncientSharkbiteStaff : ModItem
    {
        #region 数值
        public const int BaseDamage = 48;               //面板,静止目标每发约 8 到 8.5 倍面板
        public const int UseTime = 30;                  //帧,每秒 2 发
        public const int ManaCost = 12;                 //每发魔力
        public const float ShootSpeed = 16f;            //像素/帧,导弹巡航速度
        public const float Knockback = 4f;              //任务说明建议值
        public const int SellGold = 8;                  //金币,策划售价
        public const int SpriteWidth = 40;              //像素,物品贴图宽
        public const int SpriteHeight = 66;             //像素,物品贴图高
        public const float MuzzleReach = 54f;           //像素,出膛点离玩家中心,落在杖头里
        public const float CastPitchVariance = 0.16f;   //施法音随机音高幅度
        public const float CastVolume = 0.8f;           //施法音音量
        #endregion

        public override void SetDefaults() {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.shoot = ModContent.ProjectileType<AncientSharkbiteMissile>();
            Item.shootSpeed = ShootSpeed;
            Item.UseSound = SoundID.Item43 with { PitchVariance = CastPitchVariance, Volume = CastVolume };
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
            //杖头出膛,贴脸或隔墙时不前移
            Vector2 muzzle = position + velocity.SafeNormalize(Vector2.UnitX * player.direction) * MuzzleReach;
            if (!Collision.CanHitLine(position, 0, 0, muzzle, 0, 0))
                return;
            float hitPoint = 0f;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.friendly && !npc.dontTakeDamage && Collision.CheckAABBvLineCollision(npc.position, npc.Size, position, muzzle, AncientSharkbiteMissile.HitboxSize, ref hitPoint))
                    return;
            }
            position = muzzle;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int held = ModContent.ProjectileType<AncientSharkbiteStaffHeld>();
            if (player.ownedProjectileCounts[held] < 1)
                Projectile.NewProjectile(source, player.MountedCenter, velocity, held, 0, 0f, player.whoAmI);
            return true;
        }
    }

    /// <summary>
    /// 持杖体,只画法杖与手臂,不带伤害也不带联机状态
    /// 各端按 itemAnimation 回升自己判定开火,寿命归拥有者端
    /// </summary>
    public class AncientSharkbiteStaffHeld : ModProjectile
    {
        #region 数值
        public const int HitboxSize = 16;               //像素,仅供屏幕剔除
        public const int LingerFrames = 10;             //帧,使用动画结束后法杖多留这么久
        public const float GripX = 3f;                  //贴图像素,握点横坐标
        public const float GripY = 55f;                 //贴图像素,握点纵坐标
        public const float AxisAngle = -1.117f;         //弧度,贴图里握点指向杖头,约右上 64 度
        public const float StaffLength = 58f;           //像素,握点到杖头
        public const float KickImpulse = 0.5f;          //开火瞬间注入的后坐速度
        public const float KickVelDecay = 0.5f;         //后坐速度每帧保留
        public const float KickDecay = 0.82f;           //后坐量每帧保留,峰值约 0.55
        public const float KickAngle = 0.5f;            //弧度,单位后坐量对应的杖头上扬,峰值约 0.27
        public const float KickSlide = 9f;              //像素,单位后坐量对应的法杖后缩,峰值约 5
        public const int RefireGuardFrames = 8;         //帧,两次开火表现的最短间隔
        public const int MuzzleDustCount = 9;           //杖口沙尘粒数
        public const float MuzzleDustSpread = 0.55f;    //弧度,沙尘散角
        public const float MuzzleDustSpeedMin = 2f;     //像素/帧
        public const float MuzzleDustSpeedMax = 6.5f;   //像素/帧
        public const float MuzzleDustScaleMin = 0.9f;
        public const float MuzzleDustScaleMax = 1.4f;
        public const int MuzzleSparkCount = 4;          //杖口青色火花数
        public const float MuzzleSparkSpread = 0.3f;    //弧度
        public const float MuzzleSparkSpeedMin = 5f;    //像素/帧
        public const float MuzzleSparkSpeedMax = 11f;   //像素/帧
        public const float MuzzleSparkScaleMin = 0.4f;
        public const float MuzzleSparkScaleMax = 0.7f;
        public const int MuzzleSparkLife = 9;           //帧
        #endregion

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/SandseaDragon/Sharkbite/AncientSharkbiteStaff";

        private int lastItemAnimation;   //上一帧的 itemAnimation,回升即刚开火
        private float kick;              //后坐量
        private float kickVel;           //后坐速度
        private int refireGuard;         //帧,挡掉联机回补 itemAnimation 造成的重复触发

        public override void SetDefaults() {
            Projectile.width = Projectile.height = HitboxSize;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.hide = true;   //只经 heldProj 画在玩家图层里
            Projectile.timeLeft = LingerFrames;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            bool mine = Projectile.owner == Main.myPlayer;
            if (mine && (player.dead || player.CCed || player.HeldItem.type != ModContent.ItemType<AncientSharkbiteStaff>())) {
                Projectile.Kill();
                return;
            }
            Projectile.extraUpdates = 0;
            //寿命归拥有者端,别端等消亡包
            if (!mine || player.ItemAnimationActive)
                Projectile.timeLeft = LingerFrames;

            Vector2 aim = player.mouseWorld() - player.MountedCenter;
            if (aim == Vector2.Zero)
                aim = Vector2.UnitX * player.direction;
            int dir = aim.X >= 0f ? 1 : -1;
            float aimRot = aim.ToRotation();
            float rot = aimRot - dir * KickAngle * kick;
            float armRot = rot - MathHelper.PiOver2;
            player.ChangeDir(dir);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
            player.heldProj = Projectile.whoAmI;

            Projectile.Center = player.GetFrontHandPositionImproved(player.compositeFrontArm) + Vector2.UnitY * player.gfxOffY;
            Projectile.rotation = rot;
            Projectile.velocity = rot.ToRotationVector2();
            Projectile.spriteDirection = dir;

            //开火当帧不后坐,下一帧才起
            bool fired = player.itemAnimation > lastItemAnimation && refireGuard <= 0;
            lastItemAnimation = player.itemAnimation;
            if (refireGuard > 0)
                refireGuard--;
            if (fired) {
                refireGuard = RefireGuardFrames;
                kickVel = KickImpulse;
                if (!Main.dedServ)
                    MuzzleEffects(Projectile.Center + rot.ToRotationVector2() * StaffLength, aimRot.ToRotationVector2());
            }
            kick = (kick + kickVel) * KickDecay;
            kickVel *= KickVelDecay;
        }

        private static void MuzzleEffects(Vector2 tip, Vector2 dir) {
            for (int i = 0; i < MuzzleDustCount; i++) {
                Vector2 v = dir.RotatedByRandom(MuzzleDustSpread) * Main.rand.NextFloat(MuzzleDustSpeedMin, MuzzleDustSpeedMax);
                Dust d = Dust.NewDustPerfect(tip, DustID.Sand, v, 0, default, Main.rand.NextFloat(MuzzleDustScaleMin, MuzzleDustScaleMax));
                d.noGravity = true;
            }
            for (int i = 0; i < MuzzleSparkCount; i++) {
                Vector2 v = dir.RotatedByRandom(MuzzleSparkSpread) * Main.rand.NextFloat(MuzzleSparkSpeedMin, MuzzleSparkSpeedMax);
                PRTLoader.NewParticle<PRT_LineCal>(tip, v, AncientSharkbiteMissile.FinTeal, Main.rand.NextFloat(MuzzleSparkScaleMin, MuzzleSparkScaleMax)).Configure(false, MuzzleSparkLife);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            bool right = Projectile.spriteDirection > 0;
            //朝左时握点与轴向镜像
            Vector2 origin = right ? new Vector2(GripX, GripY) : new Vector2(tex.Width - GripX, GripY);
            float drawRot = right ? Projectile.rotation - AxisAngle : Projectile.rotation - (MathHelper.Pi - AxisAngle);
            Vector2 pos = Projectile.Center - Projectile.rotation.ToRotationVector2() * (KickSlide * kick) - Main.screenPosition;
            Main.EntitySpriteDraw(tex, pos, null, lightColor, drawRot, origin, Projectile.scale, right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            return false;
        }
    }

    /// <summary>
    /// 沙鲨导弹,<c>ai[0]</c> 阶段、<c>ai[1]</c> 锁定槽位 + 1、<c>ai[2]</c> 阶段计时
    /// 命中只在拥有者端结算,阶段切换时打 netUpdate,别端按同一套运动公式自推
    /// </summary>
    public class AncientSharkbiteMissile : ModProjectile
    {
        #region 数值
        public const int HitboxSize = 18;               //像素,贴图 22 每边收 2
        public const int MaxLifetime = 600;             //帧,兜底寿命,各阶段另有自己的时限
        public const float FirstBiteMult = 2f;          //首次命中 200%
        public const float SecondBiteMult = 1f;         //折返命中 100%
        public const int MistCount = 2;                 //消失时放出的毒雾团数
        public const float LaunchSpeedFactor = 0.6f;    //出膛速度占巡航速度的比例
        public const int LaunchRampFrames = 8;          //帧,出膛加速到巡航
        public const int StraightLifetime = 90;         //帧,直飞未命中的时限,约 90 格
        public const int PierceClearance = 20;          //像素,目标判定箱外扩这么多才算穿出
        public const int PierceCoastFrames = 6;         //帧,穿出后滑行减速
        public const float PierceDrag = 0.88f;          //滑行时速度每帧保留
        public const int PierceMaxFrames = 45;          //帧,穿出阶段上限,目标过大时强制折返
        public const float ReturnAccel = 1.2f;          //像素/帧²
        public const float ReturnSpeedMax = 20f;        //像素/帧
        public const float ReturnTurnBase = 0.2f;       //弧度/帧,折返起始转向上限
        public const float ReturnTurnGain = 0.03f;      //弧度/帧²,转向上限逐帧放宽
        public const int ReturnDirectFrames = 20;       //帧,此后直扑,免得绕小目标转圈
        public const int ReturnTimeout = 100;           //帧,折返超时就地爆散
        public const float ReturnBlindArc = 2.6f;       //弧度,偏离航向超过它算正后方
        public const float ReturnNudge = 0.5f;          //弧度,正后方时先偏转的量
        public const float BiteSoundRange = 48f;        //像素,消失时离锁定目标这么近才响咬合音

        public const float FlipDeadzone = 1.5f;         //像素/帧,横向速度低于它不换朝向
        public const float SwimRate = 0.55f;            //弧度/帧,摆尾相位速度
        public const float SwimRateReturn = 0.8f;       //弧度/帧,折返时摆得更快
        public const float SwimAmplitude = 0.13f;       //弧度,摆尾幅度
        public const int TrailLength = 10;              //残影采样数,巡航时拖约 150 像素
        public const int TrailMode = 2;                 //原版拖尾缓存模式,记位置、旋转与朝向
        public const float TrailOpacity = 0.5f;         //最近一格残影的不透明度
        public const float TrailGlow = 0.9f;            //残影加色层强度,贴图偏暗故取高
        public const float TrailEndScale = 0.7f;        //最远一格残影的缩放
        public const float BodyMinGlow = 0.35f;         //本体自发光占比,暗处不至于全黑
        public const float LightStrength = 0.35f;       //飞行时照明强度
        public const int SandFallChance = 3;            //每帧 1/N 概率额外掉一粒受重力的沙
        public const float SandShedDrag = 0.15f;        //尾迹沙尘继承的反向速度比例
        public const float SandShedJitter = 1f;         //像素/帧,沙粒随机速度

        public const int BiteSparkCount = 7;            //首次命中的火花数,顺着弹道喷
        public const float BiteSparkSpread = 0.45f;     //弧度
        public const float BiteSparkSpeedMin = 6f;      //像素/帧
        public const float BiteSparkSpeedMax = 14f;     //像素/帧
        public const int BiteDustCount = 8;             //首次命中的沙尘粒数
        public const float BiteDustSpread = 0.7f;       //弧度
        public const float BiteDustSpeedMin = 2f;       //像素/帧
        public const float BiteDustSpeedMax = 7f;       //像素/帧
        public const float BiteRingScale = 0.3f;        //锁定闪环终值,环贴图边长 156
        public const int BiteRingLife = 10;             //帧
        public const float BitePitchMin = 1.15f;        //首次咬合音高,1 为原调
        public const float BitePitchMax = 1.35f;
        public const float BiteVolume = 0.45f;
        public const int BiteSoundInstances = 4;        //每秒 2 发,每发咬两次

        public const int BurstSparkCount = 14;          //爆散的黄色火花数
        public const float BurstSparkSpeedMin = 4f;     //像素/帧
        public const float BurstSparkSpeedMax = 11f;    //像素/帧
        public const int BurstDustCount = 12;           //爆散的沙尘粒数
        public const float BurstDustSpeed = 5f;         //像素/帧
        public const float BurstRingScale = 0.45f;      //爆散环终值,直径约 70 像素
        public const int BurstRingLife = 14;            //帧
        public const float BurstBitePitchMin = 0.85f;   //第二口比第一口低沉
        public const float BurstBitePitchMax = 1f;
        public const float BurstBiteVolume = 0.55f;
        public const float GasPitchMin = 1.5f;          //毒雾喷出音高
        public const float GasPitchMax = 1.8f;
        public const float GasVolume = 0.4f;
        public const int GasSoundInstances = 3;

        public const float SparkScaleMin = 0.45f;       //火花缩放,命中与爆散共用
        public const float SparkScaleMax = 0.8f;
        public const int SparkLifeMin = 10;             //帧
        public const int SparkLifeMax = 17;             //帧
        public const float DustScaleMin = 0.9f;
        public const float DustScaleMax = 1.4f;
        public const float RingStartScale = 0.04f;      //闪环初始缩放

        public static readonly Color SandYellow = new Color(212, 192, 100);   //族调色板的禁戒沙黄
        public static readonly Color SandOrange = new Color(255, 166, 87);    //毒针贴图的橙
        public static readonly Color FinTeal = new Color(88, 164, 168);       //法杖与导弹尾鳍的青
        private static readonly float LaunchRampStep = MathF.Pow(1f / LaunchSpeedFactor, 1f / LaunchRampFrames);   //出膛加速每帧倍率
        #endregion

        public const int PhaseStraight = 0;   //直飞,可命中任意敌人
        public const int PhasePierce = 1;     //穿出,不造成伤害
        public const int PhaseReturn = 2;     //折返,只咬锁定目标

        private ref float Phase => ref Projectile.ai[0];
        private ref float LockedSlot => ref Projectile.ai[1];   //NPC 下标 + 1,0 为未锁定
        private ref float PhaseTimer => ref Projectile.ai[2];   //帧

        private int lockedType = -1;     //锁定时的 NPC 类型,拥有者端据此识别槽位换人
        private int pierceFrames;        //穿出阶段已过帧数
        private int shownPhase;          //已播过表现的阶段,各端自己记
        private float swim;              //摆尾相位
        private bool hitTile;            //撞物块而消失
        private bool secondBite;         //拥有者端,折返命中而消失
        private Vector2 tileAxis;        //撞物块时毒雾沿物块表面散开的方向

        private NPC LockedTarget {
            get {
                int slot = (int)LockedSlot - 1;
                if (slot < 0 || slot >= Main.maxNPCs)
                    return null;
                NPC npc = Main.npc[slot];
                return npc.active ? npc : null;
            }
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = MaxLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   //一敌一口,折返前单独解除锁定目标
        }

        public override void AI() {
            int phase = (int)Phase;
            Projectile.tileCollide = phase == PhaseStraight;
            NPC target = LockedTarget;
            if (phase != PhaseStraight && Projectile.owner == Main.myPlayer && TargetLost(target)) {
                Projectile.Kill();
                return;
            }
            if (shownPhase < phase) {
                //别端凭同步阶段补播咬合
                if (shownPhase == PhaseStraight && !Main.dedServ)
                    BiteEffects(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX));
                shownPhase = phase;
            }

            switch (phase) {
                case PhaseStraight:
                    FlyStraight();
                    break;
                case PhasePierce:
                    FlyThrough(target);
                    break;
                default:
                    FlyBack(target);
                    break;
            }
            if (!Projectile.active)
                return;

            if (MathF.Abs(Projectile.velocity.X) > FlipDeadzone)
                Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;
            swim += (int)Phase == PhaseReturn ? SwimRateReturn : SwimRate;
            //贴图头朝左,右飞翻转,左飞补半圈
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection > 0 ? 0f : MathHelper.Pi) + MathF.Sin(swim) * SwimAmplitude;

            if (!Main.dedServ)
                FlightEffects();
        }

        private bool TargetLost(NPC target) {
            return target == null || target.type != lockedType || target.life <= 0 || target.dontTakeDamage || target.friendly;
        }

        private void FlyStraight() {
            int t = (int)PhaseTimer;
            if (t == 0)
                Projectile.velocity *= LaunchSpeedFactor;
            else if (t <= LaunchRampFrames)
                Projectile.velocity *= LaunchRampStep;
            PhaseTimer++;
            if (t >= StraightLifetime && Projectile.owner == Main.myPlayer)
                Projectile.Kill();
        }

        private void FlyThrough(NPC target) {
            pierceFrames++;
            if (target != null && pierceFrames < PierceMaxFrames) {
                Rectangle box = target.Hitbox;
                box.Inflate(PierceClearance, PierceClearance);
                if (box.Contains(Projectile.Center.ToPoint())) {
                    PhaseTimer = 0;
                    return;
                }
            }
            Projectile.velocity *= PierceDrag;
            PhaseTimer++;
            if (PhaseTimer >= PierceCoastFrames)
                BeginReturn();
        }

        private void BeginReturn() {
            Phase = PhaseReturn;
            PhaseTimer = 0;
            int slot = (int)LockedSlot - 1;
            if (slot >= 0 && slot < Main.maxNPCs)
                Projectile.localNPCImmunity[slot] = 0;
            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        private void FlyBack(NPC target) {
            if (target != null) {
                float speed = MathF.Min(ReturnSpeedMax, Projectile.velocity.Length() + ReturnAccel);
                float heading = Projectile.velocity.ToRotation();
                float want = (target.Center - Projectile.Center).ToRotation();
                //正后方时按 identity 奇偶选边
                if (PhaseTimer == 0 && MathF.Abs(MathHelper.WrapAngle(want - heading)) > ReturnBlindArc)
                    heading += (Projectile.identity % 2 == 0 ? 1f : -1f) * ReturnNudge;
                float turnCap = PhaseTimer >= ReturnDirectFrames ? MathHelper.Pi : ReturnTurnBase + ReturnTurnGain * PhaseTimer;
                Projectile.velocity = heading.AngleTowards(want, turnCap).ToRotationVector2() * speed;
            }
            PhaseTimer++;
            if (PhaseTimer > ReturnTimeout && Projectile.owner == Main.myPlayer)
                Projectile.Kill();
        }

        private void FlightEffects() {
            Lighting.AddLight(Projectile.Center, SandYellow.ToVector3() * LightStrength);
            //悬停沙尘连成尾迹,弹亡后仍在
            Vector2 at = Projectile.Center - Projectile.velocity * Main.rand.NextFloat();
            Vector2 jitter = Main.rand.NextVector2Circular(SandShedJitter, SandShedJitter);
            Dust wake = Dust.NewDustPerfect(at, DustID.Sand, jitter - Projectile.velocity * SandShedDrag, 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
            wake.noGravity = true;
            if (Main.rand.NextBool(SandFallChance))
                Dust.NewDustPerfect(at, DustID.Sand, jitter, 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
        }

        public override bool? CanHitNPC(NPC target) {
            int phase = (int)Phase;
            if (phase == PhaseStraight)
                return null;
            if (phase == PhaseReturn && target.whoAmI == (int)LockedSlot - 1)
                return null;
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= (int)Phase == PhaseStraight ? FirstBiteMult : SecondBiteMult;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if ((int)Phase != PhaseStraight) {
                secondBite = true;
                Projectile.Kill();
                return;
            }
            Phase = PhasePierce;
            LockedSlot = target.whoAmI + 1;
            PhaseTimer = 0;
            lockedType = target.type;
            pierceFrames = 0;
            Projectile.netUpdate = true;
            shownPhase = PhasePierce;
            if (!Main.dedServ)
                BiteEffects(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX));
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            //消亡归拥有者端,别端等消亡包
            if (Projectile.owner != Main.myPlayer)
                return false;
            //横向受阻即撞墙,雾沿表面散
            hitTile = true;
            tileAxis = Projectile.velocity.X != oldVelocity.X ? Vector2.UnitY : Vector2.UnitX;
            return true;
        }

        public override void OnKill(int timeLeft) {
            if (!Main.dedServ)
                BurstEffects();
            if (Projectile.owner != Main.myPlayer)
                return;
            //雾一团顺速度穿过目标,一团退回
            Vector2 axis = hitTile ? tileAxis : Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int mist = ModContent.ProjectileType<AncientSharkbiteMist>();
            int mistDamage = (int)(Projectile.damage * AncientSharkbiteMist.DamageRatio);
            for (int i = 0; i < MistCount; i++) {
                float side = i % 2 == 0 ? 1f : -1f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, axis * side * AncientSharkbiteMist.DriftSpeed, mist, mistDamage, 0f, Projectile.owner);
            }
        }

        private static void BiteEffects(Vector2 at, Vector2 dir) {
            for (int i = 0; i < BiteSparkCount; i++) {
                Vector2 v = dir.RotatedByRandom(BiteSparkSpread) * Main.rand.NextFloat(BiteSparkSpeedMin, BiteSparkSpeedMax);
                Color c = Color.Lerp(SandYellow, SandOrange, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_LineCal>(at, v, c, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(false, Main.rand.Next(SparkLifeMin, SparkLifeMax));
            }
            for (int i = 0; i < BiteDustCount; i++) {
                Vector2 v = dir.RotatedByRandom(BiteDustSpread) * Main.rand.NextFloat(BiteDustSpeedMin, BiteDustSpeedMax);
                Dust.NewDustPerfect(at, DustID.Sand, v, 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
            }
            PRTLoader.NewParticle<PRT_PulseRing>(at, Vector2.Zero, FinTeal, RingStartScale).Configure(BiteRingScale, BiteRingLife);
            CEUtils.PlaySound("DnBite", Main.rand.NextFloat(BitePitchMin, BitePitchMax), at, BiteSoundInstances, BiteVolume);
        }

        private void BurstEffects() {
            Vector2 at = Projectile.Center;
            for (int i = 0; i < BurstSparkCount; i++) {
                Vector2 v = Main.rand.NextVector2Unit() * Main.rand.NextFloat(BurstSparkSpeedMin, BurstSparkSpeedMax);
                Color c = Color.Lerp(SandYellow, SandOrange, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_LineCal>(at, v, c, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(true, Main.rand.Next(SparkLifeMin, SparkLifeMax));
            }
            for (int i = 0; i < BurstDustCount; i++) {
                Dust d = Dust.NewDustPerfect(at, DustID.Sandnado, Main.rand.NextVector2Circular(BurstDustSpeed, BurstDustSpeed), 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
                d.noGravity = true;
            }
            PRTLoader.NewParticle<PRT_PulseRing>(at, Vector2.Zero, SandYellow, RingStartScale).Configure(BurstRingScale, BurstRingLife);

            //别端无命中钩子,按距离判咬合音
            NPC target = LockedTarget;
            if (secondBite || ((int)Phase == PhaseReturn && target != null && target.Hitbox.Distance(at) < BiteSoundRange))
                CEUtils.PlaySound("DnBite", Main.rand.NextFloat(BurstBitePitchMin, BurstBitePitchMax), at, BiteSoundInstances, BurstBiteVolume);
            CEUtils.PlaySound("SporeGas", Main.rand.NextFloat(GasPitchMin, GasPitchMax), at, GasSoundInstances, GasVolume);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = tex.Size() / 2f;
            Color body = Color.Lerp(lightColor, Color.White, BodyMinGlow);
            Color glow = (int)Phase == PhaseReturn ? FinTeal : SandYellow;
            glow.A = 0;

            //oldPos[0] 即当前位置,从 1 画起
            int length = Projectile.oldPos.Length;
            for (int i = length - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float fade = 1f - i / (float)length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                SpriteEffects flip = Projectile.oldSpriteDirection[i] > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                float scale = Projectile.scale * MathHelper.Lerp(TrailEndScale, 1f, fade);
                Main.EntitySpriteDraw(tex, pos, null, body * (fade * TrailOpacity), Projectile.oldRot[i], origin, scale, flip, 0);
                Main.EntitySpriteDraw(tex, pos, null, glow * (fade * TrailGlow), Projectile.oldRot[i], origin, scale, flip, 0);
            }

            SpriteEffects bodyFlip = Projectile.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, body, Projectile.rotation, origin, Projectile.scale, bodyFlip, 0);
            return false;
        }
    }

    /// <summary>
    /// 毒雾,圆形判定随可见雾团长大,造型以 identity 为种子,各端一致
    /// </summary>
    public class AncientSharkbiteMist : ModProjectile
    {
        #region 数值
        public const float DamageRatio = 0.5f;          //毒雾伤害占导弹伤害的比例
        public const int Lifetime = 150;                //帧,2.5 秒
        public const int HitCooldown = 15;              //帧,局部无敌帧
        public const int VenomTime = 600;               //帧,酸性毒液 10 秒
        public const float DriftSpeed = 1.6f;           //像素/帧,初始飘散速度
        public const float DriftDrag = 0.985f;          //速度每帧保留,全程约飘 96 像素
        public const int ArmFrames = 6;                 //帧,淡入到看得清之前不判定
        public const float HitRadiusStart = 18f;        //像素,初始判定半径,可见雾团约 21
        public const float HitRadiusEnd = 46f;          //像素,末期判定半径,可见雾团约 56

        public const int FrameCount = 3;                //贴图竖排的雾瓣变体数
        public const int OuterLobes = 5;                //外圈雾瓣数
        public const int InnerLobes = 3;                //内圈雾瓣数,另有正中一瓣
        public const float OuterDistStart = 8f;         //像素,外圈离中心的距离
        public const float OuterDistEnd = 31f;
        public const float InnerDistStart = 3f;         //像素,内圈离中心的距离
        public const float InnerDistEnd = 14f;
        public const float LobeScaleStart = 0.8f;       //雾瓣缩放,单瓣贴图约 32 像素宽
        public const float LobeScaleEnd = 1.45f;
        public const float CoreScaleStart = 0.95f;      //正中一瓣的缩放
        public const float CoreScaleEnd = 1.6f;
        public const float InnerAngleOffset = 0.9f;     //弧度,内圈相对外圈的错开角
        public const float SpinOverLife = 0.5f;         //弧度,全程内外圈反向转过的角度
        public const float DistWobble = 0.07f;          //外圈距离的浮动比例
        public const float DistWobbleRate = 0.07f;      //弧度/帧
        public const float DistWobblePhase = 2.1f;      //弧度,相邻雾瓣的相位差
        public const float ScaleWobble = 0.05f;         //外圈缩放的浮动比例
        public const float ScaleWobbleRate = 0.05f;     //弧度/帧
        public const float ScaleWobblePhase = 1.3f;     //弧度,相邻雾瓣的相位差
        public const float GoldenTurn = 0.618034f;      //按 identity 散开起始角用的黄金比
        public const int FadeInFrames = 10;             //帧
        public const int FadeOutFrames = 24;            //帧
        public const float FadeOutPower = 0.7f;         //淡出曲线指数,小于 1 则末尾才急降
        public const float ThinEnd = 0.62f;             //末期整体不透明度比例,越散越淡
        public const float InnerOpacity = 0.5f;         //内圈与正中的单瓣不透明度
        public const float OuterOpacity = 0.42f;        //外圈单瓣不透明度
        public const float HaloOpacity = 0.16f;         //外圈放大一层的不透明度,软化边缘
        public const float HaloScale = 1.3f;            //外圈放大层的倍率
        public const float SelfLight = 0.5f;            //自发光占比,暗处仍看得见
        public const float LightStrength = 0.25f;       //照明强度
        public const int MoteChance = 5;                //每帧 1/N 概率冒一粒毒液微粒
        public const float MoteSpread = 0.7f;           //微粒出生范围占判定半径的比例
        public const float MoteRise = 0.4f;             //像素/帧,微粒上飘速度
        public const int MoteAlpha = 120;               //微粒透明度,0-255
        public const float MoteScaleMin = 0.7f;
        public const float MoteScaleMax = 1.1f;

        private static readonly float[] LobeDistVar = { 1f, 0.84f, 1.12f, 0.92f, 1.06f };     //外圈各瓣的距离倍率,打散五角对称
        private static readonly float[] LobeAngleVar = { 0f, 0.22f, -0.18f, 0.3f, -0.26f };   //弧度,外圈各瓣的角度偏差
        private static readonly Color MistGlow = new Color(151, 112, 172);                    //毒针贴图的淡紫
        #endregion

        private int Timer => Lifetime - Projectile.timeLeft;   //帧,已存在时间

        private float LifeProgress => MathHelper.Clamp(Timer / (float)Lifetime, 0f, 1f);

        private float HitRadius => MathHelper.Lerp(HitRadiusStart, HitRadiusEnd, Grow(LifeProgress));

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = FrameCount;
            ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = (int)(HitRadiusEnd * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;   //改画在 NPC 身后,见 DrawBehind
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
        }

        //快起慢收的扩散曲线,0-1
        private static float Grow(float progress) => 1f - (1f - progress) * (1f - progress);

        //整体不透明度包络,0-1
        private float Envelope() {
            float fadeIn = MathHelper.Clamp(Timer / (float)FadeInFrames, 0f, 1f);
            float fadeOut = MathF.Pow(MathHelper.Clamp(Projectile.timeLeft / (float)FadeOutFrames, 0f, 1f), FadeOutPower);
            return fadeIn * fadeOut * MathHelper.Lerp(1f, ThinEnd, LifeProgress);
        }

        public override void AI() {
            Projectile.velocity *= DriftDrag;
            if (Main.dedServ)
                return;
            Lighting.AddLight(Projectile.Center, MistGlow.ToVector3() * (LightStrength * Envelope()));
            if (Timer >= ArmFrames && Main.rand.NextBool(MoteChance)) {
                float spread = HitRadius * MoteSpread;
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(spread, spread), DustID.Venom, new Vector2(0f, -MoteRise), MoteAlpha, default, Main.rand.NextFloat(MoteScaleMin, MoteScaleMax));
                d.noGravity = true;
            }
        }

        public override bool? CanCutTiles() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Timer < ArmFrames)
                return false;
            float radius = HitRadius;
            return Vector2.DistanceSquared(Projectile.Center, targetHitbox.ClosestPointInRect(Projectile.Center)) < radius * radius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Venom, VenomTime);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            int frameHeight = tex.Height / FrameCount;
            Vector2 origin = new Vector2(tex.Width / 2f, frameHeight / 2f);
            Vector2 center = Projectile.Center - Main.screenPosition;
            float time = Timer;
            float progress = LifeProgress;
            float grow = Grow(progress);
            float envelope = Envelope();
            Color tint = Color.Lerp(lightColor, Color.White, SelfLight);

            int seed = Projectile.identity;
            float seedAngle = seed * GoldenTurn % 1f * MathHelper.TwoPi;
            float spin = (seed % 2 == 0 ? 1f : -1f) * SpinOverLife * progress;
            float outerDist = MathHelper.Lerp(OuterDistStart, OuterDistEnd, grow);
            float innerDist = MathHelper.Lerp(InnerDistStart, InnerDistEnd, grow);
            float lobeScale = MathHelper.Lerp(LobeScaleStart, LobeScaleEnd, grow);

            //由外到里画,越靠里越实
            for (int pass = 0; pass < 2; pass++) {
                Color color = tint * ((pass == 0 ? HaloOpacity : OuterOpacity) * envelope);
                float passScale = pass == 0 ? HaloScale : 1f;
                for (int i = 0; i < OuterLobes; i++) {
                    int variant = (i + seed) % OuterLobes;
                    float angle = seedAngle + i * MathHelper.TwoPi / OuterLobes + spin + LobeAngleVar[variant];
                    float dist = outerDist * (1f + DistWobble * MathF.Sin(time * DistWobbleRate + i * DistWobblePhase)) * LobeDistVar[variant];
                    float scale = lobeScale * (1f + ScaleWobble * MathF.Sin(time * ScaleWobbleRate + i * ScaleWobblePhase));
                    DrawLobe(tex, center + angle.ToRotationVector2() * dist, (i + seed) % FrameCount, frameHeight, origin, color, scale * passScale);
                }
            }
            Color innerColor = tint * (InnerOpacity * envelope);
            for (int i = 0; i < InnerLobes; i++) {
                float angle = seedAngle + InnerAngleOffset + i * MathHelper.TwoPi / InnerLobes - spin;
                DrawLobe(tex, center + angle.ToRotationVector2() * innerDist, (i + seed + 1) % FrameCount, frameHeight, origin, innerColor, lobeScale);
            }
            DrawLobe(tex, center, seed % FrameCount, frameHeight, origin, innerColor, MathHelper.Lerp(CoreScaleStart, CoreScaleEnd, grow));
            return false;
        }

        private static void DrawLobe(Texture2D tex, Vector2 position, int frame, int frameHeight, Vector2 origin, Color color, float scale) {
            Rectangle source = new Rectangle(0, frame * frameHeight, tex.Width, frameHeight);
            Main.EntitySpriteDraw(tex, position, source, color, 0f, origin, scale, SpriteEffects.None, 0);
        }
    }
}
