using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core
{
    /// <summary>
    /// 星月贤者的节奏、判定、伤害和招式数值都在这里,状态里只留姿态、亮度和悬停手感这类系数
    /// 数值存普通档基值,专家和大师由原版在血量缩放与命中结算时乘
    /// 招式骨架取自先知,拍点都是状态 Timer 的区间
    /// </summary>
    public static class SageDirector
    {
        //==================== 基础数值 ====================

        /// <summary>和太阳女神同档,普通 / 专家 / 大师 = 280000 / 560000 / 840000</summary>
        public const int BaseLife = 280000;
        /// <summary>接触伤害只在冲刺窗里开</summary>
        public const int BaseDamage = 130;
        public const int BaseDefense = 56;
        public const float DRPhase12 = 0.1f;
        public const float DRPhase3 = 0.18f;
        public const float Phase2LifeRatio = 0.66f;
        public const float Phase3LifeRatio = 0.33f;
        public const float MaxFindDistance = 6400f;
        public const int NoTargetDespawnFrames = 150;

        //==================== 伤害档(普通档基值) ====================
        //弹幕 damage 字段存这里的数,命中时原版乘 2 再乘难度倍率 1 / 2 / 3

        public const int DmgWisp = 50;
        public const int DmgRune = 56;
        public const int DmgCross = 60;
        public const int DmgCrescent = 58;
        public const int DmgMeteor = 62;
        public const int DmgBeam = 90;

        //==================== 节奏 ====================

        public static int ConnectorFrames(int phase) => phase >= 3 ? 30 : phase >= 2 ? 36 : 44;
        /// <summary>连接段末尾的起势拍,月轮亮起</summary>
        public const int ConnectorPostureFrames = 12;
        public const float ConnectorFlySpeed = 20f;
        /// <summary>新招锚点超过这个距离就星遁过去,贤者比女神更爱闪</summary>
        public const float ConnectorBlinkDistance = 900f;
        public static readonly Vector2 DefaultAnchor = new Vector2(300f, -200f);
        public const int ClutterThreshold = 45;
        public const int ClutterWaitMax = 40;
        public const int AttackTimeoutFrames = 60 * 14;
        /// <summary>星遁总帧数,前半淡出,过半换位,后半淡入,落点从第一帧起就有标记</summary>
        public const int BlinkDuration = 24;
        /// <summary>星遁落地后的无接触伤害窗</summary>
        public const int PostBlinkGrace = 24;
        /// <summary>落点离玩家不近于这个距离,太近就沿连线外推</summary>
        public const float BlinkMinPlayerDistance = 300f;
        /// <summary>朝向每帧追目标角的比例</summary>
        public const float TurnRate = 0.12f;
        /// <summary>各招起手飞向锚点的默认速度上限</summary>
        public const float HoverSpeed = 12f;

        //==================== 演出 ====================

        /// <summary>出场 150 帧:0-110 星门展开本体淡入,110 爆闪展翼,110-150 落定</summary>
        public const int EntranceDuration = 150;
        public const int EntranceBurstFrame = 110;
        public const float EntranceSpawnHeight = 320f;
        /// <summary>转阶段 100 帧:0-50 聚星,50 爆闪并切阶段,50-100 落定</summary>
        public const int ShiftDuration = 100;
        public const int ShiftBurstFrame = 50;
        /// <summary>死亡 170 帧:0-120 星爆逐个炸开,120 十字星爆,120-170 星尘散尽</summary>
        public const int DeathDuration = 170;
        public const int DeathNovaFrame = 120;
        /// <summary>客户端计时可能落后几帧,收到击杀包时不把自己救活</summary>
        public const int DeathKillTolerance = 10;

        //==================== 星流扇(先知的符文洪流) ====================

        public static readonly Vector2 TorrentAnchor = new Vector2(420f, -160f);
        public static int TorrentBursts(int phase) => phase >= 3 ? 3 : 2;
        /// <summary>每轮先锁向并亮出航线,弹幕档前摇下限 30 帧</summary>
        public const int TorrentTelegraph = 30;
        /// <summary>每轮先刹停这么多帧再锁向,航线从锁向拍亮到第一波出手</summary>
        public const int TorrentLockBeat = 8;
        public const int TorrentVolleys = 4;
        public const int TorrentVolleyInterval = 8;
        public const int TorrentBurstGap = 16;
        public const int TorrentBurstCycle = TorrentTelegraph + TorrentVolleys * TorrentVolleyInterval + TorrentBurstGap;
        /// <summary>偶数轮 5 道正中对准玩家,奇数轮 4 道把玩家夹在正中空隙</summary>
        public static int TorrentLanes(int burst) => burst % 2 == 0 ? 5 : 4;
        public const float TorrentLaneSpread = 0.3f;
        public const float TorrentSpeed = 11f;
        /// <summary>星魂沿航线左右摆动的幅度,相邻两波反相,织成麻花</summary>
        public const float TorrentWeaveAmplitude = 30f;
        public const int TorrentWeavePeriod = 44;
        public const int TorrentLife = 170;
        public const float TorrentLaneLength = 1300f;
        public const int TorrentTail = 30;

        //==================== 彗星冲(先知的冲刺) ====================

        public static int ChargeReps(int phase) => phase >= 2 ? 3 : 2;
        public const float ChargeStandoff = 520f;
        /// <summary>接触档前摇 36 帧,最后 ChargeLockLead 帧航道冻结</summary>
        public const int ChargeAimFrames = 36;
        public const int ChargeLockLead = 12;
        public const int ChargeFlyFrames = 26;
        public const int ChargeBrakeFrames = 12;
        /// <summary>两冲之间星遁到下一个站位的帧数</summary>
        public const int ChargeRepositionFrames = BlinkDuration + 2;
        public const int ChargeCycle = ChargeAimFrames + ChargeFlyFrames + ChargeBrakeFrames + ChargeRepositionFrames;
        public const float ChargeSpeed = 46f;
        public const int ChargeRampFrames = 6;
        public const float ChargeBrakeDrag = 0.8f;
        /// <summary>速度高于它才有接触伤害</summary>
        public const float ChargeContactSpeed = 18f;
        public const float ChargeLaneLength = 1400f;
        /// <summary>航道边线的间距,本体判定框 110,斜冲时扫过的宽度约 155</summary>
        public const float ChargeLaneWidth = 156f;
        /// <summary>相邻两冲站位方位的步进</summary>
        public const float ChargeAngleStep = 2.5f;
        /// <summary>二阶段起冲刺沿途向两侧撒星魂</summary>
        public const int ChargeBoltInterval = 5;
        public const float ChargeBoltSpeed = 4f;
        public const int ChargeBoltLife = 150;
        public const int ChargeTail = 16;

        //==================== 星环(先知的符文球) ====================

        /// <summary>本体绕玩家慢转的半径和角速度</summary>
        public const float RingOrbitRadius = 420f;
        public const float RingOrbitSpeed = 0.012f;
        public const int RingWindup = 36;
        public static int RingCount(int phase) => phase >= 3 ? 12 : phase >= 2 ? 10 : 8;
        public static int RingInterval(int phase) => phase >= 3 ? 7 : phase >= 2 ? 8 : 9;
        /// <summary>星符绕本体的半径</summary>
        public const float RingSlotRadius = 130f;
        public const float RingSlotSpin = 0.045f;
        /// <summary>星符脱环后停住锁向的帧数</summary>
        public const int RingTelegraph = 24;
        public const float RingSpeed = 18f;
        public const int RingFlyFrames = 110;
        /// <summary>奇数星符瞄预判点,提前量上限</summary>
        public const float RingLeadMax = 380f;
        public const float RuneHitRadius = 13f;
        public const int RingTail = 40;

        //==================== 新月斩(先知的符文剑) ====================

        public static readonly Vector2 CrescentAnchor = new Vector2(380f, -120f);
        public static int CrescentVolleys(int phase) => phase >= 2 ? 2 : 1;
        public static int CrescentCount(int phase, int volley) => volley == 0 ? 3 : phase >= 3 ? 4 : 2;
        public const int CrescentSpawnBeat = 6;
        /// <summary>月刃起飞前先画出整条回旋轨迹</summary>
        public const int CrescentTelegraph = 30;
        public const int CrescentVolleyInterval = 60;
        public const float CrescentSpread = 0.5f;
        /// <summary>回旋轨迹的远端距离和横向鼓出的宽度</summary>
        public const float CrescentReach = 640f;
        public const float CrescentBulge = 150f;
        public const int CrescentFlightFrames = 96;
        public const float CrescentHitRadius = 34f;
        public const int CrescentTail = 56;

        //==================== 十字星爆(星月套装的十字星爆) ====================

        public static readonly Vector2 CrossAnchor = new Vector2(380f, -220f);
        public const int CrossWindup = 20;
        public static int CrossWaves(int phase) => phase >= 3 ? 5 : phase >= 2 ? 4 : 3;
        public const int CrossWaveInterval = 34;
        /// <summary>十字标记亮出到爆开的帧数,射线档前摇下限 36</summary>
        public const int CrossTelegraph = 40;
        public const int CrossActiveFrames = 14;
        public const int CrossFadeFrames = 10;
        public const float CrossArmLength = 520f;
        /// <summary>判定半宽 9,可见亮体半宽约 17,判定藏在可见体里</summary>
        public const float CrossHitHalfWidth = 9f;
        public const float CrossDrawHalfWidth = 17f;
        public const int CrossSatellites = 4;
        public const float CrossSatelliteRadius = 340f;
        public const float CrossLeadMax = 380f;
        public const int CrossTail = 50;

        //==================== 流星雨(二阶段签名招) ====================

        public static readonly Vector2 MeteorAnchor = new Vector2(0f, -420f);
        public const int MeteorWindup = 30;
        public static int MeteorWaves(int phase) => phase >= 3 ? 6 : 4;
        public const int MeteorWaveInterval = 40;
        public const int MeteorLanes = 13;
        public static float MeteorSpacing(int phase) => phase >= 3 ? 140f : 160f;
        /// <summary>航线与水平的夹角,左右两个方向逐波交替</summary>
        public const float MeteorAngle = 1.05f;
        /// <summary>流星出生点沿航线退到玩家上方的距离</summary>
        public const float MeteorSpawnDistance = 900f;
        public const int MeteorTelegraph = 40;
        /// <summary>同一波里相邻航线的起飞间隔,整波像雨幕一样扫过去</summary>
        public const int MeteorRipple = 2;
        public const float MeteorSpeed = 30f;
        public const int MeteorFlyFrames = 70;
        public const float MeteorHitRadius = 16f;
        public const int MeteorTail = 60;

        //==================== 满月光炮(三阶段签名招) ====================

        public static readonly Vector2 BeamAnchor = new Vector2(620f, -60f);
        public const int BeamSweeps = 2;
        /// <summary>预告画出起始线和整片扫掠扇面</summary>
        public const int BeamTelegraph = 44;
        /// <summary>点亮后先停这么多帧再开始扫</summary>
        public const int BeamStaticFrames = 16;
        public const int BeamSweepFrames = 74;
        public const int BeamFadeFrames = 12;
        public const int BeamLife = BeamTelegraph + BeamStaticFrames + BeamSweepFrames + BeamFadeFrames;
        public const int BeamGap = 20;
        public const int BeamCycle = BeamLife + BeamGap;
        /// <summary>起始线落在玩家方位的扫掠反方向这么多弧度处,预告期间可以穿到起始线背后</summary>
        public const float BeamStartOffset = 0.42f;
        /// <summary>每帧 0.6 度,半径 600 处的切向速度约 6.3px</summary>
        public const float BeamAngularSpeed = 0.0105f;
        public const float BeamLength = 2400f;
        /// <summary>判定半宽 36,可见光柱半宽 56</summary>
        public const float BeamHitHalfWidth = 36f;
        public const float BeamDrawHalfWidth = 56f;
        public const int BeamTail = 30;

        //==================== 表现 ====================

        /// <summary>嘴部到本体中心的距离,弹幕从这里出</summary>
        public const float MuzzleDistance = 62f;
        /// <summary>月轮常态亮度,随阶段抬高</summary>
        public static float CrestIdle(int phase) => phase >= 3 ? 0.62f : phase >= 2 ? 0.44f : 0.26f;
        /// <summary>月轮的弧长:新月、弦月、满月</summary>
        public static float CrestArc(int phase) => phase >= 3 ? MathHelper.TwoPi : phase >= 2 ? MathHelper.Pi * 1.35f : MathHelper.Pi * 0.8f;
        public const float SkyFadePerTick = 0.02f;

        /// <summary>已经过的帧数在区间里的进度,钳到 0..1</summary>
        public static float Progress(int timer, int from, int length)
            => length <= 0 ? 1f : MathHelper.Clamp((timer - from) / (float)length, 0f, 1f);
    }
}