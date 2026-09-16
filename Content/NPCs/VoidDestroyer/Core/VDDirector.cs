using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.Core
{
    /// <summary>
    /// 虚空驱逐舰调参中心:全部数字、全部阶段档位都从这里出,状态里不许出现裸数字。
    /// 策划表以大师 ×3 显示值书写,代码存普通模式基值;弹幕伤害经 <see cref="VoidDestroyer.ProjDamage"/> 折算。
    /// 身份:月后 T2 的虚空战舰,三阶段(75% 变形展翼 / 30% 护盾 + 压轴主炮),
    /// 节奏对标快节奏 Boss:连接段几帧、冷却十几帧、每招内部就位达标即跳拍,没有等自己计时的空转
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

        //==================== 节奏(hub 连接段与冷却:P1 → P3 越来越急)====================

        /// <summary>hub 重瞄悬停帧数:只是换招的一口气,不是巡游</summary>
        public static int HubConnectorFrames(int phase) => phase >= 3 ? 6 : phase >= 2 ? 10 : 14;
        /// <summary>收招后的冷却</summary>
        public static int AttackCooldown(int phase) => phase >= 3 ? 4 : phase >= 2 ? 10 : 18;
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
        public const int ArcVolleyInterval = 25;
        public const int ArcVolleys = 3;
        public const float ArcBoltSpeed = 14f;
        /// <summary>两侧四发的张角(度)</summary>
        public const float ArcInnerDeg = 25f;
        public const float ArcOuterDeg = 50f;
        /// <summary>末轮后收尾(旧 40 → 20:剪死等)</summary>
        public const int ArcTail = 20;

        //==================== 追踪导弹(蓄力 + 激光流 + 导弹环 + 核弹)====================

        public static readonly Vector2 MissileHoverOffset = new Vector2(0f, -260f);
        public const float MissileHoverSpeed = 6f;
        public const float MissileHoverAccel = 0.05f;
        public const float MissileHoverSlow = 150f;
        public const int MissileChargeFrames = 60;
        public const int MissileLaserEnd = 180;
        public const int MissileLaserInterval = 4;
        public const float MissileLaserSpeed = 22f;
        public const float MissileLaserJitter = 0.05f;
        public static int MissileVolleys(int phase) => phase >= 2 ? 4 : 3;
        public static int MissileVolleyInterval(int phase) => phase >= 2 ? 90 : 120;
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

        //==================== 幻影冲刺(门后待机 + 五连冲)====================

        public const int PhantomDashes = 5;
        public const float PhantomDashSpeed = 40f;
        public const int PhantomDashFrames = 30;
        public const int PhantomFadeFrames = 6;
        public const float PhantomPortalOffset = 480f;
        public const int PhantomPortalLife = 60;
        public const int PhantomWaitFrames = 30;
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
        public const int FlameVolleyInterval = 90;
        public const int FlameVolleys = 6;
        public const int FlameCountMin = 9;
        public const int FlameCountMax = 12;
        public const float FlameSpeedMin = 8f;
        public const float FlameSpeedMax = 12f;
        public const float FlameSpread = 1.05f;
        public const int FlameRiseDelay = 20;
        /// <summary>每轮出手前 20 帧的向下锥形预告</summary>
        public const int FlameTelegraph = 20;
        /// <summary>末轮后收尾(旧 70 → 20)</summary>
        public const int FlameTail = 20;

        //==================== 相位激光(无人机纵列 + 井字网)====================

        public static int LaserColumns(int phase) => phase >= 2 ? 11 : 10;
        public static readonly Vector2 LaserHoverOffset = new Vector2(0f, -400f);
        public const int LaserDecorDrones = 12;
        public const int LaserDecorFrames = 34;
        public const float LaserColumnX = 35f * 16f;
        public const int LaserColumnDrones = 9;
        public const float LaserDroneSpacing = 48f;
        public const int LaserWarnTime = 60;
        public const float LaserColumnLength = 2400f;
        public const int LaserColumnsToGrid = 90;
        public const int LaserGridRows = 9;
        public const int LaserGridCols = 13;
        public const float LaserGridOffset = 600f;
        public const float LaserGridLength = 1200f;
        public const int LaserGridTail = 110;
        /// <summary>第 k 列的出现时刻:P1 固定 45 帧;P2 起间隔从 45 线性收缩到 30</summary>
        public static int LaserColumnTime(int k, bool ex) => ex ? (int)(45f * k - 0.75f * k * (k - 1)) : 45 * k;

        //==================== 传送火弹(四角依次闪现扇射)====================

        /// <summary>四角顺序:左上 → 左下 → 右下 → 右上</summary>
        public static readonly int[] TeleportFireOrder = { 0, 2, 3, 1 };
        public const float TeleportFireOffset = 480f;
        public const int TeleportFireShotFrame = 8;
        public const int TeleportFireBolts = 5;
        public const float TeleportFireSpreadDeg = 15f;
        public const float TeleportFireBoltSpeed = 7f;
        public const int TeleportFireCornerFrames = 30;
        /// <summary>末角后收尾(旧 30 → 20)</summary>
        public const int TeleportFireTail = 20;

        //==================== 支援投送 ====================

        public static readonly Vector2 ReinforceHoverOffset = new Vector2(0f, -500f);
        public const float ReinforceHoldStiffness = 0.06f;
        public const float ReinforceHoldLerp = 0.2f;
        public const float ReinforceHoldMaxSpeed = 20f;
        public const int ReinforceSpawnFrame = 20;
        /// <summary>收招(旧 120 → 90)</summary>
        public const int ReinforceDuration = 90;
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
        public const int RedHellTridentFrame1 = 60;
        public const int RedHellTridentFrame2 = 70;
        public const int RedHellTridentFrameFTW = 80;
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
        public const int JungleSpawnFrame = 30;
        public static int JungleDashes(int phase) => phase >= 3 ? 7 : 6;
        public const int JungleTail = 60;

        //==================== 蓝色天空(全息小白龙 + 形状弹幕)====================

        public const int SkyWyvernFrame = 20;
        public const int SkyBurstStart = 60;
        public const int SkyBurstDuration = 720;
        public static int SkyBurstInterval(int phase) => phase >= 3 ? 24 : 30;
        public const int SkyBurstCount = 60;
        public const float SkyBurstRadius = 120f;
        public const float SkyBurstSpeed = 6f;
        public const int SkyTail = 60;

        //==================== 裂隙斩(空间缝线预告 → 拉开 → 猛合喷弹)====================

        /// <summary>缝线可见到开口的预告帧数:0.4 秒,快节奏预告档</summary>
        public const int RiftAimFrames = 24;
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
        public const int OrbitalMarkFrames = 40;
        public static int OrbitalPillars(int phase) => phase >= 3 ? 5 : 4;
        /// <summary>柱子错拍间隔 8 帧:玩家能一根根穿</summary>
        public const int OrbitalPillarStagger = 8;
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
        public const int FleetPortalLife = 60;
        public const int FleetAimFrames = 30;
        public const float FleetDashSpeed = 38f;
        public const int FleetDashFrames = 34;
        public static int FleetWaves(int phase) => phase >= 2 ? 2 : 1;
        public const int FleetTrailInterval = 6;
        public const int FleetEndFade = 8;
        /// <summary>真身门环亮度倍率:破绽要读得出来</summary>
        public const float FleetRealPortalGlow = 1.6f;

        //==================== 湮灭主炮(P3 压轴:锁定 → 出手 → 扫射 → 过热)====================

        public static readonly Vector2 CannonLockOffset = new Vector2(0f, -600f);
        public const int CannonChargeFrames = 60;
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
    }
}
