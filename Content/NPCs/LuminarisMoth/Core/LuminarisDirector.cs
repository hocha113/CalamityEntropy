using CalamityEntropy.Core.CalamityRef;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.Core
{
    /// <summary>
    /// Luminaris 的唯一数字出口。状态里不许出现裸数字(纯局部插值系数除外)。
    /// <para>
    /// 本文件是状态机迁移时从原 <c>Luminaris.AttackPlayer()</c> / <c>SetAISyyle()</c> / <c>AI()</c>
    /// 逐个搬出来的,<b>数值一律照搬,没有一处调整</b>。注释写的是「这个数在原代码里干什么」,
    /// 不是「这个数为什么该是这样」——原作者没留依据的地方不替他编理由。
    /// </para>
    /// </summary>
    internal static class LuminarisDirector
    {
        //==================== 难度系数 enrange ====================

        /// <summary>
        /// 难度系数。原代码每帧在 <c>AttackPlayer</c> 开头重算一次,七个来源依次作用:
        /// 专家 +0.1、大师 +0.1、复仇 +0.15、死亡 +0.15,然后熵灾模式 ×1.4、getGood ×1.1、天顶 ×0.85。
        /// <para>
        /// 加法项先全部累完再乘乘法项,顺序不可换(先乘后加会得到不同结果)。
        /// 它直接乘进速度、射速与弹幕参数,所以七个开关必须在各端一致——都是世界级已同步量。
        /// </para>
        /// <para>装灾厄读复仇/死亡,缺席仍走专家/大师兜底。勿连带改熵灾那一项。</para>
        /// </summary>
        public static float Enrange() {
            float enrange = 1f;
            if (Main.expertMode) {
                enrange += 0.1f;
            }
            if (Main.masterMode) {
                enrange += 0.1f;
            }
            if (CECal.IsRevengeance) {
                enrange += 0.15f;
            }
            if (CECal.IsDeathMode) {
                enrange += 0.15f;
            }
            if (CalamityEntropy.EntropyMode) {
                enrange *= 1.4f;
            }
            if (Main.getGoodWorld) {
                enrange *= 1.1f;
            }
            if (Main.zenithWorld) {
                enrange *= 0.85f;
            }
            return enrange;
        }

        //==================== 全局 ====================

        /// <summary>
        /// 状态总龄上限。原代码<b>没有</b>任何超时兜底,这是迁移时新加的纯安全网:
        /// 最长的状态是 SmashDown(402 帧),再加上脱战期计时仍在走的最多 150 帧,
        /// 1800 帧在正常对局里到不了,存在的意义只是不让状态机死在某个状态里、Boss 靠惯性飘走
        /// </summary>
        public const int StateTimeoutFrames = 1800;

        /// <summary>
        /// 生成后的冻结帧数。原代码是 <c>if (SD-- &gt; 0) return;</c>,初值 4:
        /// 前四帧 AI 直接返回,第五帧才开始跑
        /// </summary>
        public const int SpawnFreezeFrames = 4;

        /// <summary>脱战倒计时字段初值(原 <c>deactiveCount = 120</c>)</summary>
        public const int DeactiveFramesInitial = 120;

        /// <summary>接战时把脱战倒计时重置成这个值(原 <c>deactiveCount = 150</c>),归零即 <c>NPC.active = false</c></summary>
        public const int DeactiveFrames = 150;

        /// <summary>脱战运动:速度阻尼 0.998,每帧再向上 0.3(原代码是 <c>velocity.Y -= 0.3f</c>,负 Y 为上)</summary>
        public const float DisengageDrag = 0.998f;
        public const float DisengageRise = 0.3f;

        /// <summary>
        /// 转二阶段的血量线。原代码写的是<b>整数除法</b> <c>NPC.life &lt;= NPC.lifeMax / 2</c>,
        /// 这里保持整数除法以免 lifeMax 为奇数时差一格。单向,不回退
        /// </summary>
        public const int Phase2LifeDivisor = 2;

        //==================== 表现累加量(宿主每帧结算) ====================

        /// <summary>大尾迹强度的自衰减:原 <c>if (MegaTrail &gt; 0) MegaTrail -= 0.05f;</c>,在 AI 开头</summary>
        public const float MegaTrailDecay = 0.05f;

        /// <summary>残影窗口。状态每帧写满 16,宿主每帧减 1,所以离开状态后还残留 16 帧</summary>
        public const int AfterImageFrames = 16;

        /// <summary>尾迹采样上限 = 24 + <c>(int)MegaTrail</c> × 16(整数截断,所以只有 MegaTrail ≥ 1 才加长)</summary>
        public const int TrailBaseLength = 24;
        public const int TrailPerMegaTrail = 16;

        /// <summary>
        /// 每帧最多裁掉的采样点数。原代码是 <c>for (int i = 0; i &lt; 3; i++) if (Count &gt; odMax) RemoveAt(0);</c>,
        /// 每帧只加一个点,所以三次裁剪是为了 odMax 变短时能快点收敛
        /// </summary>
        public const int TrailTrimPerFrame = 3;

        //==================== 尾巴绳(Rigs2D VerletStrand,数值以 Assets/Rigs/Luminaris.rig.json 为准) ====================
        //原 Utilities.Rope:10 质点、节长 11.6、逐帧重力 (0, 0.12)、阻尼 0.054(速度乘 1/1.054 ≈ 0.9488)、30 次约束迭代,
        //每帧沿「上一帧位置 → 本帧位置」走 5 个整步;绳根在本体下方 (∓14, 32) 随朝向旋转并乘 scale。
        //对应骨架里两条 9 节 VerletStrand:substeps 5、gravity 3.0(3.0 × 0.2² × 5 = 0.6 = 5 × 0.12)、
        //damping 0.769(0.9488⁵)、iterations 30,anchorA / anchorB 骨偏移 (∓14, 32)

        //==================== 天顶世界的分身 ====================

        /// <summary>
        /// 天顶世界里本体额外生成 5 只 realLife 从属分身:X 方向 ±500 随机、Y 方向正上方 2000。
        /// 每只(含本体)的倒计时被随机成 210~270,靠这个错开彼此的第一手
        /// </summary>
        public const int ZenithCloneCount = 5;
        public const int ZenithCloneSpreadX = 500;
        public const int ZenithCloneOffsetY = 2000;
        public const int ZenithCloneCountdownMin = 210;
        public const int ZenithCloneCountdownMax = 270;

        //==================== 弹幕 ====================

        /// <summary>弹幕基础伤害 = <c>(int)(NPC.damage / 6.2f)</c>,击退固定 4,owner 传 -1</summary>
        public const float ProjDamageDivisor = 6.2f;
        public const float ProjKnockback = 4f;

        //==================== 出招时长表 ====================

        /// <summary>
        /// 选招后写死的兜底时长。原代码先无条件 <c>AIChangeCounter = 20;</c>,再用十一个 if 逐招覆盖;
        /// 十一个招式全部有覆盖值,所以这个 20 在原代码里<b>到不了</b>,照搬保留
        /// </summary>
        public const int PickFallbackFrames = 20;

        public const int RoundShootingFrames = 200;
        public const int AboveMovingShootingFrames = 290;
        public const int Waiting1SecFrames = 90;
        public const int SubductionFrames = 230;
        public const int StayAboveAndShootingFrames = 240;
        public const int DashingFrames = 200;
        public const int AstralSpikeFrames = 100;
        public const int Shoot360Frames = 160;
        public const int RoundAndDashFrames = 200;
        public const int SmashDownFrames = 400;
        public const int ShootTriangleFrames = 200;

        /// <summary>
        /// 原 <c>SetAISyyle()</c> 之后那一串时长赋值,逐条搬过来。
        /// 写成同样的「先兜底再覆盖」形状,不折成 switch,免得把那条到不了的兜底路径优化掉
        /// </summary>
        public static int DurationOf(LuminarisStateIndex state) {
            int duration = PickFallbackFrames;
            if (state == LuminarisStateIndex.RoundShooting) {
                duration = RoundShootingFrames;
            }
            if (state == LuminarisStateIndex.AboveMovingShooting) {
                duration = AboveMovingShootingFrames;
            }
            if (state == LuminarisStateIndex.Waiting1Sec) {
                duration = Waiting1SecFrames;
            }
            if (state == LuminarisStateIndex.Subduction) {
                duration = SubductionFrames;
            }
            if (state == LuminarisStateIndex.StayAboveAndShooting) {
                duration = StayAboveAndShootingFrames;
            }
            if (state == LuminarisStateIndex.Dashing) {
                duration = DashingFrames;
            }
            if (state == LuminarisStateIndex.AstralSpike) {
                duration = AstralSpikeFrames;
            }
            if (state == LuminarisStateIndex.Shoot360) {
                duration = Shoot360Frames;
            }
            if (state == LuminarisStateIndex.RoundAndDash) {
                duration = RoundAndDashFrames;
            }
            if (state == LuminarisStateIndex.SmashDown) {
                duration = SmashDownFrames;
            }
            if (state == LuminarisStateIndex.ShootTriangle) {
                duration = ShootTriangleFrames;
            }
            return duration;
        }

        //==================== RoundShooting:定点绕转喷涡 ====================

        /// <summary>倒计时跌到 160 之前是入位,之后转为绕着玩家转圈</summary>
        public const int RoundShootingOrbitFrame = 160;
        /// <summary>入位插值跨度 40 帧(倒计时 200 → 160)</summary>
        public const float RoundShootingApproachSpan = 40f;
        /// <summary>入位落点:玩家水平方向 ±440(取当前左右侧),上方 440</summary>
        public const float RoundShootingApproachX = 440f;
        public const float RoundShootingApproachY = -440f;
        /// <summary>绕转角速度(乘方向 ±1 与 enrange)</summary>
        public const float RoundShootingOrbitSpeed = 0.036f;
        /// <summary>喷涡间隔 <c>(int)(38 / enrange)</c> 帧</summary>
        public const float RoundShootingShootIntervalBase = 38f;
        public const float RoundShootingVortexSpeed = 8f;

        //==================== AstralSpike:随机方向滑行撒刺 ====================

        /// <summary>倒计时跌到 40 之前在滑行并撒刺,之后原地不动到收招</summary>
        public const int AstralSpikeMoveEndFrame = 40;
        /// <summary>滑行终点 = 起点 + 随机方向 × 800</summary>
        public const float AstralSpikeTravelDistance = 800f;
        /// <summary>每 2 帧一根刺,其中 4 的倍数出蓝刺、否则红刺</summary>
        public const int AstralSpikeShootInterval = 2;
        public const int AstralSpikeBlueInterval = 4;
        public const float AstralSpikeProjSpeed = 1.4f;

        //==================== AboveMovingShooting:高空横移 + 砸落弹雨 ====================

        /// <summary>倒计时 290 → 261 只是悬停,跌到 260 才开始入位</summary>
        public const int AboveMovingGateFrame = 260;
        /// <summary>第一段入位(260 → 241):玩家水平 ±360、上方 440,跨度 20 帧</summary>
        public const int AboveMovingStage1Frame = 240;
        public const float AboveMovingStage1Span = 20f;
        public const float AboveMovingStage1X = 360f;
        public const float AboveMovingStage1Y = -440f;
        /// <summary>第二段入位(240 → 221):挪到玩家正上方 420,跨度 20 帧</summary>
        public const int AboveMovingStage2Frame = 220;
        public const float AboveMovingStage2Span = 20f;
        public const float AboveMovingStage2Y = -420f;
        /// <summary>横移段:余弦频率 0.056,幅度 800,竖直向「玩家上方 500」按 0.1 收敛</summary>
        public const float AboveMovingSwayFreq = 0.056f;
        public const float AboveMovingSwayAmp = 800f;
        public const float AboveMovingHoverAbove = 500f;
        public const float AboveMovingHoverLerp = 0.1f;
        /// <summary>横移段的朝向:直接拿本帧位移的 X 分量乘 0.01 当倾角</summary>
        public const float AboveMovingTiltFactor = 0.01f;
        /// <summary>幅度渐入:倒计时 219 → 160 线性涨满,之后恒为 1</summary>
        public const int AboveMovingRampFrame = 160;
        public const float AboveMovingRampSpan = 60f;
        /// <summary>横移段的开火窗:倒计时小于 156,间隔 <c>(int)(8 / enrange)</c> 帧</summary>
        public const int AboveMovingShootStartFrame = 156;
        public const float AboveMovingShootIntervalBase = 8f;
        /// <summary>左右两道横扫星弹:从玩家 ±1400、下方 340 起飞,水平初速 26 × enrange</summary>
        public const float AboveMovingSideOffsetX = 1400f;
        public const float AboveMovingSideOffsetY = 340f;
        public const float AboveMovingSideSpeed = 26f;
        /// <summary>横扫星弹的重力载荷:方向竖直向下(ai0)、强度 -0.3 × enrange(ai1)、延迟 30 帧(ai2)</summary>
        public const float AboveMovingSideGravity = -0.3f;
        public const float AboveMovingGravityDelay = 30f;
        /// <summary>本体正下方那一发:初速 14 × enrange,重力方向朝上、强度 0.3 × enrange</summary>
        public const float AboveMovingDownSpeed = 14f;
        public const float AboveMovingDownGravity = 0.3f;
        /// <summary>横扫预告线:画在玩家上方 516,横坐标用「上一次开火那一帧」的余弦相位</summary>
        public const float AboveMovingPreviewOffsetY = -516f;
        public const float AboveMovingPreviewAlpha = 0.82f;
        /// <summary>落地拍(倒计时 220):屏震 1800→1000 映射到 0→4.5,闪屏 0.36</summary>
        public const float AboveMovingSlamShakeFar = 1800f;
        public const float AboveMovingSlamShakeNear = 1000f;
        public const float AboveMovingSlamShakeAmp = 4.5f;
        public const float AboveMovingSlamFlash = 0.36f;
        public const float AboveMovingSlamPitch = 1.3f;
        /// <summary>落地拍的 14 发散射:水平 ±16、竖直 12,再随机偏转 0.3 rad,整体乘 enrange</summary>
        public const int AboveMovingSlamShots = 14;
        public const float AboveMovingSlamSpeedX = 16f;
        public const float AboveMovingSlamSpeedY = 12f;
        public const float AboveMovingSlamScatter = 0.3f;
        public const float AboveMovingSlamGravity = 0.6f;
        public const float AboveMovingSlamGravityDelay = 6f;

        //==================== Subduction:两次俯冲 + 追撞 ====================

        /// <summary>第一次抬升的终点帧(230 → 190),落点为玩家水平 ±380 × enrange、上方 400</summary>
        public const int SubductionRise1EndFrame = 190;
        public const float SubductionRiseX = 380f;
        public const float SubductionRiseY = -400f;
        /// <summary>第一次俯冲:190 → 160,跨度 30 帧,过程中额外叠一条 400 高的余弦拱形</summary>
        public const int SubductionDive1EndFrame = 160;
        public const float SubductionDive1Span = 30f;
        public const float SubductionArcHeight = 400f;
        /// <summary>第二轮的入口帧。注意 160 → 151 之间没有任何分支接管位置,本体靠残余速度滑行 9 帧</summary>
        public const int SubductionRound2Frame = 150;
        /// <summary>第二次抬升的终点帧(150 → 130)</summary>
        public const int SubductionRise2EndFrame = 130;
        /// <summary>第二次俯冲:130 → 90,跨度 40 帧</summary>
        public const int SubductionDive2EndFrame = 90;
        public const float SubductionDive2Span = 40f;
        /// <summary>收尾追撞:每帧朝玩家加速 0.6、阻尼 0.99,倾角取速度 X × 0.01</summary>
        public const float SubductionChaseAccel = 0.6f;
        public const float SubductionChaseDrag = 0.99f;
        public const float SubductionTiltFactor = 0.01f;
        /// <summary>
        /// 倒计时等于 1 时的刹车。<b>这一拍在原代码里没有效果</b>:
        /// 同一帧上面的追撞分支已经先写过速度,而下一帧(0)与再下一帧(-1)追撞又会继续加速
        /// </summary>
        public const int SubductionBrakeFrame = 1;

        //==================== StayAboveAndShooting:悬停环射 ====================

        /// <summary>
        /// 悬停锚点:<c>player.Center + new Vector2(600 × sign, -400) / enrange</c>。
        /// 注意除法只作用在偏移向量上(优先级),不作用在玩家坐标上
        /// </summary>
        public const float StayAboveAnchorX = 600f;
        public const float StayAboveAnchorY = -400f;
        /// <summary>每帧朝锚点加速 1,阻尼 0.97</summary>
        public const float StayAboveAccel = 1f;
        public const float StayAboveDrag = 0.97f;
        /// <summary>
        /// 开火条件是四个判定的合取:<c>C &gt; 60/enrange</c>、<c>C % (int)(50/enrange) == 0</c>、
        /// <c>C &lt; 150×enrange</c>、<c>C &gt; 30</c>
        /// </summary>
        public const float StayAboveShootLowGate = 60f;
        public const float StayAboveShootIntervalBase = 50f;
        public const float StayAboveShootHighGate = 150f;
        public const int StayAboveShootFloor = 30;
        /// <summary>开火后坐:朝远离玩家的方向 6</summary>
        public const float StayAboveRecoil = 6f;
        /// <summary>
        /// 环射发数 <c>(int)(10 × enrange)</c>;角度步进 <c>360 / 发数</c>。
        /// <b>这个角度是「度」却被塞进按弧度解释的 ai0</b>(弹幕用 <c>ai[0].ToRotationVector2()</c> 当重力方向),
        /// 所以实际重力方向是 0、36、72… 弧度。原代码如此,照搬
        /// </summary>
        public const float StayAboveShotsPerEnrange = 10f;
        public const float StayAboveAngleTotal = 360f;
        public const float StayAboveProjSpeed = 6f;
        public const float StayAboveDamageMult = 0.9f;
        public const float StayAboveProjGravity = 0.12f;
        public const float StayAboveProjGravityDelay = 2f;
        /// <summary>开火屏震 1800→1000 映射到 0→2</summary>
        public const float StayAboveShakeFar = 1800f;
        public const float StayAboveShakeNear = 1000f;
        public const float StayAboveShakeAmp = 2f;
        public const float StayAbovePitch = 1.6f;
        public const float StayAboveVolume = 0.5f;

        //==================== Dashing:两次锁向冲撞 ====================

        /// <summary>第一次锁向:倒计时 200 → 180,插值系数走重复余弦</summary>
        public const int DashingLock1EndFrame = 180;
        /// <summary>第二次锁向:倒计时 130 → 110,插值系数是<b>线性</b>的(和第一次不同,原代码如此)</summary>
        public const int DashingLock2StartFrame = 130;
        public const int DashingLock2EndFrame = 110;
        /// <summary>冲撞速度</summary>
        public const float DashingSpeed = 40f;
        /// <summary>
        /// 冲撞中的持续追瞄速率:<c>0.25f * enrange.ToRadians()</c>。
        /// <c>ToRadians</c> 把 enrange 本身当角度换算,所以实际速率只有 0.004~0.010 rad/帧。
        /// 看着像笔误,但这是原代码的写法,改了就是手感改动
        /// </summary>
        public const float DashingTrackRate = 0.25f;

        //==================== Shoot360:贴近后多轮环爆 ====================

        /// <summary>入位段的终点帧(160 → 120)</summary>
        public const int Shoot360ApproachEndFrame = 120;
        /// <summary>入位落点:玩家朝本体方向 <c>280 / enrange</c> 处</summary>
        public const float Shoot360ApproachDistance = 280f;
        /// <summary>开火窗 110 → 80,每 10 帧一轮</summary>
        public const int Shoot360ShootStartFrame = 110;
        public const int Shoot360ShootEndFrame = 80;
        public const int Shoot360ShootInterval = 10;
        /// <summary>每轮的爆闪粒子:基准 6,外层再 ×1.2</summary>
        public const float Shoot360ImpactScale = 6f;
        public const float Shoot360ImpactOuterMult = 1.2f;
        /// <summary>环爆:从随机起始角开始每 80 度一发,每个角度出两发(法线 ±90°)</summary>
        public const int Shoot360AngleStep = 80;
        public const float Shoot360ProjSpeed = 4f;
        public const float Shoot360ProjGravity = 0.16f;

        //==================== RoundAndDash:绕场蓄力 + 穿场直冲 ====================

        /// <summary>整段招式体只在倒计时大于 1 时跑,最后三帧(1、0、-1)什么都不做</summary>
        public const int RoundAndDashBodyGate = 1;
        /// <summary>拉开距离段的终点帧(200 → 140),落点是玩家朝本体方向 700 处</summary>
        public const int RoundAndDashSwingFrame = 140;
        public const float RoundAndDashSwingSpan = 60f;
        public const float RoundAndDashRadius = 700f;
        /// <summary>绕场角度的随机跨度:6~10 rad,方向 ±1</summary>
        public const float RoundAndDashSweepMin = 6f;
        public const float RoundAndDashSweepMax = 10f;
        /// <summary>冲刺起点帧;之前是绕场,之后是穿场</summary>
        public const int RoundAndDashLaunchFrame = 50;
        /// <summary>大尾迹开启窗:倒计时 49 → 17</summary>
        public const int RoundAndDashTrailStartFrame = 50;
        public const int RoundAndDashTrailEndFrame = 16;
        public const float RoundAndDashTrailStrength = 1.2f;
        public const float RoundAndDashLaunchFlash = 0.3f;
        /// <summary>穿场段:半径从 +700 线性穿到 -700(即从一侧穿到另一侧),重映射的上界写的是 49</summary>
        public const int RoundAndDashCrossSpanFrame = 49;

        //==================== SmashDown:四轮高空砸落 ====================

        /// <summary>
        /// 每轮长度。原代码用 <c>ac = AIChangeCounter % 100 + 1</c> 折出轮内进度,
        /// 所以 400 帧拆成四轮;但首帧(倒计时 400)折出的是 ac = 1、末帧(-1)折出 ac = 0,
        /// 于是实际序列是「1 → (100…2) ×4 → 0」,首尾各多一帧自由落体
        /// </summary>
        public const int SmashDownCycleFrames = 100;
        /// <summary>轮内进度大于等于 40 是抬升段,小于 40 是砸落段</summary>
        public const int SmashDownRiseEndFrame = 40;
        /// <summary>抬升落点:玩家位置 + 速度 × 36 + 上方 520 再随机 ±80</summary>
        public const float SmashDownLeadFrames = 36f;
        public const float SmashDownRiseHeight = -520f;
        public const float SmashDownRiseJitter = 80f;
        /// <summary>砸落段每帧竖直加速 2</summary>
        public const float SmashDownGravity = 2f;
        /// <summary>起砸拍(轮内进度 39):闪屏 0.4,并把尾迹清空重采</summary>
        public const int SmashDownLaunchFrame = 39;
        public const float SmashDownLaunchFlash = 0.4f;
        /// <summary>砸落中的左右撒弹:轮内进度小于 34,间隔 <c>(int)(9 / enrange)</c> 帧</summary>
        public const int SmashDownShootStartFrame = 34;
        public const float SmashDownShootIntervalBase = 9f;
        public const float SmashDownShotSpeedX = 10f;
        public const float SmashDownShotGravity = 0.2f;
        public const float SmashDownShotGravityDelayBase = 24f;
        /// <summary>大尾迹开启窗:轮内进度大于 10(所以砸落全程都带,抬升段不带)</summary>
        public const int SmashDownTrailFrame = 10;
        public const float SmashDownTrailStrength = 1f;

        //==================== ShootTriangle:绕圈三角弹 ====================

        /// <summary>入位段的分界帧:倒计时大于 160 是入位,等于 160 时定半径与初始角</summary>
        public const int ShootTriangleOrbitFrame = 160;
        public const float ShootTriangleApproachSpan = 40f;
        public const float ShootTriangleApproachX = 440f;
        public const float ShootTriangleApproachY = -440f;
        /// <summary>快转开火窗:倒计时 159 → 148,每帧 30 度(12 帧正好一圈),每 2 帧换色</summary>
        public const int ShootTriangleFastEndFrame = 148;
        public const float ShootTriangleFastStep = 30f;
        /// <summary>三角弹散射:法线 ±36 度,速度 10 × 随机 0.8~1.2 × enrange</summary>
        public const float ShootTriangleSpread = 36f;
        public const float ShootTriangleProjSpeed = 10f;
        public const float ShootTriangleSpeedJitterMin = 0.8f;
        public const float ShootTriangleSpeedJitterMax = 1.2f;
        /// <summary>慢转段:每帧 10 度,绕的是快转段最后记下的玩家位置(固定点)</summary>
        public const float ShootTriangleSlowStep = 10f;
        /// <summary>慢转段的速度阻尼。本状态每帧开头已经把速度清零了,所以这一句没有效果,照搬</summary>
        public const float ShootTriangleSlowDrag = 0.9f;
    }
}
