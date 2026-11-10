using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core
{
    /// <summary>
    /// 太阳女神的节奏、判定、伤害和招式数值都在这里,状态里只留姿态、亮度和悬停手感这类系数
    /// 数值存普通档基值,专家和大师由原版在血量缩放与命中结算时乘
    /// 招式骨架取自光之女皇,拍点都是状态 Timer 的区间
    /// </summary>
    public static class SunDirector
    {
        //==================== 基础数值 ====================

        /// <summary>驱逐舰 248000 之后的一档,普通 / 专家 / 大师 = 300000 / 600000 / 900000</summary>
        public const int BaseLife = 300000;
        /// <summary>接触伤害只在冲刺窗里开,普通 / 专家 / 大师 = 136 / 272 / 408</summary>
        public const int BaseDamage = 136;
        public const int BaseDefense = 64;
        public const float DRPhase12 = 0.12f;
        public const float DRPhase3 = 0.2f;
        public const float Phase2LifeRatio = 0.66f;
        public const float Phase3LifeRatio = 0.33f;
        public const float MaxFindDistance = 6400f;
        public const int NoTargetDespawnFrames = 150;

        //==================== 伤害档(普通档基值) ====================
        //弹幕 damage 字段存这里的数,命中时原版乘 2 再乘难度倍率 1 / 2 / 3

        public const int DmgBird = 52;
        public const int DmgLance = 58;
        public const int DmgRay = 64;
        public const int DmgFlare = 52;
        public const int DmgSunfall = 84;
        /// <summary>烈阳灼烧时长,单位是帧</summary>
        public const int BurnFrames = 120;

        //==================== 节奏 ====================

        /// <summary>连接段总长,女皇原版是 45 / 20,这里留出起势拍</summary>
        public static int ConnectorFrames(int phase) => phase >= 3 ? 32 : phase >= 2 ? 38 : 46;
        /// <summary>连接段末尾的起势拍,日轮亮起</summary>
        public const int ConnectorPostureFrames = 12;
        public const float ConnectorFlySpeed = 22f;
        /// <summary>新招锚点超过这个距离就化光闪过去</summary>
        public const float ConnectorBlinkDistance = 1300f;
        /// <summary>没有专属锚点的招用这个悬停点,X 乘侧向</summary>
        public static readonly Vector2 DefaultAnchor = new Vector2(180f, -260f);
        /// <summary>场上本体弹幕超过这个数,连接段最多再等 ClutterWaitMax 帧</summary>
        public const int ClutterThreshold = 45;
        public const int ClutterWaitMax = 40;
        public const int AttackTimeoutFrames = 60 * 14;
        /// <summary>化光闪现总帧数,前半淡出,过半换位,后半淡入</summary>
        public const int BlinkDuration = 20;
        /// <summary>闪现落地后的无接触伤害窗</summary>
        public const int PostBlinkGrace = 24;

        //==================== 演出 ====================

        /// <summary>出场 150 帧:0-110 日轮升起本体淡入,110 爆闪展翼,110-150 落定</summary>
        public const int EntranceDuration = 150;
        public const int EntranceBurstFrame = 110;
        public const float EntranceSpawnHeight = 340f;
        /// <summary>转阶段 100 帧:0-50 蓄光,50 爆闪并切阶段,50-100 落定</summary>
        public const int ShiftDuration = 100;
        public const int ShiftBurstFrame = 50;
        /// <summary>死亡 170 帧:0-120 光柱迸裂,120 超新星,120-170 余烬</summary>
        public const int DeathDuration = 170;
        public const int DeathNovaFrame = 120;
        /// <summary>客户端计时可能落后几帧,收到击杀包时不把自己救活</summary>
        public const int DeathKillTolerance = 10;

        //==================== 金乌齐飞(原版棱彩弹) ====================

        public static readonly Vector2 BirdAnchor = new Vector2(190f, -250f);
        /// <summary>弹幕档前摇下限 30 帧</summary>
        public const int BirdWindup = 30;
        public static int BirdCount(int phase) => phase >= 3 ? 15 : phase >= 2 ? 12 : 9;
        public static int BirdInterval(int phase) => phase >= 3 ? 4 : phase >= 2 ? 5 : 6;
        public const int BirdTail = 24;
        public const float BirdLaunchSpeed = 10f;
        /// <summary>起飞扇面半角,围绕正上方摆开</summary>
        public const float BirdFanHalfAngle = 1.25f;
        /// <summary>起飞后这么多帧只减速不追踪</summary>
        public const int BirdGlideFrames = 30;
        /// <summary>追踪窗长度,过窗后只直飞,横移就能甩开</summary>
        public const int BirdHomingFrames = 55;
        public const float BirdTurnRate = 0.05f;
        public static float BirdCruiseSpeed(int phase) => phase >= 3 ? 17f : phase >= 2 ? 16f : 15f;
        public const float BirdMaxSpeed = 20f;
        public const int BirdLife = 190;

        //==================== 曜日冲(原版冲刺) ====================

        public static int DashReps(int phase) => phase >= 3 ? 3 : phase >= 2 ? 2 : 1;
        /// <summary>站位到玩家的横向距离,原版 550</summary>
        public const float DashStandoff = 560f;
        /// <summary>接触档前摇 38 帧,最后 DashLockLead 帧航道冻结</summary>
        public const int DashAimFrames = 38;
        public const int DashLockLead = 12;
        /// <summary>飞行约 1175px 加刹车 210px,收在玩家另一侧 800px 左右,下一冲的站位就在这一侧</summary>
        public const int DashFlyFrames = 26;
        public const int DashBrakeFrames = 14;
        public const int DashCycle = DashAimFrames + DashFlyFrames + DashBrakeFrames;
        public const float DashSpeed = 50f;
        /// <summary>加速到满速用的帧数</summary>
        public const int DashRampFrames = 6;
        public const float DashBrakeDrag = 0.82f;
        /// <summary>速度高于它才有接触伤害</summary>
        public const float DashContactSpeed = 20f;
        public const float DashLaneLength = 1500f;
        /// <summary>航道边线的间距,本体判定框高 120,斜冲时扫过的宽度约 140</summary>
        public const float DashLaneWidth = 140f;
        /// <summary>三阶段第三冲从斜上方起手的站位</summary>
        public static readonly Vector2 DashDiagonalStandoff = new Vector2(420f, -420f);
        /// <summary>二阶段起冲刺沿途每隔这么多帧向两侧撒一对火羽,间距约 250px</summary>
        public const int DashFeatherInterval = 5;
        public const float DashFeatherSpeed = 4.5f;
        public const int DashFeatherLife = 150;
        public const int DashTail = 16;

        //==================== 日冕之舞(原版太阳舞) ====================

        public static readonly Vector2 CoronaAnchor = new Vector2(0f, -300f);
        public const int CoronaWindup = 36;
        public static int CoronaWaves(int phase) => phase >= 3 ? 4 : 3;
        public static int CoronaRays(int phase) => phase >= 3 ? 8 : 6;
        public const int CoronaWaveInterval = 60;
        /// <summary>细线预告帧数,射线档前摇下限 36</summary>
        public const int RayTelegraphFrames = 36;
        public const int RayActiveFrames = 78;
        public const int RayFadeFrames = 14;
        public const int RayLife = RayTelegraphFrames + RayActiveFrames + RayFadeFrames;
        /// <summary>每波光芒在存活期内的总转角,原版是 π/9</summary>
        public const float RaySweep = MathHelper.Pi / 9f;
        public const float RayLength = 1000f;
        /// <summary>判定半宽 15,可见亮体半宽约 23,判定藏在可见体里</summary>
        public const float RayHitHalfWidth = 15f;
        public const float RayDrawHalfWidth = 23f;
        public const int CoronaTail = 16;
        /// <summary>舞蹈期间朝玩家缓慢漂移的速度上限</summary>
        public const float CoronaDriftSpeed = 3f;

        //==================== 日矛·追猎(原版以太长枪) ====================

        public static readonly Vector2 LanceAnchor = new Vector2(0f, -330f);
        public const int LanceWindup = 30;
        public static int LanceCount(int phase) => phase >= 3 ? 22 : phase >= 2 ? 18 : 14;
        public static int LanceInterval(int phase) => phase >= 3 ? 4 : phase >= 2 ? 5 : 6;
        /// <summary>长枪出生点到玩家的距离,原版 300 到 450</summary>
        public const float LanceSpawnRadius = 380f;
        /// <summary>相邻两枪的方位步进,取无理比让方位铺开</summary>
        public const float LanceAngleStep = 2.4f;
        /// <summary>航线预告帧数,出生即锁向</summary>
        public const int LanceTelegraphFrames = 50;
        public const float LanceSpeed = 38f;
        public const int LanceFlyFrames = 70;
        /// <summary>奇数枪瞄预判点,提前量按预告帧数算,上限这么多像素</summary>
        public const float LanceLeadMax = 420f;
        public const int LanceTail = 40;
        public const float LanceHitHalfWidth = 7f;
        public const float LanceHitLength = 90f;

        //==================== 日珥绽放(原版永恒彩虹) ====================

        public static readonly Vector2 BloomAnchor = new Vector2(0f, -300f);
        public static int BloomPetals(int phase) => phase >= 3 ? 13 : phase >= 2 ? 11 : 9;
        /// <summary>花瓣弹生成拍,生成后先画弧线预告</summary>
        public const int BloomSpawnBeat = 6;
        public const int BloomPetalDelay = 34;
        /// <summary>二阶段起的第二圈,等第一圈扫过玩家身位再出</summary>
        public const int BloomSecondBeat = 78;
        public const float BloomPetalSpeed = 9f;
        /// <summary>花瓣圈半径 600,角速度 = 速度 / 半径</summary>
        public const float BloomPetalRadius = 600f;
        public const int BloomPetalFlyFrames = 200;
        public const int BloomTrailPoints = 36;
        /// <summary>火带判定半宽 9,亮体半宽从头部的 12 收到判定段末尾的 9,再往后的尾段不判定</summary>
        public const float BloomHitHalfWidth = 9f;
        public static int BloomDuration(int phase) => phase >= 2 ? 150 : 82;

        //==================== 日矛·天罗(原版二阶段枪墙) ====================

        public static readonly Vector2 WallAnchor = new Vector2(0f, -360f);
        public const int WallWindup = 30;
        public static int WallWaves(int phase) => phase >= 3 ? 5 : 4;
        public const int WallWaveInterval = 46;
        public const int WallLances = 11;
        public static float WallSpacing(int phase) => phase >= 3 ? 150f : 170f;
        /// <summary>枪墙到玩家的距离</summary>
        public const float WallDistance = 760f;
        public const int WallTail = 70;

        //==================== 坠日(二阶段签名招) ====================

        public static readonly Vector2 SunfallAnchor = new Vector2(260f, -300f);
        public const int SunfallOrbBeat = 4;
        /// <summary>落点锁定拍,之后还有 24 帧蓄力加 22 帧飞行可以离开</summary>
        public const int SunfallLockBeat = 46;
        public const int SunfallThrowBeat = 70;
        public const int SunfallFlightFrames = 22;
        public const float SunfallBlastRadius = 170f;
        public const int SunfallBlastFrames = 8;
        public const int SunfallRingBolts = 18;
        public const float SunfallRingSpeed = 7f;
        public const float SunfallSecondRingSpeed = 5.5f;
        public const int SunfallSecondRingDelay = 12;
        public const int SunfallDuration = 150;
        /// <summary>日球悬在头顶的高度</summary>
        public const float SunfallOrbHeight = 120f;

        //==================== 炎阳天轮(三阶段签名招) ====================

        public static readonly Vector2 StormAnchor = new Vector2(0f, -300f);
        public const int StormWindup = 50;
        /// <summary>光芒生成拍,生成后到前摇结束都是细线预告</summary>
        public const int StormRayBeat = 6;
        public const int StormActiveFrames = 240;
        public const int StormRays = 4;
        /// <summary>每帧转 0.6 度,半径 400 处的切向速度约 4.2px</summary>
        public const float StormAngularSpeed = 0.0105f;
        /// <summary>转速从 0 爬到满值的帧数,预告位置就是起转位置</summary>
        public const int StormSpinUp = 30;
        public const float StormRayLength = 1300f;
        public const int StormRingInterval = 44;
        /// <summary>第一圈火羽晚于光芒点亮的帧数</summary>
        public const int StormFirstRingDelay = 20;
        /// <summary>火羽飞到光芒末端附近就消失</summary>
        public const int StormRingBoltLife = 210;
        public const int StormRingBolts = 14;
        public const float StormRingSpeed = 5.5f;
        public const int StormTail = 40;

        //==================== 火羽弹 ====================

        public const int FlareBoltLife = 240;

        //==================== 表现 ====================

        /// <summary>左翼贴图中心相对身体中心的偏移,由参考图量得,右翼镜像</summary>
        public static readonly Vector2 WingOffset = new Vector2(-61f, -45f);
        /// <summary>翼根在左翼贴图里的像素坐标,翼绕它转</summary>
        public static readonly Vector2 WingPivot = new Vector2(112f, 140f);
        /// <summary>左臂帧中心相对身体中心的偏移,由参考图量得</summary>
        public static readonly Vector2 ArmOffset = new Vector2(-5f, -13f);
        public const int ArmFrames = 7;
        /// <summary>平举施法时手掌相对身体中心的位置,X 乘侧向</summary>
        public static readonly Vector2 HandCastOffset = new Vector2(72f, -39f);
        /// <summary>双臂高举时手掌的位置</summary>
        public static readonly Vector2 HandRaisedOffset = new Vector2(42f, -79f);
        /// <summary>日轮常态亮度,随阶段抬高</summary>
        public static float HaloIdle(int phase) => phase >= 3 ? 0.62f : phase >= 2 ? 0.44f : 0.26f;
        public const float SkyFadePerTick = 0.02f;

        /// <summary>已经过的帧数在区间里的进度,钳到 0..1</summary>
        public static float Progress(int timer, int from, int length)
            => length <= 0 ? 1f : MathHelper.Clamp((timer - from) / (float)length, 0f, 1f);
    }
}