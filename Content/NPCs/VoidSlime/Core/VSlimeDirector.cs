using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidSlime.Core
{
    /// <summary>
    /// 虚空史莱姆的节奏、判定、伤害和招式数值都在这里,状态里只留姿态和亮度这类系数
    /// 数值存普通档基值,专家和大师由原版在血量缩放与命中结算时乘
    /// 设计稿的距离按格写,这里一律换成像素,1 格 = 16 像素;拍点都是状态 Timer 的区间
    /// </summary>
    public static class VSlimeDirector
    {
        public const float Tile = 16f;

        //==================== 基础数值 ====================

        /// <summary>虚空形态血量,普通 / 专家 / 大师 = 588000 / 1176000 / 1764000,SetDefaults 用它,生命同步才占 4 字节</summary>
        public const int VoidLife = 588000;
        /// <summary>苍白形态血量 52000 / 104000 / 156000,按 VoidLife 的缩放比例折算</summary>
        public const int PaleLife = 52000;
        public const int PaleDefense = 60;
        public const int VoidDefense = 80;
        /// <summary>虚空形态起的伤害减免,苍白形态没有</summary>
        public const float VoidDR = 0.2f;
        /// <summary>苍白形态接触伤害 120 / 240 / 360</summary>
        public const int PaleContact = 120;
        /// <summary>虚空形态接触伤害 140 / 280 / 420,SetDefaults 用它</summary>
        public const int VoidContact = 140;
        /// <summary>血量降到这个比例进奇点形态,设计稿两处写的是 55% 和 50%,取转阶段那一句的 50%</summary>
        public const float SingularityLifeRatio = 0.5f;
        public const float DeathStarLifeRatio = 0.2f;
        /// <summary>死兆星阶段承受的伤害倍率</summary>
        public const float DeathStarDamageTaken = 4f;
        /// <summary>目标超过 250 格就脱战</summary>
        public const float MaxFindDistance = 250f * Tile;
        public const int NoTargetDespawnFrames = 150;
        /// <summary>史莱姆形态的受击判定框</summary>
        public static readonly Vector2 BodySize = new Vector2(170f, 110f);
        /// <summary>史莱姆贴图 168x118 的绘制倍率,屏上约 218x153,和史莱姆王同档</summary>
        public const float BodyDrawScale = 1.3f;
        /// <summary>判定框每帧最多变化这么多像素,两端差一两帧时位置只偏几像素</summary>
        public const float HitboxStep = 14f;
        /// <summary>虚空之火时长,单位是帧,不随难度延长</summary>
        public const int VoidFireFrames = 180;

        //==================== 伤害档(普通档基值) ====================
        //弹幕 damage 字段存这里的数,命中时原版乘 2 再乘难度倍率 1 / 2 / 3,所以都是设计稿普通档的一半
        //Contact 结尾的是本体接触伤害,存设计稿普通档原值,宿主按它和 VoidContact 的比例乘倍率

        /// <summary>灰色凝胶球 120 / 240 / 360</summary>
        public const int DmgGreyGel = 60;
        /// <summary>苍白形态砸地冲击波 160 / 320 / 480</summary>
        public const int DmgPaleShock = 80;
        public const int PaleSmashContact = 160;
        /// <summary>暗物质球 140 / 280 / 420</summary>
        public const int DmgDarkOrb = 70;
        /// <summary>暗物质凝胶球 125 / 250 / 375,62.5 取整到 63</summary>
        public const int DmgDarkGel = 63;
        /// <summary>星体爆发冲击 180 / 360 / 540</summary>
        public const int DmgBurst = 90;
        /// <summary>紫色大辉星 140 / 280 / 420</summary>
        public const int DmgBigStar = 70;
        /// <summary>蓝色和橙色小辉星 125 / 250 / 375</summary>
        public const int DmgStarlet = 63;
        /// <summary>星体爆炸里的紫色小辉星 120 / 240 / 360</summary>
        public const int DmgHomingStarlet = 60;
        public const int BlackHoleContact = 200;
        /// <summary>星体残骸 125 / 250 / 375</summary>
        public const int DmgDebris = 63;
        /// <summary>紫色伽玛射线 180 / 360 / 540</summary>
        public const int DmgPurpleRay = 90;
        /// <summary>引力融合的黑洞冲击 140 / 280 / 420</summary>
        public const int FusionContact = 140;
        /// <summary>蓝色伽玛射线 170 / 340 / 510</summary>
        public const int DmgBlueRay = 85;
        /// <summary>十字星爆炸 130 / 260 / 390</summary>
        public const int DmgCrossStar = 65;
        /// <summary>主序星的淡蓝激光 125 / 250 / 375</summary>
        public const int DmgStarLaser = 63;
        /// <summary>蓝恒星和红巨星本体 200 / 400 / 600</summary>
        public const int StarContact = 200;
        /// <summary>蓝色奇点黑洞 240 / 480 / 720</summary>
        public const int SingularityContact = 240;

        //==================== 引力场 ====================

        /// <summary>限制圈半径 75 格</summary>
        public const float ArenaRadius = 75f * Tile;
        /// <summary>无尽之噬期间缩到 60 格</summary>
        public const float ArenaRadiusDevour = 60f * Tile;
        /// <summary>展开时的起始半径</summary>
        public const float ArenaOpenRadius = 260f;
        /// <summary>半径每帧最多变化这么多像素</summary>
        public const float ArenaRadiusStep = 6f;
        /// <summary>圈心每帧最多追这么多像素,玩家的飞行速度远高于它,不会被场地推着走</summary>
        public const float ArenaFollowSpeed = 1.6f;
        /// <summary>本体离圈心超过这个距离圈心才动,小幅跃迁不带着场地晃</summary>
        public const float ArenaFollowDeadzone = 120f;
        /// <summary>落点距圈心不超过半径减这个数,本体不会贴着圈沿出招</summary>
        public const float ArenaAnchorMargin = 340f;
        /// <summary>圈外吸力:越界 0 到 PullFullDepth 像素线性升到这个速度,单位是像素每帧</summary>
        public const float ArenaPullSpeed = 9f;
        public const float ArenaPullFullDepth = 220f;
        /// <summary>衰变和宇宙空间每次续的时长,离开条件后很快消失</summary>
        public const int DecayRefreshFrames = 12;
        public const int CosmicRefreshFrames = 30;

        //==================== 节奏 ====================

        /// <summary>苍白形态的连接段</summary>
        public const int PaleConnectorFrames = 34;
        /// <summary>虚空形态起切招前摇 2 秒:跃迁 → 落定 → 起势</summary>
        public const int ConnectorFrames = 120;
        /// <summary>跃迁起跳前的压缩帧数</summary>
        public const int LeapCrouchFrames = 12;
        public const int LeapFlyFrames = 56;
        /// <summary>距离小于它就不跃迁,原地等待</summary>
        public const float LeapMinDistance = 90f;
        /// <summary>距离大于它改用闪现</summary>
        public const float LeapMaxDistance = 1700f;
        /// <summary>弧顶高度取距离的这个比例,再钳到上下限</summary>
        public const float LeapArcRatio = 0.22f;
        public const float LeapArcMin = 50f;
        public const float LeapArcMax = 240f;
        /// <summary>连接段末尾的起势拍,描边亮起</summary>
        public const int ConnectorPostureFrames = 24;
        /// <summary>场上本体弹幕超过这个数,连接段最多再等 ClutterWaitMax 帧</summary>
        public const int ClutterThreshold = 70;
        public const int ClutterWaitMax = 60;
        public const int AttackTimeoutFrames = 60 * 16;
        /// <summary>闪现总帧数,前半淡出,过半换位,后半淡入</summary>
        public const int BlinkDuration = 24;
        /// <summary>闪现落地后的无接触伤害窗</summary>
        public const int PostBlinkGrace = 20;
        /// <summary>没有专属锚点的招站在玩家旁边 30 格</summary>
        public const float SideAnchorDistance = 30f * Tile;
        /// <summary>虫洞形态悬在玩家头顶 35 格,奇点形态的星体爆炸是 30 格</summary>
        public const float OverheadHeight = 35f * Tile;
        public const float OverheadHeightEx = 30f * Tile;

        //==================== 演出 ====================

        /// <summary>登场:从高空坠下,落地后停 EntranceSettle 帧开打,超过 EntranceMaxFrames 没落地也开打</summary>
        public const float EntranceSpawnHeight = 760f;
        public const int EntranceMaxFrames = 300;
        public const int EntranceSettle = 44;
        /// <summary>破壳:0-150 膨胀震颤,150 爆开换形态,150-230 落定</summary>
        public const int BurstSwellFrames = 150;
        public const int BurstDuration = 230;
        /// <summary>破壳前慢慢浮起的高度</summary>
        public const float BurstRise = 80f;
        /// <summary>转奇点形态:0-100 头顶虹光成环,100-160 溅散露出奇点,160 爆闪换形态,160-250 重聚</summary>
        public const int SingularityShiftHaloFrames = 100;
        public const int SingularityShiftBurstFrame = 160;
        public const int SingularityShiftDuration = 250;
        /// <summary>转死兆星:0-90 飞回圈心天幕压黑,90 爆闪换形态,90-150 落定</summary>
        public const int DeathStarShiftBurstFrame = 90;
        public const int DeathStarShiftDuration = 150;
        /// <summary>终幕:黑洞在 4 秒内缩成白点,白光闪过再结算击杀</summary>
        public const int DeathShrinkFrames = 240;
        public const int DeathDuration = 300;
        /// <summary>客户端计时可能落后几帧,收到击杀包时不把自己救活</summary>
        public const int DeathKillTolerance = 10;

        //==================== 苍白:跳跃 ====================

        /// <summary>重力和终端速度倍率,比史莱姆王落得快;终端速度放到 30,下砸的速度才不会被原版剪掉</summary>
        public const float PaleGravityMult = 1.6f;
        public const float PaleMaxFallMult = 3f;
        /// <summary>正常跳跃持续 8 秒</summary>
        public const int HopDuration = 480;
        /// <summary>落地后蓄这么多帧再起跳</summary>
        public const int HopWait = 16;
        public const float HopSpeedX = 9.5f;
        /// <summary>两小跳一大跳,小跳弧高约 138 像素跨 456,大跳弧高约 250 跨 613</summary>
        public static float HopSpeedY(int hop) => hop % 3 == 2 ? -15.5f : -11.5f;

        //==================== 苍白:传送凝胶球 ====================

        public const int SlamReps = 2;
        /// <summary>压扁沉下的帧数,结束时起闪现</summary>
        public const int SlamSinkFrames = 30;
        /// <summary>出现在玩家头顶这么高,26 格</summary>
        public const float SlamHoverHeight = 26f * Tile;
        /// <summary>悬停帧数,这段时间下方画落点航道,航道不再跟玩家</summary>
        public const int SlamHoverFrames = 26;
        public const float SlamFallSpeed = 26f;
        /// <summary>下落窗,窗内落地就弹球,窗尽还没落地也在原地弹;每轮总长固定,拍点都能由 Timer 算出</summary>
        public const int SlamFallWindow = 50;
        public const int SlamRecoverFrames = 36;
        public const int SlamCycle = SlamSinkFrames + BlinkDuration + SlamHoverFrames + SlamFallWindow + SlamRecoverFrames;
        public const float SlamLaneWidth = 190f;
        public const float SlamLaneLength = 1100f;
        /// <summary>两圈灰色凝胶球,内圈慢外圈快,都往上半平面弹</summary>
        public const int SlamRingInner = 8;
        public const int SlamRingOuter = 12;
        public const float SlamRingInnerSpeed = 6.5f;
        public const float SlamRingOuterSpeed = 10.5f;
        /// <summary>弹射扇面半角,围绕正上方摊开,贴地的角度留空</summary>
        public const float SlamRingHalfAngle = 1.25f;
        public const float GreyGelGravity = 0.28f;
        public const float GreyGelBounce = 0.72f;
        public const int GreyGelMaxBounces = 3;
        public const int GreyGelLife = 300;

        //==================== 苍白:史莱姆猛击 ====================

        /// <summary>压缩蓄力帧数,接触档前摇</summary>
        public const int SmashChargeFrames = 44;
        /// <summary>起跳初速,重力 0.48 时升空 44 帧到弧顶,高约 460 像素</summary>
        public const float SmashLeapSpeed = 21f;
        public const float SmashLeapMaxSpeedX = 14f;
        public const int SmashLeapFrames = 44;
        /// <summary>弧顶悬停帧数,起始帧锁定落点和俯冲方向,航道亮起</summary>
        public const int SmashHoldFrames = 18;
        public const float SmashDiveSpeed = 28f;
        /// <summary>俯冲窗,窗内落地就出冲击波,窗尽还没落地也在原地出</summary>
        public const int SmashDiveWindow = 60;
        /// <summary>冲击波扩到 15 格</summary>
        public const float SmashShockRadius = 15f * Tile;
        public const float SmashShockSpeed = 11f;
        public const int SmashRecoverFrames = 46;
        public const int SmashDuration = SmashChargeFrames + SmashLeapFrames + SmashHoldFrames + SmashDiveWindow + SmashRecoverFrames;
        public const float SmashLaneWidth = 190f;

        //==================== 虚空:引力星环 ====================

        /// <summary>四圈半径 16 / 32 / 48 / 64 格,每圈 7 / 14 / 21 / 28 颗,相邻两颗的弧距都约 230 像素</summary>
        public const int RingCount = 4;
        public const float RingRadiusStep = 16f * Tile;
        public const int RingOrbsStep = 7;
        /// <summary>暗物质球的切向速度,各圈相同,外圈角速度自然更慢</summary>
        public const float RingTangentSpeed = 2.2f;
        /// <summary>生成 2 秒后才有判定和淡紫描边</summary>
        public const int RingArmFrames = 120;
        /// <summary>第一轮凝胶球的拍点,之后每 1.5 秒一轮,共三轮</summary>
        public const int RingVolleyStart = 130;
        public const int RingVolleyInterval = 90;
        public const int RingVolleys = 3;
        public const int RingVolleyGels = 6;
        /// <summary>凝胶球在本体周围成形的半径</summary>
        public const float RingVolleyRadius = 150f;
        /// <summary>成形后悬停这么多帧才发射,相邻两颗错开 GelStagger 帧</summary>
        public const int RingVolleyHover = 40;
        public const int GelStagger = 4;
        /// <summary>瞄准凝胶球初速低,逐帧加速到上限</summary>
        public const float GelAimStartSpeed = 3f;
        public const float GelAimAccel = 0.22f;
        public const float GelAimMaxSpeed = 15f;
        public const int GelAimLife = 200;
        /// <summary>蓄力拍:紫边和 64 格预警圈亮起,4 秒后爆发</summary>
        public const int RingChargeStart = 380;
        public const int RingChargeFrames = 240;
        public const float RingBurstRadius = 64f * Tile;
        /// <summary>爆发波面的扩散速度,比玩家快得多,圈内等同整面命中</summary>
        public const float RingBurstSpeed = 32f;
        public const int RingTail = 60;

        //==================== 虚空:星体爆炸 ====================

        public const int BlastStars = 4;
        /// <summary>第一颗的出手拍,之后每 100 帧一颗</summary>
        public const int BlastFirstThrow = 70;
        public const int BlastThrowInterval = 100;
        /// <summary>落点在玩家左右 38 格</summary>
        public const float BlastSideDistance = 38f * Tile;
        /// <summary>大辉星飞行帧数,落点标记同时亮起</summary>
        public const int BigStarFlight = 54;
        public const float BigStarOrbitRadius = 150f;
        public const float BigStarOrbitSpeed = 0.045f;
        public static int BlastBlueCount(int phase) => phase >= VSlimeForm.Singularity ? 24 : 20;
        public static int BlastHomingCount(int phase) => phase >= VSlimeForm.Singularity ? 4 : 0;
        /// <summary>爆出的蓝色小辉星速度和寿命,飞 300 帧约 900 像素</summary>
        public const float BlastStarletSpeed = 3f;
        public const int BlastStarletLife = 300;
        public const float HomingStarletSpeed = 3.8f;
        /// <summary>弱追踪:每帧最多转这么多弧度,追踪窗过后只直飞</summary>
        public const float HomingTurnRate = 0.014f;
        public const int HomingFrames = 150;
        public const int HomingStarletLife = 330;
        public const int BlastTail = 70;

        //==================== 虚空:黑洞爆发 ====================

        /// <summary>蓄力变黑洞的帧数,这段只有本体判定</summary>
        public const int HoleChargeFrames = 70;
        /// <summary>红色大黑洞半径 10 格,持续 6 秒</summary>
        public const float HoleRadius = 10f * Tile;
        public const int HoleActiveFrames = 360;
        public const float HoleDriftSpeed = 2.2f;
        /// <summary>对玩家的吸力,远端衰减到 0</summary>
        public const float HolePullSpeed = 2.6f;
        public const float HolePullRange = 1500f;
        public const int DebrisInterval = 80;
        public const int DebrisPerRing = 12;
        public const float DebrisStartSpeed = 3.2f;
        public const float DebrisAccel = 0.035f;
        public const float DebrisMaxSpeed = 9f;
        public const int DebrisLife = 420;
        public const int HoleDissolveFrames = 60;

        //==================== 虚空:凝胶喷射 ====================

        public const int SprayWindup = 36;
        /// <summary>喷射 6 秒,结束 1 秒后所有凝胶球摧毁</summary>
        public const int SprayFrames = 360;
        public const int SprayLinger = 60;
        public const int SprayInterval = 2;
        /// <summary>喷出时的横向和纵向速度范围</summary>
        public const float SpraySpeedX = 12f;
        public const float SpraySpeedYMin = 4f;
        public const float SpraySpeedYMax = 8.5f;
        /// <summary>下落加速度随存活帧增长,终端速度封顶,落到玩家高度至少要 86 帧</summary>
        public const float SprayGravity = 0.09f;
        public const float SprayGravityGrowth = 0.0012f;
        public const float SprayMaxFall = 6.5f;
        public const float SprayDragX = 0.985f;
        public const int SprayTail = 20;

        //==================== 虚空:晦暗星雨 ====================

        /// <summary>蓄力 1 秒,射线档前摇</summary>
        public const int RainChargeFrames = 60;
        public const int SkyRayActiveFrames = 50;
        public const int SkyRayFadeFrames = 14;
        public const float SkyRayLength = 2600f;
        /// <summary>判定半宽 20,可见亮体半宽 34</summary>
        public const float SkyRayHitHalfWidth = 20f;
        public const float SkyRayDrawHalfWidth = 34f;
        /// <summary>射线发出 1.5 秒后星雨从画面边缘进场,持续 6 秒后整片消失</summary>
        public const int RainStartDelay = 90;
        public const int RainFrames = 360;
        /// <summary>射线发出后这么多帧开始在画面外生成,先头的星在 1.5 秒拍前后越过画面边缘</summary>
        public const int RainSpawnDelay = 40;
        public const int RainInterval = 5;
        public const int RainPerSpawn = 4;
        /// <summary>落速 7.5,每 5 帧 4 颗铺在 2600 像素宽的带上,星与星的平均间距约 156 像素</summary>
        public const float RainSpeed = 7.5f;
        /// <summary>星雨方向与竖直的夹角</summary>
        public const float RainSlant = 0.52f;
        /// <summary>生成线在玩家上游这么远,竖直分量约 1130 像素,4K 屏也在画面外;横向铺这么宽</summary>
        public const float RainUpstream = 1300f;
        public const float RainHalfWidth = 1300f;
        /// <summary>射线一发出就开始生成,先头的星要飞 1300 像素才到玩家;离消失拍不足这么多帧就不再生成,那些星飞不到玩家身边</summary>
        public const int RainSpawnCutoff = 150;
        public const int RainTail = 30;

        //==================== 奇点:引力融合 ====================

        public const int FusionCycles = 4;
        /// <summary>起手分裂消失 1 秒</summary>
        public const int FusionSplitFrames = 60;
        /// <summary>每轮:出现并亮航道 1 秒 → 对撞 → 融合停留 → 消失 1 秒</summary>
        public const int FusionTelegraphFrames = 60;
        public const int FusionDashFrames = 20;
        public const int FusionMergeFrames = 20;
        public const int FusionVanishFrames = 60;
        public const int FusionCycle = FusionTelegraphFrames + FusionDashFrames + FusionMergeFrames + FusionVanishFrames;
        /// <summary>两个小黑洞各距玩家 35 格,半径 6 格</summary>
        public const float FusionDistance = 35f * Tile;
        public const float FusionHoleRadius = 6f * Tile;
        /// <summary>连线偏离水平不超过 60 度</summary>
        public const float FusionMaxTilt = MathHelper.Pi / 3f;
        public const int FusionGels = 6;
        public const float FusionGelSpeed = 2.6f;
        public const int FusionGelLife = 300;
        public const int FusionReformFrames = 50;

        //==================== 奇点:无尽之噬 ====================

        /// <summary>预告拍:虫洞成形,五角星航线亮起,圈缩到 60 格</summary>
        public const int DevourWindup = 60;
        /// <summary>五角星外接圆半径,五条边各跑 DevourEdgeFrames 帧,边长约 1522 像素</summary>
        public const float DevourStarRadius = 800f;
        public const int DevourEdgeFrames = 25;
        public const float DevourLaneWidth = 150f;
        /// <summary>闪回圈心并张开黑洞的帧数</summary>
        public const int DevourHoleOpenFrames = 46;
        public const float DevourHoleRadius = 12f * Tile;
        /// <summary>十五层蓝色小辉星,每层 6 颗,层间隔 16 帧,相邻层错开 12 度连成旋臂</summary>
        public const int DevourLayers = 15;
        public const int DevourArms = 6;
        public const int DevourLayerInterval = 16;
        public const float DevourLayerTwist = 0.2094f;
        /// <summary>小辉星向心速度和逆时针角速度,半径 600 处切向约 6.6 像素每帧</summary>
        public const float DevourInwardSpeed = 3f;
        public const float DevourAngularSpeed = 0.011f;
        /// <summary>小辉星进到这个半径就被吞掉</summary>
        public const float DevourAbsorbRadius = 150f;
        /// <summary>吸完第 10 层后每秒一圈 20 颗凝胶球,共 3 圈</summary>
        public const int DevourGelRings = 3;
        public const int DevourGelInterval = 60;
        public const int DevourGelCount = 20;
        public const float DevourGelSpeed = 2.8f;
        public const int DevourGelLife = 300;
        public const int DevourReformFrames = 50;
        /// <summary>黑光蓄力 2 秒,两道射线预告线亮在 12 点和 6 点</summary>
        public const int DevourBeamCharge = 120;
        /// <summary>顺时针转 360 度用 4 秒,前 DevourBeamRamp 帧转速从 0 爬升</summary>
        public const int DevourBeamFrames = 240;
        public const int DevourBeamRamp = 30;
        public const float DevourBeamLength = 1040f;
        public const float BlueRayHitHalfWidth = 14f;
        public const float BlueRayDrawHalfWidth = 24f;
        public const int DevourTail = 60;

        /// <summary>第 layer 层小辉星被吞掉的拍点,相对旋臂开始</summary>
        public static int DevourAbsorbBeat(int layer)
            => layer * DevourLayerInterval + (int)((ArenaRadiusDevour - 12f - DevourAbsorbRadius) / DevourInwardSpeed);

        //==================== 奇点:星河重击 ====================

        public const int GalaxyReps = 4;
        /// <summary>花 1.5 秒排出 14 颗间隔 12 格的凝胶球</summary>
        public const int GalaxySummonFrames = 90;
        public const int GalaxyGels = 14;
        public const float GalaxyGelSpacing = 12f * Tile;
        /// <summary>发白光的锁定拍,这段本体不再跟玩家,航道亮起</summary>
        public const int GalaxyLockFrames = 26;
        /// <summary>下砸 70 格</summary>
        public const float GalaxyFallDistance = 70f * Tile;
        public const int GalaxyFallFrames = 28;
        /// <summary>砸完停 1 秒再闪回玩家头顶</summary>
        public const int GalaxyRestFrames = 60;
        public const int GalaxyCycle = GalaxySummonFrames + GalaxyLockFrames + GalaxyFallFrames + GalaxyRestFrames + BlinkDuration;
        public const int GalaxyStarlets = 3;
        public const float GalaxyStarletSpeed = 3.4f;
        /// <summary>横排凝胶球松开后的下落加速度和终端速度,比本体慢得多</summary>
        public const float RowGelAccel = 0.07f;
        public const float RowGelMaxFall = 4.5f;
        public const int RowGelLife = 440;
        public const float GalaxyLaneWidth = 210f;

        //==================== 奇点:超星系轰炸 ====================

        /// <summary>蓝光一闪跃入背景的帧数</summary>
        public const int BombardDiveFrames = 46;
        /// <summary>轰炸 7 秒</summary>
        public const int BombardFrames = 420;
        public const int BombardInterval = 8;
        public const int BombardReturnFrames = 50;
        /// <summary>十字星从背景逼近的帧数,这段只有标记没有判定</summary>
        public const int CrossApproachFrames = 56;
        public const int CrossActiveFrames = 10;
        public const int CrossFadeFrames = 12;
        public const float CrossArmLength = 230f;
        /// <summary>判定半宽 12,可见亮体半宽 22,判定取臂长的 0.78</summary>
        public const float CrossHitHalfWidth = 12f;
        public const float CrossDrawHalfWidth = 22f;
        public const float CrossHitLengthRatio = 0.78f;
        /// <summary>落点散布半径,每三颗里有一颗瞄预判点</summary>
        public const float BombardScatter = 520f;
        public const float BombardLeadMax = 360f;

        //==================== 死兆星:主序星 ====================

        /// <summary>蓝恒星半径 20 格,成形用 90 帧</summary>
        public const float MainStarRadius = 20f * Tile;
        public const int MainFormFrames = 90;
        public const int MainVolleys = 10;
        /// <summary>每轮:白色预警线 1 秒 → 激光 → 熄灭,设计稿预警线写 20 根激光写 24 道,预告和出手统一成 24</summary>
        public const int MainVolleyInterval = 90;
        public const int StarLaserCount = 24;
        public const int StarLaserTelegraph = 60;
        public const int StarLaserActive = 16;
        public const int StarLaserFade = 8;
        public const float StarLaserLength = 1500f;
        public const float StarLaserHitHalfWidth = 11f;
        public const float StarLaserDrawHalfWidth = 19f;
        /// <summary>收尾两圈小辉星 24 和 20 颗</summary>
        public const int MainRingA = 24;
        public const int MainRingB = 20;
        public const float MainRingSpeedA = 2.6f;
        public const float MainRingSpeedB = 3.3f;
        public const int MainRingLife = 360;
        public const int MainTail = 50;

        /// <summary>第 volley 轮激光的基准角:相邻两轮错半格,再叠一个慢漂移,站两个固定点来回换躲不完</summary>
        public static float MainVolleyAngle(int volley)
            => volley * (MathHelper.Pi / StarLaserCount) + volley * volley * 0.021f;

        //==================== 死兆星:红巨星 ====================

        /// <summary>红巨星半径 24 格</summary>
        public const float GiantRadius = 24f * Tile;
        public const int GiantSwellFrames = 60;
        /// <summary>出现 2.5 秒后开始爆发,每秒一次共 8 次</summary>
        public const int GiantFirstShock = 150;
        public const int GiantShockInterval = 60;
        public const int GiantShocks = 8;
        public const float GiantShockSpeed = 18f;
        public const float GiantShockMaxRadius = 1320f;
        /// <summary>第 5 次起每次伴 6 颗橙色小辉星</summary>
        public const int GiantStarletFrom = 4;
        public const int GiantStarlets = 6;
        public const float GiantStarletSpeed = 3f;
        public const int GiantStarletLife = 320;
        /// <summary>四个图腾在半径 50 格的轨道上,每个半径 7 格,每秒顺时针走 15 格</summary>
        public const int TotemCount = 4;
        public const float TotemOrbitRadius = 50f * Tile;
        public const float TotemRadius = 7f * Tile;
        public const float TotemAngularSpeed = 15f * Tile / 60f / TotemOrbitRadius;
        public const int GiantTail = 80;

        //==================== 死兆星:超新星 ====================

        /// <summary>红巨星压成白色超新星的帧数,1.5 秒后十字星开始</summary>
        public const int NovaCompressFrames = 90;
        public const float NovaRadius = 110f;
        /// <summary>轰炸 8 秒,密度比超星系轰炸高 50%</summary>
        public const int NovaFrames = 480;
        public const int NovaInterval = 5;
        public const int NovaTail = 60;

        //==================== 死兆星:奇点 ====================

        /// <summary>半径每秒涨 2.5 格</summary>
        public const float SingularityGrowth = 2.5f * Tile / 60f;
        public const float SingularityStartRadius = 24f;
        /// <summary>血量已到 1 时涨到 12 格就收缩,否则一直涨到 100 格</summary>
        public const float SingularityEndRadius = 12f * Tile;
        public const float SingularityMaxRadius = 100f * Tile;
        /// <summary>受击判定框边长的上限,黑洞再大也只认中心这一块</summary>
        public const float SingularityHitboxMax = 640f;

        //==================== 表现 ====================

        public const float SkyFadePerTick = 0.02f;
        /// <summary>头顶虹光环离本体中心的高度</summary>
        public const float HaloHeight = 120f;
        /// <summary>退入背景时的绘制倍率</summary>
        public const float DepthScale = 0.5f;

        /// <summary>已经过的帧数在区间里的进度,钳到 0..1</summary>
        public static float Progress(int timer, int from, int length)
            => length <= 0 ? 1f : MathHelper.Clamp((timer - from) / (float)length, 0f, 1f);

        public static float EaseOut(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - (1f - t) * (1f - t);
        }

        public static float EaseIn(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t;
        }

        public static float Smooth(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>把点钳进引力场内侧,距圈心不超过半径减 margin</summary>
        public static Vector2 ClampToArena(VSlimeStateContext ctx, Vector2 point, float margin = ArenaAnchorMargin) {
            if (!ctx.ArenaActive) {
                return point;
            }
            Vector2 diff = point - ctx.ArenaCenter;
            float limit = Math.Max(0f, ctx.ArenaRadius - margin);
            float dist = diff.Length();
            return dist <= limit ? point : ctx.ArenaCenter + diff / dist * limit;
        }

        /// <summary>五角星第 index 个顶点,0 号在正上方,按画星的顺序每次跳两个角</summary>
        public static Vector2 PentagramVertex(Vector2 center, int index) {
            float angle = -MathHelper.PiOver2 + MathHelper.TwoPi * 2f / 5f * index;
            return center + angle.ToRotationVector2() * DevourStarRadius;
        }

        /// <summary>第 index 颗环绕大辉星相对本体中心的位置,绘制和出手用同一个 Timer</summary>
        public static Vector2 OrbitStarOffset(int index, int timer)
            => (timer * BigStarOrbitSpeed + MathHelper.TwoPi * index / BlastStars).ToRotationVector2() * BigStarOrbitRadius;

        /// <summary>第 index 个图腾的圆心,基准角由状态按 Timer 声明</summary>
        public static Vector2 TotemCenter(Vector2 arenaCenter, float baseAngle, int index)
            => arenaCenter + (baseAngle + MathHelper.TwoPi * index / TotemCount).ToRotationVector2() * TotemOrbitRadius;
    }
}
