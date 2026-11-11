using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.Core
{
    /// <summary>
    /// 沙海冥龙的全部数值,状态里只留插值系数
    /// 伤害存普通档基值,专家和大师由原版在血量缩放与命中结算时乘
    /// 拍点都是状态 Timer 的区间,1 格 = 16 像素
    /// </summary>
    public static class SandseaDirector
    {
        //==================== 基础数值 ====================

        /// <summary>普通 / 专家 / 大师 = 48400 / 96800 / 145200</summary>
        public const int BaseLife = 48400;
        /// <summary>接触伤害,普通 / 专家 / 大师 = 75 / 150 / 225</summary>
        public const int BaseDamage = 75;
        public const int DefenseHead = 40;
        public const int DefenseBody = 35;
        public const int DefenseTail = 20;
        public const float DamageReduction = 0.2f;
        public const float Phase2LifeRatio = 0.6f;
        /// <summary>锁血线,到线后进入鲨卷风</summary>
        public const float Phase3LifeRatio = 0.1f;
        /// <summary>头部接触施加的酸性毒液,不随难度变,单位是帧</summary>
        public const int VenomFrames = 120;
        /// <summary>目标超出 150 格即脱战</summary>
        public const float DisengageDistance = 2400f;
        /// <summary>超距的容错帧数,防单帧误判</summary>
        public const int DisengageGraceFrames = 30;
        /// <summary>目标离开沙漠 3 秒后脱战</summary>
        public const int LeaveDesertFrames = 180;
        public const int DespawnFrames = 150;
        /// <summary>脱战下潜这么多帧内目标回来就接着打</summary>
        public const int DespawnReturnWindow = 90;

        //==================== 伤害档(普通档基值) ====================
        //弹幕 damage 字段存这里的数,命中时原版乘 2 再乘难度倍率 1 / 2 / 3

        /// <summary>毒针 56 / 112 / 168</summary>
        public const int DmgNeedle = 28;
        /// <summary>沙鲨射弹 60 / 120 / 180</summary>
        public const int DmgShark = 30;
        /// <summary>甲虫炸弹,一阶段 70 / 140 / 210,二阶段 72 / 144 / 216</summary>
        public static int DmgBomb(int phase) => phase >= 2 ? 36 : 35;
        /// <summary>沙弹 56 / 112 / 168</summary>
        public const int DmgSandBlast = 28;
        /// <summary>边界旋风与鲨卷风 120 / 240 / 360</summary>
        public const int DmgWall = 60;
        /// <summary>小型禁戒龙卷风,策划未给值,取沙鲨档 60 / 120 / 180</summary>
        public const int DmgTwister = 30;
        public const int SharkCursedFrames = 120;
        public const int SharkIchorFrames = 300;
        public const int SharkSlowFrames = 120;

        //==================== 身体 ====================
        //关节坐标是贴图像素,贴图头朝左,由离线拼装量得(Doc/plans/2026-11-11-sandsea-dragon/tools/boss/assemble.py)

        /// <summary>各节贴图里接前一节的点,头部这一项对应 NPC.Center</summary>
        public static readonly Vector2[] JointFront =
        {
            new(104f, 104f), new(34f, 76f), new(30f, 78f), new(30f, 56f), new(34f, 62f),
        };
        /// <summary>各节贴图里接后一节的点,尾部这一项是尾尖</summary>
        public static readonly Vector2[] JointBack =
        {
            new(168f, 100f), new(138f, 80f), new(126f, 82f), new(132f, 62f), new(200f, 60f),
        };
        public const int SegmentCount = 5;
        /// <summary>链上记的头部轨迹点间距,单位是像素</summary>
        public const float TrailSpacing = 10f;
        /// <summary>轨迹点上限,960 像素盖住 533 像素的身长</summary>
        public const int TrailCapacity = 96;
        /// <summary>摆尾波长,约 0.8 个身长,身上同时看得到大半个 S</summary>
        public const float WaveLength = 420f;
        /// <summary>尾尖摆幅,头部为 0,沿身体按 1.3 次方增长</summary>
        public const float WaveAmpTail = 34f;
        public const float WaveSpeedBase = 0.09f;
        /// <summary>每 1 像素/帧的速度给摆尾相位加的角速度</summary>
        public const float WaveSpeedPerVelocity = 0.011f;
        /// <summary>弓身满值时尾尖的侧向偏移</summary>
        public const float CurlTailOffset = 150f;
        /// <summary>朝向翻面的死区,竖直游动时不来回翻</summary>
        public const float FlipDeadZone = 0.3f;
        /// <summary>下颌张开的最大角,单位是弧度</summary>
        public const float JawMaxOpen = 0.36f;
        /// <summary>下颌转轴在头部贴图里的像素坐标,与拆图脚本 derive.py 的 HINGE 一致</summary>
        public static readonly Vector2 JawHinge = new(112f, 132f);

        //==================== 运动 ====================

        public const float Gravity = 0.42f;
        public const float MaxFallSpeed = 28f;
        /// <summary>节距约 100、相邻两节最大折角 42 度时头部的最小转弯半径是 140,所有转向都不小于它</summary>
        public const float TurnRadius = 150f;
        /// <summary>埋在沙里时的转弯半径:身体被物块挡着,折角看不见,可以转得急</summary>
        public const float BuriedTurnRadius = 80f;
        /// <summary>沙里上航道用的更急的半径与更短的前视距离,短距离内就能对准</summary>
        public const float TightTurnRadius = 45f;
        public const float TightLookahead = 70f;
        /// <summary>沙里巡航时离沙面不到这么深就不许再往上,免得无故冒头</summary>
        public const float MinSwimDepth = 64f;
        /// <summary>随玩家平移的参考系速度封顶,玩家冲刺时本体不跟着暴冲</summary>
        public const float CarryMax = 10f;
        /// <summary>破沙上浮前先潜到这么深就位,再沿竖直线游到 RiseStartDepth,出沙时头朝上且横坐标对准</summary>
        public const float RiseDeepDepth = 420f;
        public const float RiseStartDepth = 150f;
        /// <summary>沿竖直线上游占潜行段的最后这么多帧</summary>
        public const int RiseLeadFrames = 34;
        public const float SwimSpeed = 14f;
        /// <summary>巡航时头部在沙面下的深度,背鳍不露</summary>
        public const float CruiseDepth = 230f;
        /// <summary>浅游深度,背鳍刚好破出沙面</summary>
        public const float ShallowDepth = 96f;
        /// <summary>沙面探测向上下各找的格数</summary>
        public const int SurfaceProbeTiles = 140;

        //==================== 连接段 ====================

        public static int ConnectorFrames(int phase) => phase >= 2 ? 44 : 54;
        public const int AttackTimeoutFrames = 60 * 20;

        //==================== 竞技场 ====================

        /// <summary>两道边界旋风各离召唤点 100 格</summary>
        public const float ArenaHalfWidth = 1600f;
        /// <summary>旋风高 100 格,另向地面以下延伸 25 格</summary>
        public const float ArenaWallHeight = 1600f;
        public const float ArenaWallBelow = 400f;
        /// <summary>判定半宽,可见体半宽约 76</summary>
        public const float ArenaWallHalfWidth = 52f;
        /// <summary>旋风成形帧数,成形前没有判定</summary>
        public const int ArenaWallFormFrames = 90;
        /// <summary>三阶段每秒合拢 2 格</summary>
        public const float ArenaCloseSpeed = 32f / 60f;
        /// <summary>合拢下限,停在鲨卷风顶宽之外</summary>
        public const float ArenaMinHalfWidth = 420f;

        //==================== 演出 ====================

        /// <summary>登场 190 帧:0-70 沙面震动、旋风升起,70 破沙跃过玩家头顶,落回沙里收尾</summary>
        public const int EntranceDuration = 190;
        public const int EntranceLeapAt = 70;
        /// <summary>破沙点离玩家 520 像素,弧顶在玩家正上方,横向速度约 10,弧顶足够圆</summary>
        public const float EntranceBreachDist = 520f;
        public const float EntranceApex = 430f;
        public const float EntranceMaxHorizontalSpeed = 14f;
        /// <summary>转阶段 120 帧:游到展示位,60 帧战吼亮鳍并切阶段,随后下潜</summary>
        public const int ShiftDuration = 120;
        public const int ShiftBurstFrame = 60;
        public const float ShiftDisplayHeight = 280f;
        /// <summary>锁血喷白 70 帧后赶往场地中央,最多再给 150 帧</summary>
        public const int FinaleBurstFrames = 70;
        public const int FinaleTravelMax = 150;
        public const float FinaleTravelSpeed = 20f;
        /// <summary>死亡 70 帧:每 10 帧从尾到身炸掉一节,60 帧头部落地</summary>
        public const int DeathDuration = 70;
        public const int DeathSegmentInterval = 10;
        public const int DeathHeadDropFrame = 60;
        /// <summary>客户端计时可能落后几帧,收到击杀包时不把自己救活</summary>
        public const int DeathKillTolerance = 10;

        //==================== 跳跃追逐(C) ====================

        public const int LeapCount = 3;
        /// <summary>首跳出手帧,其后每 2.5 秒一跳</summary>
        public const int LeapFirstAt = 80;
        public const int LeapPeriod = 150;
        /// <summary>起跳前摇:收速下沉加沙面喷涌</summary>
        public const int LeapWindup = 30;
        /// <summary>出手前这么多帧锁定落点,弹道随即画出</summary>
        public const int LeapLockLead = 12;
        /// <summary>前摇开始时到玩家的水平距离;前摇里再滑近约 100,实际起跳距离约 350</summary>
        public const float LeapLaunchDist = 460f;
        /// <summary>每跳之前先游到起跳点外侧这么远再回头,掉头在沙里完成</summary>
        public const float LeapOuterMargin = 110f;
        /// <summary>沙里游向起跳点的速度,落地到下次前摇只有 60 帧,要游过 700 像素</summary>
        public const float LeapStalkSpeed = 19f;
        /// <summary>从外侧待机点上航道留的帧数,剩得比这多就先在外侧深处等</summary>
        public const int LeapApproachFrames = 40;
        public const float LeapWindupSpeed = 2.5f;
        /// <summary>锁点后把头朝起跳方向抬起来的速度,出手时不折角</summary>
        public const float LeapLiftSpeed = 4f;
        /// <summary>弧顶高出沙面的下限,玩家站在地面时也跳出一个可见的弧</summary>
        public const float LeapMinApex = 110f;
        /// <summary>弧顶高出锁定点的量,保证下落段穿过锁定点</summary>
        public const float LeapExtraRise = 96f;
        /// <summary>前两跳最高 30 格,第三跳 60 格</summary>
        public static float LeapMaxApex(int index) => index >= 2 ? 960f : 480f;
        /// <summary>第三跳重力加三成,读作速度略快</summary>
        public static float LeapGravity(int index) => index >= 2 ? Gravity * 1.3f : Gravity;
        public const float LeapMaxHorizontalSpeed = 16f;
        /// <summary>横向速度下限:弧顶的曲率半径是横向速度的平方除以重力,7 对应 117 像素,再小身体就在弧顶对折</summary>
        public const float LeapMinHorizontalSpeed = 7f;
        /// <summary>深处起跳这么多帧还没出沙就不算这一跳</summary>
        public const int LeapAbortFrames = 40;
        public static int LeapLaunchAt(int index) => LeapFirstAt + index * LeapPeriod;
        /// <summary>第三跳落地后一阶段的收招帧</summary>
        public const int LeapTailFrames = 130;

        //二阶段追加:跳完后停到玩家正下方,放三轮合拢的小龙卷
        public const int TwisterCycles = 3;
        /// <summary>停到玩家脚下的落位帧数</summary>
        public const int PerchSettleFrames = 70;
        public const int TwisterGrow = 20;
        /// <summary>成形后原地停留 0.5 秒</summary>
        public const int TwisterHold = 30;
        /// <summary>合拢用时 2 秒</summary>
        public const int TwisterClose = 120;
        public const int TwisterFade = 10;
        public const int TwisterLife = TwisterGrow + TwisterHold + TwisterClose + TwisterFade;
        /// <summary>生成点离合拢点 20 格</summary>
        public const float TwisterStartOffset = 320f;
        /// <summary>高 12 格</summary>
        public const float TwisterHeight = 192f;
        public const float TwisterHalfWidth = 26f;
        public const int TwisterRingBlasts = 10;
        public const float TwisterRingSpeed = 3.4f;
        /// <summary>环形沙弹从合拢点上方这么高处放出,免得贴地即灭</summary>
        public const float TwisterRingHeight = 84f;
        /// <summary>头部探出沙面时中心在沙面上方的高度</summary>
        public const float PerchHeadRise = 30f;
        public const float PerchSpeed = 12f;
        /// <summary>探头前先潜到玩家正下方这么深,再沿竖直线上来,头是竖着出沙的</summary>
        public const float PerchDeepDepth = 430f;
        public static int PerchStartAt => LeapLaunchAt(LeapCount - 1) + LeapTailFrames;
        public static int TwisterSpawnAt(int cycle) => PerchStartAt + PerchSettleFrames + cycle * TwisterLife;
        public static int PerchEndAt => TwisterSpawnAt(TwisterCycles) + 20;

        //==================== 跳跃冲刺(A) ====================

        public const int DashCount = 3;
        /// <summary>对齐时到玩家的水平距离</summary>
        public const float DashStandoff = 420f;
        /// <summary>首冲的锁向帧,此前是潜行、上浮与对齐</summary>
        public const int DashFirstLockAt = 150;
        /// <summary>沙中潜到上浮点的帧数</summary>
        public const int DashSubmergeFrames = 46;
        /// <summary>航道冻结到出手的帧数,期间只沿航道缓慢滑行</summary>
        public const int DashLockFrames = 14;
        public const int DashFlyFrames = 46;
        /// <summary>两次冲刺之间的间隔,含下一次的锁向帧</summary>
        public const int DashInterval = 100;
        public const int DashCycle = DashFlyFrames + DashInterval;
        /// <summary>接近猪鲨二阶段,46 帧飞 60 格;二阶段速度与距离各加 15%</summary>
        public static float DashSpeed(int phase) => phase >= 2 ? 24.15f : 21f;
        public const int DashRampFrames = 4;
        /// <summary>冲完收到折返速度的帧数</summary>
        public const int DashRecoverFrames = 8;
        /// <summary>锁向期间沿航道滑行的速度,14 帧滑 70 像素让颈部摆正</summary>
        public const float DashGlideSpeed = 5f;
        /// <summary>上浮后压上航道的四分之一圆:从对齐位外侧一个半径、航道下方一个半径处头朝上起转,终点正好在对齐位</summary>
        public const float DashArcRadius = TurnRadius;
        public const float DashArcSpeed = 15f;
        /// <summary>上浮要在这一帧前到达起转高度,玩家越高上浮越快</summary>
        public const int DashRiseBy = 110;
        public const float RiseSpeedMin = 17f;
        public const float RiseSpeedMax = 34f;
        /// <summary>首冲锁向前这么多帧还没到起转高度就强制起转</summary>
        public const int DashForceTurnLead = 34;
        /// <summary>
        /// 两冲之间的折返:向上兜 270 度再反向转 90 度,合起来是一个 e 字,终点水平落回原航道、朝向玩家,
        /// 比起点靠近玩家两个半径;半径 130 时尾节折角约 63 度,15 像素/帧走完要 55 帧
        /// </summary>
        public const float DashLoopRadius = 130f;
        public const float DashLoopSpeed = 15f;
        /// <summary>驶入航道的前视距离</summary>
        public const float LaneLookahead = 200f;
        public const float DashLaneLength = 1180f;
        /// <summary>航道边线到中线的距离,本体判定高 110</summary>
        public const float DashLaneHalfWidth = 62f;
        public static int DashLockAt(int index) => DashFirstLockAt + index * DashCycle;
        public static int DashFlyAt(int index) => DashLockAt(index) + DashLockFrames;
        public static int DashEndAt(int index) => DashFlyAt(index) + DashFlyFrames;
        /// <summary>末冲后下潜入沙的帧数</summary>
        public const int DashExitFrames = 80;

        //末冲沿途的毒针
        public const int NeedleCount = 12;
        public const float NeedleStartSpeed = 1.6f;
        /// <summary>每帧速度乘数</summary>
        public const float NeedleAccel = 1.05f;
        public const float NeedleMaxSpeed = 15f;
        /// <summary>出生后这么多帧只悬停蓄势,不前进</summary>
        public const int NeedleChargeFrames = 20;
        public const int NeedleLife = 260;
        /// <summary>单数针瞄的提前量,按玩家速度乘这么多帧</summary>
        public const float NeedleLeadFrames = 26f;

        //二阶段随冲的沙鲨,与本体排成 > 形
        public static int EscortPerSide => Main.getGoodWorld ? 5 : 3;
        /// <summary>相邻两只的竖向间隔 4 格</summary>
        public const float EscortSpacingY = 64f;
        public const float EscortSpacingBack = 72f;
        /// <summary>本体收速后继续沿原路飞出场外的帧数</summary>
        public const int EscortExitFrames = 150;

        //==================== 沙鲨冲击(B) ====================

        /// <summary>悬停点相对玩家的偏移,X 乘侧向,合 36 格</summary>
        public static readonly Vector2 BarrageHoverOffset = new(407f, -407f);
        /// <summary>
        /// 上浮后压向悬停点的弧:头朝上起转 135 度,终点在悬停点上,头正对玩家
        /// 起转点在悬停点外侧 R(1+√½)、下方 R√½ 处,半径 150 时是外 256 下 106
        /// </summary>
        public const float BarrageArcRadius = TurnRadius;
        public const float BarrageArcSpeed = 15f;
        /// <summary>上浮要在这一帧前到达起转高度</summary>
        public const int BarrageRiseBy = 74;
        /// <summary>到这一帧还没到起转高度就强制起转</summary>
        public const int BarrageForceTurnAt = 100;
        public const int BarrageSubmergeFrames = 40;
        /// <summary>首波开始帧,此前是潜行、上浮与驶入</summary>
        public const int BarrageFirstVolleyAt = 140;
        public static int BarrageSharks => Main.getGoodWorld ? 20 : 10;
        public static float BarrageRingRadius => Main.getGoodWorld ? 320f : 240f;
        /// <summary>一圈沙鲨在 1 秒内生成完</summary>
        public const int BarrageSpawnSpan = 60;
        /// <summary>生成完再等 1 秒才发射</summary>
        public const int BarrageLaunchAt = 120;
        /// <summary>眼部蓝光先于发射的帧数</summary>
        public const int BarrageEyeFlashLead = 14;
        public static int BarrageLaunchGap => Main.getGoodWorld ? 2 : 3;
        public static int BarrageVolleys(int phase) => phase >= 2 ? 3 : 1;
        /// <summary>二阶段两波之间等 100 帧</summary>
        public const int BarrageVolleyWait = 100;
        public static int BarrageVolleyLength => BarrageLaunchAt + BarrageSharks * BarrageLaunchGap + 10;
        public static int BarrageVolleyAt(int index) => BarrageFirstVolleyAt + index * (BarrageVolleyLength + BarrageVolleyWait);
        public const int BarrageExitFrames = 90;
        public const float BarrageHoldSpeed = 26f;
        /// <summary>与世纪之花种子相近</summary>
        public const float SharkLaunchSpeed = 14f;
        public const int SharkLife = 300;
        /// <summary>沙鲨圈的转速,弧度每帧,约 8.7 秒转一圈</summary>
        public const float SharkRingSpin = 0.012f;

        //==================== 甲虫炸弹(D) ====================

        public const int BombSubmergeFrames = 50;
        public const int BombWindup = 30;
        public static int BombLaunchAt => BombSubmergeFrames + BombWindup;
        /// <summary>破沙点到玩家的水平距离,35 格</summary>
        public const float BombBreachDist = 560f;
        /// <summary>弧顶停在玩家近侧这么远,给俯冲的转向弧留出地方</summary>
        public const float BombApexShort = 170f;
        /// <summary>跃起的横向速度上下限,下限保证弧顶是圆的(曲率半径 134 像素)</summary>
        public const float BombMinHorizontalSpeed = 7.5f;
        public const float BombMaxHorizontalSpeed = 12f;
        /// <summary>破沙前停在破沙点正下方这么深</summary>
        public const float BombLaunchDepth = 230f;
        /// <summary>弧顶高出玩家的量</summary>
        public const float BombApexAbovePlayer = 260f;
        public const float BombMinApex = 320f;
        /// <summary>最高跃到沙面上方 70 格</summary>
        public const float BombMaxApex = 1120f;
        public static int BombCount(int phase) => phase >= 2 ? 7 : 5;
        /// <summary>爆炸半径 8 格,FTW 12 格</summary>
        public static float BombRadius => Main.getGoodWorld ? 192f : 128f;
        /// <summary>抛出 3 秒后爆炸</summary>
        public const int BombFuse = 180;
        public const int BombTravelFrames = 40;
        public const int BombBlastFrames = 8;
        /// <summary>出手后这么多帧开始抛,之后每 4 帧一个</summary>
        public const int BombTossDelay = 12;
        public const int BombTossInterval = 4;
        /// <summary>悬停点离玩家的最大水平距离,40 格</summary>
        public const float BombSpreadX = 640f;
        /// <summary>悬停带在玩家上方 14 格到下方 4 格之间,带外总是安全</summary>
        public const float BombBandUp = 224f;
        public const float BombBandDown = 64f;
        /// <summary>悬停点离沙面的最小高度</summary>
        public const float BombGroundClear = 80f;
        /// <summary>两个炸弹圆心的最小间距,按爆炸半径的倍数</summary>
        public const float BombMinSpacing = 1.5f;
        /// <summary>弧顶前这么多帧锁定俯冲方向并画出航道</summary>
        public const int DiveLockLead = 14;
        public const float DiveSpeed = 22f;
        /// <summary>弧顶转入俯冲的转弯半径与每帧加速;预告航道从这段弧的终点画起</summary>
        public const float DiveTurnRadius = 140f;
        public const float DiveAccel = 2.5f;
        /// <summary>俯冲线与水平的最小夹角 35 度,保证扎得进地面</summary>
        public const float DiveMinAngle = 0.61f;
        public const int DiveMaxFrames = 110;
        public const float DiveLaneLength = 1400f;
        public const int SlamBlastsMin = 24;
        public const int SlamBlastsMax = 28;
        /// <summary>沙弹只在正上方左右各 50 度的扇面里,扇面外是安全区</summary>
        public const float SlamFanHalfAngle = 0.87f;
        public const float SlamSpeedMin = 6.5f;
        public const float SlamSpeedMax = 13f;
        /// <summary>沙弹上升 1.5 秒后转入缓落</summary>
        public const int SandBlastRiseFrames = 90;
        public const float SandBlastFallAccel = 0.05f;
        public const float SandBlastFallMax = 3.2f;
        public const int SandBlastLife = 460;
        public const int BombExitFrames = 46;

        //==================== 鲨卷风(三阶段) ====================

        /// <summary>盘旋半径,不小于最小转弯半径;533 像素的身长绕满半圈多一点</summary>
        public const float SpinRadius = 160f;
        public const float SpinSpeed = 14f;
        /// <summary>盘旋圆心跟随玩家高度的速度上限</summary>
        public const float SpinFollowSpeed = 6f;
        public const float SpinAlpha = 0.55f;
        /// <summary>龙卷风 2.5 秒成形</summary>
        public const int MaelstromFormFrames = 150;
        /// <summary>高 80 格,顶宽 40 格</summary>
        public const float MaelstromHeight = 1280f;
        public const float MaelstromTopHalfWidth = 320f;
        public const float MaelstromBottomHalfWidth = 56f;
        public const int MaelstromFadeFrames = 40;
        /// <summary>沙鲨雨在进入本态 3 秒后开始</summary>
        public const int RainStartAt = 180;
        /// <summary>生成率从 3 只/秒爬到上限用的帧数</summary>
        public const int RainRampFrames = 360;
        public const float RainRateMin = 3f;
        /// <summary>每秒最多 8 只,FTW 10 只</summary>
        public static float RainRateMax => Main.getGoodWorld ? 10f : 8f;
        /// <summary>每秒下落 10 格</summary>
        public const float RainSpeed = 160f / 60f;
        /// <summary>斜落方向与竖直的夹角</summary>
        public const float RainSlant = 0.6f;
        public const int RainLife = 600;
        /// <summary>生成点在玩家上方这么高,出屏幕上沿</summary>
        public const float RainSpawnHeight = 640f;
        public const float RainHalfSpan = 1100f;
        /// <summary>相邻两只生成点的最小横向间距,不排成墙</summary>
        public const float RainMinGap = 150f;
        /// <summary>GFB 里沙鲨换成仙人球的概率</summary>
        public const float RainCactusChance = 0.2f;
        /// <summary>三阶段每隔这么多帧查一次沙尘暴还在不在</summary>
        public const int WeatherCheckFrames = 300;

        //==================== 表现 ====================

        /// <summary>眼睛在头部贴图里的像素坐标</summary>
        public static readonly Vector2 EyeInSprite = new(54f, 94f);
        /// <summary>二阶段背鳍辉光的常态亮度</summary>
        public const float GlowPhase2 = 0.75f;
        public const float GlowPhase3 = 1f;

        /// <summary>已经过的帧数在区间里的进度,钳到 0..1</summary>
        public static float Progress(int timer, int from, int length)
            => length <= 0 ? 1f : MathHelper.Clamp((timer - from) / (float)length, 0f, 1f);

        /// <summary>普通档基值转成弹幕 damage 字段,FTW 世界补上本体接触伤害多乘的比例</summary>
        public static int ProjDamage(int normalBase) {
            float mult = 1f;
            if (Main.getGoodWorld && !Main.GameModeInfo.IsJourneyMode) {
                float m = Main.GameModeInfo.EnemyDamageMultiplier;
                mult = (m + 1f) / m;
            }
            return Math.Max(1, (int)Math.Round(normalBase * mult));
        }
    }
}
