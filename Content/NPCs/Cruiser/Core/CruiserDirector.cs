using CalamityEntropy.Core.CalamityRef;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.Core
{
    /// <summary>
    /// 巡游者的唯一数字出口。状态里不许出现裸数字(纯局部插值系数除外)。
    /// <para>
    /// 本文件是 2026-09-17 状态机迁移时从原 <c>CruiserHead.AI()</c> 与 <c>changeAi()</c>
    /// 逐个搬出来的,<b>数值一律照搬,没有一处调整</b>。注释写的是「这个数在原代码里干什么」,
    /// 不是「这个数为什么该是这样」,原作者没留依据的地方不替他编理由。
    /// </para>
    /// </summary>
    internal static class CruiserDirector
    {
        //==================== 全局 ====================

        /// <summary>
        /// 状态总龄上限。原代码<b>没有</b>任何超时兜底,这是迁移时新加的纯安全网。
        /// 最长的实战状态是 AroundPlayerAndShootVoidStar(350 帧)与 AroundSpawnVoidBomb(340 帧),
        /// 1800 帧在正常对局里到不了;它存在的唯一意义是 BiteAndDash 的「等咬中」窗口
        /// 在原代码里可以无限等下去(追不上玩家就卡死),不让状态机死在那里
        /// </summary>
        public const int StateTimeoutFrames = 1800;

        /// <summary>登场窗帧数:骑着虚空之瓶蓄力,期间无敌、不绘制。与 <c>VoidBottleThrow</c> 的 280 tick 寿命对齐</summary>
        public const int IntroFrames = 280;

        /// <summary>无目标持续这么多帧就离场。<b>原代码从不清零 notargettime</b>,所以它是全场累计值</summary>
        public const int DespawnNoTargetFrames = 190;

        /// <summary>无目标时每帧的上浮加速度(原代码写的是 <c>velocity.Y += -1f</c>)</summary>
        public const float NoTargetRise = -1f;

        /// <summary>转二阶段的血量除数。原代码是<b>整数除法</b> <c>life &lt; lifeMax / 2</c>,照搬</summary>
        public const int Phase2LifeDivisor = 2;

        //==================== SetDefaults 的定义值 ====================

        public const int Width = 96;
        public const int Height = 96;
        /// <summary>转二阶段后本体判定盒放大</summary>
        public const int WidthPhase2 = 156;
        public const int HeightPhase2 = 156;
        /// <summary>转阶段保留的体节(链序 5~8)缩到这个尺寸,链序 8 以上直接移除</summary>
        public const int SegmentSizePhase2 = 26;
        public const int SegmentKeepMaxIndex = 8;
        public const int SegmentShrinkMinIndex = 4;

        public const int BaseDamage = 200;
        /// <summary>专家/大师/死亡各 +4,复仇 +2(加法项,顺序照搬)</summary>
        public const int DamageExpert = 4;
        public const int DamageMaster = 4;
        public const int DamageDeath = 4;
        public const int DamageRevenge = 2;

        public const int Defense = 80;
        /// <summary>二阶段每次选招都把护甲压到 50(原 changeAi 的二阶段分支里)</summary>
        public const int DefensePhase2 = 50;

        public const int LifeMax = 1120000;
        public const int LifeMaxGetGoodBonus = 750000;
        public const float Value = 100000f;

        public const float ScaleMaster = 1.05f;
        public const float ScaleGetGood = 1.3f;
        public const float ScaleZenith = 1.5f;

        /// <summary>虚空侵蚀减伤:一阶段 0.9,转阶段后 0.4</summary>
        public const float VoidTouchDR = 0.9f;
        public const float VoidTouchDRPhase2 = 0.4f;

        /// <summary>本体承伤系数的起手值与每帧回升量(原 <c>NPC.Entropy().damageMul</c>,上限 1)</summary>
        public const float DamageMulStart = 0.1f;
        public const float DamageMulRamp = 1f / 10000f;

        /// <summary>原灾厄 DR 体系的本地等效:一阶段减伤 54%,二阶段 42%</summary>
        public const float DRPhase1 = 0.54f;
        public const float DRPhase2 = 0.42f;

        /// <summary>同一发弹幕重复命中时伤害衰减记录每帧向 1 回归的比例</summary>
        public const float HitRecordDecay = 0.09f;

        //==================== 链条骨架 ====================

        /// <summary>体节数基数(实际生成 length + 1 个实体,最后一个是尾节)</summary>
        public const int ChainSegments = 20;
        /// <summary>死亡模式 +4,复仇 +3,天顶世界直接钉成 10</summary>
        public const int ChainSegmentsDeathBonus = 4;
        public const int ChainSegmentsRevengeBonus = 3;
        public const int ChainSegmentsZenith = 10;

        /// <summary>骨节间距(乘 NPC.scale)。头部集中绘制与体节实体的 wormFollow 共用这个数</summary>
        public const int ChainSpacing = 80;
        /// <summary>骨节朝向向前一节收敛的速率(比例式,非固定角速度)</summary>
        public const float ChainRotateRate = 0.12f;
        /// <summary>体节实体的暖机帧数:自身年龄不到这个数就整帧不动(等头部把链条铺开)</summary>
        public const int SegmentWarmupFrames = 5;
        /// <summary>体节实体的年龄超过这个数之后,除了硬跟随还额外叠一次带转向收敛的跟随</summary>
        public const int SegmentTightFollowFrames = 120;

        //==================== 战场半径(越界上虚空侵蚀) ====================

        /// <summary>半径的初值与目标初值,每帧按 <see cref="ArenaRadiusLerp"/> 逼近目标</summary>
        public const float ArenaRadiusStart = 6000f;
        public const float ArenaRadiusTargetStart = 2900f;
        public const float ArenaRadiusLerp = 0.001f;
        /// <summary>接战后的目标半径;转阶段期间收紧到 6000(同时把中心挪到玩家身上)</summary>
        public const float ArenaRadiusEngaged = 12000f;
        public const float ArenaRadiusPhaseTrans = 6000f;
        /// <summary>越界玩家每帧续上的虚空侵蚀帧数</summary>
        public const int ArenaDebuffFrames = 5;

        /// <summary>旧 crSky 计时的续租值(各端本地写自己的 LocalPlayer)</summary>
        public const int LegacySkyTimer = 30;

        //==================== 天空演出 ====================

        /// <summary>登场窗内天空强度渐临的上限与分母(<c>(1 - noaitime / 280) * 0.6</c>)</summary>
        public const float SkyIntroCap = 0.6f;
        public const float SkyIntroDivisor = 280f;
        /// <summary>二阶段躁动 = phaseTrans / 122 钳到 0~1</summary>
        public const float SkyAgitationDivisor = 122f;
        /// <summary>登场揭幕(noaitime 恰好归零那一帧)与转阶段首帧的闪电条数</summary>
        public const int SkyIntroBurstBolts = 4;
        public const int SkyPhaseTransBurstBolts = 6;

        //==================== 死亡演出 ====================

        public const int DeathAnmFrames = 200;
        /// <summary>镜头推近插值每帧 +0.025,满 1 之后直接钉成 24(原代码就是这么跳的)</summary>
        public const float DeathCamRamp = 0.025f;
        public const float DeathCamHold = 24f;
        /// <summary>速度高于 6 时每帧 ×0.96</summary>
        public const float DeathSpeedFloor = 6f;
        public const float DeathDrag = 0.96f;
        /// <summary>白化每帧 +1/160</summary>
        public const float DeathWhiteRamp = 1f / 160f;
        /// <summary>每 6 tick 一颗爆闪粒子(判据是 DeathAnmCount,倒着数)</summary>
        public const int DeathBurstInterval = 6;
        /// <summary>死亡爆散的虚空粒子数与屏震幅度</summary>
        public const int DeathVoidParticles = 86;
        public const float DeathVoidScatter = 16f;
        public const float DeathVoidDrag = 0.97f;
        public const float DeathShake = 16f;

        //==================== 嘴部(纯绘制) ====================

        /// <summary>张嘴预备:距离从 900 到 50 线性映射到每帧 0~7 的张角增量</summary>
        public const float MouthOpenFar = 900f;
        public const float MouthOpenNear = 50f;
        public const float MouthOpenRate = 7f;
        /// <summary>咬合触发距离 = max(30, |v|) × 4.6</summary>
        public const float BiteTriggerSpeedFloor = 30f;
        public const float BiteTriggerFactor = 4.6f;
        /// <summary>咬合期每帧 -12,张角低于 -48 解除咬合;非咬合期每帧 ×0.9;下限钳 -48</summary>
        public const float BiteCloseRate = -12f;
        public const float MouthDecay = 0.9f;
        public const float MouthMin = -48f;

        //==================== 尾鞭(da / tail_vj)与尾部新星 ====================

        /// <summary>鞭击起手的角速度,之后每帧 -1.5;角度落回 0 以下即触发新星</summary>
        public const float WhipLaunchSpeed = 12f;
        public const float WhipDecel = 1.5f;
        /// <summary>静息角度 = (100 / (|v| × 3)) × 5,负值钳 0,再按 0.1 逼近</summary>
        public const float FlagellumRestNumerator = 100f;
        public const float FlagellumRestSpeedFactor = 3f;
        public const float FlagellumRestScale = 5f;
        public const float FlagellumLerp = 0.1f;

        /// <summary>新星的基础环数/每环数量/初速(专家大师档在下面叠加)</summary>
        public const int NovaNum = 8;
        public const int NovaCounts = 3;
        public const float NovaSpeed = 9f;
        /// <summary>每打完一环初速 ×0.7</summary>
        public const float NovaRingSpeedDecay = 0.7f;
        /// <summary>新星生成点相对尾节的后方偏移(乘 NPC.scale)</summary>
        public const float NovaTailOffset = 172f;
        /// <summary>新星弹幕的伤害除数与击退</summary>
        public const float NovaStarDamageDivisor = 6.5f;
        public const float NovaStarKnockback = 1f;
        public const float NovaExplodeDamageDivisor = 6f;
        public const float NovaExplodeKnockback = 0f;
        /// <summary>天顶世界额外在每一节吐星:每节 3~9 发,初速 ×3</summary>
        public const float NovaZenithSpeedFactor = 3f;
        public const int NovaZenithCountMin = 3;
        public const int NovaZenithCountMax = 10;
        /// <summary>新星音效音高</summary>
        public const float NovaClapPitch = 1.4f;

        /// <summary>
        /// 新星的难度折算。装灾厄读复仇/死亡,缺席仍走专家/大师兜底;
        /// 原代码里专家与大师是<b>叠在</b>复仇/死亡之上的,顺序不可换
        /// </summary>
        public static void NovaScale(ref int num, ref int counts, ref float speed) {
            if (CECal.IsRevengeance) {
                num = 11;
                counts = 4;
                speed = 12f;
            }
            if (CECal.IsDeathMode) {
                num = 11;
                counts = 5;
                speed = 18f;
            }
            if (Main.expertMode) {
                num += 2;
                speed *= 1.25f;
            }
            if (Main.masterMode) {
                num += 2;
                counts += 1;
                speed *= 1.4f;
            }
        }

        /// <summary>绕飞吐星那一手的新星会被削弱:环数 -2,每环数量减半(整数除法),初速 ×0.45</summary>
        public const int NovaAroundCountsDelta = -2;
        public const float NovaAroundSpeedFactor = 0.45f;

        //==================== 二阶段尾焰(纯绘制) ====================

        /// <summary>喷口相对本体的后方偏移;两组各 4 颗(第二组再退一半速度)</summary>
        public const float ExhaustNozzleBack = 60f;
        public const int ExhaustCount = 4;
        public const float ExhaustOpacity = 1.6f;
        public const float ExhaustFade = 0.013f;
        public const float ExhaustJitterX = 0.3f;
        public const float ExhaustJitterY = 1.3f;

        //==================== 弹幕通用 ====================

        /// <summary>状态里 <c>Shoot</c> 的伤害除数与击退。owner 传 -1(原代码如此)</summary>
        public const float ProjDamageDivisor = 6.9f;
        public const float ProjKnockback = 3f;

        //==================== TryToClosePlayer:直扑 ====================

        public const float CloseInSpeedCap = 36f;
        public const float CloseInAccel = 1.02f;
        /// <summary>推力/阻尼/转向插值都按到玩家的距离重映射(近端 0 / 远端 700 或 1000)</summary>
        public const float CloseInRemapNear = 0f;
        public const float CloseInRemapFar = 700f;
        public const float CloseInThrustNear = 1f;
        public const float CloseInThrustFar = 3f;
        public const float CloseInDragNear = 0.98f;
        public const float CloseInDragFar = 0.97f;
        public const float CloseInAimRemapFar = 1000f;
        public const float CloseInAimLerpNear = 0f;
        public const float CloseInAimLerpFar = 0.1f;
        /// <summary>收招:计时过 600,或距离小于 700 + 当前速度</summary>
        public const int CloseInDuration = 600;
        public const float CloseInHandoffDistance = 700f;

        //==================== StayAwayAndShootVoidStar:拉开并甩尾吐星 ====================

        public const float StayAwaySpeedCap = 30f;
        public const float StayAwayAccel = 1.1f;
        public const float StayAwayDrag = 0.97f;
        /// <summary>第 90 帧发出一次尾鞭(尾部新星的唯一触发口)</summary>
        public const int StayAwayWhipCue = 90;
        /// <summary>第 70 帧起开始把速度往玩家方向掰</summary>
        public const int StayAwayTurnStart = 70;
        public const float StayAwayTurnLerp = 0.02f;
        public const float StayAwayTurnAccel = 1.02f;
        /// <summary>第 120 帧起加速逼近</summary>
        public const int StayAwayPushStart = 120;
        public const float StayAwayPushAccel = 1.046f;
        public const float StayAwayPushThrust = 0.1f;
        public const float StayAwayPushLerp = 0.03f;
        public const float StayAwayPushDrag = 0.998f;
        public const int StayAwayDuration = 140;

        //==================== AroundPlayerAndShootVoidStar:绕飞吐星 ====================

        /// <summary>绕飞锚点:玩家方向旋转 0.6 弧度外推 600</summary>
        public const float AroundOrbitRadius = 600f;
        public const float AroundOrbitAngle = 0.6f;
        public const float AroundThrust = 3f;
        public const float AroundDrag = 0.98f;
        /// <summary>每 40 帧一次尾鞭</summary>
        public const int AroundWhipInterval = 40;
        /// <summary>收招:40 × 8 + 30</summary>
        public const int AroundDuration = 40 * 8 + 30;

        //==================== EnergyBall:能量球 ====================

        /// <summary>开局那一帧生成能量球(ai0 = 本体 whoAmI)</summary>
        public const float EnergyBallDamageMult = 1.15f;
        public const int EnergyBallDuration = 240;
        /// <summary>距离大于 1000 时推力 4,否则 1;每帧阻尼 0.92</summary>
        public const float EnergyBallFarDistance = 1000f;
        public const float EnergyBallThrustFar = 4f;
        public const float EnergyBallThrustNear = 1f;
        public const float EnergyBallDrag = 0.92f;

        //==================== VoidResidue:虚空残渣 ====================

        public const int ResidueMouthOpenUntil = 80;
        public const int ResidueMouthCloseUntil = 100;
        public const float ResidueMouthOpenRate = -4.8f;
        public const float ResidueMouthCloseRate = 5f;
        /// <summary>第 2 帧的蓄力音</summary>
        public const int ResidueSoundFrame = 2;
        public const float ResidueSoundPitch = 0.8f;
        /// <summary>蓄力期距离大于 1000 时快速接近,否则慢速微调</summary>
        public const float ResidueApproachDistance = 1000f;
        public const float ResidueFarDrag = 0.95f;
        public const float ResidueFarThrust = 4f;
        public const float ResidueNearDrag = 0.92f;
        public const float ResidueNearThrust = 0.36f;
        /// <summary>第 80 帧喷出 80 发残渣:沿速度方向 ±2 弧度随机,速度 24 × 0.2~1</summary>
        public const int ResidueBurstFrame = 80;
        public const int ResidueBurstCount = 80;
        public const float ResidueBurstSpeed = 24f;
        public const float ResidueBurstSpread = 2f;
        public const float ResidueBurstSpeedMin = 0.2f;
        public const float ResidueBurstSpeedMax = 1f;
        public const float ResidueDamageMult = 0.8f;
        /// <summary>第 140 帧起顺着朝向冲一段</summary>
        public const int ResidueLungeStart = 140;
        public const float ResidueLungeThrust = 6f;
        public const float ResidueLungeDrag = 0.98f;
        public const int ResidueDuration = 200;

        //==================== VoidSpike:虚空尖刺 ====================

        /// <summary>速度按 0.08 逼近 46,朝向按 0.0376 比例掰向玩家</summary>
        public const float SpikeSpeedTarget = 46f;
        public const float SpikeSpeedLerp = 0.08f;
        public const float SpikeTurnRate = 0.0376f;
        /// <summary>四个齐射帧,每次沿 30 度一圈打 12 发,初速 5</summary>
        public const int SpikeRingFrameA = 40;
        public const int SpikeRingFrameB = 60;
        public const int SpikeRingFrameC = 80;
        public const int SpikeRingFrameD = 100;
        public const float SpikeRingAngleStep = 30f;
        public const float SpikeRingSpeed = 5f;
        public const int SpikeDuration = 150;

        //==================== BiteAndDash:咬住并拖拽 ====================

        /// <summary>咬合判定:本体前方 160 处与玩家的距离小于 160 即咬中</summary>
        public const float BiteGrabOffset = 160f;
        public const float BiteGrabRange = 160f;
        public const float BiteApproachDrag = 0.9f;
        public const float BiteApproachThrust = 6f;
        /// <summary>咬中后 20 帧的拖拽窗:速度按 0.2 逼近 80,嘴每帧 -5</summary>
        public const int BiteDragFrames = 20;
        public const float BiteMouthRate = -5f;
        public const float BiteDragSpeedTarget = 80f;
        public const float BiteDragSpeedLerp = 0.2f;
        /// <summary>拖拽期给玩家挂的无敌帧</summary>
        public const int BiteImmuneFrames = 12;
        /// <summary>前方 360 处撞墙就把计时直接跳到 60(跳过甩出,进入绕飞收尾)</summary>
        public const float BiteWallProbe = 360f;
        public const int BiteWallSkipTo = 60;
        /// <summary>甩出判定用的前方探测距离(比咬合判定短一半)</summary>
        public const float BiteLaunchProbe = 80f;
        /// <summary>甩出:玩家速度 = 本体速度 × 1.6,并挂 100 帧反重力</summary>
        public const float BiteLaunchSpeedMult = 1.6f;
        public const int BiteAntiGravFrames = 100;
        /// <summary>甩出同帧布下的刀光阵:17 排 × 13 列,排距 300,列角 0.125 弧度</summary>
        public const int BiteSlashRows = 18;
        public const int BiteSlashColumns = 7;
        public const float BiteSlashSpacing = 300f;
        public const float BiteSlashAngleStep = 0.125f;
        /// <summary>布阵后本体自己刹到 0.3</summary>
        public const float BiteSlashSelfDrag = 0.3f;
        /// <summary>收尾绕飞:玩家方向旋转 0.6 外推 1600</summary>
        public const float BiteOrbitRadius = 1600f;
        public const float BiteOrbitAngle = 0.6f;
        public const float BiteOrbitThrust = 1f;
        public const float BiteOrbitDrag = 0.98f;
        public const int BiteDuration = 120;

        //==================== AroundSpawnVoidBomb:巡游布雷 ====================

        public const float BombSpeedTarget = 32f;
        public const float BombSpeedLerp = 0.08f;
        public const float BombTurnRate = 0.022f;
        /// <summary>前 180 帧每 7 帧一颗,散布 8,附加朝玩家方向 20 的初速</summary>
        public const int BombWindow = 180;
        public const int BombInterval = 7;
        public const float BombScatter = 8f;
        public const float BombLead = 20f;
        public const int BombDuration = 340;

        //==================== Cruise:巡航 ====================

        public const float CruiseSpeedTarget = 40f;
        public const float CruiseSpeedLerp = 0.2f;
        public const float CruiseThrust = 0.1f;
        public const float CruiseAimLerp = 0.058f;
        public const float CruiseDrag = 0.998f;
        /// <summary>100 帧后开始每帧 1/150 的概率收招,200 帧硬收。<b>全局唯一一处影响出招的骰点</b></summary>
        public const int CruiseRollStart = 100;
        public const int CruiseRollChance = 150;
        public const int CruiseHardEnd = 200;

        //==================== SplittingVoidStar:裂空吐星 ====================

        public const int SplitMouthOpenUntil = 100;
        public const int SplitMouthCloseUntil = 120;
        public const float SplitMouthOpenRate = -4.8f;
        public const float SplitMouthCloseRate = 5f;
        public const int SplitSoundFrame = 20;
        public const float SplitSoundPitch = 1.05f;
        public const float SplitApproachDistance = 900f;
        public const float SplitFarDrag = 0.95f;
        public const float SplitFarThrust = 1f;
        public const float SplitNearDrag = 0.94f;
        public const float SplitNearThrust = 0.26f;
        /// <summary>第 100 帧喷出 80 发虚空星</summary>
        public const int SplitBurstFrame = 100;
        public const int SplitBurstCount = 80;
        public const float SplitBurstSpeed = 24f;
        public const float SplitBurstSpread = 2f;
        public const float SplitBurstSpeedMin = 0.2f;
        public const float SplitBurstSpeedMax = 1f;
        public const float SplitDamageMult = 0.75f;
        public const int SplitDuration = 140;

        //==================== QuickDash:短冲 ====================

        /// <summary>开局那一帧锁死朝向,之后 38 帧沿锁定方向加速</summary>
        public const int QuickDashThrustFrames = 38;
        public const float QuickDashThrust = 3.5f;
        /// <summary>38 帧后转为追瞄</summary>
        public const float QuickDashChaseDrag = 0.97f;
        public const float QuickDashChaseThrust = 1.4f;
        public const int QuickDashDuration = 100;

        //==================== VoidLaser:虚空激光 ====================

        /// <summary>
        /// 瞄准窗:前 35 帧追瞄刹速(判据是 <c>LaserAim++ &lt; 35</c>,自增后再判 <c>&gt; 36</c> 才进发射段,
        /// 所以第 36 帧(0 基)是一个两边都不进的空帧。照搬)
        /// </summary>
        public const int LaserAimFrames = 35;
        public const int LaserActiveFrom = 36;
        public const float LaserAimRotateRate = 0.16f;
        /// <summary>瞄准窗的预判量:玩家速度 × 46 × 0.8</summary>
        public const float LaserAimLeadFrames = 46f;
        public const float LaserLeadFactor = 0.8f;
        public const float LaserAimDrag = 0.96f;
        public const float LaserAimBackThrust = -0.4f;
        /// <summary>发射段:6 轮,每轮 46 帧</summary>
        public const int LaserCycle = 46;
        public const int LaserCycles = 6;
        /// <summary>每轮的开火帧 u:按轮次从 42 线性收到 18(越到后面越快出手)</summary>
        public const float LaserLeadHigh = 42f;
        public const float LaserLeadLow = 18f;
        /// <summary>预告粒子的两档尺寸</summary>
        public const float LaserWarnScaleBig = 1.8f;
        public const float LaserWarnScaleSmall = 0.8f;
        /// <summary>光束初速,以及本体冲刺距离的换算(距离 + 1400) / (45 - u)</summary>
        public const float LaserBeamSpeed = 10f;
        public const float LaserDashDistanceBonus = 1400f;
        public const float LaserDashDivisorBase = 45f;
        /// <summary>每轮第 45 帧把速度归一化到 4</summary>
        public const int LaserCycleBrakeFrame = 45;
        public const float LaserCycleBrakeSpeed = 4f;
        /// <summary>预警光束的绘制阈值(瞄准窗内)</summary>
        public const int LaserWarningAimFrames = 31;

        //==================== PhaseTransing:转阶段 ====================

        /// <summary>转阶段总帧数。计数器是 <c>phaseTrans</c> 本身,不是状态计时</summary>
        public const int PhaseTransFrames = 122;
        /// <summary>前 60 帧清场(清掉能量球与残渣,并把尾鞭状态复位)</summary>
        public const int PhaseTransClearWindow = 60;
        /// <summary>透明度:转阶段期间每帧 ×0.967,转完每帧 +0.02 回到 1</summary>
        public const float PhaseTransAlphaDecay = 0.967f;
        public const float PhaseTransAlphaRise = 0.02f;
        /// <summary>转阶段的运动:速度低于 8 每帧 ×1.01,否则 ×0.98</summary>
        public const float PhaseTransSpeedFloor = 8f;
        public const float PhaseTransAccel = 1.01f;
        public const float PhaseTransDrag = 0.98f;
        /// <summary>转阶段每一骨节每帧一颗虚空粒子,透明度 0.2~1.4</summary>
        public const float PhaseTransParticleScatter = 6f;
        public const float PhaseTransParticleOpacityMin = 0.2f;
        public const float PhaseTransParticleOpacityMax = 1.4f;
        /// <summary>二阶段贴图切换的门槛(原代码用的是 <c>phaseTrans &gt; 120</c> / <c>&gt;= 120</c>,两个数都照搬)</summary>
        public const int PhaseTransDrawSwitch = 120;

        //==================== 绘制常数(纯表现) ====================

        /// <summary>二阶段体节:遍历 0~8 号骨节,<b>跳过 0 号与 2 号</b>(七张帧图按剩下的顺序贴)</summary>
        public const int P2BodyNodeCount = 9;
        public const int P2BodySkipA = 0;
        public const int P2BodySkipB = 2;
        /// <summary>二阶段上下颚的锚点偏移与张角折算(乘 0.8),以及两张贴图的原点</summary>
        public const float P2JawOffset = 54f;
        public const float P2JawRotFactor = 0.8f;
        public const float P2JawOriginX = 28f;
        public const float P2JawOriginY = 20f;
        /// <summary>一阶段上下颚的锚点偏移与原点宽度(原点是 <c>new Vector2(58, 贴图高) / 2</c>)</summary>
        public const float P1JawOffset = 42f;
        public const float P1JawOriginX = 58f;
        /// <summary>鞭毛贴图相对骨节的前移与左右两片的基准角(180 ± da)</summary>
        public const float FlagellumDrawOffset = 36f;
        public const float FlagellumBaseAngle = 180f;
        /// <summary>预警光束:一阶段/二阶段的插值速率,可见阈值,以及触发距离</summary>
        public const float WarningLerpPhase1 = 0.064f;
        public const float WarningLerpPhase2 = 0.2f;
        public const float WarningVisibleThreshold = 0.002f;
        public const float WarningCloseInDistance = 1200f;
        /// <summary>预警光束的两层缩放:长度固定 20,宽度按阶段取 0.6/0.8 与 0.5/0.65(第二层再乘三次方)</summary>
        public const float WarningLength = 20f;
        public const float WarningWidthOuterP1 = 0.6f;
        public const float WarningWidthOuterP2 = 0.8f;
        public const float WarningWidthInnerP1 = 0.5f;
        public const float WarningWidthInnerP2 = 0.65f;
        public const float WarningOpacity = 0.7f;

        //==================== 出招轮换 ====================

        /// <summary>
        /// 一阶段 20 槽。<see cref="CruiserStateIndex.PhaseTransing"/> 在这里当哨兵用:
        /// 原代码的 6 号与 19 号槽不是固定招,而是「上一手若是拉开就退回直扑并把序号退一格,
        /// 否则骰能量球或残渣」,见 <see cref="CruiserRotation"/>
        /// </summary>
        public static readonly CruiserStateIndex[] Phase1 =
        {
            CruiserStateIndex.TryToClosePlayer,             //0
            CruiserStateIndex.StayAwayAndShootVoidStar,     //1
            CruiserStateIndex.TryToClosePlayer,             //2
            CruiserStateIndex.StayAwayAndShootVoidStar,     //3
            CruiserStateIndex.TryToClosePlayer,             //4
            CruiserStateIndex.StayAwayAndShootVoidStar,     //5
            CruiserStateIndex.PhaseTransing,                //6  哨兵:退格 or 骰招
            CruiserStateIndex.TryToClosePlayer,             //7
            CruiserStateIndex.StayAwayAndShootVoidStar,     //8
            CruiserStateIndex.TryToClosePlayer,             //9
            CruiserStateIndex.StayAwayAndShootVoidStar,     //10
            CruiserStateIndex.TryToClosePlayer,             //11
            CruiserStateIndex.StayAwayAndShootVoidStar,     //12
            CruiserStateIndex.AroundPlayerAndShootVoidStar, //13
            CruiserStateIndex.StayAwayAndShootVoidStar,     //14
            CruiserStateIndex.TryToClosePlayer,             //15
            CruiserStateIndex.StayAwayAndShootVoidStar,     //16
            CruiserStateIndex.TryToClosePlayer,             //17
            CruiserStateIndex.StayAwayAndShootVoidStar,     //18
            CruiserStateIndex.PhaseTransing,                //19 哨兵:同 6 号
        };

        /// <summary>二阶段 10 槽,纯确定性序列</summary>
        public static readonly CruiserStateIndex[] Phase2 =
        {
            CruiserStateIndex.VoidSpike,            //0
            CruiserStateIndex.AroundSpawnVoidBomb,  //1
            CruiserStateIndex.VoidSpike,            //2
            CruiserStateIndex.SplittingVoidStar,    //3
            CruiserStateIndex.QuickDash,            //4
            CruiserStateIndex.VoidSpike,            //5
            CruiserStateIndex.VoidLaser,            //6
            CruiserStateIndex.VoidResidue,          //7
            CruiserStateIndex.Cruise,               //8
            CruiserStateIndex.BiteAndDash,          //9
        };
    }
}
