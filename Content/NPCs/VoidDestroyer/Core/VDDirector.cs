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

        /// <summary>出场 260 帧:0-40 深空跃迁闪光(Z 6 处一枚小门 + 天幕冲击环);40-130 朝镜头飞来(Z 6 → -0.45,立方缓入);130 掠过镜头;130-170 从镜头后拉回平面;170-260 落定威压</summary>
        public const int EntranceDuration = 260;
        public const int EntranceWarpFrames = 40;
        public const int EntranceApproachEnd = 130;
        public const int EntranceArriveFrame = 170;
        public const int EntranceCameraFrames = 200;
        /// <summary>出场起点深度:Z = 6 缩到 0.14,是虚空天幕里一颗会动的星;越过镜头的深度 -0.45 放大到 1.8,正好擦着屏幕上缘飞过</summary>
        public const float EntranceStartDepth = 6f;
        public const float EntrancePassDepth = -0.45f;
        /// <summary>变形 132 帧:0-50 颤抖后撤到 Z 1.2;50-84 远处逐帧变形(小而雾化);84 爆闪换二阶段贴图;84-118 俯冲回平面;118-132 落地展翼</summary>
        public const int TransformDuration = 132;
        public const int TransformRetreatEnd = 50;
        public const int TransformBurstFrame = 84;
        public const int TransformDiveEnd = 118;
        /// <summary>变形后撤深度:Z 1.2 缩到 0.45,退到看得清「它在变」又明显离开了平面的距离</summary>
        public const float TransformRetreatDepth = 1.2f;
        /// <summary>护盾展开连接段:清弹 + 盾亮 + 暗角轻压,给压轴一个干净起点</summary>
        public const int ShieldUpDuration = 45;
        /// <summary>死亡 330 帧:0-150 平面逐级爆炸;150-210 失去动力翻滚着漂进深处(Z 0 → 3);210-290 远处连锁小爆,250 帧全天幕闪光;290-330 收干;330 真死</summary>
        public const int DeathDuration = 330;
        public const int DeathExplosionEnd = 150;
        public const int DeathDriftEnd = 210;
        public const int DeathFinalFlashFrame = 250;
        public const int DeathFadeEnd = 290;
        /// <summary>死亡漂入的深度:Z 3 缩到 0.25,远得像坠向那颗被啃的星球,又近得看清最后一炸</summary>
        public const float DeathDriftDepth = 3f;
        /// <summary>死亡真死前给客户端的容差:客户端计时可能落后几帧,免得收到击杀包时把自己救活</summary>
        public const int DeathKillTolerance = 10;
        /// <summary>撤离:60 帧跃迁遁入 Z 6(与出场起点同一深度,读成「回去了」),透明度另按 NoTargetDespawnFrames 收干</summary>
        public const int DespawnWarpFrames = 60;
        public const float DespawnDepth = 6f;

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

        //==================== 纵深(伪 3D:Z = 0 玩家平面,+Z 越远越深,-Z 朝镜头;数学在 VDDepth,分层在 VDDepthStage / VDDepthRenderHandle)====================

        /// <summary>透视焦距(Z 单位):Scale = Focal / (Focal + Z)。取 1 让 Z=1 恰好半身、Z=3 四分之一,档位口算得出来</summary>
        public const float DepthFocal = 1f;
        /// <summary>近端 Z 钳位:Scale 在 Z → -1 发散,-0.85 时已是 6.7 倍,再近只会是一团糊</summary>
        public const float DepthNearClamp = -0.85f;
        /// <summary>远景层门槛:Z ≥ 0.6(缩到 0.625 以下)才进墙后物块前的远景层;更近的东西还在与玩家同层互动,压到物块后面会读成穿模</summary>
        public const float DepthFarLayerZ = 0.6f;
        /// <summary>近景层门槛:Z ≤ -0.25(放大到 1.33 以上)进玩家之上的近景层,读成「在镜头前掠过」</summary>
        public const float DepthNearLayerZ = -0.25f;
        /// <summary>判定带下限:|Z| ≤ 0.1 才有碰撞;配合速度倍率保证穿越平面至少 3~4 帧有判定,不会帧间擦过</summary>
        public const float DepthHitBandMin = 0.1f;
        /// <summary>判定带按 Z 速度放宽的倍率:1.5 倍单帧 Z 位移,快弹也留得住 3 帧</summary>
        public const float DepthHitBandVelMult = 1.5f;
        /// <summary>近景剪影透明度上限:掠过镜头的东西挡视线,0.45 以上就看不见后面的弹幕</summary>
        public const float DepthNearAlphaMax = 0.45f;
        /// <summary>近景剪影完全淡出的 Z:-0.7 已是 3.3 倍,再大就是一块紫色,直接消失</summary>
        public const float DepthNearFadeZ = -0.7f;
        /// <summary>远端雾化满值的 Z:Z = 3 时雾色占满,再远只剩轮廓</summary>
        public const float DepthFogFullZ = 3f;
        /// <summary>远端透明度地板:再远也留 55%,深空里的东西是暗不是透</summary>
        public const float DepthFarAlphaFloor = 0.55f;
        /// <summary>
        /// 本体深度视觉追踪步长:0.5 = 只落后声明值一帧。贯穿冲刺的 Z 以 -0.05/帧线性走,旧值 0.2 会让绘制落后 0.2 个 Z 单位,
        /// 穿过平面那一帧画出来的船离判定盒差 50px;俯冲与出场的曲线本身就是平滑的,不需要再靠追踪抹
        /// </summary>
        public const float DepthTrack = 0.5f;
        /// <summary>掠过镜头的呼啸触发 Z:-0.35 时放大到 1.54 倍,正是「擦着镜头过去」的一瞬</summary>
        public const float DepthWhooshZ = -0.35f;
        /// <summary>呼啸的震屏与滤镜径向拖影强度</summary>
        public const float WhooshShake = 3f;
        public const float WhooshStreak = 0.6f;
        /// <summary>多普勒:逼近弹的音高从 0.7 升到 1.3,按 Z 从 DepthFogFullZ 到 0 线性</summary>
        public const float DepthDopplerLow = 0.7f;
        public const float DepthDopplerHigh = 1.3f;
        /// <summary>落点标记提前量(帧):Z 弹距平面还有这么多帧时标记开始亮,与弹幕档前摇一致</summary>
        public const int DepthMarkerLeadFrames = WindupBarrage;
        /// <summary>落点标记基础半径(px):比虚空弹判定盒略大一圈,读得出「会落在这里」</summary>
        public const float DepthMarkerRadius = 30f;
        /// <summary>俯冲归位帧数:立方曲线 20 帧从任意深度回平面,「朝镜头飞来」</summary>
        public const int DiveFrames = 20;
        /// <summary>俯冲落地接触窗:落地那 6 帧本体有接触伤害,落点大环从俯冲起手就画</summary>
        public const int DiveContactFrames = 6;
        /// <summary>俯冲落地震屏与落点大环半径(px)</summary>
        public const float DiveShake = 9f;
        public const float DiveMarkerRadius = 120f;
        /// <summary>远景总时长预算:一轮轮换里本体带外(不可攻击)的时间占比上限,策划表自检用,不进代码逻辑</summary>
        public const float FarTimeBudget = 0.30f;
        /// <summary>远端本体经雾化着色器的模糊半径(px,按缩放后尺寸)与热闪幅度</summary>
        public const float DepthFogBlur = 1.5f;
        public const float DepthFogShimmer = 0.006f;

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
        /// <summary>
        /// 纵深回旋火:两侧四发不再平面包裹,而是抛入深处(顶点 Z 1.6,缩到 0.38 成远处绕行的小点)再在锁定点上空回头、
        /// 越来越大地穿过平面命中,再掠过镜头。往返 70 帧,回程最后 30 帧亮落点标记;中弹仍平面直射,给一条能读的基准线
        /// </summary>
        public const float ArcBoomerangApex = 1.6f;
        public const int ArcBoomerangFrames = 70;

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

        //---- 深空导弹群:导弹环先射入深处再重新锁定扑回;核弹抛入深处成一颗星再回落 ----

        /// <summary>导弹环去程 30 帧退到 Z 1.5(缩到 0.4,一圈向消失点收拢的小点),再 40 帧重新锁定扑回平面(越来越大,各带落点小环)</summary>
        public const float MissileDiveDepth = 1.5f;
        public const int MissileOutFrames = 30;
        public const int MissileReturnFrames = 40;
        /// <summary>去程平面速度从环速衰减到的下限:导弹在背景里明显减速,「掉头」读得出来</summary>
        public const float MissileOutMinSpeed = 4f;
        /// <summary>回程转向上限(弧度/帧)与平面速度上限:追得动、甩得掉;回程预测提前量按剩余帧数的一半算</summary>
        public const float MissileReturnTurn = 0.12f;
        public const float MissileReturnMaxSpeed = 24f;
        /// <summary>导弹落点小环半径(px)</summary>
        public const float MissileMarkerRadius = 24f;
        /// <summary>核弹抛物线顶点深度:Z 4 缩到 0.2,引信正中(90 帧)时是背景里的一颗星,再回落到平面按原半径爆炸;爆炸范围圈始终画在平面上</summary>
        public const float NukeApexDepth = 4f;

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

        //---- 三维幻影冲刺(P2 起):平面冲刺与贯穿冲刺交替,贯穿的冲刺向量穿过 Z 轴 ----

        /// <summary>第 k 冲的种类:P1 全平面;P2 起 平面 / 远→近 / 平面 / 近→远 循环。0 平面,1 远门贯穿到镜头后,2 近门缩进平面再遁入深处</summary>
        public static int PhantomDashKind(int phase, int k) => phase < 2 ? 0 : k % 4 == 1 ? 1 : k % 4 == 3 ? 2 : 0;
        /// <summary>贯穿冲刺的深度端点:远门 Z 1.2(缩到 0.45),越过镜头到 -0.6(放大 2.5 倍、剪影已淡到一成);近门开在 -0.45(1.8 倍,屏幕边缘的半透明巨门)</summary>
        public const float PhantomFarDepth = 1.2f;
        public const float PhantomPassDepth = -0.6f;
        public const float PhantomNearDepth = -0.45f;
        /// <summary>贯穿冲刺 36 帧,平面速度 28:远→近在第 24 帧穿过平面(Z 速度 -0.05,判定带 0.1 = 4 帧接触),读成「从平面里浮出来的鲨鱼」;近→远第 10 帧就穿过,大部分时间是在往深处遁</summary>
        public const int PhantomPierceFrames = 36;
        public const float PhantomPierceSpeed = 28f;
        /// <summary>穿越点用玩家预测位置(提前 14 帧):门开那一刻穿越点就钉死,大环从待机起就画</summary>
        public const float PhantomPierceLead = 14f;
        /// <summary>穿越点大环半径(px)</summary>
        public const float PhantomPierceMarkerRadius = 110f;

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

        //---- 越肩火雨(P2 起):本体闪到镜头后方的屏幕下缘,火雨由大缩小落向玩家周围的扇形落点,命中后遁入深处 ----

        /// <summary>P2 起本体所在深度:-0.4 放大 1.67 倍、剪影三成透明,从屏幕下缘升起的巨影;表观位置玩家正下方 360px</summary>
        public const float FlameNearDepth = -0.4f;
        public static readonly Vector2 FlameNearApparent = new Vector2(0f, 360f);
        /// <summary>每发从镜头后飞到平面的帧数(40,标记提前 30 帧亮)与落点扇形:以玩家为心、张角 ±FlameSpread、半径 120~300 随机</summary>
        public const int FlameNearFrames = 40;
        public const float FlameNearLandMin = 120f;
        public const float FlameNearLandMax = 300f;

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
        /// <summary>末列到点阵之间的静默 60 帧:点阵是这招的重拍,前面要空一口</summary>
        public const int LaserColumnsToGrid = 60;
        /// <summary>第 k 列的出现时刻:P1 固定 40 帧;P2 起间隔从 40 线性收缩到约 29</summary>
        public static int LaserColumnTime(int k, bool ex) => ex ? (int)(40f * k - 0.75f * k * (k - 1)) : 40 * k;
        /// <summary>装饰无人机从 Z 2 降入环阵的帧数:装饰即深度预告</summary>
        public const float LaserDecorDepth = 2f;
        public const int LaserDecorDescend = 14;

        //---- 透视点阵(相位激光的重拍):一片无人机停在 Z 1.8 的背景里,各自朝镜头发一道 Z 射线,落点是平面上一格格的圆 ----

        /// <summary>点阵深度:1.8 缩到 0.36,一片小点悬在背景,预警线从它们收敛到脚下的格点</summary>
        public const float LatticeDepth = 1.8f;
        /// <summary>7 × 9 个格点、格距 110:覆盖 660 × 880,人站进格子中央离四周落点各 55px,判定半径 40 留 15px 余量</summary>
        public const int LatticeRows = 7;
        public const int LatticeCols = 9;
        public const float LatticeSpacing = 110f;
        public const float LatticeSpotRadius = 40f;
        /// <summary>每格 60 帧预警(射线档),再 6 帧判定</summary>
        public const int LatticeWarn = WindupBeam;
        public const int LatticeStrikeFrames = 6;
        /// <summary>P3 错半格再来一轮,间隔 30 帧起手</summary>
        public static int LatticePulses(int phase) => phase >= 3 ? 2 : 1;
        public const int LatticePulseGap = 30;
        public const int LatticeTail = 30;

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
        /// <summary>
        /// 立体四角:第 k 角的深度。P2 三角 远 / 平面 / 近,P3 四角 远 / 平面 / 近 / 远。
        /// 远角(Z 1.2)扇射纵深贯穿弹从背景飞来,近角(Z -0.4)扇射越肩弹从镜头后缩进平面,平面角保持直飞;三种来向逼玩家读三种运动签名
        /// </summary>
        public static float TeleportFireDepth(int k) => k % 4 == 0 || k % 4 == 3 ? 1.2f : k % 4 == 2 ? -0.4f : 0f;
        /// <summary>深度角的落点扇:垂直于角→玩家方向排 5 个落点、间距 60,飞行 40 帧(标记提前 30 帧亮)</summary>
        public const float TeleportFireLandSpacing = 60f;
        public const int TeleportFireZFrames = 40;
        public const float TeleportFireZLead = 12f;

        //==================== 支援投送 ====================

        public static readonly Vector2 ReinforceHoverOffset = new Vector2(0f, -500f);
        public const float ReinforceHoldStiffness = 0.06f;
        public const float ReinforceHoldLerp = 0.2f;
        public const float ReinforceHoldMaxSpeed = 20f;
        /// <summary>
        /// 14 帧地面开门 + 投送舱在 Z 2.5 的高空出现,44 帧舱落地释放教徒:30 帧坠落(弹幕档前摇),舱越来越大地落向门,
        /// 地面门本身就是落点标记(旧版门与教徒同帧出现,再旧的版本连门都没有)
        /// </summary>
        public const int ReinforcePortalFrame = 14;
        public const int ReinforceSpawnFrame = 44;
        /// <summary>投送舱出现的深度:Z 2.5 缩到 0.29,从背景里的一点滑落到地面门上;坠落帧数 = 落地帧 − 开门帧</summary>
        public const float ReinforcePodDepth = 2.5f;
        public const int ReinforcePodFrames = ReinforceSpawnFrame - ReinforcePortalFrame;
        /// <summary>舱体落地接触伤害:半径 60px、6 帧判定;站在门上等教徒出来的人会被舱砸</summary>
        public const float ReinforcePodImpactRadius = 60f;
        public const int ReinforcePodImpactFrames = 6;
        public const int DmgDropPod = 300;
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

        //---- 深空红魔:红恶魔投影在 Z 2.5 的远景层,红射线是从它射向镜头的 Z 射线,三叉戟从背景收敛而来 ----

        /// <summary>红恶魔的深度与表观位置(玩家侧上方 480 / -200):Z 2.5 缩到 0.29,平面缩放 8 → 表观 2.3 倍,一尊压在虚空天幕上的巨影</summary>
        public const float RedDevilDepth = 2.5f;
        public static readonly Vector2 RedDevilApparent = new Vector2(480f, -200f);
        public const float RedDevilPlaneScale = 8f;
        /// <summary>探照光盘:落点半径 90(判定圆),预警期紧跟玩家(lerp 0.25),发射期每帧最多挪 4px 慢慢追(跑得动、站不住)</summary>
        public const float RedRaySpotRadius = 90f;
        public const float RedRayWarnTrack = 0.25f;
        public const float RedRaySpotTrackSpeed = 4f;
        /// <summary>三叉戟从红魔处 45 帧收敛到平面(标记提前 30 帧亮),落点排在以玩家为心、半径 150 的弧上</summary>
        public const int RedHellTridentFrames = 45;
        public const float RedHellLandRadius = 150f;
        public const float RedHellTridentLead = 12f;

        //==================== 绿色丛林(全息陆龟)====================

        public static readonly Vector2 JungleHoverOffset = new Vector2(0f, -400f);
        /// <summary>陆龟放出前 30 帧蓄力:弹幕档前摇</summary>
        public const int JungleSpawnFrame = WindupBarrage;
        /// <summary>陆龟冲刺次数 P1/P2 4、P3 5(旧 6/7:十二秒的同一招);奇数次改穿层冲锋</summary>
        public static int JungleDashes(int phase) => phase >= 3 ? 5 : 4;
        public const int JungleTail = 60;
        /// <summary>
        /// 穿层陆龟:奇数次冲锋传送到 Z 1.5(缩到 0.4)、表观在玩家侧上方 600 / -180 的背景里,18 帧待机后沿三维直线冲向锁定点,
        /// 第 30 帧穿过平面(Z 速度 -0.05,判定带 0.1 = 4 帧接触),再 12 帧遁到 -0.6 消失;毒刺扇在穿过平面那一帧放
        /// </summary>
        public const float TortoiseZDepth = 1.5f;
        public static readonly Vector2 TortoiseZApparent = new Vector2(600f, -180f);
        public const int TortoiseZCrossFrame = 30;
        public const int TortoiseZDashFrames = 42;
        public const float TortoiseZLead = 10f;
        /// <summary>陆龟出现到起冲 18 帧(旧 12:传送到侧面后要能被看到一下再冲);平面横冲 200 格用 90 帧</summary>
        public const int TortoiseAppearFrames = 18;
        public const int TortoiseDashFrames = 90;
        public const float TortoiseDashDistance = 200f * 16f;
        public const float TortoiseStartOffset = 80f * 16f;
        /// <summary>第 k 次冲锋的总帧数(待机 + 冲):奇数次是穿层冲锋;状态按它累加时长</summary>
        public static int TortoiseCycleFrames(int k) => TortoiseAppearFrames + (k % 2 == 1 ? TortoiseZDashFrames : TortoiseDashFrames);

        //==================== 蓝色天空(全息小白龙 + 形状弹幕)====================

        /// <summary>小白龙 30 帧蓄力放出:弹幕档前摇(旧 20)</summary>
        public const int SkyWyvernFrame = WindupBarrage;
        public const int SkyBurstStart = 60;
        /// <summary>形状弹持续 420(旧 720):形状弹改在龙头穿过平面那一帧释放,节拍钉在角速度上(每半圈一发,P1/P2 60 帧、P3 50 帧)</summary>
        public const int SkyBurstDuration = 420;
        public const int SkyBurstCount = 60;
        public const float SkyBurstRadius = 120f;
        public const float SkyBurstSpeed = 6f;
        public const int SkyTail = 60;
        /// <summary>
        /// 倾斜轨道:小白龙绕本体的圆改成绕 X 轴倾 55° 的三维椭圆(平面上短轴 = 半径 × cos55°),Z = sinθ × 振幅:
        /// 下半圈退到 Z 1.4 的远处(小而雾化),上半圈压到 -0.3 的镜头前(1.4 倍、半透明巨影),θ = 0 / π 两点穿过平面才有判定。
        /// 半径 60 格(FTW 50 格,旧 75 / 60:近端放大后要留在屏内);角速度 P1/P2 每圈 120 帧、P3 100 帧,穿越点就是节拍
        /// </summary>
        public static float WyvernOrbitRadius => (Main.getGoodWorld ? 50f : 60f) * 16f;
        public const float WyvernTiltDeg = 55f;
        public const float WyvernFarAmp = 1.4f;
        public const float WyvernNearAmp = 0.3f;
        public static float WyvernAngularSpeed(int phase) => MathHelper.TwoPi / (phase >= 3 ? 100f : 120f);
        /// <summary>龙身逐节的判定带:|Z| ≤ 0.15 的节才碰(比通用带宽一点,龙身穿越处要有两三节在带内)</summary>
        public const float WyvernHitBand = 0.15f;

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
        /// <summary>猛合喷弹里奇数位改纵深回旋:抛入深处(顶点 Z 1.2)40 帧后回头收敛到喷出时最近玩家的位置,往返 60 帧;偶数位留平面,两层弹雨一近一远</summary>
        public const float RiftBoomerangApex = 1.2f;
        public const int RiftBoomerangFrames = 60;
        /// <summary>P3 穿心刀的喷弹改纵深贯穿:沿缝在 Z 1.0 的背景里生成,30 帧收敛到缝两侧 160px 的落点,再掠过镜头</summary>
        public const float RiftPierceDepth = 1f;
        public const int RiftPierceFrames = 30;
        public const float RiftPierceLandOffset = 160f;

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
        /// <summary>退入深处时本体的表观悬停点(玩家头顶 380px,看得见在轰炸的是谁);世界坐标由 VDDepth.WorldOffset 按深度换算</summary>
        public static readonly Vector2 OrbitalHoverOffset = new Vector2(0f, -380f);
        /// <summary>退入的深度:Z = 2.2 缩到 0.31,小到读成「在背景里」、大到还能看清它在开炮(旧版是无投影的 0.45 缩放)</summary>
        public const float OrbitalFarDepth = 2.2f;
        /// <summary>P3 在光柱之间补射的纵深贯穿炮弹数:3 发夹在 5 根柱子的错拍空档里,不与柱子同帧</summary>
        public static int OrbitalShells(int phase) => phase >= 3 ? 3 : 0;
        /// <summary>炮弹从背景飞到平面的帧数:40 帧从 Z 2.2 到 0,标记提前 30 帧亮,玩家有整整半秒看它变大</summary>
        public const int OrbitalShellFrames = 40;
        /// <summary>炮弹瞄准的预测提前量(帧)</summary>
        public const float OrbitalShellLead = 20f;

        //==================== 纵深环门(退到 Z 1.3 → 从 Z 3.2 逐个推出带缺口的弹环 → 环逼近平面 → 俯冲归位)====================

        /// <summary>本体推环时所在深度:1.3 缩到 0.43,在远景层(不可攻击)但仍是画面里最大的东西,看得出环是它推出来的</summary>
        public const float GateDepth = 1.3f;
        /// <summary>推环时的表观悬停点:玩家头顶 300px 偏一侧,不挡环心</summary>
        public static readonly Vector2 GateHoverOffset = new Vector2(220f, -300f);
        /// <summary>起势 30 帧:弹幕档前摇,第一环在起势末尾才出现</summary>
        public const int GateWindup = WindupBarrage;
        /// <summary>环数 P1 5 / P2 6 / P3 7:每环 22 帧,连俯冲总长 4.2 秒以内</summary>
        public static int GateRings(int phase) => phase >= 3 ? 7 : phase >= 2 ? 6 : 5;
        /// <summary>环与环的出手间隔:22 帧让同屏始终有两个环在逼近,读成隧道</summary>
        public const int GateInterval = 22;
        /// <summary>环出现的深度与逼近速度:3.2 处缩到 0.24 是绕在玩家投影位置周围的一圈小点,-0.07/帧 46 帧到平面</summary>
        public const float GateStartDepth = 3.2f;
        public const float GateZVel = -0.07f;
        /// <summary>环的平面半径 260:玩家在环心附近有 200px 以上的活动余地,到达时缺口宽约 77°(2 弹缺 = 3 个间距)</summary>
        public const float GateRadius = 260f;
        public const int GateBolts = 14;
        public const int GateGapBolts = 2;
        /// <summary>相邻环缺口的转角(度),P3 交替正负:玩家要在环到达前挪到新缺口</summary>
        public const float GateGapStepDeg = 75f;
        /// <summary>环心 = 玩家预测点(提前 10 帧),再向玩家速度反向偏一点,别正好套在人头上</summary>
        public const float GateCenterLead = 10f;
        /// <summary>末环到达平面后到开始俯冲的静默帧</summary>
        public const int GateTail = 6;
        public const int DmgGateBolt = 380;

        //==================== 深空掠袭(闪到 Z 2 的一侧 → 两艘幻影护航成梯队 → 横越背景一路射纵深贯穿弹 → 对侧俯冲归位)====================

        /// <summary>掠袭深度:Z 2 缩到 0.33,三艘船是背景里一排清楚的剪影,弹从那里飞过来要 42 帧</summary>
        public const float StrafeDepth = 2f;
        /// <summary>航线的表观高度与半跨度:从屏幕一侧 520px 外飞到另一侧,90 帧约 11.5px/帧的表观速度</summary>
        public const float StrafeApparentY = -320f;
        public const float StrafeApparentHalfSpan = 520f;
        /// <summary>起势 36 帧(接触档):护航舰从门里出来、引擎亮起,第一发在起势末尾</summary>
        public const int StrafeWindup = WindupContact;
        public const int StrafeFrames = 90;
        /// <summary>每 12 帧一轮:本体一对、护航各一发,共 4 弹;7 轮 28 弹,每弹自带落点标记</summary>
        public const int StrafeBoltInterval = 12;
        /// <summary>弹从 Z 2 飞到平面的帧数:42 帧,标记提前 30 帧亮</summary>
        public const int StrafeBoltFrames = 42;
        public const float StrafeBoltLead = 20f;
        /// <summary>本体那一对弹的落点左右散开量</summary>
        public const float StrafeBoltSpread = 40f;
        /// <summary>护航舰的表观编队偏移(x 按航向取反:拖在本体后方斜下)</summary>
        public static readonly Vector2[] StrafeEscortOffsets = { new Vector2(150f, 90f), new Vector2(300f, 180f) };

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
        /// <summary>
        /// 三维螺旋弹:绕奇点的倾斜轨道(倾 55°,Z 振幅 0.9),角速度 0.09 rad/帧、半径每帧长 2.2px 螺旋外扩;一圈两次穿过平面才有判定。
        /// 从盘边 140px 起,150 帧活跃期内一发最远飘到 470px,正好覆盖牵引半径的一半
        /// </summary>
        public const float SingOrbitTiltDeg = 55f;
        public const float SingOrbitDepthAmp = 0.9f;
        public const float SingOrbitAngular = 0.09f;
        public const float SingOrbitRadiusGrowth = 2.2f;
        public const int SingCollapseFrames = 20;
        /// <summary>环爆 24 发分三向:8 发平面、8 发朝镜头(起点即带内,Z 速度 -0.03,25 帧后掠过镜头)、8 发遁入深处(纯演出,Z 速度 +0.04)</summary>
        public const int SingBurstCount = 24;
        public const float SingBurstNearZVel = -0.03f;
        public const float SingBurstFarZVel = 0.04f;
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
        /// <summary>平面舰的参考冲刺速度(现在各舰速度由「同帧穿过预测点」反推,这个值只给 hub 替补阀与旧读数参考)</summary>
        public const float FleetDashSpeed = 38f;
        /// <summary>冲刺总帧数:穿过预测点后再飞 16 帧(远舰此时已越过镜头淡出,近舰已遁回平面后方)</summary>
        public const int FleetDashFrames = 36;
        public static int FleetWaves(int phase) => phase >= 2 ? 2 : 1;
        /// <summary>两波之间全员静止 20 帧:段落之间的一口气</summary>
        public const int FleetWaveHold = 20;
        public const int FleetTrailInterval = 6;
        public const int FleetEndFade = 8;
        /// <summary>真身门环亮度倍率:破绽要读得出来</summary>
        public const float FleetRealPortalGlow = 1.6f;
        /// <summary>
        /// 立体舰队的四门深度(按门序;第二波整体轮转一位):两远(Z 1.4,缩到 0.42)、一平面、一近(Z -0.45,屏幕边缘的半透明巨门)。
        /// 四舰沿各自的三维直线在同一帧穿过玩家预测点:远舰放大着来、近舰缩小着来、平面舰不变,读成三种运动签名
        /// </summary>
        public static readonly float[] FleetDepths = { 1.4f, 0f, 1.4f, -0.45f };
        /// <summary>四舰同步瞄准后第 20 帧穿过预测点(远舰 Z 速度 -0.07,判定带 0.105 = 3 帧接触),再飞 16 帧收尾</summary>
        public const int FleetCrossFrame = 20;
        /// <summary>穿越点预测提前量(帧)</summary>
        public const float FleetCrossLead = 10f;

        //==================== 湮灭主炮(P3 压轴:锁定 → 出手 → 扫射 → 过热)====================

        /// <summary>
        /// 越肩主炮:本体闪到镜头后方 Z -0.45(1.8 倍、剪影两成半透明)、表观在玩家头顶 480px,从屏幕上缘压下来的巨影;
        /// 它在平面上的落点(世界坐标,玩家头顶 264px)就是光锥打进画面的枢,扫射线仍绕这个枢转,几何与旧版一致
        /// </summary>
        public const float CannonNearDepth = -0.45f;
        public static readonly Vector2 CannonNearApparent = new Vector2(0f, -480f);
        /// <summary>过热时从镜头后俯冲回平面的帧数(落地 = 惩罚窗开始)</summary>
        public const int CannonOverheatDive = 20;
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

        /// <summary>
        /// 常态底噪:平时就是一圈清楚可见的能量缘光(反馈 2026-09-18:0.1 档几乎看不见),随阶段升级抬高。
        /// 底噪只管亮度,不进热色与侵蚀的判定,那两样看活跃度(蓄力 / CoreGlow 折算,见 <see cref="VoidDestroyer.UpdateVisualState"/>)
        /// </summary>
        public static float RimIdle(int phase) => phase >= 3 ? RimIdleP3 : phase >= 2 ? RimIdleP2 : RimIdleP1;
        public const float RimIdleP1 = 0.40f;
        public const float RimIdleP2 = 0.50f;
        public const float RimIdleP3 = 0.60f;
        /// <summary>常态呼吸:底噪上叠 ±25% 的慢起伏,角速度 2.2 rad/s 约 2.9 秒一个来回</summary>
        public const float RimBreathAmp = 0.25f;
        public const float RimBreathSpeed = 2.2f;
        /// <summary>描边整体亮度倍率(加法混合下允许大于 1),常态与蓄力一起抬;爆闪在着色器里另乘 (1 + flash)</summary>
        public const float RimBrightness = 1.35f;
        /// <summary>CoreGlow 折算成描边活跃度的系数:20 招的起势/蓄力/出手都在推 CoreGlow,这一个系数就是全招免费覆盖的总闸</summary>
        public const float RimFromCoreGlow = 0.9f;
        /// <summary>强度追踪步长:约 6 帧追上目标,蓄力斜坡不被抹平,状态停止声明时也不闪断</summary>
        public const float RimTrack = 0.18f;
        /// <summary>爆闪每帧衰减:约 10 帧从 1 落到 0.1,爆闪只是一瞬</summary>
        public const float RimFlashFall = 0.80f;
        /// <summary>配色追踪步长:约 10 帧过渡,换招换色不硬切</summary>
        public const float RimColorTrack = 0.10f;
        /// <summary>压暗追踪步长:静默拍要快(3 帧内压下去),否则 6 帧的静默还没暗完核弹就出了</summary>
        public const float RimSuppressTrack = 0.4f;
        /// <summary>活跃度(不含底噪)过这个阈值起缘光向热色偏,到 1 全热(「蓄力发红」的通用来源,不靠逐招写死)</summary>
        public const float RimHeatStart = 0.3f;
        /// <summary>爆闪把热色再往纯白推的比例:蓄力是红热,出手是白热</summary>
        public const float RimFlashWhiten = 0.8f;
        /// <summary>默认蓄力热色:烧红的危险色</summary>
        public static readonly Color RimHeatRed = new Color(255, 110, 80);

        /// <summary>外扩叠画抽数:6 抽绕圈,再多 GPU 白花,再少能看出多边形</summary>
        public const int RimTaps = 6;
        /// <summary>外扩基础半径(px,乘绘制缩放):常态一圈 4px 的晕,轮廓外侧要能读出来</summary>
        public const float RimBaseRadius = 4f;
        /// <summary>蓄力满时的外扩半径增量:能量逸散得更远</summary>
        public const float RimChargeRadius = 7f;
        /// <summary>爆闪瞬间的外扩半径增量:整圈猛地炸开一下</summary>
        public const float RimFlashRadius = 12f;
        /// <summary>叠画各抽的亮度分摊:6 抽合成后约 2 倍单抽亮度</summary>
        public const float RimTapOpacity = 0.32f;
        /// <summary>内缘锐光(压在本体之上那一遍)的亮度</summary>
        public const float RimEdgeOpacity = 1f;
        /// <summary>叠画偏移绕圈的角速度(rad/s):逸散的丝在慢慢转</summary>
        public const float RimSpin = 1.6f;

        //---- 四种风格各自的差异量:几何(半径 / 抽数 / 拖长)、噪声流向(径向外逸 / 向内吸 / 沿速度反向)、侵蚀比例、闪烁 ----

        /// <summary>逸散风格:极坐标噪声层向外流的速度(噪声 UV/s),能量看着是从轮廓往外跑</summary>
        public const float RimRadialSpeed = 0.9f;
        /// <summary>逸散 / 塌缩 / 过热风格里极坐标层的混合权重:流向主要由径向层表达</summary>
        public const float RimRadialMixDefault = 0.7f;
        /// <summary>拖尾风格里极坐标层的权重:丝主要沿速度反向飞,径向只留一点</summary>
        public const float RimRadialMixStreak = 0.2f;
        /// <summary>塌缩风格:向内吸的流速比往外漏快,吸进去要急</summary>
        public const float RimCollapseInSpeed = 1.6f;
        /// <summary>塌缩风格:蓄力满时半径压到基础半径的这个倍数,贴边;声明一停弹回</summary>
        public const float RimCollapseMin = 0.2f;
        /// <summary>拖尾风格:整圈沿速度反向抹开的最大长度(px),按速度比例;远端抽逐级变暗</summary>
        public const float RimStreakLength = 28f;
        /// <summary>拖尾风格:达到最大拖长所需的速度(px/f),与残影门控 18 起点相衔接</summary>
        public const float RimStreakFullSpeed = 30f;
        /// <summary>拖尾风格:直角噪声层沿速度反向流的速度(噪声 UV/s),丝往身后飞</summary>
        public const float RimStreakScrollSpeed = 1.8f;
        /// <summary>过热风格:外圈再叠一环(半径倍率与亮度),整圈厚实的白炽电晕</summary>
        public const float RimOverheatOuterMult = 1.8f;
        public const float RimOverheatOuterOpacity = 0.55f;
        /// <summary>过热风格的高频闪烁角速度(rad/s)与幅度</summary>
        public const float RimOverheatFlickerSpeed = 38f;
        public const float RimOverheatFlickerAmp = 0.22f;

        /// <summary>噪声侵蚀比例:常态留一半成丝,蓄力收成实心带,爆闪时归 0 整圈实心;过热风格几乎不侵蚀</summary>
        public const float RimErodeIdle = 0.55f;
        public const float RimErodeCharge = 0.2f;
        public const float RimErodeOverheat = 0.05f;
        /// <summary>直角噪声层的常态漂移(噪声 UV/s,图样朝这个方向流):x 慢横流,y 负值向上逸散</summary>
        public static readonly Vector2 RimNoiseScroll = new Vector2(0.05f, -0.22f);

        //---- 配色:八个家族 + 三阶段各不相同,常态描边本身就报阶段 ----

        /// <summary>连接段 / 演出的描边色随阶段走:一阶段虚空紫、二阶段紫粉、三阶段盾色淡紫</summary>
        public static Color RimIdleColor(int phase) => phase >= 3 ? VDVfx.ShieldLavender : phase >= 2 ? RimIdleColorP2 : VDVfx.VoidPurple;
        public static readonly Color RimIdleColorP2 = new Color(225, 90, 255);
        /// <summary>区域封锁家族:切开空间的冷青,与冲刺的白、弹幕的粉拉开</summary>
        public static readonly Color RimZoneCyan = new Color(140, 235, 255);
        /// <summary>引力家族:深靛,比虚空深紫亮得出来,仍是「往里吸」的冷色</summary>
        public static readonly Color RimGravityIndigo = new Color(120, 70, 255);
        /// <summary>召唤家族:暖金,地面开门投兵是唯一的暖色招</summary>
        public static readonly Color RimSummonGold = new Color(255, 200, 110);
        /// <summary>冲刺家族的热色:冷白偏蓝(速度是冷的),不与弹幕的烧红混</summary>
        public static readonly Color RimDashIce = new Color(200, 240, 255);

        /// <summary>描边主色:全息三招跟各自的投影色,其余按家族;连接段 / 演出按阶段</summary>
        public static Color RimColorFor(VDStateIndex state, int phase) {
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
                    return RimGravityIndigo;
                case VDAttackFamily.Dash:
                    return VDVfx.VoidWhite;
                case VDAttackFamily.Barrage:
                    return VDVfx.VoidPink;
                case VDAttackFamily.Zone:
                    return RimZoneCyan;
                case VDAttackFamily.Summon:
                    return RimSummonGold;
                default:
                    return RimIdleColor(phase);
            }
        }

        /// <summary>蓄力热色:弹幕 / 连接段 / 演出 / 红色地狱烧红;冲刺冷白;区域与召唤各自白化;绿丛林 / 蓝天空各自白化;主炮炮芯色;奇点冷白</summary>
        public static Color RimHeatColorFor(VDStateIndex state) {
            switch (state) {
                case VDStateIndex.GreenJungle:
                    return Color.Lerp(VDVfx.JungleGreen, Color.White, 0.6f);
                case VDStateIndex.BlueSky:
                    return Color.Lerp(VDVfx.SkyBlue, Color.White, 0.6f);
            }
            switch (VDRotation.FamilyOf(state)) {
                case VDAttackFamily.Finale:
                    return VDVfx.CannonCore;
                case VDAttackFamily.Gravity:
                    return VDVfx.VoidWhite;
                case VDAttackFamily.Dash:
                    return RimDashIce;
                case VDAttackFamily.Zone:
                    return Color.Lerp(RimZoneCyan, Color.White, 0.6f);
                case VDAttackFamily.Summon:
                    return Color.Lerp(RimSummonGold, Color.White, 0.6f);
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
        /// <summary>死亡:本体漂进深处到位(210)起虚空随它离开,到真死(330)收干</summary>
        public const int SkyDeathFadeStart = DeathDriftEnd;
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
