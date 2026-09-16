using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.Core
{
    /// <summary>
    /// 虚无双子的唯一数字出口。状态里不许出现裸数字(纯局部插值系数除外)。
    /// <para>
    /// 本文件是状态机迁移时从原 <c>NihilityActeriophage.AI()</c> 的十五段 <c>aitype</c> 分支里
    /// 逐个搬出来的,<b>数值一律照搬,没有一处调整</b>。注释写的是「这个数在原代码里干什么」,
    /// 不是「这个数为什么该是这样」。
    /// </para>
    /// <para>
    /// 原代码没有难度系数(enrange)那一套,一阶段与二阶段各七手的手感全靠这些常量,
    /// 所以这里不补任何按难度缩放的折算口。
    /// </para>
    /// </summary>
    internal static class NihilityDirector
    {
        //==================== 全局 ====================

        /// <summary>
        /// 状态总龄上限。原代码<b>没有</b>任何超时兜底,这是迁移时新加的纯安全网。
        /// 最长的实战状态是二阶段激光(460 帧)与一阶段对拉(500 帧),5400 帧在正常对局里到不了,
        /// 存在的意义只是不让状态机死在某个状态里、Boss 靠惯性飘走
        /// </summary>
        public const int StateTimeoutFrames = 5400;

        /// <summary>出场动画帧数。归零前 AI 直接 return,状态机不推进</summary>
        public const int SpawnAnimFrames = 150;
        /// <summary>出场动画的屏震包络:每帧 +5/120,振幅 = 3.2 × 包络</summary>
        public const float SpawnShakeRise = 5f / 120f;
        public const float SpawnShakeAmplitude = 3.2f;

        /// <summary>细胞丢失后的重生轮询间隔(权威端)</summary>
        public const int CellRespawnPollFrames = 5;
        /// <summary>原代码的 netSpam 手动钳位:超过 10 压回 9 并强制同步</summary>
        public const int NetSpamClamp = 10;
        public const int NetSpamClampTo = 9;
        /// <summary>天顶分身的生成散布半径</summary>
        public const float ZenithCloneScatter = 500f;

        /// <summary>虚无天空滤镜的逐帧续期值(仅本地玩家)</summary>
        public const int NihSkyRefresh = 20;

        /// <summary>每帧最后一道统一阻尼,所有状态都吃</summary>
        public const float GlobalDrag = 0.996f;

        /// <summary>绳索:段数 30、刚度 0.006、迭代 15;段长按两端距离 / 35 动态给</summary>
        public const int RopeSegments = 30;
        public const float RopeStiffness = 0.006f;
        public const int RopeIterations = 15;
        public const float RopeSegmentDivisor = 35f;
        /// <summary>绳索尾端的挂点:本体中心沿朝向 +90° 偏移 64</summary>
        public const float RopeAnchorOffset = 64f;

        /// <summary>脱战:上浮加速度、逐帧阻尼、细胞回收力,以及消失倒计时</summary>
        public const float EscapeRiseAccel = 1.26f;
        public const float EscapeDrag = 0.98f;
        public const float EscapeCellPull = 0.0022f;
        public const int EscapeDespawnFrames = 180;

        /// <summary>转二阶段:原代码是<b>整数除法</b> <c>life &lt; lifeMax / 2</c>,照搬</summary>
        public const int Phase2LifeDivisor = 2;
        /// <summary>转阶段瞬间给全体玩家的无敌帧</summary>
        public const int Phase2GraceFrames = 120;

        //==================== 弹幕伤害折算 ====================

        /// <summary>绝大多数弹幕:<c>NPC.damage / 6</c>(整数除法)</summary>
        public const int ProjDamageDivisor = 6;
        /// <summary>一阶段扇形齐射的中路那一发单独用 7,原代码如此</summary>
        public const int ProjDamageDivisorCenter = 7;
        /// <summary>二阶段激光:<c>(int)(NPC.damage / 5f)</c>,浮点除法后取整</summary>
        public const float LaserDamageDivisor = 5f;
        /// <summary>细胞弹的击退 4,尖刺的击退 2</summary>
        public const float BulletKnockback = 4f;
        public const float SpikeKnockback = 2f;
        public const float LaserKnockback = 0f;

        //==================== 整备(原 aitype == -1) ====================

        /// <summary>本体与细胞的逐帧阻尼</summary>
        public const float RegroupDrag = 0.98f;
        /// <summary>一阶段:细胞离本体超过这个距离才往回收,收力 0.6</summary>
        public const float RegroupCellLeash = 120f;
        public const float RegroupCellPullP1 = 0.6f;
        /// <summary>二阶段:细胞改为直接扑向玩家,推力 0.5</summary>
        public const float RegroupCellPushP2 = 0.5f;
        /// <summary>本体扑向玩家的推力</summary>
        public const float RegroupThrust = 0.6f;
        /// <summary>整备时长:<c>aicounter &gt; 80</c> 才选下一手</summary>
        public const int RegroupFrames = 80;

        //==================== 一阶段 0:突进环射 ====================

        /// <summary>追击窗帧数。距离超过 <see cref="RushLeashDistance"/> 时被重新写满</summary>
        public const float RushChaseFrames = 36f;
        public const float RushLeashDistance = 1400f;
        /// <summary>追击窗内的推力与阻尼</summary>
        public const float RushChaseThrust = 1.2f;
        public const float RushChaseDrag = 0.98f;
        /// <summary>射击窗内的推力、加速上限与加速率(速度不足 30 就每帧 ×1.076)</summary>
        public const float RushFireThrust = 0.1f;
        public const float RushSpeedCap = 30f;
        public const float RushAccel = 1.076f;
        /// <summary>细胞环射间隔(按全局帧计数取模)与单发速度,步进 40°</summary>
        public const int RushRingInterval = 24;
        public const float RushRingSpeed = 16f;
        public const int RushRingStepDeg = 40;
        /// <summary>本体侧刺间隔与速度</summary>
        public const int RushSpikeInterval = 15;
        public const float RushSpikeSpeed = 20f;
        /// <summary>尾迹粒子的采样段数(沿本帧位移均分)</summary>
        public const int TrailSamples = 10;
        /// <summary>细胞的弱回收力</summary>
        public const float RushCellPull = 0.0022f;
        /// <summary>收招门槛:计时过线<b>且</b>正处在追击窗里</summary>
        public const int RushDuration = 360;

        //==================== 一阶段 1:自旋狙击 ====================

        /// <summary>本体阻尼与位置弹簧(原代码先乘阻尼再整段覆盖速度,两句都保留)</summary>
        public const float SpinDrag = 0.98f;
        public const float SpinFollow = 0.007f;
        /// <summary>自旋角速度累加器:每帧 +0.134 再 ×0.62</summary>
        public const float SpinRotAccel = 0.134f;
        public const float SpinRotDamp = 0.62f;
        /// <summary>细胞阻尼、挂载距离(本体后方 120)与收敛率</summary>
        public const float SpinCellDrag = 0.98f;
        public const float SpinCellOffset = -120f;
        public const float SpinCellLerp = 0.14f;
        /// <summary>起手静默帧数、射程门槛与弹速(负号 = 朝本体背后打)</summary>
        public const int SpinWindup = 60;
        public const float SpinFireRange = 720f;
        public const float SpinBulletSpeed = -14f;
        public const int SpinDuration = 200;

        //==================== 一阶段 2:细胞长矛 ====================

        /// <summary>起手前的接敌门槛与逼近弹簧</summary>
        public const float LanceApproachDistance = 1200f;
        public const float LanceApproachFollow = 0.08f;
        /// <summary>蓄力段(计时 &lt; 30):本体刹速,细胞外推到本体前方 260</summary>
        public const int LanceWindupFrames = 30;
        public const float LanceWindupDrag = 0.86f;
        public const float LanceCellReach = 260f;
        public const float LanceCellLerp = 0.1f;
        /// <summary>锁存的突刺方向长度(每帧重算直到蓄力结束)</summary>
        public const float LanceAimSpeed = 10f;
        /// <summary>突刺段(30 ~ 140):推力按 <c>计时 / 140</c> 渐强,本体吃反冲</summary>
        public const int LanceThrustFrames = 140;
        public const float LanceCellAccel = 0.44f;
        public const float LanceBodyRecoil = 0.01f;
        /// <summary>突刺期的双侧散射间隔与弹速</summary>
        public const int LanceFireInterval = 6;
        public const float LanceBulletSpeed = 12f;
        /// <summary>收势段:本体阻尼 + 弱跟随,细胞回收</summary>
        public const float LanceRecoverDrag = 0.9f;
        public const float LanceRecoverFollow = 0.006f;
        public const float LanceCellRecall = 0.05f;
        public const int LanceDuration = 220;

        //==================== 一阶段 3:悬停爆发 ====================

        /// <summary>起手前的接敌门槛</summary>
        public const float HoverApproachDistance = 800f;
        /// <summary>接敌期:限速 30,朝玩家上方 200 弹簧靠近,快到位就 ×0.36 刹一脚</summary>
        public const float HoverApproachSpeedCap = 30f;
        public const float HoverApproachFollow = 0.08f;
        public const float HoverApproachHeight = 200f;
        public const float HoverApproachBrake = 0.36f;
        /// <summary>本段时长(判定写在推进之前,所以收招帧不出招)</summary>
        public const int HoverDuration = 200;
        /// <summary>进入爆发后的阻尼与推力</summary>
        public const float HoverDrag = 0.995f;
        public const float HoverThrust = 0.2f;
        /// <summary>细胞外推距离与收敛率</summary>
        public const float HoverCellReach = 200f;
        public const float HoverCellLerp = 0.1f;
        /// <summary>环射间隔、步进 60°、弹速</summary>
        public const int HoverRingInterval = 30;
        public const int HoverRingStepDeg = 60;
        public const float HoverRingSpeed = 12f;

        //==================== 一阶段 4:广角自旋 ====================

        /// <summary>与自旋狙击同一套本体运动,只是自旋量打了 0.26 折、细胞甩得更远</summary>
        public const float WideDrag = 0.98f;
        public const float WideFollow = 0.007f;
        public const float WideRotScale = 0.26f;
        public const float WideRotAccel = 0.134f;
        public const float WideRotDamp = 0.62f;
        public const float WideCellDrag = 0.98f;
        public const float WideCellOffset = -520f;
        public const float WideCellLerp = 0.14f;
        /// <summary>起手静默、齐射间隔</summary>
        public const int WideWindup = 60;
        public const int WideFireInterval = 30;
        /// <summary>扇形:六层,层间横向 -30、纵向 ±14,统一速度 14</summary>
        public const int WideFanLayers = 6;
        public const float WideFanBackStep = -30f;
        public const float WideFanSideStep = 14f;
        public const float WideFanSpeed = 14f;
        /// <summary>扇形之后附带的一圈环射:步进 60°、速度 10</summary>
        public const int WideRingStepDeg = 60;
        public const float WideRingSpeed = 10f;
        public const int WideDuration = 220;

        //==================== 一阶段 5:对拉旋转 ====================

        /// <summary>开场给全体玩家的无敌帧</summary>
        public const int OrbitGraceFrames = 60;
        /// <summary>本体与细胞分居玩家两侧的半径</summary>
        public const float OrbitRadius = 840f;
        /// <summary>两端的位置弹簧收敛率</summary>
        public const float OrbitFollow = 0.04f;
        /// <summary>转场帧:过线后两端一起上浮 1.2</summary>
        public const int OrbitReleaseFrame = 460;
        public const float OrbitRiseAccel = 1.2f;
        /// <summary>开火窗 80 ~ 460,每帧 1/3 概率出一发,再 1/2 分流到两种弹</summary>
        public const int OrbitFireStart = 80;
        public const int OrbitFireChance = 3;
        public const int OrbitSplitChance = 2;
        /// <summary>两种弹的角速度与初速(角度直接取全局帧计数,原代码如此)</summary>
        public const float OrbitBulletSpin = 0.09f;
        public const float OrbitBulletSpeed = 10f;
        public const float OrbitFireSpin = -0.09f;
        public const float OrbitFireSpeed = -14f;
        /// <summary>本体每帧自转 1.6°,连带把两端的对拉轴一起转起来</summary>
        public const float OrbitSelfSpinDeg = 1.6f;
        public const int OrbitDuration = 500;

        //==================== 一阶段 6:绕细胞盘旋 ====================

        /// <summary>细胞阻尼与扑向玩家的推力</summary>
        public const float CircleCellDrag = 0.98f;
        public const float CircleCellThrust = 0.36f;
        /// <summary>写进细胞 <c>ai[2]</c> 的发光续期值(细胞自己按它拉起 al)</summary>
        public const int CircleCellGlow = 4;
        /// <summary>本体恒定速度与绕向细胞的转向速率</summary>
        public const float CircleSpeed = 18f;
        public const float CircleTurnRate = 0.07f;
        /// <summary>环射基准角的自转量(每帧 0.5°),基准角本身在第 1 帧随机抽</summary>
        public const float CircleRingSpinDeg = 0.5f;
        public const int CircleRingRollFrame = 1;
        /// <summary>本体侧刺间隔(按全局游戏帧)与速度</summary>
        public const int CircleSpikeInterval = 40;
        public const float CircleSpikeSpeed = 16f;
        /// <summary>前段:每 10 帧一圈五发,步进 72°,速度 16</summary>
        public const int CircleRingInterval = 10;
        public const int CircleRingStepDeg = 72;
        public const float CircleRingSpeed = 16f;
        /// <summary>后段起点:改为逐帧按 1/3 概率散射,位置抖动 ±44,速度 26</summary>
        public const int CircleBurstFrame = 160;
        public const int CircleBurstChance = 3;
        public const int CircleBurstScatter = 44;
        public const float CircleBurstSpeed = 26f;
        public const int CircleDuration = 360;

        //==================== 二阶段 0:冲刺齐射 ====================

        /// <summary>冲刺次数上限(判定是「超过」,所以实际冲 6 次)</summary>
        public const int DashVolleyReps = 5;
        /// <summary>子计时从 20 倒数,跌破 -30 就重置为 20 并记一次冲刺</summary>
        public const float DashSubTimerReset = 20f;
        public const float DashSubTimerFloor = -30f;
        /// <summary>子计时为正 = 推进窗,每帧沿朝向 +5;为负 = 转向窗,转向速率 0.09</summary>
        public const float DashThrust = 5f;
        public const float DashTurnRate = 0.09f;
        /// <summary>细胞扑向玩家的推力,与本体的逐帧阻尼</summary>
        public const float DashCellThrust = 0.36f;
        public const float DashDrag = 0.96f;
        /// <summary>细胞环射:间隔 30,步进 90°,速度 18</summary>
        public const int DashRingInterval = 30;
        public const int DashRingStepDeg = 90;
        public const float DashRingSpeed = 18f;
        /// <summary>本体侧刺:间隔 10,枪口抖动 ±16,速度 20</summary>
        public const int DashSpikeInterval = 10;
        public const float DashSpikeScatter = 16f;
        public const float DashSpikeSpeed = 20f;

        //==================== 二阶段 1:细胞炮 ====================

        /// <summary>起手前把细胞收到本体 90 以内,收力 2,本体刹速 0.9</summary>
        public const float CannonDockDistance = 90f;
        public const float CannonDockPull = 2f;
        public const float CannonDockDrag = 0.9f;
        /// <summary>蓄力窗上限(= 发射帧)</summary>
        public const int CannonChargeFrames = 100;
        /// <summary>蓄力期:本体阻尼 + 位置弹簧,自旋每帧 +0.03 再 ×0.94</summary>
        public const float CannonDrag = 0.98f;
        public const float CannonFollow = 0.007f;
        public const float CannonRotAccel = 0.03f;
        public const float CannonRotDamp = 0.94f;
        /// <summary>细胞被直接焊在本体后方 100(每帧改写 Center,速度清零)</summary>
        public const float CannonCellOffset = -100f;
        /// <summary>蓄力期每 2 帧额外 +1 计时,所以蓄力实际只要 ~67 帧</summary>
        public const int CannonDoubleTickInterval = 2;
        /// <summary>发射初速(细胞被甩出去的那一脚)</summary>
        public const float CannonLaunchSpeed = 60f;
        /// <summary>发射后的自旋衰减窗(100 ~ 130),细胞按锁存朝向反向定速</summary>
        public const int CannonRecoilEndFrame = 130;
        public const float CannonRecoilRotDamp = 0.96f;
        public const float CannonTrailSpeed = -60f;
        /// <summary>细胞飞行期的侧向散射间隔与弹速</summary>
        public const int CannonFireInterval = 4;
        public const float CannonBulletSpeed = 18f;
        /// <summary>回收窗起点:细胞恢复阻尼并重新扑向玩家</summary>
        public const int CannonRecallFrame = 150;
        public const float CannonCellDrag = 0.98f;
        public const float CannonCellThrust = 0.6f;
        public const int CannonDuration = 160;

        //==================== 二阶段 2:螺旋冲刺 ====================

        /// <summary>冲刺次数上限(判定是「超过」,所以实际冲 3 次)</summary>
        public const int SpiralReps = 2;
        /// <summary>细胞螺旋弹幕:间隔 6,步进 72°,速度 6,基准角取全局帧 ×19 度</summary>
        public const int SpiralRingInterval = 6;
        public const int SpiralRingStepDeg = 72;
        public const float SpiralRingSpeed = 6f;
        public const int SpiralAngleScale = 19;
        /// <summary>本体侧刺间隔与速度</summary>
        public const int SpiralSpikeInterval = 40;
        public const float SpiralSpikeSpeed = 20f;

        //==================== 二阶段 3:对撞合体 ====================

        /// <summary>对撞前的蓄力帧数,期间两端反向拉开(力 1)</summary>
        public const int MergeWindupFrames = 40;
        public const float MergeSpreadForce = 1f;
        /// <summary>对撞加速度</summary>
        public const float MergeClosingForce = 3.4f;
        /// <summary>命中判定的余量:两端距离小于两者速度之和 + 6</summary>
        public const float MergeContactPadding = 6f;
        /// <summary>撞上后两端沿轴各退 20 的定位量</summary>
        public const float MergeSplitOffset = 20f;
        /// <summary>爆散环:步进 10°,速度 16,基准角取全局帧 ×73 度</summary>
        public const int MergeBurstStepDeg = 10;
        public const float MergeBurstSpeed = 16f;
        public const int MergeAngleScale = 73;
        /// <summary>撞完 1/2 概率原地再来一次(原代码在 prepareAiChange 之后翻的这枚硬币)</summary>
        public const int MergeRepeatChance = 2;

        //==================== 二阶段 4:分裂增殖 ====================

        /// <summary>生成小细胞的拍点与个数</summary>
        public const int SpawnCueFrame = 2;
        public const int SpawnCellCount = 3;
        /// <summary>选招时的抑制线:场上小细胞超过 8 只就重掷(见 <see cref="NihilityRotation"/>)</summary>
        public const int SpawnCellCap = 8;
        /// <summary>细胞扑向玩家的推力与本体阻尼</summary>
        public const float SplitCellThrust = 0.5f;
        public const float SplitDrag = 0.98f;
        /// <summary>环射:间隔 30,步进 40°,速度 18</summary>
        public const int SplitRingInterval = 30;
        public const int SplitRingStepDeg = 40;
        public const float SplitRingSpeed = 18f;
        public const int SplitDuration = 360;

        //==================== 二阶段 5:口部激光 ====================

        /// <summary>起手音效拍点</summary>
        public const int LaserCueFrame = 2;
        /// <summary>抬头窗:本体弱跟随并把口部转到背离玩家的方向(定速 4°)</summary>
        public const int LaserAimFrames = 40;
        public const float LaserAimFollow = 0.009f;
        public const float LaserAimRateDeg = 4f;
        /// <summary>扫射窗:过 160 帧后加一档定速追瞄 1.4°,全程再叠一次比例追瞄 0.01</summary>
        public const int LaserFastTrackFrame = 160;
        public const float LaserFastTrackDeg = 1.4f;
        public const float LaserSlowTrackRate = 0.01f;
        /// <summary>光束存活帧数(写进弹幕 ai1)</summary>
        public const float LaserBeamLifetime = 400f;
        /// <summary>扫射期细胞的伴随散射:每帧 1/3 概率,枪口抖动 ±16,速度 12</summary>
        public const int LaserSprayChance = 3;
        public const float LaserSprayScatter = 16f;
        public const float LaserSpraySpeed = 12f;
        /// <summary>细胞推力与本体位置弹簧</summary>
        public const float LaserCellThrust = 0.62f;
        public const float LaserFollow = 0.007f;
        public const int LaserDuration = 460;

        //==================== 二阶段 6:能量球 ====================

        /// <summary>三个投放拍点</summary>
        public const int BallCueFrame1 = 2;
        public const int BallCueFrame2 = 62;
        public const int BallCueFrame3 = 122;
        /// <summary>每次九颗,步进 40°</summary>
        public const int BallStepDeg = 40;
        /// <summary>本体阻尼、推力,细胞推力</summary>
        public const float BallDrag = 0.996f;
        public const float BallThrust = 0.36f;
        public const float BallCellThrust = 0.4f;
        public const int BallDuration = 360;

        //==================== 选招 ====================

        /// <summary>随机出招的面数:每阶段各七手,<c>Main.rand.Next(7)</c></summary>
        public const int AttackRollFaces = 7;

        /// <summary>「分裂增殖」对应的掷点面。二阶段掷到它且场上小细胞超标时整段重掷</summary>
        public const int SplitRoll = 4;

        /// <summary>阶段 + 掷点 → 状态索引。两个阶段的 0~6 是两套完全不同的招</summary>
        public static NihilityStateIndex StateFor(int phase, int roll) {
            if (phase >= 2) {
                return roll switch {
                    0 => NihilityStateIndex.P2DashVolley,
                    1 => NihilityStateIndex.P2CellCannon,
                    2 => NihilityStateIndex.P2DashSpiral,
                    3 => NihilityStateIndex.P2Merge,
                    4 => NihilityStateIndex.P2Split,
                    5 => NihilityStateIndex.P2Laser,
                    _ => NihilityStateIndex.P2EnergyBall,
                };
            }
            return roll switch {
                0 => NihilityStateIndex.P1Rush,
                1 => NihilityStateIndex.P1SpinSnipe,
                2 => NihilityStateIndex.P1CellLance,
                3 => NihilityStateIndex.P1HoverBurst,
                4 => NihilityStateIndex.P1WideSpin,
                5 => NihilityStateIndex.P1Orbit,
                _ => NihilityStateIndex.P1Circle,
            };
        }

        /// <summary>场上小细胞数量。只有权威端会问它(选招裁决用)</summary>
        public static int CountSmallCells(int smallCellType) {
            int sum = 0;
            foreach (NPC n in Main.ActiveNPCs) {
                if (n.type == smallCellType) {
                    sum++;
                }
            }
            return sum;
        }
    }
}
