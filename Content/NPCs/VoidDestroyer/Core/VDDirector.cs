using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.Core
{
    /// <summary>
    /// 状态里不能写裸数字,数值一律存普通档基值,专家 ×2 大师 ×3 由原版在血量缩放与命中结算时各乘一次
    /// 手与手之间有连接段三拍,每招第一拍是前摇,单招不超 8 秒
    /// </summary>
    public static class VDDirector
    {
        //==================== 基础数值 ====================

        /// <summary>血量普通 / 专家 / 大师 = 218000 / 436000 / 654000</summary>
        public const int BaseLife = 218000;
        /// <summary>接触伤害普通 / 专家 / 大师 = 126 / 252 / 378</summary>
        public const int BaseDamage = 126;
        public const int BaseDefense = 70;
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

        /// <summary>限制圈半径:80 格,圆心随本体</summary>
        public const float ArenaRadius = 80f * 16f;
        /// <summary>出圈回拉基础力与每 400px 超出量的增量;沿拉回方向的速度封顶 28</summary>
        public const float ArenaPullBase = 0.35f;
        public const float ArenaPullPer400 = 1f;
        public const float ArenaPullMax = 1.6f;
        public const float ArenaInwardSpeedCap = 28f;
        /// <summary>切技传送点到玩家的对角距离:约 30 格</summary>
        public const float SwitchTeleportOffset = 340f;
        /// <summary>闪现的总帧数里,前半段淡出,过半时换位,后半段淡入</summary>
        public const int BlinkDuration = 16;
        /// <summary>传送落地后的无接触窗口(防落地贴脸秒杀)</summary>
        public const int PostTeleportGrace = 30;

        //==================== 演出 ====================

        /// <summary>出场 220 帧:0-36 平面上开门;36-120 本体从门后 Z 3.5 沿视线逼近(压在门上原地放大);120 落定冲击、门收拢;120-220 威压</summary>
        public const int EntranceDuration = 220;
        public const int EntranceWarpFrames = 36;
        public const int EntranceArriveFrame = 120;
        public const int EntranceCameraFrames = 170;
        /// <summary>出场起点深度:Z 3.5 缩到 0.22,门后深处的一点</summary>
        public const float EntranceStartDepth = 3.5f;
        /// <summary>变形 120 帧,全程在平面上:0-36 颤抖蓄能;36-84 逐帧变形;84 爆闪换二阶段贴图;84-120 展翼</summary>
        public const int TransformDuration = 120;
        public const int TransformShudderEnd = 36;
        public const int TransformBurstFrame = 84;
        /// <summary>演出起手时本体若还在深处(轨道轰炸被打断),先用这么多帧沿视线浮回平面</summary>
        public const int SurfaceFrames = 16;
        /// <summary>护盾展开连接段:清弹 + 盾亮 + 暗角轻压,给最后一段一个干净的起点</summary>
        public const int ShieldUpDuration = 45;
        /// <summary>死亡 330 帧:0-150 平面逐级爆炸;150-210 失去动力翻滚着沿视线沉进深处(原地缩小并缓缓上飘);210-290 远处连锁小爆,250 帧全天幕闪光;290-330 收尾;330 真死</summary>
        public const int DeathDuration = 330;
        public const int DeathExplosionEnd = 150;
        public const int DeathDriftEnd = 210;
        public const int DeathFinalFlashFrame = 250;
        public const int DeathFadeEnd = 290;
        /// <summary>死亡沉入的深度:Z 3 缩到 0.25,远得像坠向那颗被啃的星球,又近得看清最后一炸</summary>
        public const float DeathDriftDepth = 3f;
        /// <summary>沉入时表观位置的上飘速度(px/帧)</summary>
        public const float DeathDriftRise = 0.6f;
        /// <summary>死亡真死前给客户端的容差:客户端计时可能落后几帧,免得收到击杀包时把自己救活</summary>
        public const int DeathKillTolerance = 10;
        /// <summary>撤离用 60 帧沿视线遁到 Z 6,表观位置缓缓上飘,透明度另外按 NoTargetDespawnFrames 收完</summary>
        public const int DespawnWarpFrames = 60;
        public const float DespawnDepth = 6f;
        public const float DespawnRise = 2f;

        //==================== 节奏 ====================

        /// <summary>连接段的总长包含落定、重瞄和起势,冷却也算在这里</summary>
        public static int ConnectorFrames(int phase) => phase >= 3 ? 66 : phase >= 2 ? 78 : 90;
        /// <summary>第一拍落定会在这一拍里做完 16 帧换位闪现,落地就刹停</summary>
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
        public const int RecoveryFrames = 30;
        /// <summary>单招时长上限(主炮除外):超的剪,长招是「乱」的另一半来源</summary>
        public const int AttackDurationCap = 60 * 8;
        /// <summary>任何攻击状态的总龄上限:超过强制收招(正常收招远早于此)</summary>
        public const int AttackTimeoutFrames = 60 * 20;
        /// <summary>替补阀:玩家拉远到此距离,Dash 槽位用传送逼近的幻影冲刺而不是围绕玩家开门的舰队</summary>
        public const float FarDashDistance = 1200f;

        //==================== 纵深(Z = 0 玩家平面,+Z 越远;数学在 VDDepth,分层在 VDDepthStage / VDDepthRenderHandle)====================
        //伤害弹幕一律留在平面,纵深只给本体:轨道轰炸退入背景,出场 / 死亡 / 撤离演出
        //本体变深时沿视线走(PlaceOverPlanePoint),表观位置不动只缩放,不许整体滑向屏幕中心
        //不用镜头前(Z < 0)那一层:2D 侧视里没有任何线索能让半透明巨影读成「在镜头前」

        /// <summary>透视焦距(Z 单位):Scale = Focal / (Focal + Z)。取 1 让 Z=1 恰好半身、Z=3 四分之一,档位口算得出来</summary>
        public const float DepthFocal = 1f;
        /// <summary>近端 Z 钳位:Scale 在 Z → -1 发散</summary>
        public const float DepthNearClamp = -0.85f;
        /// <summary>远景层门槛:Z ≥ 0.6(缩到 0.625 以下)才进墙后物块前的远景层;更近的东西压到物块后面会读成穿模</summary>
        public const float DepthFarLayerZ = 0.6f;
        /// <summary>近景层门槛(框架保留,现有招式不用)</summary>
        public const float DepthNearLayerZ = -0.25f;
        /// <summary>判定带下限:|Z| ≤ 0.1 才有碰撞</summary>
        public const float DepthHitBandMin = 0.1f;
        /// <summary>判定带按 Z 速度放宽的倍率:1.5 倍单帧 Z 位移</summary>
        public const float DepthHitBandVelMult = 1.5f;
        /// <summary>近景剪影透明度上限与完全淡出的 Z(框架保留)</summary>
        public const float DepthNearAlphaMax = 0.45f;
        public const float DepthNearFadeZ = -0.7f;
        /// <summary>远端雾化满值的 Z:Z = 3 时雾色占满,再远只剩轮廓</summary>
        public const float DepthFogFullZ = 3f;
        /// <summary>远端透明度地板 0.8(旧 0.55):远处的舰体是暗、冷、糊,不是半透明的鬼影</summary>
        public const float DepthFarAlphaFloor = 0.8f;
        /// <summary>0.5 只落后一帧,旧 0.2 会让穿平面那帧绘制离判定盒约 50px</summary>
        public const float DepthTrack = 0.5f;
        /// <summary>掠过镜头的呼啸触发 Z 与强度(框架保留,深度弹幕基类用)</summary>
        public const float DepthWhooshZ = -0.35f;
        public const float WhooshShake = 3f;
        public const float WhooshStreak = 0.6f;
        /// <summary>多普勒:逼近的音高从 0.7 升到 1.3,按 Z 从 DepthFogFullZ 到 0 线性</summary>
        public const float DepthDopplerLow = 0.7f;
        public const float DepthDopplerHigh = 1.3f;
        /// <summary>深度弹幕基类的落点标记提前量与半径(框架保留)</summary>
        public const int DepthMarkerLeadFrames = WindupBarrage;
        public const float DepthMarkerRadius = 30f;
        /// <summary>俯冲落地接触窗:落地那 6 帧本体有接触伤害,落点大环从俯冲起手就画</summary>
        public const int DiveContactFrames = 6;
        /// <summary>俯冲落地震屏与落点大环半径(px)</summary>
        public const float DiveShake = 9f;
        public const float DiveMarkerRadius = 120f;
        /// <summary>远端本体经雾化着色器的模糊半径(px,按缩放后尺寸)与热闪幅度</summary>
        public const float DepthFogBlur = 1.5f;
        public const float DepthFogShimmer = 0.006f;
        /// <summary>自发光只吃三成多雾,远端变冷但不整段蓝灰</summary>
        public const float BeamFogMult = 0.4f;
        /// <summary>透视射线退化路径(着色器缺失)的沿轴分段数</summary>
        public const int BeamFallbackSegments = 16;

        //==================== 伤害档(普通档基值)====================
        //弹幕 damage 字段就存这里的数,命中时原版再乘 1 / 2 / 3,字段里不能先带难度(按 NPC.defDamage 折算会把难度乘两次)
        //弹幕档 48 / 96 / 144,小激光档(含红射线锁定、叉子、丛林陆龟)63 / 126 / 189,核弹与大激光档 80 / 160 / 240(普通 / 专家 / 大师)
        //本体接触见 BaseDamage 126 / 252 / 378,小白龙与支援舱走 DmgBody 106 / 212 / 318,不随本体接触涨

        public const int DmgBullet = 48;
        public const int DmgBody = 106;
        public const int DmgLaser = 63;
        public const int DmgHeavy = 80;

        public const int DmgVoidBolt = DmgBullet;
        public const int DmgCoreLaser = DmgBullet;
        public const int DmgMissile = DmgBullet;
        public const int DmgTrident = DmgLaser;
        public const int DmgStinger = DmgBullet;
        public const int DmgSnowflake = DmgBullet;
        public const int DmgGateRing = DmgBullet;
        /// <summary>幻影舰低于真身接触一档(幻影是读破绽的代价,不是真身)</summary>
        public const int DmgPhantomShip = DmgBullet;
        public const int DmgHoloBeast = DmgBody;
        /// <summary>丛林陆龟单列,小白龙仍走 DmgHoloBeast</summary>
        public const int DmgHoloTortoise = DmgLaser;
        public const int DmgSporeLaser = DmgLaser;
        public const int DmgRedRay = DmgLaser;
        public const int DmgRiftSeam = DmgLaser;
        public const int DmgVoidPillar = DmgLaser;
        public const int DmgSingularityCore = DmgLaser;
        /// <summary>只有核弹与主炮吃到这一档</summary>
        public const int DmgNuke = DmgHeavy;
        public const int DmgAnnihilationBeam = DmgHeavy;

        /// <summary>棱形虚空弹(VDVoidBolt)的飞行速度倍率:弧形包抄与火焰回转的转弯角速度同乘,弧线半径不变,炸弹与螺旋弹不吃</summary>
        public const float VoidBoltSpeedMult = 2f;

        //==================== 弧形火球(斜上逼近 + 三轮包抄)====================

        /// <summary>悬停点:玩家斜上方(SideDir*400, -300)</summary>
        public static readonly Vector2 ArcHoverOffset = new Vector2(400f, -300f);
        public const float ArcApproachSpeed = 26f;
        public const float ArcApproachAccel = 0.14f;
        public const float ArcApproachSlow = 90f;
        /// <summary>就位上限 45 帧,15 帧后离目标 60px 以内就跳拍</summary>
        public const int ArcApproachMax = 45;
        public const int ArcApproachMin = 15;
        public const float ArcArriveDist = 60f;
        public const float ArcHoldSpeed = 10f;
        public const float ArcHoldAccel = 0.05f;
        public const float ArcHoldSlow = 200f;
        /// <summary>到位后的蓄力前摇(汇聚 + 核心 + 翼张)再首轮:弹幕档 30</summary>
        public const int ArcChargeFrames = WindupBarrage;
        /// <summary>轮间隔:P3 36 帧,其余 44</summary>
        public static int ArcVolleyInterval(int phase) => phase >= 3 ? 36 : 44;
        public const int ArcVolleys = 3;
        public const float ArcBoltSpeed = 9f * VoidBoltSpeedMult;
        /// <summary>中弹直射,两侧弧弹朝出手时的锁定点包抄(张角,度);P2 起再加一对 50° 的外弧,P3 再加 70°</summary>
        public const float ArcInnerDeg = 25f;
        public const float ArcOuterDeg = 50f;
        public const float ArcWideDeg = 70f;
        /// <summary>末轮后收招拍</summary>
        public const int ArcTail = RecoveryFrames;

        //==================== 追踪导弹(蓄力 + 激光流 + 导弹环 + 核弹)====================

        public static readonly Vector2 MissileHoverOffset = new Vector2(0f, -260f);
        public const float MissileHoverSpeed = 6f;
        public const float MissileHoverAccel = 0.05f;
        public const float MissileHoverSlow = 150f;
        /// <summary>蓄力 60:射线档前摇,核心汇聚可读</summary>
        public const int MissileChargeFrames = WindupBeam;
        /// <summary>激光流 60~150:每 8 帧一发匀速直飞,不加速</summary>
        public const int MissileLaserEnd = 150;
        public const int MissileLaserInterval = 8;
        public const float MissileLaserSpeed = 15f;
        public const float MissileLaserJitter = 0.05f;
        public static int MissileVolleys(int phase) => 3;
        public static int MissileVolleyInterval(int phase) => phase >= 2 ? 100 : 110;
        public static int MissileRingCount(int phase) => phase >= 2 ? 8 : 6;
        /// <summary>导弹环起点离本体中心 16px:贴着舰体生成,先在舰体周围展开</summary>
        public const float MissileRingRadius = 16f;
        /// <summary>导弹全程匀速,不加速</summary>
        public const float MissileRingSpeed = 8f * 2f;
        /// <summary>导弹径向飞出 8 格后转头,只追这么多帧就改直飞:躲过一次就甩掉了</summary>
        public const int MissileHomingFrames = 40;
        /// <summary>核弹在末轮后 40 帧出手;出手前 6 帧粒子静默</summary>
        public const int MissileNukeDelay = 40;
        public const int MissileNukeSilence = 6;
        public const float MissileNukeSpeed = 6f;
        public const float MissileNukeSideSpeed = 7f;
        /// <summary>核弹出手后 50 帧收招(核弹自己有 3 秒引信,本体不必陪着)</summary>
        public const int MissileTail = 50;
        /// <summary>核弹爆炸半径 36 格(旧 50 格 = 800px,半块屏幕都在圈里,只能直线狂奔)</summary>
        public const float NukeRadius = 36f * 16f;

        //==================== 幻影冲刺(门后待机 + 沿航道连冲)====================

        /// <summary>连冲次数:P1 三段,P2 起四段</summary>
        public static int PhantomDashes(int phase) => phase >= 2 ? 4 : 3;
        public const float PhantomDashSpeed = 40f;
        public const int PhantomDashFrames = 30;
        public const int PhantomFadeFrames = 6;
        public const float PhantomPortalOffset = 480f;
        public const int PhantomPortalLife = 66;
        /// <summary>门后待机 36 帧(接触档前摇):开门那一刻冲刺方向就锁死并画出航道,冲刺严格沿航道飞(旧版出手帧重新瞄准,门和航向对不上)</summary>
        public const int PhantomWaitFrames = WindupContact;
        /// <summary>航道长度盖住整段冲刺(40×30 = 1200px),宽度略大于本体高</summary>
        public const float PhantomLaneLength = 1300f;
        public const float PhantomLaneWidth = 100f;
        /// <summary>冲完先硬刹 10 帧再淡出:冲刺 → 急停 → 消失,不是冲完就闪掉</summary>
        public const int PhantomBrakeFrames = 10;
        /// <summary>P2 起沿冲刺路径留慢速虚空弹的间隔,弹不加速</summary>
        public const int PhantomTrailInterval = 10;
        public const float PhantomTrailSpeed = 2f * VoidBoltSpeedMult;
        public const int PhantomEndFade = 8;
        /// <summary>接触伤害的速度门槛(冲刺段才开窗)</summary>
        public const float PhantomContactSpeed = 20f;

        //==================== 虚空火焰(压到玩家下方向下扇散,弹再转头向上)====================

        public static readonly Vector2 FlameOffset = new Vector2(0f, 320f);
        public const float FlameApproachSpeed = 30f;
        public const float FlameApproachAccel = 0.15f;
        public const float FlameApproachSlow = 100f;
        public const int FlameApproachFrames = 30;
        public const float FlameHoldStiffness = 0.12f;
        public const float FlameHoldLerp = 0.3f;
        public const float FlameHoldMaxSpeed = 34f;
        /// <summary>轮间隔 90,轮数 P1/P2 3、P3 4</summary>
        public const int FlameVolleyInterval = 90;
        public static int FlameVolleys(int phase) => phase >= 3 ? 4 : 3;
        /// <summary>每轮弹数区间(上界不含):P1 5~7,P2 起 7~9</summary>
        public static int FlameCountMin(int phase) => phase >= 2 ? 7 : 5;
        public static int FlameCountMax(int phase) => phase >= 2 ? 10 : 8;
        public const float FlameSpeedMin = 6f * VoidBoltSpeedMult;
        public const float FlameSpeedMax = 9f * VoidBoltSpeedMult;
        public const float FlameSpread = 1.05f;
        public const int FlameRiseDelay = 20;
        /// <summary>P2 起喷口逐轮左右交替偏移,两轮火幕错开,站在原地会被下一轮正好盖住</summary>
        public const float FlameSideShift = 180f;
        /// <summary>P3 每轮再补三成晚转向的火,两层火幕一前一后升起</summary>
        public const int FlameLateRiseDelay = 34;
        /// <summary>每轮出手前 30 帧的向下锥形预告:弹幕档前摇</summary>
        public const int FlameTelegraph = WindupBarrage;
        /// <summary>末轮后收招拍</summary>
        public const int FlameTail = RecoveryFrames;

        //==================== 相位激光(无人机纵列 + 井字网)====================

        /// <summary>纵列数 P1 3、P2/P3 4(P3 靠两轮井字网加压,整招仍压在 8 秒上下)</summary>
        public static int LaserColumns(int phase) => phase >= 2 ? 4 : 3;
        public static readonly Vector2 LaserHoverOffset = new Vector2(0f, -400f);
        public const int LaserDecorDrones = 12;
        /// <summary>装饰无人机绕本体 36 帧再闪现:接触档前摇长度,无人机环就是预告</summary>
        public const int LaserDecorFrames = WindupContact;
        /// <summary>纵列离玩家的水平距离;偶数列在左朝右打,奇数列在右朝左打</summary>
        public const float LaserColumnX = 35f * 16f;
        /// <summary>一列 9 架、间距 56,列高 ±224px;掷骰空出相邻 2 架当安全口(净宽约 140px),口不开在玩家当下那一格</summary>
        public const int LaserColumnDrones = 9;
        public const float LaserDroneSpacing = 56f;
        public const int LaserGapDrones = 2;
        public const int LaserWarnTime = 60;
        public const float LaserColumnLength = 2400f;
        /// <summary>末列到井字网之间静默 50 帧,网是这一招的重拍,前面留出这段空档</summary>
        public const int LaserColumnsToGrid = 50;
        /// <summary>第 k 列的出现时刻:P1 固定 55 帧;P2 起间隔从 55 线性收缩</summary>
        public static int LaserColumnTime(int k, bool ex) => ex ? (int)(55f * k - 1f * k * (k - 1)) : 55 * k;

        //---- 井字网(相位激光的重拍):横竖各五道射线以玩家为心落下,玩家起手正踩交点 ----

        /// <summary>横竖各 5 道、间距 160:斜挪 80px 进格心,格内净宽 160 - 26 = 134px</summary>
        public const int LatticeLines = 5;
        public const float LatticeSpacing = 160f;
        public const float LatticeBeamWidth = 26f;
        /// <summary>射线半长 1000:远比网宽,逃出网外不比挪进格子更省事</summary>
        public const float LatticeHalfLength = 1000f;
        /// <summary>每轮 60 帧预警(射线档),再 8 帧判定</summary>
        public const int LatticeWarn = WindupBeam;
        public const int LatticeStrikeFrames = 8;
        /// <summary>P3 错半格再来一轮(上一轮的格心成了新的交点),间隔 30 帧起手</summary>
        public static int LatticePulses(int phase) => phase >= 3 ? 2 : 1;
        public const int LatticePulseGap = 30;
        public const int LatticeTail = 30;

        //==================== 传送火弹(四角依次闪现扇射)====================

        /// <summary>四角顺序:左上 → 左下 → 右下 → 右上</summary>
        public static readonly int[] TeleportFireOrder = { 0, 2, 3, 1 };
        /// <summary>角数 P2 3、P3 4</summary>
        public static int TeleportFireCorners(int phase) => phase >= 3 ? 4 : 3;
        public const float TeleportFireOffset = 480f;
        /// <summary>落地后 30 帧核心蓄力再射(弹幕档前摇)</summary>
        public const int TeleportFireShotFrame = WindupBarrage;
        /// <summary>每角扇形弹数:P2 3、P3 5</summary>
        public static int TeleportFireBolts(int phase) => phase >= 3 ? 5 : 3;
        public const float TeleportFireSpreadDeg = 15f;
        /// <summary>扇形弹匀速直飞,不加速</summary>
        public const float TeleportFireBoltSpeed = 7f * VoidBoltSpeedMult;
        /// <summary>每角 60 帧:射完还能看见它停在角上</summary>
        public const int TeleportFireCornerFrames = 60;
        /// <summary>末角后收招拍</summary>
        public const int TeleportFireTail = RecoveryFrames;

        //==================== 支援投送 ====================

        public static readonly Vector2 ReinforceHoverOffset = new Vector2(0f, -500f);
        public const float ReinforceHoldStiffness = 0.06f;
        public const float ReinforceHoldLerp = 0.2f;
        public const float ReinforceHoldMaxSpeed = 20f;
        /// <summary>地面门先开,舱从落点正上方落下,门就是落点</summary>
        public const int ReinforcePortalFrame = 14;
        public const int ReinforceSpawnFrame = 44;
        /// <summary>舱从落点正上方 900px 处落下,坠落帧数 = 落地帧 − 开门帧(30 帧,二次缓入,末速约 60px/帧)</summary>
        public const float ReinforcePodFallHeight = 900f;
        public const int ReinforcePodFrames = ReinforceSpawnFrame - ReinforcePortalFrame;
        /// <summary>舱体落地接触伤害:半径 60px、6 帧判定;站在门上等教徒出来的人会被舱砸</summary>
        public const float ReinforcePodImpactRadius = 60f;
        public const int ReinforcePodImpactFrames = 6;
        public const int DmgDropPod = DmgBody;
        /// <summary>收招</summary>
        public const int ReinforceDuration = 100;
        public const float ReinforceSpreadX = 400f;
        public const int ReinforcePortalLife = 70;
        /// <summary>场上前卫教徒上限,防止连续投送堆积</summary>
        public const int MaxVanguards = 8;
        public static int ReinforceCount() => (Main.masterMode ? 4 : Main.expertMode ? 3 : 2) * (Main.zenithWorld ? 3 : 1);
        /// <summary>找地面向下扫的格数</summary>
        public const int GroundScanTiles = 60;

        //==================== 红色地狱(全息红恶魔 + 红射线 + 三叉戟扇)====================

        public const int RedHellRounds = 2;
        public const int RedHellCycle = 190;
        /// <summary>射线预警 60 帧(射线档前摇),发射 36 帧:一次站桩最多吃一下</summary>
        public const int RedHellRayWarn = WindupBeam;
        public const int RedHellRayDuration = 36;
        /// <summary>三叉戟第一排压在射线收束之后:100、第二排 124,FTW 第三排 148</summary>
        public const int RedHellTridentFrame1 = 100;
        public const int RedHellTridentFrame2 = 124;
        public const int RedHellTridentFrameFTW = 148;
        public static int RedHellTridents(int phase) => phase >= 3 ? 6 : 5;
        /// <summary>三叉戟扇面张角(度),对准玩家预测点:P1/P2 36°、P3 44°;第二排错半格补缝</summary>
        public static float RedHellFanDeg(int phase) => phase >= 3 ? 44f : 36f;
        /// <summary>三叉戟匀速直飞,不加速</summary>
        public const float RedHellTridentSpeed = 9f * 1.66f;
        public const float RedHellTridentLead = 12f;
        public static readonly Vector2 RedHellHoverOffset = new Vector2(0f, -480f);
        public const float RedHellHoldStiffness = 0.1f;
        public const float RedHellHoldLerp = 0.3f;
        public const float RedHellHoldMaxSpeed = 36f;
        public const int RedHellTail = 40;
        /// <summary>全息红恶魔每轮现身在玩家侧上方 (±480, -220) 的平面位置,缩放 2.4;只是投影,没有判定</summary>
        public static readonly Vector2 RedDevilOffset = new Vector2(480f, -220f);
        public const float RedDevilScale = 2.4f;
        /// <summary>探照光盘:落点半径 90(判定圆),预警前段松松跟着玩家(lerp 0.1),末 24 帧冻结在原地,发射期不再追:看见锁定就能跑开</summary>
        public const float RedRaySpotRadius = 90f;
        public const float RedRayWarnTrack = 0.1f;
        public const int RedRayLockFrames = 24;
        /// <summary>红射线源光(Glow 256px)的缩放:约 115px,盖在红恶魔胸口</summary>
        public const float RedRaySourceGlow = 0.45f;

        //==================== 绿色丛林(全息陆龟横冲)====================

        public static readonly Vector2 JungleHoverOffset = new Vector2(0f, -400f);
        /// <summary>陆龟放出前 30 帧蓄力:弹幕档前摇</summary>
        public const int JungleSpawnFrame = WindupBarrage;
        /// <summary>陆龟冲刺次数 P1/P2 3、P3 4,左右交替</summary>
        public static int JungleDashes(int phase) => phase >= 3 ? 4 : 3;
        /// <summary>起冲时扇形毒刺:3 发匀速,张角 ±25°</summary>
        public const int StingerCount = 3;
        public const float StingerSpreadDeg = 25f;
        public const float StingerSpeed = 9f;
        public const int JungleTail = 30;
        /// <summary>陆龟在玩家左右 720px(屏内)现身,待机 36 帧(接触档)画出整条水平航道;航道 y 锁在现身那一刻,冲刺不再上下修正</summary>
        public const int TortoiseAppearFrames = WindupContact;
        public const float TortoiseStartOffset = 720f;
        /// <summary>横冲 32 × 1.33 ≈ 42.6px/帧、50 帧,走约 2100px:约 17 帧撞到玩家所在处,再冲出屏幕另一侧</summary>
        public const float TortoiseDashSpeed = 32f * 1.33f;
        public const int TortoiseDashFrames = 50;
        /// <summary>航道宽度:陆龟判定盒 128 再加一点</summary>
        public const float TortoiseLaneWidth = 140f;
        /// <summary>一次冲锋的总帧数(待机 + 冲)</summary>
        public const int TortoiseCycleFrames = TortoiseAppearFrames + TortoiseDashFrames;

        //==================== 蓝色天空(全息小白龙在玩家上空横越,两程各落一排雪花;本体搁在一旁待机)====================

        /// <summary>小白龙 30 帧蓄力放出:弹幕档前摇</summary>
        public const int SkyWyvernFrame = WindupBarrage;
        /// <summary>龙在玩家上方 50 格,高度与横越中心在放出那一刻锁死,不再跟人</summary>
        public const float SkyPassHeight = 50f * 16f;
        /// <summary>单程从中心 ∓1300 飞到 ±1300(整条龙从屏外进、屏外出)</summary>
        public const float SkyPassHalfSpan = 1300f;
        public const int SkyPassFrames = 90;
        /// <summary>两程起点间隔:前一程的龙身在这段时间里飞出屏外,第二程从另一侧进</summary>
        public const int SkyPassStride = 130;
        public const int SkyPasses = 2;
        /// <summary>龙头每越过一个格点放一朵雪花,格点间距 180、第二程错半格</summary>
        public const float SkyFlakeSpacing = 180f;
        /// <summary>雪花初始下落速度 3,每帧加速 0.03:落到玩家高度约 153 帧,到底时约 7.6</summary>
        public const float SkyFlakeFall = 3f;
        public const float SkyFlakeAccel = 0.03f;
        /// <summary>散布:出手点横向 ±45px、纵向 ±40px 随机,横向漂移速度 ±1.1px/帧</summary>
        public const float SkyFlakeJitterX = 45f;
        public const float SkyFlakeJitterY = 40f;
        public const float SkyFlakeDrift = 1.1f;
        /// <summary>从龙下落到玩家高度的帧数(按初速与加速度积分),末程落完才收招</summary>
        public static readonly int SkyFlakeFallFrames = (int)System.Math.Ceiling((-SkyFlakeFall + System.Math.Sqrt(SkyFlakeFall * SkyFlakeFall + 2f * SkyFlakeAccel * SkyPassHeight)) / SkyFlakeAccel) + 1;
        /// <summary>到玩家高度之后再留 100 帧,雪花总寿命约 253 帧</summary>
        public const int SkyFlakeLinger = 100;
        public const int SkyTail = 30;
        /// <summary>本体待机悬停点:玩家一侧斜上方,够不着也不挡视线</summary>
        public static readonly Vector2 SkyIdleOffset = new Vector2(460f, -200f);
        public const int SkyWyvernSegments = 24;
        /// <summary>龙从放出到消散的总龄:末程飞完再留一段让龙尾出屏</summary>
        public const int SkyWyvernLife = SkyPassStride * (SkyPasses - 1) + SkyPassFrames + 50;

        //==================== 裂隙斩(空间缝线预告 → 拉开 → 猛合喷弹)====================

        /// <summary>缝线可见到开口的预告帧数:36,缝过的是预测点,玩家要有时间离开它</summary>
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
        /// <summary>猛合时每侧喷弹数与两档初速:偶数位快、奇数位慢,一排喷弹分成前后两层</summary>
        public const int RiftBoltsPerSide = 4;
        public const float RiftBoltSpeed = 7f * VoidBoltSpeedMult;
        public const float RiftBoltSlowSpeed = 4.5f * VoidBoltSpeedMult;
        /// <summary>缝数:P1 两道过预测点,P2 三道米字,P3 六边形笼 + 中心一刀</summary>
        public static int RiftSeams(int phase) => phase >= 3 ? 7 : phase >= 2 ? 3 : 2;
        /// <summary>P3 六边形笼半径(边线到玩家)</summary>
        public const float RiftCageRadius = 380f;
        /// <summary>预测提前量</summary>
        public const float RiftPredictLead = 14f;
        public static readonly Vector2 RiftHoverOffset = new Vector2(300f, -260f);
        public const int RiftTail = 16;

        //==================== 轨道轰炸(退入背景 → 标记 → 光柱砸落 → 俯冲归位;全场唯一的远景招)====================

        public const int OrbitalAscendFrames = 30;
        /// <summary>落点标记 50 帧</summary>
        public const int OrbitalMarkFrames = 50;
        public static int OrbitalPillars(int phase) => phase >= 3 ? 4 : 3;
        /// <summary>柱子错拍间隔 10 帧:玩家能一根根穿</summary>
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
        /// <summary>退入深处时本体的表观悬停点(玩家头顶 380px,看得见在轰炸的是谁)</summary>
        public static readonly Vector2 OrbitalHoverOffset = new Vector2(0f, -380f);
        /// <summary>退入的深度:Z 1.6 缩到 0.38,一眼读成「退到背景里」又看得清它在开炮(旧 2.2 只剩一枚小紫点)</summary>
        public const float OrbitalFarDepth = 1.6f;
        /// <summary>俯冲归位的落点:玩家侧上方 (±200, -300)</summary>
        public static readonly Vector2 OrbitalLandOffset = new Vector2(200f, -300f);

        //==================== 收缩环门(带缺口的能量环以玩家为心收拢,缺口逐环转 75°)====================

        /// <summary>本体悬停在玩家侧上方(平面,可被打),看得出环是它推出来的</summary>
        public static readonly Vector2 GateHoverOffset = new Vector2(220f, -320f);
        /// <summary>起势 30 帧:弹幕档前摇,第一环在起势末尾才出现</summary>
        public const int GateWindup = WindupBarrage;
        /// <summary>环数 P1 4 / P2 5 / P3 6</summary>
        public static int GateRings(int phase) => phase >= 3 ? 6 : phase >= 2 ? 5 : 4;
        /// <summary>环与环间隔 50 帧(原 32):同屏约三个半环,P3 六环整招约 395 帧,在单招上限内</summary>
        public const int GateInterval = 50;
        /// <summary>环出现先在起始半径停 12 帧亮出缺口(预告),再用 132 帧从 640 收到 24 后消散,约 4.7px/帧</summary>
        public const int GatePreviewFrames = 12;
        public const int GateCloseFrames = 132;
        public const float GateStartRadius = 640f;
        public const float GateEndRadius = 24f;
        /// <summary>环带判定半宽</summary>
        public const float GateThickness = 14f;
        /// <summary>缺口半角 22°:在 150px 处口宽约 115px,站到环心附近就挤不过去</summary>
        public const float GateGapHalfDeg = 22f;
        /// <summary>相邻环缺口的转角(度):P1/P2 75°,P3 105°(两环之间要沿环挪得更远)</summary>
        public static float GateGapStepDeg(int phase) => phase >= 3 ? 105f : 75f;
        /// <summary>环心 = 玩家预测点(提前 10 帧)</summary>
        public const float GateCenterLead = 10f;
        public const int GateTail = 20;

        //==================== 轰炸掠袭(本体带两艘幻影护航从一侧低空横越,一路投弹)====================

        /// <summary>航线的高度与半跨度(相对玩家):从一侧 640px 外飞到另一侧,96 帧约 13px/帧</summary>
        public const float BombRunY = -340f;
        public const float BombRunHalfSpan = 640f;
        /// <summary>起势 36 帧(接触档):护航舰从门里出来、引擎亮起</summary>
        public const int BombWindup = WindupContact;
        public const int BombRunFrames = 96;
        /// <summary>每 18 帧一轮:本体一发、护航各一发</summary>
        public const int BombDropInterval = 18;
        /// <summary>炸弹继承舰速的 40%,初始下落 2,重力 0.3/帧,末速 18:从 340px 高处约 40 帧落到玩家高度</summary>
        public const float BombInherit = 0.4f;
        public const float BombDropSpeed = 2f;
        public const float BombGravity = 0.3f;
        public const float BombMaxFall = 18f;
        /// <summary>护航舰的编队偏移(x 按航向取反:拖在本体后方斜上,炸弹晚一拍落下)</summary>
        public static readonly Vector2[] BombEscortOffsets = { new Vector2(160f, -60f), new Vector2(320f, -120f) };
        public const int BombTail = RecoveryFrames;

        //==================== 虚空奇点(后撤蓄力 → 放出 → 牵引 → 塌缩环爆)====================

        public const int SingChargeFrames = 45;
        /// <summary>放出前 8 帧把方向定死,预告之后不再改向</summary>
        public const int SingLockLead = 8;
        public const int SingActiveFrames = 150;
        /// <summary>牵引加速度(px/帧²)与朝奇点方向的速度封顶:翼/坐骑能逃</summary>
        public static float SingPullAccel(int phase) => phase >= 3 ? 0.72f : 0.58f;
        public const float SingPullMaxSpeed = 7f;
        /// <summary>牵引半径 1100,速度封顶 7 不变,翼 / 坐骑仍能逃</summary>
        public const float SingPullRadius = 1100f;
        public const float SingCoreRadius = 34f;
        public const float SingDiskRadius = 140f;
        public const int SingOrbitBoltInterval = 20;
        /// <summary>两条螺旋臂交替出弹:每颗弹角速度 0.045、半径每帧 +2.2,臂整体每帧再转 0.012,臂间的安全带随之慢慢挪</summary>
        public const float SingSpiralAngular = 0.045f;
        public const float SingSpiralGrowth = 2.2f;
        public const float SingArmDrift = 0.012f;
        public const int SingCollapseFrames = 20;
        /// <summary>环爆 16 发,两档速度交替(16 / 10),一快一慢两层环,弹匀速</summary>
        public const int SingBurstCount = 16;
        public const float SingBurstSpeed = 8f * VoidBoltSpeedMult;
        public const float SingBurstSlowSpeed = 5f * VoidBoltSpeedMult;
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
        /// <summary>四舰同步瞄准 40 帧:读四扇门里哪扇是真身要这么久;交汇点就是四扇门的圆心,开门那一刻定下,四条航道全程不跟人,冲刺沿途不留弹</summary>
        public const int FleetAimFrames = 40;
        /// <summary>冲刺总帧数:穿过交汇点后再飞 16 帧</summary>
        public const int FleetDashFrames = 36;
        public static int FleetWaves(int phase) => phase >= 2 ? 2 : 1;
        /// <summary>两波之间全员静止 30 帧:段落之间的一口气</summary>
        public const int FleetWaveHold = 30;
        public const int FleetEndFade = 8;
        /// <summary>真身门环亮度倍率:破绽要读得出来</summary>
        public const float FleetRealPortalGlow = 1.6f;
        /// <summary>出手后第 20 帧四舰同帧穿过交汇点(520 / 20 = 26px/帧)</summary>
        public const int FleetCrossFrame = 20;
        public const float FleetLaneWidth = 90f;

        //==================== 湮灭主炮(P3 压轴:锁定 → 出手 → 扫射 → 过热)====================

        /// <summary>闪到玩家正上方 480px;出手后本体钉在原地不跟人,躲到炮口水平线以上是正解</summary>
        public static readonly Vector2 CannonHoverOffset = new Vector2(0f, -480f);
        /// <summary>蓄力 75:压轴的吸气可以更长,导引线仍在出手前 40 帧亮</summary>
        public const int CannonChargeFrames = 75;
        /// <summary>导引线在出手前 40 帧亮起</summary>
        public const int CannonGuideLead = 40;
        /// <summary>汇聚粒子在 72% 处硬切成静默:尖叫前的吸气</summary>
        public const float CannonSilenceAt = 0.72f;
        public const int CannonSweepFrames = 150;
        /// <summary>扫射 180°:从玩家对侧水平线起扫,一半时间(75 帧)走到正下方,扫到玩家一侧水平线收;角速度 1.2°/帧</summary>
        public const float CannonSweepDegrees = 180f;
        public const float CannonStartOffsetDeg = CannonSweepDegrees / 2f;
        public const float CannonBeamWidth = 160f;
        public const float CannonBeamLength = 4000f;
        public const float CannonRecoil = 8f;
        public const int CannonRainInterval = 20;
        public const float CannonRainSpeed = 6f * VoidBoltSpeedMult;
        /// <summary>过热 75 帧:本体下沉冒烟、无接触、减伤清零,这是整场最大的输出窗</summary>
        public const int CannonOverheatFrames = 75;
        public const float CannonOverheatSink = 2.5f;
        public const float CannonVignette = 0.55f;
        /// <summary>亮核 = 宽×2.2,光环×1.6,要盖住射线起点的渐入</summary>
        public const float CannonHubGlowMult = 2.2f;
        public const float CannonHubRingMult = 1.6f;
        public const float CannonHubPulse = 0.08f;

        //==================== 描边 ====================
        //外环 VDRimHalo 12 抽偏移 RimBaseRadius,垫在本体下
        //内环 8 抽、半径 × RimInnerRadiusMult,亮线 VDRimLight 零偏移压在本体上
        //静默拍只压到 RimSuppressMax,不归零

        /// <summary>底噪只管亮度,不进热色与侵蚀,那两样看活跃度</summary>
        public static float RimIdle(int phase) => phase >= 3 ? RimIdleP3 : phase >= 2 ? RimIdleP2 : RimIdleP1;
        public const float RimIdleP1 = 0.55f;
        public const float RimIdleP2 = 0.65f;
        public const float RimIdleP3 = 0.75f;
        /// <summary>常态呼吸:底噪上叠 ±25% 的慢起伏,角速度 2.2 rad/s 约 2.9 秒一个来回</summary>
        public const float RimBreathAmp = 0.25f;
        public const float RimBreathSpeed = 2.2f;
        /// <summary>描边整体亮度倍率(加法混合下允许大于 1),常态与蓄力一起抬;爆闪在着色器里另乘 (1 + flash)</summary>
        public const float RimBrightness = 1.5f;
        /// <summary>静默拍最多压掉多少描边:压到 40% 留着,「常驻」意味着任何拍子都不能整圈熄灭;爆闪不受压</summary>
        public const float RimSuppressMax = 0.6f;
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

        /// <summary>外环叠画抽数:12 抽绕圈(30° 一抽),10px 半径下外沿的多边形棱看不出来;再多 GPU 白花</summary>
        public const int RimTaps = 12;
        /// <summary>内环叠画抽数:8 抽,半径小、相互重叠多,不需要更密</summary>
        public const int RimInnerTaps = 8;
        /// <summary>外环基础半径(px,乘绘制缩放)是常态一圈 10px 的外扩描边带,本体 180×122,10px 能一眼读出有描边,又不会盖住机体细节</summary>
        public const float RimBaseRadius = 10f;
        /// <summary>内环半径 = 外环半径 × 0.45:贴身那圈实心亮带,与外环的丝之间有一层由亮到暗的落差</summary>
        public const float RimInnerRadiusMult = 0.45f;
        /// <summary>蓄力满时的外扩半径增量:能量逸散得更远</summary>
        public const float RimChargeRadius = 8f;
        /// <summary>爆闪瞬间的外扩半径增量:整圈猛地炸开一下</summary>
        public const float RimFlashRadius = 14f;
        /// <summary>外环各抽的亮度分摊:外沿只有 1~2 抽重叠,单抽 0.18 合成后 0.2~0.35,是一圈软光而不是实色块</summary>
        public const float RimTapOpacity = 0.18f;
        /// <summary>内环各抽的亮度分摊:8 抽在贴身处重叠 3~4 层,合成后 ≈ 1,实心亮带</summary>
        public const float RimInnerTapOpacity = 0.30f;
        /// <summary>贴边亮线(压在本体之上那一遍)的亮度:2 texel 宽的线要压过机体自身颜色,加法下给到 1.6</summary>
        public const float RimEdgeOpacity = 1.6f;
        /// <summary>贴边亮线的颜色向 VoidWhite 偏这么多:紫线压紫机体读不出来,偏白的能量边才是「描边」</summary>
        public const float RimEdgeWhiten = 0.45f;
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

        /// <summary>常态 0.25 基本实线,旧 0.55 会打成碎丝,爆闪归 0,过热几乎不侵蚀</summary>
        public const float RimErodeIdle = 0.25f;
        public const float RimErodeCharge = 0.1f;
        public const float RimErodeOverheat = 0.05f;
        /// <summary>光晕环的侵蚀比例:外环 0.6 碎成向外逸散的丝(能量感),内环 0.15 实心贴身(常驻感);两值再各乘亮线那套的活跃度收缩</summary>
        public const float RimHaloErodeOuter = 0.6f;
        public const float RimHaloErodeInner = 0.15f;
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
        /// <summary>出场时虚空跟着门进来盖住天空,从 0 到 1 用 90 帧,比门开的 60 帧略慢,门开到一半时天空先暗</summary>
        public const int SkyEntranceFadeFrames = 90;
        /// <summary>死亡时本体漂到深处到位(210)之后虚空跟着离开,到真死(330)收完</summary>
        public const int SkyDeathFadeStart = DeathDriftEnd;
        public const int SkyDeathFadeEnd = DeathDuration;
        /// <summary>撤离用 60 帧收掉天空,本体要 190 帧才消失,天空先结束</summary>
        public const int SkyDespawnFadeFrames = 60;
        /// <summary>强度跟随上报值的每 tick 最大步长:20 帧内追上,出场/死亡的编排斜坡不会被抹平</summary>
        public const float SkyTrackPerTick = 1f / 20f;
        /// <summary>没人继续上报(本体消失)时的衰减步长,1 秒收完</summary>
        public const float SkyFallPerTick = 1f / 60f;
        /// <summary>阶段配色平滑(每 tick lerp):约 1.5 秒换完色</summary>
        public const float SkyPhaseLerp = 0.03f;
        /// <summary>星球侵蚀量三档:P1 已被啃四分之一,P3 过半;变形/护盾演出期间肉眼可见它再被啃掉一块</summary>
        public static float SkyErosion(int phase) => phase >= 3 ? 0.70f : phase >= 2 ? 0.45f : 0.25f;
        /// <summary>侵蚀逼近步长(每 tick):0.2 的档差走 120 帧,恰好铺满变形演出</summary>
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
        /// <summary>星球随镜头的视差(屏高单位 / 世界像素 ÷ 屏高):整个 80 格场地跑满也只挪约 0.05 屏</summary>
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
        /// <summary>画面中心(玩家所在)底幕的压暗量与半径(屏高单位),紫弹在那里和天幕拉开明度</summary>
        public const float SkyFocusDim = 0.3f;
        public const float SkyFocusRadius = 0.45f;
    }
}
