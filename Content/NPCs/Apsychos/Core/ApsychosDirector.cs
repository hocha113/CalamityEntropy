using CalamityEntropy.Core.CalamityRef;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.Core
{
    /// <summary>
    /// Apsychos 的唯一数字出口。状态里不许出现裸数字(纯局部插值系数除外)。
    /// <para>
    /// 本文件是 2026-09-17 状态机重构时从原 <c>Apsychos.AttackPlayer()</c> 与 <c>SetAIStyle()</c>
    /// 逐个搬出来的,<b>数值一律照搬,没有一处调整</b>。注释写的是「这个数在原代码里干什么」,
    /// 不是「这个数为什么该是这样」——原作者没留依据的地方不替他编理由。
    /// </para>
    /// </summary>
    internal static class ApsychosDirector
    {
        //==================== 难度系数 enrange ====================

        /// <summary>
        /// 难度系数。原代码每帧在 <c>AttackPlayer</c> 开头重算一次,七个来源依次作用:
        /// 专家 +0.1、大师 +0.1、复仇 +0.15、死亡 +0.15,然后熵灾模式 ×1.4、getGood ×1.1、天顶 ×0.85。
        /// <para>
        /// 加法项先全部累完再乘乘法项,顺序不可换(先乘后加会得到不同结果)。
        /// 它直接乘进速度与射速,所以七个开关必须在各端一致——都是世界级已同步量。
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
        /// 状态总龄上限。原代码<b>没有</b>任何超时兜底,这是重构时新加的纯安全网:
        /// 最长的状态是低血 TailDash(六次鞭击 ≈ 410 帧),1800 帧在正常对局里到不了,
        /// 存在的意义只是不让状态机死在某个状态里、Boss 靠惯性飘走
        /// </summary>
        public const int StateTimeoutFrames = 1800;

        /// <summary>目标距离超过它就强制回 MoveToTarget(选招时判定)</summary>
        public const float ForceApproachDistance = 2500f;

        /// <summary>目标距离超过它、或无有效目标,进入脱战倒计时</summary>
        public const float DisengageDistance = 5000f;

        /// <summary>脱战倒计时帧数,归零即 <c>NPC.active = false</c></summary>
        public const int DeactiveFrames = 160;

        /// <summary>脱战时的朝向目标(正下方)与转向速率,以及巡航速度</summary>
        public const float DisengageRotateRate = 0.07f;
        public const float DisengageSpeed = 15f;

        /// <summary>转二阶段的血量比例。原代码是浮点乘法 <c>life &lt; lifeMax * 0.5f</c>,照搬</summary>
        public const float Phase2LifeRatio = 0.5f;

        /// <summary>
        /// 低血分界的除数。原代码写的是<b>整数除法</b> <c>lifeMax / 4</c>(不是乘 0.25f),
        /// 这里保持整数除法以免在 lifeMax 为奇数时出现一格之差。
        /// 低血轮换表与 TailDash 的鞭击次数共用这条线
        /// </summary>
        public const int LowLifeDivisor = 4;

        //==================== 声明通道的自衰减率 ====================

        /// <summary>描边:未被声明时每帧 ×0.9(原 OutlineFlag 的默认结算)</summary>
        public const float OutlineDecay = 0.9f;
        /// <summary>尾焰亮度:未被声明时每帧 ×0.96(原 TailLightFlag 的默认结算,实际无人关闭)</summary>
        public const float TailLightDecay = 0.96f;
        /// <summary>尾巴速度:未被声明时每帧 ×0.96(原 TailSpeedMultFlag,只有 FireballShooting 关闭)</summary>
        public const float TailSpeedDecay = 0.96f;
        /// <summary>白化强度:除 PhaseTrans 外每帧 ×0.94(原代码挂在 PhaseTrans 的 else 上)</summary>
        public const float HighLightDecay = 0.94f;
        /// <summary>脱战时三个发光量统一走这个更快的衰减</summary>
        public const float DisengageGlowDecay = 0.9f;

        //==================== 弹幕伤害 ====================

        /// <summary>弹幕基础伤害除数:一阶段 6.5,二阶段 5.4(分母变小 = 二阶段更疼)</summary>
        public const float ProjDamageDivisorPhase1 = 6.5f;
        public const float ProjDamageDivisorPhase2 = 5.4f;
        /// <summary>弹幕击退</summary>
        public const float ProjKnockback = 4f;

        //==================== 尾巴骨架(Rigs2D,数值以 Assets/Rigs/Apsychos.rig.json 为准) ====================

        /// <summary>尾骨节数(不含尾尖)。骨架定义里的 seg0..seg11 与代码里的句柄数组按它对齐,改一处要改两处</summary>
        public const int TailSegCount = 12;
        //以下四组数只作对照,真正生效的是 rig.json 里的同名参数:
        //Follow 档:骨节间距 46(gaps)、尾尖间距 36、转向速率 0.12(ChainFollow poseWeightBase);
        //骨链起点相对本体后退 70(neck 骨偏移);
        //贝塞尔档:第一控制点在本体后方 300 = 颈长 70 + handleA 230,位置跟随率 0.6(followRate,锚点系);
        //TwoPoint 档:第二控制点相对尾尖后退 160(handleB)。
        //原「骨节朝向参考点前移 16」(SegFacingOffset)只影响第一节的朝向算法,迁移后由骨轴方向取代,不再需要

        //==================== MoveToTarget:接近 ====================

        /// <summary>转向:先按固定速率转,再按比例补一次(原代码两次连调)</summary>
        public const float ApproachRotateFixed = 0.02f;
        public const float ApproachRotateLerp = 0.06f;
        /// <summary>每帧速度阻尼</summary>
        public const float ApproachDrag = 0.9f;
        /// <summary>推力按距离重映射:近处 0.4,远处 1.0</summary>
        public const float ApproachNearDistance = 200f;
        public const float ApproachFarDistance = 900f;
        public const float ApproachThrustNear = 0.4f;
        public const float ApproachThrustFar = 1f;
        /// <summary>贴脸阈值:距离小于它,计时每帧额外再 +1(逼近时缩短本段时长)</summary>
        public const float ApproachCloseDistance = 400f;
        /// <summary>本段时长基数,除以 enrange</summary>
        public const float ApproachDurationBase = 280f;

        //==================== Dash:冲刺 ====================

        /// <summary>蓄力段:前 30 帧刹速 + 快速转向 + 描边涨起</summary>
        public const int DashWindupFrames = 30;
        public const float DashWindupDrag = 0.9f;
        public const float DashWindupRotate = 0.1f;
        /// <summary>描边每帧涨 1/25、收时每帧落 1/20</summary>
        public const float DashOutlineRise = 1f / 25f;
        public const float DashOutlineFall = 1f / 20f;
        /// <summary>点火帧(除以 enrange),之后 20 帧是推进窗,再 20 帧收尾</summary>
        public const float DashIgniteBase = 60f;
        public const int DashThrustFrames = 20;
        public const int DashRecoverFrames = 40;
        /// <summary>推进窗内的阻尼与推力</summary>
        public const float DashThrustDrag = 0.9f;
        public const float DashThrust = 5f;
        /// <summary>收尾段阻尼</summary>
        public const float DashRecoverDrag = 0.94f;
        /// <summary>喷口计数上限(同时也是尾迹的持续帧数)</summary>
        public const int DashPlumeFrames = 16;
        /// <summary>闪屏强度,地狱高度下加强</summary>
        public const float DashFlash = 0.32f;
        public const float DashFlashUnderworld = 0.55f;
        /// <summary>尾迹插值步长:isp 从 0 到 1,每步生成一组双通道粒子</summary>
        public const float DashTrailStep = 0.05f;
        /// <summary>双侧喷口相对本体的偏移(x 向后 40,y 左右 ±134)</summary>
        public const float DashNozzleBack = -40f;
        public const float DashNozzleSide = 134f;
        public const float DashDustScale = 1.6f;
        public const float DashDustVelFactor = 0.16f;
        public const float DashDustScatter = 4f;
        public const float DashSmokeScale = 0.45f;
        public const float DashSmokeScaleStart = 0.6f;
        public const int DashSmokeLife = 32;

        //==================== FireballShooting:三连火球 ====================

        /// <summary>两段阻尼与推力(原代码先 ×0.94 再 ×0.92,保留两次)</summary>
        public const float FireballDragA = 0.94f;
        public const float FireballDragB = 0.92f;
        public const float FireballRotate = 0.1f;
        public const float FireballThrust = 0.2f;
        /// <summary>尾巴前伸距离与跟随率</summary>
        public const float FireballTailReach = 180f;
        public const float FireballTailLerp = 0.08f;
        /// <summary>起手延迟(除以 enrange)与齐射间隔(除以 enrange)</summary>
        public const float FireballWindupBase = 80f;
        public const float FireballIntervalBase = 40f;
        /// <summary>齐射次数上限(num1 走到 total 之后再多一发才停,与原代码的 &lt;= 判定一致)</summary>
        public const int FireballVolleys = 5;
        /// <summary>收尾延迟(除以 enrange)</summary>
        public const float FireballTailoffBase = 60f;
        /// <summary>枪口相对尾尖的前伸</summary>
        public const float FireballMuzzleOffset = 32f;
        /// <summary>一阶段:中弹 3.8,侧弹 3.0,张角 ±0.44</summary>
        public const float FireballSpeedCenterP1 = 3.8f;
        public const float FireballSpeedSideP1 = 3f;
        public const float FireballSpreadP1 = 0.44f;
        /// <summary>二阶段:三发同速 4.0,张角收到 ±0.3(更密更直)</summary>
        public const float FireballSpeedP2 = 4f;
        public const float FireballSpreadP2 = 0.3f;
        /// <summary>开火后坐(尾巴反冲)</summary>
        public const float FireballRecoil = 18f;
        /// <summary>本状态自管尾巴速度衰减(不走默认的 0.96)</summary>
        public const float FireballTailDrag = 0.98f;
        /// <summary>蓄力期尾焰涨速</summary>
        public const float FireballTailLightRise = 0.04f;
        public const float FireballDamageMult = 1f;

        //==================== FlameThrow:喷火 ====================

        public const float FlameTailSwayReach = 180f;
        /// <summary>尾巴摆动:频率 0.06,幅度 100</summary>
        public const float FlameSwayFreq = 0.06f;
        public const float FlameSwayAmp = 100f;
        public const float FlameTailLerp = 0.24f;
        /// <summary>尾巴瞄向玩家的转向速率</summary>
        public const float FlameTailAimRate = 0.08f;
        public const float FlameTailLightRise = 0.08f;
        /// <summary>本体转向速率(乘 enrange)、阻尼、推力</summary>
        public const float FlameRotate = 0.1f;
        public const float FlameDrag = 0.96f;
        public const float FlameThrust = 0.16f;
        /// <summary>尾巴归位距离与跟随率(与摆动叠加,原代码两次赋值都保留)</summary>
        public const float FlameTailReach = 160f;
        public const float FlameTailHomeLerp = 0.08f;
        /// <summary>喷射窗:60 到 140 帧</summary>
        public const int FlameStartFrame = 60;
        public const int FlameEndFrame = 140;
        /// <summary>起势渐强的终点帧(60→100 线性涨到 1)</summary>
        public const int FlameRampFrame = 100;
        public const float FlameRampSpan = 40f;
        /// <summary>收势渐弱的起点帧。喷射窗 140 帧就结束了,所以这一支在原代码里到不了,照搬保留</summary>
        public const int FlameFadeFrame = 150;
        /// <summary>尾巴的持续后坐</summary>
        public const float FlameTailRecoilPerFrame = -0.8f;
        /// <summary>火焰枪口前伸与初速</summary>
        public const float FlameMuzzleOffset = 60f;
        public const float FlameSpeed = 52f;
        public const float FlameDamageMult = 1.2f;
        /// <summary>火焰存活帧数:一阶段 40,每升一阶段 +24</summary>
        public const float FlameLifeBase = 40f;
        public const float FlameLifePerPhase = 24f;
        /// <summary>本段时长</summary>
        public const int FlameDuration = 190;

        //==================== FireballBig:巨型火球 ====================

        public const float BigRotate = 0.1f;
        public const float BigDrag = 0.9f;
        public const float BigThrust = 0.16f;
        /// <summary>蓄力帧数 = 65 - 阶段×15(一阶段 50,二阶段 35)</summary>
        public const float BigChargeBase = 65f;
        public const float BigChargePerPhase = 15f;
        public const float BigTailReach = 180f;
        public const float BigTailLerp = 0.08f;
        public const float BigTailLightRise = 0.08f;
        /// <summary>发射后坐</summary>
        public const float BigRecoil = 26f;
        /// <summary>初速 = 6 + 2×阶段</summary>
        public const float BigSpeedBase = 6f;
        public const float BigSpeedPerPhase = 2f;
        public const float BigDamageMult = 1.2f;
        /// <summary>发射次数,超过即收招</summary>
        public const int BigShotCount = 3;

        //==================== PhaseTrans:转阶段 ====================

        public const float TransDrag = 0.98f;
        /// <summary>白化与配色插值涨满的帧数</summary>
        public const int TransRampFrames = 80;
        /// <summary>本段时长</summary>
        public const int TransDuration = 120;

        //==================== TailDash:甩尾 ====================

        /// <summary>蓄力帧数,到点发射</summary>
        public const int TailDashWindupFrames = 60;
        /// <summary>第 4 帧后尾巴开始向内收(收到本体后方 180)</summary>
        public const int TailDashRetractStartFrame = 4;
        public const float TailDashRetractTarget = -180f;
        public const float TailDashRetractLerp = 0.04f;
        /// <summary>第 12 帧后才开始转向瞄人(给玩家一个"已经锁定"的读秒窗)</summary>
        public const int TailDashAimStartFrame = 12;
        public const float TailDashRotateFixed = 0.02f;
        public const float TailDashRotateLerp = 0.08f;
        public const float TailDashWindupDrag = 0.9f;
        /// <summary>蓄力期推力按距离重映射:近 0.2,远 0.4</summary>
        public const float TailDashThrustNear = 0.2f;
        public const float TailDashThrustFar = 0.4f;
        /// <summary>发射初速(乘 enrange)</summary>
        public const float TailDashLaunchSpeed = 32f;
        /// <summary>甩出段:本体阻尼,尾巴伸出的速度累加器(每帧 ×0.997 再 +12)</summary>
        public const float TailDashLungeDrag = 0.9f;
        public const float TailDashExtendDamp = 0.997f;
        public const float TailDashExtendAccel = 12f;
        /// <summary>尾巴伸到这个距离就打出尾刺并开始下一次鞭击</summary>
        public const float TailDashStrikeDistance = 180f;
        public const float TailDashProjSpeed = 5f;
        public const float TailDashDamageMult = 1.2f;
        /// <summary>鞭击次数上限:高血 2,血量低于四分之一时 5(判定是"超过",所以实际打 3 / 6 次)</summary>
        public static int TailDashReps(NPC npc) => npc.life < npc.lifeMax / LowLifeDivisor ? 5 : 2;

        //==================== Laser:激光 ====================

        public const float LaserTailSwayReach = 140f;
        /// <summary>尾巴摆动:频率 0.04,幅度 200(比喷火更慢更大)</summary>
        public const float LaserSwayFreq = 0.04f;
        public const float LaserSwayAmp = 200f;
        public const float LaserTailLerp = 0.24f;
        /// <summary>尾巴朝向的参考点:本体正前方 800</summary>
        public const float LaserAimReach = 800f;
        public const float LaserTailLightRise = 0.1f;
        /// <summary>前 30 帧照常追瞄</summary>
        public const int LaserTrackFrames = 30;
        public const float LaserTrackRotate = 0.1f;
        public const float LaserTrackDrag = 0.96f;
        public const float LaserTrackThrust = 0.2f;
        /// <summary>80 帧后转为极慢的追瞄(比例 0.02 + 固定 0.008),给玩家绕出去的余地</summary>
        public const int LaserSlowTrackFrame = 80;
        public const float LaserSlowRotateLerp = 0.02f;
        public const float LaserSlowRotateFixed = 0.008f;
        public const float LaserDrag = 0.96f;
        /// <summary>后退推力(负号 = 向后)</summary>
        public const float LaserBackThrust = -0.04f;
        public const float LaserTailReach = 160f;
        public const float LaserTailHomeLerp = 0.08f;
        /// <summary>发射后坐、枪口前伸、光束初速</summary>
        public const float LaserRecoil = -0.8f;
        public const float LaserMuzzleOffset = 60f;
        public const float LaserSpeed = 36f;
        public const float LaserDamageMult = 1.2f;
        /// <summary>本段时长</summary>
        public const int LaserDuration = 350;

        //==================== 轮换表 ====================

        /// <summary>
        /// 一阶段,10 槽。每一发实招之间都垫一手接近,原表如此。
        /// 表是纯确定性的(一处随机都没有),所以出招裁决收归权威端不改变任何一次选招结果
        /// </summary>
        public static readonly ApsychosStateIndex[] Phase1 =
        {
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.Dash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.FireballShooting,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.Dash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.FlameThrow,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.FireballBig,
        };

        /// <summary>二阶段高血(&gt; 25%),15 槽。加入甩尾与激光,接近的占比开始下降</summary>
        public static readonly ApsychosStateIndex[] Phase2 =
        {
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.Dash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.FireballShooting,
            ApsychosStateIndex.Dash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.FlameThrow,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.FireballBig,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.TailDash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.Dash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.Laser,
        };

        /// <summary>
        /// 二阶段低血(&le; 25%),13 槽。接近只剩 5 手,冲刺 4 手、激光 2 手,末尾连着两手冲刺收尾。
        /// 注意换表<b>不重置</b>序号:血量跌破线的那一刻沿用当前 AIRound 直接进新表
        /// </summary>
        public static readonly ApsychosStateIndex[] Phase2Low =
        {
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.FireballBig,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.TailDash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.Dash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.Laser,
            ApsychosStateIndex.Dash,
            ApsychosStateIndex.MoveToTarget,
            ApsychosStateIndex.Laser,
            ApsychosStateIndex.Dash,
            ApsychosStateIndex.Dash,
        };

        /// <summary>按阶段与血量取表</summary>
        public static ApsychosStateIndex[] TableFor(int phase, NPC npc) {
            if (phase == 1) {
                return Phase1;
            }
            return npc.life > npc.lifeMax / LowLifeDivisor ? Phase2 : Phase2Low;
        }
    }
}
