using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.Core
{
    /// <summary>
    /// 虚空驱逐舰调参中心:全部数字、全部阶段档位都从这里出,状态里不许出现裸数字。
    /// 策划表以大师 ×3 显示值书写,代码存普通模式基值;弹幕伤害经 <see cref="VoidDestroyer.ProjDamage"/> 折算。
    /// 身份:月后 T2 的虚空战舰,三阶段(75% 变形展翼 / 30% 护盾 + 压轴主炮)。
    /// 节奏语法(实机反馈 2026-09-17 后定稿):快 = 每招内部紧、就位达标即跳拍、收招不拖;
    /// 但每一手之间必有连接段三拍(落定 → 重瞄 → 起势),每招第一拍必是可读的前摇,单招不超 8 秒。快不等于乱
    /// </summary>
    public static class VDDirector
    {
        //==================== 基础数值 ====================

        public const int BaseLife = 294000;
        public const int BaseDamage = 116;
        public const int BaseDefense = 80;
        /// <summary>接触伤害的大师显示值,弹幕伤害按它折算</summary>
        public const int MasterContactDamage = BaseDamage * 3;
        public const float DRPhase12 = 0.12f;
        public const float DRPhase3 = 0.25f;
        /// <summary>75%:变形展翼</summary>
        public const float Phase2LifeRatio = 0.75f;
        /// <summary>30%:护盾 + 压轴解锁</summary>
        public const float Phase3LifeRatio = 0.30f;
        /// <summary>虚空之触承伤系数</summary>
        public const float VoidTouchDR = 0.8f;
        /// <summary>目标失效多久后消失</summary>
        public const int NoTargetDespawnFrames = 190;
        /// <summary>目标感知距离(超出视为失效,走撤离)</summary>
        public const float MaxFindDistance = 6400f;

        //==================== 场地与闪现 ====================

        /// <summary>限制圈半径:200 格,圆心随本体</summary>
        public const float ArenaRadius = 200f * 16f;
        /// <summary>出圈回拉基础力与每 400px 超出量的增量;沿拉回方向的速度封顶 28</summary>
        public const float ArenaPullBase = 0.35f;
        public const float ArenaPullPer400 = 1f;
        public const float ArenaPullMax = 1.6f;
        public const float ArenaInwardSpeedCap = 28f;
        /// <summary>切技传送点到玩家的对角距离:约 30 格</summary>
        public const float SwitchTeleportOffset = 340f;
        /// <summary>闪现总帧数:前半淡出,过半换位,后半淡入</summary>
        public const int BlinkDuration = 16;
        /// <summary>传送落地后的无接触窗口(防落地贴脸秒杀)</summary>
        public const int PostTeleportGrace = 30;

        //==================== 演出 ====================

        /// <summary>出场:0-60 门开,60-180 下滑淡入,180-220 门关,220-260 停顿</summary>
        public const int EntranceDuration = 260;
        public const int EntrancePortalOpen = 60;
        public const int EntranceDescendEnd = 180;
        public const int EntrancePortalClose = 220;
        public const int EntranceCameraFrames = 200;
        /// <summary>变形 132 帧,84 帧爆闪换贴图</summary>
        public const int TransformDuration = 132;
        public const int TransformBurstFrame = 84;
        /// <summary>护盾展开连接段:清弹 + 盾亮 + 暗角轻压,给压轴一个干净起点</summary>
        public const int ShieldUpDuration = 45;
        /// <summary>死亡:0-150 逐级爆炸,150-210 门开缩入,210-290 门关,330 真死</summary>
        public const int DeathDuration = 330;
        public const int DeathExplosionEnd = 150;
        public const int DeathPortalIn = 210;
        /// <summary>死亡真死前给客户端的容差:客户端计时可能落后几帧,免得收到击杀包时把自己救活</summary>
        public const int DeathKillTolerance = 10;

        //==================== 节奏(实机反馈 2026-09-17:原 14/10/6 帧连接段 + 十几帧冷却让每手之间只剩 0.2 秒,整场读成乱)====================

        /// <summary>
        /// hub 连接段总长(P1/P2/P3):落定 → 重瞄 → 起势三拍。0.8/0.7/0.6 秒是「看清本体停下、看到它准备出手」的下限,
        /// 冷却并入这里不再单独计
        /// </summary>
        public static int ConnectorFrames(int phase) => phase >= 3 ? 36 : phase >= 2 ? 42 : 48;
        /// <summary>拍一「落定」:换位闪现(16 帧)在这一拍里做完,落地即刹停</summary>
        public const int ConnectorSettleFrames = 16;
        /// <summary>拍三「起势」:能量翼张开 + 核心亮起 + 低音,全招通用的「要出手了」信号;12 帧是能被读到又不拖的长度</summary>
        public const int ConnectorPostureFrames = 12;
        /// <summary>连接段开头允许闪现的最小距离:新招锚点在此距离内就飞过去(24px/f 飞 900px 约 38 帧,连接段够用),不闪</summary>
        public const float ConnectorBlinkDistance = 900f;
        /// <summary>连接段飞行速度</summary>
        public const float ConnectorFlySpeed = 24f;
        /// <summary>没有专属锚点的招起手时的通用悬停点:玩家斜上方(SideDir*300, -260),看得见、够不着</summary>
        public static readonly Vector2 ConnectorDefaultAnchor = new Vector2(300f, -260f);
        /// <summary>杂波阀:连接段末尾本 Boss 存活敌对弹幕超过此数就再等(新招不在上一招的弹雨里起手),最多再等 60 帧</summary>
        public const int ClutterThreshold = 10;
        public const int ClutterWaitMax = 60;
        /// <summary>危险档前摇下限(帧):弹幕/区域 30、接触/冲刺 36、射线 60。各招自己的 Windup 常量不得低于对应档</summary>
        public const int WindupBarrage = 30;
        public const int WindupContact = 36;
        public const int WindupBeam = 60;
        /// <summary>通用收招拍:刹停 + 核心熄灭,再回 hub</summary>
        public const int RecoveryFrames = 24;
        /// <summary>单招时长上限(主炮除外):超的剪,长招是「乱」的另一半来源</summary>
        public const int AttackDurationCap = 60 * 8;
        /// <summary>任何攻击状态的总龄上限:超过强制收招(正常收招远早于此)</summary>
        public const int AttackTimeoutFrames = 60 * 20;
        /// <summary>防复读历史长度:同招至少间隔 3 手</summary>
        public const int RecentHistoryLength = 3;
        /// <summary>替补阀:玩家拉远到此距离,Dash 槽位用传送逼近的幻影冲刺而不是围绕玩家开门的舰队</summary>
        public const float FarDashDistance = 1200f;
        /// <summary>沿表寻找合法招的最大步数(表长以内必有解)</summary>
        public const int RotationSearchSteps = 16;

        //==================== 通用弹幕(大师显示值)====================

        public const int DmgContact = MasterContactDamage;
        public const int DmgVoidBolt = 348;
        public const int DmgCoreLaser = 348;
        public const int DmgMissile = 312;
        public const int DmgNuke = 480;
        public const int DmgSporeLaser = 372;
        public const int DmgRedRay = 396;
        public const int DmgTrident = 450;
        public const int DmgHoloBeast = 432;
        public const int DmgStinger = 276;
        public const int DmgRiftSeam = 420;
        public const int DmgVoidPillar = 460;
        public const int DmgSingularityCore = 400;
        /// <summary>幻影舰 = 接触的 60%(幻影是读破绽的代价,不是真身)</summary>
        public const int DmgPhantomShip = 210;
        public const int DmgAnnihilationBeam = 520;

        //==================== 弧形火球(斜上逼近 + 三轮五发)====================

        /// <summary>悬停点:玩家斜上方(SideDir*400, -300)</summary>
        public static readonly Vector2 ArcHoverOffset = new Vector2(400f, -300f);
        public const float ArcApproachSpeed = 26f;
        public const float ArcApproachAccel = 0.14f;
        public const float ArcApproachSlow = 90f;
        /// <summary>就位上限 45 帧,15 帧后离目标 60px 内即跳拍</summary>
        public const int ArcApproachMax = 45;
        public const int ArcApproachMin = 15;
        public const float ArcArriveDist = 60f;
        public const float ArcHoldSpeed = 10f;
        public const float ArcHoldAccel = 0.05f;
        public const float ArcHoldSlow = 200f;
        /// <summary>到位后的蓄力前摇(汇聚 + 核心 + 翼张)再首轮:弹幕档 30</summary>
        public const int ArcChargeFrames = WindupBarrage;
        public const int ArcVolleyInterval = 30;
        public const int ArcVolleys = 3;
        public const float ArcBoltSpeed = 14f;
        /// <summary>两侧四发的张角(度)</summary>
        public const float ArcInnerDeg = 25f;
        public const float ArcOuterDeg = 50f;
        /// <summary>末轮后收招拍</summary>
        public const int ArcTail = RecoveryFrames;

        //==================== 追踪导弹(蓄力 + 激光流 + 导弹环 + 核弹)====================

        public static readonly Vector2 MissileHoverOffset = new Vector2(0f, -260f);
        public const float MissileHoverSpeed = 6f;
        public const float MissileHoverAccel = 0.05f;
        public const float MissileHoverSlow = 150f;
        /// <summary>蓄力 60:射线档前摇,核心汇聚可读</summary>
        public const int MissileChargeFrames = WindupBeam;
        /// <summary>激光流 60~150(旧到 180:与第二轮导弹环叠成一团)</summary>
        public const int MissileLaserEnd = 150;
        public const int MissileLaserInterval = 4;
        public const float MissileLaserSpeed = 22f;
        public const float MissileLaserJitter = 0.05f;
        public static int MissileVolleys(int phase) => phase >= 2 ? 4 : 3;
        public static int MissileVolleyInterval(int phase) => phase >= 2 ? 90 : 100;
        public static int MissileRingCount(int phase) => phase >= 2 ? 12 : 8;
        public const float MissileRingRadius = 40f;
        public const float MissileRingSpeed = 10f;
        /// <summary>核弹在末轮后 40 帧出手;出手前 6 帧粒子静默</summary>
        public const int MissileNukeDelay = 40;
        public const int MissileNukeSilence = 6;
        public const float MissileNukeSpeed = 6f;
        public const float MissileNukeSideSpeed = 7f;
        /// <summary>核弹出手后 50 帧收招(旧 70,核弹自己有 3 秒引信,本体不必陪着)</summary>
        public const int MissileTail = 50;

        //==================== 幻影冲刺(门后待机 + 连冲)====================

        /// <summary>连冲次数:P1 三段,P2 起四段(旧五段:五次传送把一招读成闪烁)</summary>
        public static int PhantomDashes(int phase) => phase >= 2 ? 4 : 3;
        public const float PhantomDashSpeed = 40f;
        public const int PhantomDashFrames = 30;
        public const int PhantomFadeFrames = 6;
        public const float PhantomPortalOffset = 480f;
        public const int PhantomPortalLife = 66;
        /// <summary>门后待机 36 帧:接触档前摇,门开即预告</summary>
        public const int PhantomWaitFrames = WindupContact;
        /// <summary>冲完先硬刹 10 帧再淡出:冲刺 → 急停 → 消失,不是冲完即闪</summary>
        public const int PhantomBrakeFrames = 10;
        /// <summary>P2 起沿冲刺路径留加速虚空弹的间隔</summary>
        public const int PhantomTrailInterval = 6;
        public const float PhantomTrailSpeed = 2f;
        public const int PhantomEndFade = 8;
        /// <summary>接触伤害的速度门槛(冲刺段才开窗)</summary>
        public const float PhantomContactSpeed = 20f;

        //==================== 虚空火焰(压到玩家下方向下扇散)====================

        public static readonly Vector2 FlameOffset = new Vector2(0f, 320f);
        public const float FlameApproachSpeed = 30f;
        public const float FlameApproachAccel = 0.15f;
        public const float FlameApproachSlow = 100f;
        public const int FlameApproachFrames = 30;
        public const float FlameHoldStiffness = 0.12f;
        public const float FlameHoldLerp = 0.3f;
        public const float FlameHoldMaxSpeed = 34f;
        /// <summary>轮间隔 75(旧 90),轮数 P1/P2 4、P3 5(旧 6):总长从 9 秒压到 5.5 秒</summary>
        public const int FlameVolleyInterval = 75;
        public static int FlameVolleys(int phase) => phase >= 3 ? 5 : 4;
        public const int FlameCountMin = 9;
        public const int FlameCountMax = 12;
        public const float FlameSpeedMin = 8f;
        public const float FlameSpeedMax = 12f;
        public const float FlameSpread = 1.05f;
        public const int FlameRiseDelay = 20;
        /// <summary>每轮出手前 30 帧的向下锥形预告:弹幕档前摇</summary>
        public const int FlameTelegraph = WindupBarrage;
        /// <summary>末轮后收招拍</summary>
        public const int FlameTail = RecoveryFrames;

        //==================== 相位激光(无人机纵列 + 井字网)====================

        /// <summary>纵列数 P1 7、P2 起 8(旧 10/11:十列 45 帧一列就是 7.5 秒的同一件事)</summary>
        public static int LaserColumns(int phase) => phase >= 2 ? 8 : 7;
        public static readonly Vector2 LaserHoverOffset = new Vector2(0f, -400f);
        public const int LaserDecorDrones = 12;
        /// <summary>装饰无人机绕本体 36 帧再闪现:接触档前摇长度,无人机环就是预告</summary>
        public const int LaserDecorFrames = WindupContact;
        public const float LaserColumnX = 35f * 16f;
        public const int LaserColumnDrones = 9;
        public const float LaserDroneSpacing = 48f;
        public const int LaserWarnTime = 60;
        public const float LaserColumnLength = 2400f;
        /// <summary>末列到井字网之间的静默 60 帧:井字是这招的重拍,前面要空一口</summary>
        public const int LaserColumnsToGrid = 60;
        public const int LaserGridRows = 9;
        public const int LaserGridCols = 13;
        public const float LaserGridOffset = 600f;
        public const float LaserGridLength = 1200f;
        public const int LaserGridTail = 110;
        /// <summary>第 k 列的出现时刻:P1 固定 40 帧;P2 起间隔从 40 线性收缩到约 29</summary>
        public static int LaserColumnTime(int k, bool ex) => ex ? (int)(40f * k - 0.75f * k * (k - 1)) : 40 * k;

        //==================== 传送火弹(四角依次闪现扇射)====================

        /// <summary>四角顺序:左上 → 左下 → 右下 → 右上</summary>
        public static readonly int[] TeleportFireOrder = { 0, 2, 3, 1 };
        /// <summary>角数 P2 3、P3 4(旧恒 4:四次传送 + 落地 8 帧就射,读成传送刷屏)</summary>
        public static int TeleportFireCorners(int phase) => phase >= 3 ? 4 : 3;
        public const float TeleportFireOffset = 480f;
        /// <summary>落地后 18 帧核心蓄力再射(旧 8)</summary>
        public const int TeleportFireShotFrame = 18;
        public const int TeleportFireBolts = 5;
        public const float TeleportFireSpreadDeg = 15f;
        public const float TeleportFireBoltSpeed = 7f;
        /// <summary>每角 42 帧(旧 30):射完还能看见它停在角上</summary>
        public const int TeleportFireCornerFrames = 42;
        /// <summary>末角后收招拍</summary>
        public const int TeleportFireTail = RecoveryFrames;

        //==================== 支援投送 ====================

        public static readonly Vector2 ReinforceHoverOffset = new Vector2(0f, -500f);
        public const float ReinforceHoldStiffness = 0.06f;
        public const float ReinforceHoldLerp = 0.2f;
        public const float ReinforceHoldMaxSpeed = 20f;
        /// <summary>20 帧先开地面门,44 帧教徒才落下:门即预告(旧门与教徒同帧出现)</summary>
        public const int ReinforcePortalFrame = 20;
        public const int ReinforceSpawnFrame = 44;
        /// <summary>收招</summary>
        public const int ReinforceDuration = 100;
        public const float ReinforceSpreadX = 400f;
        public const int ReinforcePortalLife = 70;
        /// <summary>场上前卫教徒上限,防止连续投送堆积</summary>
        public const int MaxVanguards = 8;
        public static int ReinforceCount() => (Main.masterMode ? 4 : Main.expertMode ? 3 : 2) * (Main.zenithWorld ? 3 : 1);
        /// <summary>找地面向下扫的格数</summary>
        public const int GroundScanTiles = 60;

        //==================== 红色地狱(全息红恶魔 + 红射线 + 三叉戟)====================

        public const int RedHellRounds = 3;
        public const int RedHellCycle = 170;
        public const int RedHellRayWarn = 30;
        public const int RedHellRayDuration = 45;
        public const float RedHellRayLength = 3000f;
        /// <summary>三叉戟第一排 60、第二排 80(旧 70:两排几乎同帧,读成一坨),FTW 第三排 95</summary>
        public const int RedHellTridentFrame1 = 60;
        public const int RedHellTridentFrame2 = 80;
        public const int RedHellTridentFrameFTW = 95;
        public static int RedHellTridents(int phase) => phase >= 3 ? 8 : 7;
        public static float RedHellArcDeg(int phase) => phase >= 3 ? 140f : 120f;
        public const float RedHellTridentSpeed = 8f;
        public const float RedHellDevilOffset = 480f;
        public static readonly Vector2 RedHellHoverOffset = new Vector2(0f, -480f);
        public const float RedHellHoldStiffness = 0.1f;
        public const float RedHellHoldLerp = 0.3f;
        public const float RedHellHoldMaxSpeed = 36f;
        public const int RedHellTail = 40;

        //==================== 绿色丛林(全息陆龟)====================

        public static readonly Vector2 JungleHoverOffset = new Vector2(0f, -400f);
        /// <summary>陆龟放出前 30 帧蓄力:弹幕档前摇</summary>
        public const int JungleSpawnFrame = WindupBarrage;
        /// <summary>陆龟冲刺次数 P1/P2 4、P3 5(旧 6/7:十二秒的同一招)</summary>
        public static int JungleDashes(int phase) => phase >= 3 ? 5 : 4;
        public const int JungleTail = 60;

        //==================== 蓝色天空(全息小白龙 + 形状弹幕)====================

        /// <summary>小白龙 30 帧蓄力放出:弹幕档前摇(旧 20)</summary>
        public const int SkyWyvernFrame = WindupBarrage;
        public const int SkyBurstStart = 60;
        /// <summary>形状弹持续 420(旧 720),间隔 36/30(旧 30/24):十四秒压到九秒,每发之间看得清形状</summary>
        public const int SkyBurstDuration = 420;
        public static int SkyBurstInterval(int phase) => phase >= 3 ? 30 : 36;
        public const int SkyBurstCount = 60;
        public const float SkyBurstRadius = 120f;
        public const float SkyBurstSpeed = 6f;
        public const int SkyTail = 60;

        //==================== 裂隙斩(空间缝线预告 → 拉开 → 猛合喷弹)====================

        /// <summary>缝线可见到开口的预告帧数:36(旧 24 太短,缝过的是预测点,玩家要有时间离开它)</summary>
        public const int RiftAimFrames = WindupContact;
        /// <summary>缝出现前本体先「抬手」12 帧(翼张 + 核心亮):挥砍的前摇</summary>
        public const int RiftRaiseFrames = 12;
        /// <summary>开口帧数 = 判定窗</summary>
        public const int RiftOpenFrames = 12;
        /// <summary>猛合帧数(合上时两侧喷弹)</summary>
        public const int RiftCloseFrames = 8;
        /// <summary>缝与缝之间的间隔</summary>
        public const int RiftGap = 10;
        /// <summary>半长 900:一道缝横贯一屏</summary>
        public const float RiftHalfLength = 900f;
        /// <summary>开口判定厚度</summary>
        public const float RiftWidth = 28f;
        /// <summary>猛合时每侧喷弹数与初速</summary>
        public const int RiftBoltsPerSide = 6;
        public const float RiftBoltSpeed = 9f;
        /// <summary>缝数:P1 两道过预测点,P2 三道米字,P3 六边形笼 + 中心一刀</summary>
        public static int RiftSeams(int phase) => phase >= 3 ? 7 : phase >= 2 ? 3 : 2;
        /// <summary>P3 六边形笼半径(边线到玩家)</summary>
        public const float RiftCageRadius = 380f;
        /// <summary>预测提前量</summary>
        public const float RiftPredictLead = 14f;
        public static readonly Vector2 RiftHoverOffset = new Vector2(300f, -260f);
        public const int RiftTail = 16;

        //==================== 轨道轰炸(退入背景 → 标记 → 光柱砸落 → 俯冲归位)====================

        public const int OrbitalAscendFrames = 30;
        /// <summary>退入背景起手的后仰反冲(px/帧,朝远离玩家方向)</summary>
        public const float OrbitalAscendRecoil = 6f;
        /// <summary>落点标记 50 帧(旧 40)</summary>
        public const int OrbitalMarkFrames = 50;
        public static int OrbitalPillars(int phase) => phase >= 3 ? 5 : 4;
        /// <summary>柱子错拍间隔 10 帧(旧 8):玩家能一根根穿</summary>
        public const int OrbitalPillarStagger = 10;
        public const float OrbitalPillarWidth = 120f;
        /// <summary>柱子出现 6 帧后开判定</summary>
        public const int OrbitalPillarDamageDelay = 6;
        public const int OrbitalPillarLife = 40;
        public const float OrbitalPillarLength = 2600f;
        /// <summary>P3 柱子缓慢横扫的速度</summary>
        public static float OrbitalPillarSweep(int phase) => phase >= 3 ? 1.6f : 0f;
        /// <summary>落点沿玩家移动方向排布,相邻间距 160px</summary>
        public const float OrbitalMarkSpacing = 160f;
        public const int OrbitalReturnFrames = 20;
        public const float OrbitalReturnShake = 9f;
        public static readonly Vector2 OrbitalHoverOffset = new Vector2(0f, -380f);
        /// <summary>背景深处的绘制缩放</summary>
        public const float OrbitalFarScale = 0.45f;

        //==================== 虚空奇点(后撤蓄力 → 放出 → 牵引 → 塌缩环爆)====================

        public const int SingChargeFrames = 45;
        /// <summary>放出前 8 帧死向:预告即承诺</summary>
        public const int SingLockLead = 8;
        public const float SingReelDistance = 220f;
        public const float SingLaunchSpeed = 9f;
        public const int SingActiveFrames = 150;
        /// <summary>牵引加速度(px/帧²)与朝奇点方向的速度封顶:翼/坐骑能逃</summary>
        public static float SingPullAccel(int phase) => phase >= 3 ? 0.7f : 0.55f;
        public const float SingPullMaxSpeed = 7f;
        public const float SingPullRadius = 900f;
        public const float SingCoreRadius = 34f;
        public const float SingDiskRadius = 140f;
        public const int SingOrbitBoltInterval = 14;
        public const float SingOrbitBoltSpeed = 5f;
        public const int SingCollapseFrames = 20;
        public const int SingBurstCount = 24;
        public const float SingBurstSpeed = 11f;
        public const float SingLensStrength = 0.09f;
        public const float SingLensRadius = 520f;
        public static readonly Vector2 SingHoverOffset = new Vector2(380f, -300f);
        public const int SingTail = 20;
        /// <summary>冲击帧持续帧数(整场唯一一次,P3 奇点塌缩帧)</summary>
        public const int ImpactFrameFrames = 12;

        //==================== 幻影舰队(四门四舰同步齐冲)====================

        public const int FleetFadeFrames = 8;
        public const float FleetPortalRadius = 520f;
        public const int FleetShipCount = 4;
        public const int FleetPortalLife = 70;
        /// <summary>四舰同步瞄准 40 帧(旧 30):读四扇门里哪扇是真身要这么久</summary>
        public const int FleetAimFrames = 40;
        public const float FleetDashSpeed = 38f;
        public const int FleetDashFrames = 34;
        public static int FleetWaves(int phase) => phase >= 2 ? 2 : 1;
        /// <summary>两波之间全员静止 20 帧:段落之间的一口气</summary>
        public const int FleetWaveHold = 20;
        public const int FleetTrailInterval = 6;
        public const int FleetEndFade = 8;
        /// <summary>真身门环亮度倍率:破绽要读得出来</summary>
        public const float FleetRealPortalGlow = 1.6f;

        //==================== 湮灭主炮(P3 压轴:锁定 → 出手 → 扫射 → 过热)====================

        public static readonly Vector2 CannonLockOffset = new Vector2(0f, -600f);
        /// <summary>蓄力 75(旧 60):压轴的吸气可以更长,导引线仍在出手前 40 帧亮</summary>
        public const int CannonChargeFrames = 75;
        /// <summary>导引线在出手前 40 帧亮起</summary>
        public const int CannonGuideLead = 40;
        /// <summary>汇聚粒子在 72% 处硬切成静默:尖叫前的吸气</summary>
        public const float CannonSilenceAt = 0.72f;
        public const int CannonSweepFrames = 150;
        public const float CannonSweepDegrees = 100f;
        /// <summary>扫射起点偏离正下方的角度(度),永远从玩家对侧开扫</summary>
        public const float CannonStartOffsetDeg = 50f;
        public const float CannonBeamWidth = 160f;
        public const float CannonBeamLength = 4000f;
        public const float CannonRecoil = 8f;
        public const int CannonRainInterval = 10;
        public const float CannonRainSpeed = 7f;
        public const int CannonOverheatFrames = 60;
        public const float CannonOverheatDrop = 60f;
        public const float CannonVignette = 0.55f;
        public const int CannonTail = 10;

        //==================== 描边(能量逸散:常态底噪 / 蓄力涨起 / 出手爆闪;着色器 VDRimLight,绘制在 VoidDestroyer.Draw)====================

        /// <summary>常态底噪:一阶段只是一圈若有若无的呼吸,随阶段升级抬高,描边是这只舰的固有质感而不只是预警</summary>
        public static float RimIdle(int phase) => phase >= 3 ? RimIdleP3 : phase >= 2 ? RimIdleP2 : RimIdleP1;
        public const float RimIdleP1 = 0.10f;
        public const float RimIdleP2 = 0.18f;
        public const float RimIdleP3 = 0.26f;
        /// <summary>常态呼吸:底噪上叠 ±30% 的慢起伏,角速度 2.2 rad/s 约 2.9 秒一个来回</summary>
        public const float RimBreathAmp = 0.3f;
        public const float RimBreathSpeed = 2.2f;
        /// <summary>CoreGlow 折算成描边强度的系数:20 招的起势/蓄力/出手都在推 CoreGlow,这一个系数就是全招免费覆盖的总闸</summary>
        public const float RimFromCoreGlow = 0.85f;
        /// <summary>强度追踪步长:约 6 帧追上目标,蓄力斜坡不被抹平,状态停止声明时也不闪断</summary>
        public const float RimTrack = 0.18f;
        /// <summary>爆闪每帧衰减:约 10 帧从 1 落到 0.1,爆闪只是一瞬</summary>
        public const float RimFlashFall = 0.80f;
        /// <summary>配色追踪步长:约 10 帧过渡,换招换色不硬切</summary>
        public const float RimColorTrack = 0.10f;
        /// <summary>强度过这个阈值起缘光向热色偏,到 1 全热(「蓄力发红」的通用来源,不靠逐招写死)</summary>
        public const float RimHeatStart = 0.45f;
        /// <summary>爆闪把热色再往纯白推的比例:蓄力是红热,出手是白热</summary>
        public const float RimFlashWhiten = 0.8f;
        /// <summary>默认蓄力热色:烧红的危险色</summary>
        public static readonly Color RimHeatRed = new Color(255, 110, 80);

        /// <summary>外扩叠画抽数:6 抽绕圈,再多 GPU 白花,再少能看出多边形</summary>
        public const int RimTaps = 6;
        /// <summary>外扩基础半径(px,乘绘制缩放):常态 3px 只是贴着轮廓的一层薄晕</summary>
        public const float RimBaseRadius = 3f;
        /// <summary>蓄力满时的外扩半径增量:能量逸散得更远</summary>
        public const float RimChargeRadius = 5f;
        /// <summary>爆闪瞬间的外扩半径增量:整圈猛地炸开一下</summary>
        public const float RimFlashRadius = 9f;
        /// <summary>叠画各抽的亮度分摊:6 抽合成后约 1.2 倍单抽亮度,不糊成一团白</summary>
        public const float RimTapOpacity = 0.2f;
        /// <summary>内缘锐光(压在本体之上那一遍)的亮度</summary>
        public const float RimEdgeOpacity = 0.9f;
        /// <summary>叠画偏移绕圈的角速度(rad/s):逸散的丝在慢慢转</summary>
        public const float RimSpin = 1.6f;
        /// <summary>拖尾风格:偏移沿速度反向拉开的最大长度(px),按速度比例</summary>
        public const float RimStreakLength = 14f;
        /// <summary>拖尾风格:达到最大拖长所需的速度(px/f),与残影门控 18 起点相衔接</summary>
        public const float RimStreakFullSpeed = 30f;
        /// <summary>塌缩风格:蓄力满时半径压到基础半径的这个倍数,贴边</summary>
        public const float RimCollapseMin = 0.25f;

        /// <summary>噪声侵蚀比例:常态碎成丝,蓄力满收成实心带,爆闪时归 0 整圈实心;过热风格常态就几乎不侵蚀</summary>
        public const float RimErodeIdle = 0.85f;
        public const float RimErodeCharge = 0.35f;
        public const float RimErodeOverheat = 0.2f;
        /// <summary>噪声漂移速度(噪声 UV/s):x 慢横流,y 向上逸散</summary>
        public static readonly Vector2 RimNoiseScroll = new Vector2(0.07f, -0.3f);
        /// <summary>过热风格的高频闪烁角速度(rad/s)与幅度</summary>
        public const float RimOverheatFlickerSpeed = 38f;
        public const float RimOverheatFlickerAmp = 0.18f;

        /// <summary>描边主色:全息三招跟各自的投影色,其余按家族取 VDVfx 配色;演出/连接段沿用虚空紫</summary>
        public static Color RimColorFor(VDStateIndex state) {
            switch (state) {
                case VDStateIndex.RedHell:
                    return VDVfx.HellRed;
                case VDStateIndex.GreenJungle:
                    return VDVfx.JungleGreen;
                case VDStateIndex.BlueSky:
                    return VDVfx.SkyBlue;
            }
            switch (VDRotation.FamilyOf(state)) {
                case VDAttackFamily.Finale:
                    return VDVfx.CannonCore;
                case VDAttackFamily.Gravity:
                    return VDVfx.VoidDeep;
                case VDAttackFamily.Dash:
                    return VDVfx.VoidWhite;
                case VDAttackFamily.Barrage:
                    return VDVfx.VoidPink;
                case VDAttackFamily.Zone:
                    return VDVfx.RiftWhite;
                default:
                    return VDVfx.VoidPurple;
            }
        }

        /// <summary>蓄力热色:默认烧红;绿丛林/蓝天空烧成各自的白化色,主炮跟炮芯色,奇点塌缩成冷白,不与招式主色打架</summary>
        public static Color RimHeatColorFor(VDStateIndex state) {
            switch (state) {
                case VDStateIndex.GreenJungle:
                    return Color.Lerp(VDVfx.JungleGreen, Color.White, 0.6f);
                case VDStateIndex.BlueSky:
                    return Color.Lerp(VDVfx.SkyBlue, Color.White, 0.6f);
                case VDStateIndex.AnnihilationCannon:
                    return VDVfx.CannonCore;
                case VDStateIndex.Singularity:
                    return VDVfx.VoidWhite;
                default:
                    return RimHeatRed;
            }
        }

        /// <summary>描边风格按家族分派:引力塌缩、冲刺拖尾、压轴过热,其余往外逸散</summary>
        public static VDRimStyle RimStyleFor(VDStateIndex state) {
            switch (VDRotation.FamilyOf(state)) {
                case VDAttackFamily.Gravity:
                    return VDRimStyle.Collapse;
                case VDAttackFamily.Dash:
                    return VDRimStyle.Streak;
                case VDAttackFamily.Finale:
                    return VDRimStyle.Overheat;
                default:
                    return VDRimStyle.Dissipate;
            }
        }

        //==================== 天幕「轨道封锁」(VDSky / VDSkyDrive 的全部数字;配色在 VDVfx)====================

        /// <summary>存在包络淡入步长(每 tick):基座 opacity,与强度相乘</summary>
        public const float SkyFadeStep = 1f / 60f;
        /// <summary>出场:虚空随门涌入吞掉天空,0→1 用 90 帧(略慢于门开的 60 帧,门开一半天先暗)</summary>
        public const int SkyEntranceFadeFrames = 90;
        /// <summary>死亡:门开缩入(210)起虚空随本体离开,到真死(330)收干</summary>
        public const int SkyDeathFadeStart = DeathPortalIn;
        public const int SkyDeathFadeEnd = DeathDuration;
        /// <summary>撤离:60 帧收干(本体 190 帧才消失,天先走,读成「它放弃了」)</summary>
        public const int SkyDespawnFadeFrames = 60;
        /// <summary>强度跟随上报值的每 tick 最大步长:20 帧内追上,出场/死亡的编排斜坡不会被抹平</summary>
        public const float SkyTrackPerTick = 1f / 20f;
        /// <summary>无人续租(本体消失)时的衰减步长:1 秒收干</summary>
        public const float SkyFallPerTick = 1f / 60f;
        /// <summary>阶段配色平滑(每 tick lerp):约 1.5 秒换完色</summary>
        public const float SkyPhaseLerp = 0.03f;
        /// <summary>星球侵蚀量三档:P1 已被啃四分之一,P3 过半;变形/护盾演出期间肉眼可见它再被啃掉一块</summary>
        public static float SkyErosion(int phase) => phase >= 3 ? 0.70f : phase >= 2 ? 0.45f : 0.25f;
        /// <summary>侵蚀逼近步长(每 tick):0.2 的档差走 120 帧,恰好铺满 132 帧的变形演出</summary>
        public const float SkyErosionPerTick = 1f / 600f;
        /// <summary>拍点闪光衰减(每 tick 乘):约 20 帧回落</summary>
        public const float SkyFlashDecay = 0.88f;
        /// <summary>转阶段 / 主炮出手的整面闪光强度</summary>
        public const float SkyFlashBeat = 1f;
        /// <summary>hub 起势拍的网格轻脉冲(全招通用的「要出手了」信号);觉得太频繁就归零</summary>
        public const float SkyFlashPosture = 0.2f;
        /// <summary>只有这个强度以上的闪光才从本体处扩散冲击环(起势脉冲不出环)</summary>
        public const float SkyFlashRingThreshold = 0.5f;
        /// <summary>冲击环扩散速度(屏高单位 / tick):约 30 帧横过一屏</summary>
        public const float SkyFlashRingSpeed = 0.035f;
        /// <summary>冲击环存活帧数</summary>
        public const int SkyFlashRingLife = 90;
        /// <summary>主炮蓄力通道:无上报时的衰减</summary>
        public const float SkyChargeDecay = 0.9f;
        /// <summary>主炮扫射与过热期间网格保持的能量读数(能量在被花掉,比蓄满时低)</summary>
        public const float SkyCannonSweepCharge = 0.6f;

        /// <summary>星球圆心(屏幕 UV,上方偏右;反重力时 y 翻到下侧)与半径(屏高单位)</summary>
        public static readonly Vector2 SkyPlanetCenter = new Vector2(0.74f, 0.30f);
        public const float SkyPlanetRadius = 0.22f;
        /// <summary>星球随镜头的视差(屏高单位 / 世界像素 ÷ 屏高):整个 200 格场地跑满也只挪 0.12 屏</summary>
        public const float SkyPlanetParallax = 0.02f;
        /// <summary>星球自转(圈 / 秒):50 秒一圈,1080p 下盘心表面约 30px/s,看得出在转又不抢戏(实机反馈 2026-09-18:原先近乎静止像贴图)</summary>
        public const float SkyPlanetSpin = 1f / 50f;
        /// <summary>碎屑环绕行(圈 / 秒):35 秒一圈,比星球快,环上的亮碎块是最先被读到的动态</summary>
        public const float SkyRingSpin = 1f / 35f;
        /// <summary>星野两层视差(远 / 近)与星云视差</summary>
        public const float SkyStarParallaxFar = 0.03f;
        public const float SkyStarParallaxNear = 0.08f;
        public const float SkyNebulaParallax = 0.05f;
        /// <summary>六边形格边长(屏高单位):1080p 下约 60px 一格</summary>
        public const float SkyGridCell = 0.055f;
        /// <summary>网格基础亮度三档:极淡,弹幕永远比它亮</summary>
        public static float SkyGridAlpha(int phase) => phase >= 3 ? 0.08f : phase >= 2 ? 0.055f : 0.03f;
        /// <summary>网格以本体为中心的亮化半径三档(屏高单位)</summary>
        public static float SkyGridBossRadius(int phase) => phase >= 3 ? 0.42f : phase >= 2 ? 0.32f : 0.24f;
        /// <summary>地表环境光向虚空暮色拉的比例(乘可见强度):白天压暗,夜里略提亮,始终看得见脚下</summary>
        public const float SkyTileTintAmount = 0.5f;
    }
}
