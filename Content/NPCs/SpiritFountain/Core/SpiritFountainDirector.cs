using CalamityEntropy.Core.CalamityRef;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.Core
{
    /// <summary>
    /// 冥魂泉的唯一数字出口。状态里不许出现裸数字(纯局部插值系数除外)。
    /// <para>
    /// 本文件是状态机重构时从原 <c>SpiritFountain.AI()</c> 逐个搬出来的,
    /// <b>数值一律照搬,没有一处调整</b>。注释写的是「这个数在原代码里干什么」,
    /// 不是「这个数为什么该是这样」,原作者没留依据的地方不替他编理由。
    /// </para>
    /// <para>部件 <see cref="SpiritRing"/> 的数字仍留在它自己的文件里:它是手写 AI 的伴生部件,不是状态文件。</para>
    /// </summary>
    internal static class SpiritFountainDirector
    {
        //==================== 实体定义 ====================

        /// <summary>本体碰撞盒(只是个占位,本体不参与碰撞判定)</summary>
        public const int BodySize = 64;
        /// <summary>接触伤害基数。专家再 +10,大师再 +4</summary>
        public const int BaseDamage = 520;
        public const int DamageExpertBonus = 10;
        public const int DamageMasterBonus = 4;
        /// <summary>血量上限。本体常年 <c>dontTakeDamage</c>,伤害由环(realLife 指向本体)转发进来</summary>
        public const int LifeMax = 9000000;
        public const float Value = 100000f;
        /// <summary>虚空触碰减伤</summary>
        public const float VoidTouchDR = 0.9f;
        /// <summary>本体自身的伤害缩放</summary>
        public const float DamageMul = 0.1f;
        /// <summary>大师模式整体放大</summary>
        public const float MasterScale = 1.12f;
        /// <summary>存活时间倍率</summary>
        public const int TimeLeftMul = 12;

        //==================== 魂环数量 ====================

        /// <summary>魂环基数。专家 +2、大师 +2、复仇 +2、死亡 +2、getGood +4、天顶 +4(全是加法,顺序无关)</summary>
        public const int SpiritCountBase = 10;
        public const int SpiritCountDifficultyBonus = 2;
        public const int SpiritCountWorldBonus = 4;

        /// <summary>装灾厄读复仇/死亡,缺席仍走专家/大师兜底。原 SetDefaults 逐条搬过来</summary>
        public static int SpiritCount() {
            int count = SpiritCountBase;
            if (Main.expertMode) {
                count += SpiritCountDifficultyBonus;
            }
            if (Main.masterMode) {
                count += SpiritCountDifficultyBonus;
            }
            if (CECal.IsRevengeance) {
                count += SpiritCountDifficultyBonus;
            }
            if (CECal.IsDeathMode) {
                count += SpiritCountDifficultyBonus;
            }
            if (Main.getGoodWorld) {
                count += SpiritCountWorldBonus;
            }
            if (Main.zenithWorld) {
                count += SpiritCountWorldBonus;
            }
            return count;
        }

        //==================== 阶段:每帧按血量比例重算 ====================

        /// <summary>
        /// 六条血量分界。原代码每帧把 <c>phase</c> 归 1 再逐条自增,所以它是<b>确定性推导量</b>:
        /// 血量本身走原版同步,各端算出同值,不需要额外过线
        /// </summary>
        public const float Phase1_2 = 0.9f;
        public const float Phase1_3 = 0.75f;
        public const float Phase2_1 = 0.66f;
        public const float Phase2_2 = 0.44f;
        public const float Phase3_1 = 0.33f;
        public const float Phase3_2 = 0.15f;

        /// <summary>进入转阶段演出的门槛:阶段号大于它(即血量跌破 <see cref="Phase2_1"/>)</summary>
        public const int PhaseTransThreshold = 3;

        /// <summary>阶段号 1~7。原代码 L246-271 的逐条自增,一条不合并</summary>
        public static int PhaseFor(NPC npc) {
            int phase = 1;
            float p = (float)npc.life / npc.lifeMax;
            if (p < Phase1_2) {
                phase++;
            }
            if (p < Phase1_3) {
                phase++;
            }
            if (p < Phase2_1) {
                phase++;
            }
            if (p < Phase2_2) {
                phase++;
            }
            if (p < Phase3_1) {
                phase++;
            }
            if (p < Phase3_2) {
                phase++;
            }
            return phase;
        }

        //==================== 难度系数 enrage ====================

        /// <summary>
        /// 难度系数,每帧在状态机之前重算。三组互斥的加法项:
        /// 大师 +0.2 否则专家 +0.1、死亡 +0.2 否则复仇 +0.1、天顶 +0.3 否则 getGood +0.15。
        /// <para>
        /// 它直接除进射速间隔、乘进摇摆推进,所以三组开关必须各端一致,都是世界级已同步量。
        /// 装灾厄读复仇/死亡,缺席仍走大师/专家兜底
        /// </para>
        /// </summary>
        public static float Enrage() {
            float enrage = 1f;
            if (Main.masterMode) {
                enrage += 0.2f;
            }
            else if (Main.expertMode) {
                enrage += 0.1f;
            }
            if (CECal.IsDeathMode) {
                enrage += 0.2f;
            }
            else if (CECal.IsRevengeance) {
                enrage += 0.1f;
            }
            if (Main.zenithWorld) {
                enrage += 0.3f;
            }
            else if (Main.getGoodWorld) {
                enrage += 0.15f;
            }
            return enrage;
        }

        //==================== 全局 ====================

        /// <summary>
        /// 状态总龄上限。原代码<b>没有</b>任何超时兜底,这是重构时新加的纯安全网:
        /// 最长的实战状态是 Moving(701 帧),出场演出约 501 帧,3600 帧在正常对局里到不了。
        /// SpiritSlicing 是终局循环态,单独覆写为不限
        /// </summary>
        public const int StateTimeoutFrames = 3600;

        /// <summary>
        /// 同一帧内最多续跑几次状态体。原 AI() 里实际能连的最长链是「转阶段触发 → PhaseTranse1 → SpiritSlicing」,
        /// 这里给 8 只是防死循环
        /// </summary>
        public const int MaxChainStepsPerFrame = 8;

        /// <summary>弹幕伤害除数。<b>整数除法</b>(<c>NPC.damage / 6</c>)后再乘倍率,不能改成乘 1/6f</summary>
        public const int ProjDamageDivisor = 6;
        /// <summary>弹幕击退</summary>
        public const float ProjKnockback = 3f;

        //==================== 序幕:双柱魂乱判定 + 双柱烟雾 ====================

        /// <summary>柱子透明度超过它才吃魂乱判定</summary>
        public const float ColumnBuffAlphaGate = 0.1f;
        /// <summary>判定线的半长(向柱子两端各取 2000)</summary>
        public const float ColumnBuffLineHalfLength = 2000f;
        /// <summary>判定线宽度基数,乘 <c>NPC.scale</c> 后取整</summary>
        public const float ColumnBuffLineWidth = 100f;
        /// <summary>魂乱持续帧数(每帧重加,等于「站在柱子上就一直挂着」)</summary>
        public const int ColumnBuffDuration = 10;

        /// <summary>双柱烟雾:每帧每柱的循环次数</summary>
        public const int ColumnSmokeLoop = 90;
        /// <summary>沿柱方向的随机散布</summary>
        public const float ColumnSmokeAlong = 1600f;
        /// <summary>垂直柱方向的随机散布:一号柱 40,二号柱 100</summary>
        public const float ColumnSmokeAcross1 = 40f;
        public const float ColumnSmokeAcross2 = 100f;
        /// <summary>屏心裁剪半径,90×双柱密度太高,不裁会把粒子槽吃光</summary>
        public const float ColumnSmokeCullRadius = 3200f;
        public const float ColumnSmokeColorMul = 3f;
        public const float ColumnSmokeScaleMin = 0.1f;
        public const float ColumnSmokeScaleMax = 0.2f;
        public const int ColumnSmokeLife = 50;
        public const float ColumnGlowColorMul = 2f;
        public const float ColumnGlowScaleMin = 0.6f;
        public const float ColumnGlowScaleMax = 0.8f;
        public const float ColumnGlowVelRadius = 6f;
        public const int ColumnGlowLife = 28;

        //==================== 尾声:脱战与眼睛 ====================

        /// <summary>无有效目标的累计帧数上限,超过即 <c>NPC.active = false</c></summary>
        public const int DeactiveFrames = 600;
        /// <summary>有目标但玩家离开这个方框(半边长 150 格)也直接消失</summary>
        public const int DespawnBoxTiles = 150;
        /// <summary>玩家一侧参与相交判定的小方框边长</summary>
        public const int DespawnPlayerBox = 10;
        /// <summary>眼睛透明度每帧向目标插值的速率</summary>
        public const float EyeAlphaLerp = 0.04f;
        /// <summary>眼睛透明度的每帧默认目标(声明通道的回落值)</summary>
        public const float EyeAlphaIdle = 0.6f;

        //==================== SpawnAnimation:出场演出 ====================

        /// <summary>聚魂倒计时。这段时间里 aiTimer 每帧被压回 0,并且整个尾声都跳过</summary>
        public const int GatheringFrames = 300;
        /// <summary>倒计时读到这个值的那一帧放两发定格闪光</summary>
        public const int GatheringShineFrame = GatheringFrames;
        public const float GatheringShineScale1 = 5f;
        public const float GatheringShineScale2 = 3.6f;
        public const int GatheringShineLife = 320;
        /// <summary>倒计时降到它以下就不再放归魂粒子</summary>
        public const int GatheringSpiritStopAt = 70;
        /// <summary>归魂粒子概率 <c>0.2 + (1 - (G - 100) / 200)</c>,随倒计时递减而变密</summary>
        public const float GatheringSpiritChanceBase = 0.2f;
        public const float GatheringSpiritChanceOffset = 100f;
        public const float GatheringSpiritChanceSpan = 200f;
        /// <summary>归魂粒子的出生半径、切向偏折、随机偏折与速度区间</summary>
        public const float GatheringSpiritRadius = 1600f;
        public const float GatheringSpiritSwirl = 2.7f;
        public const float GatheringSpiritScatter = 0.4f;
        public const float GatheringSpiritSpeedMin = 12f;
        public const float GatheringSpiritSpeedMax = 16f;

        /// <summary>落点写入时的判据:档案馆坐标无效(新世界恒为 -1)就退回目标玩家</summary>
        public const float ArchivePosValidX = 10f;

        /// <summary>过了这一帧眼睛开始追着本地玩家转</summary>
        public const int SpawnStareStartFrame = 40;
        public const float SpawnStareLerp = 0.08f;
        /// <summary>眼睛亮到 0.6 之后本体才开始显形</summary>
        public const float SpawnEyeAlphaGate = 0.6f;
        public const float SpawnEyeAlphaStep = 0.01f;
        public const float SpawnOpacityStep = 0.025f;
        /// <summary>一号柱透明度 = 本体不透明度 × 0.6</summary>
        public const float SpawnColumnAlphaFactor = 0.6f;
        /// <summary>过了这一帧喷流速度开始收到 4</summary>
        public const int SpawnSlowDownFrame = 140;
        public const float SpawnFountainSpeedTarget = 4f;
        public const float SpawnFountainSpeedLerp = 0.06f;
        /// <summary>出场演出总长,过了就定格收尾并放魂环</summary>
        public const int SpawnEndFrame = 200;
        /// <summary>收尾定格值</summary>
        public const float SpawnEndColumnAlpha = 0.6f;
        public const float SpawnEndEyeAlpha = 0.6f;
        /// <summary>出场闪柱的持续帧数(绘制用)</summary>
        public const float SpawnFlashFrames = 90f;
        public const float SpawnFlashWidth = 24f;
        public const int SpawnFlashParabolaPower = 4;

        //==================== Moving:横扫柱 ====================

        public const float MovingFountainSpeedTarget = 8f;
        public const float MovingFountainSpeedLerp = 0.06f;
        /// <summary>摇摆相位每帧推进量,乘 enrage</summary>
        public const float MovingSwayStep = 0.01f;
        /// <summary>摇摆幅度从 0 慢慢涨到 1,速率乘 enrage</summary>
        public const float MovingAmpLerp = 0.004f;
        /// <summary>柱子横向行程</summary>
        public const float MovingSwayRange = 1800f;
        public const float MovingOffsetLerp = 0.2f;
        /// <summary>过了这一帧才让柱子按位移倾斜(头三帧位移差是噪声)</summary>
        public const int MovingTiltStartFrame = 3;
        /// <summary>倾斜系数:天顶 -0.01,平常 -0.006</summary>
        public const float MovingTiltFactorZenith = -0.01f;
        public const float MovingTiltFactor = -0.006f;
        /// <summary>倾斜再按阶段放大 <c>1 + phase × 0.02</c></summary>
        public const float MovingTiltPhaseFactor = 0.02f;
        public const float MovingColumnAlpha = 0.45f;
        public const float MovingColumnAlphaLerp = 0.06f;

        /// <summary>一阶段:每 <c>20 / enrage</c> 帧一发随机方向魂弹,初速 6</summary>
        public const float MovingP1Interval = 20f;
        public const float MovingP1Speed = 6f;
        /// <summary>二阶段:每 <c>16 / enrage</c> 帧对射两发,初速 8,相位 <c>Counter × 0.04</c>,ai1 = 0.4</summary>
        public const float MovingP2Interval = 16f;
        public const float MovingP2Speed = 8f;
        public const float MovingP2Phase = 0.04f;
        public const float MovingP2Ai1 = 0.4f;
        /// <summary>三阶段:每 <c>80 / enrage</c> 帧一轮向心齐射</summary>
        public const float MovingP3Interval = 80f;
        /// <summary>扇面枚举 <c>i &lt; 350, i += 45</c>,实际八个方向</summary>
        public const float MovingP3SweepEnd = 350f;
        public const float MovingP3SweepStep = 45f;
        public const float MovingP3Phase = 0.04f;
        /// <summary>出生半径 <c>2200 / enrage</c>,向心初速 16,ai1 = ai2 = 1</summary>
        public const float MovingP3Radius = 2200f;
        public const float MovingP3Speed = 16f;
        public const float MovingP3Ai1 = 1f;
        public const float MovingP3Ai2 = 1f;
        /// <summary>沿整条弹道铺预警线,步长 0.025 即每扇 40 颗</summary>
        public const float MovingP3LineStep = 0.025f;
        public const float MovingP3LineVelFactor = 0.03f;
        public const float MovingP3LineScatter = 8f;
        public const int MovingP3LineLife = 100;
        public const float MovingP3LineScaleMin = 1.4f;
        public const float MovingP3LineScaleMax = 2f;
        /// <summary>三阶段起本体可以被直接打</summary>
        public const int MovingDamageablePhase = 3;
        /// <summary>本段时长</summary>
        public const int MovingDuration = 700;

        //==================== Boomerang:魂环回旋 ====================

        /// <summary>柱子收回中线附近轻晃。<c>Main.GameUpdateCount</c> 是各端独立的本地帧计数,见宿主里的说明</summary>
        public const float BoomerangIdleFreq = 0.02f;
        public const float BoomerangIdleAmp = 100f;
        public const float BoomerangOffsetLerp = 0.03f;
        /// <summary>过了这一帧进度量才开始涨,之前每帧压回 0</summary>
        public const int BoomerangChargeStartFrame = 80;
        /// <summary>进度步长:三阶段 0.005,其余 0.003</summary>
        public const float BoomerangStepP3 = 0.005f;
        public const float BoomerangStep = 0.003f;
        /// <summary>进度上限:三阶段 1.66,其余 1.36。魂环按自己的 Index 依次脱柱,所以它同时是「放完几圈」</summary>
        public const float BoomerangLimitP3 = 1.66f;
        public const float BoomerangLimit = 1.36f;
        /// <summary>切三阶段口径的阶段号</summary>
        public const int BoomerangPhase3 = 3;

        //==================== Lasers / RingFountains:柱子退场,火力全在魂环身上 ====================

        /// <summary>两段都只做一件事:把柱子横向偏移收回去</summary>
        public const float ColumnRetractDamp = 0.9f;
        public const int LasersDuration = 460;
        public const int RingFountainsDuration = 280;

        //==================== PhaseTranse1:转阶段演出 ====================

        public const float TransColumn2Alpha = 0.45f;
        public const float TransColumn2AlphaLerp = 0.04f;
        /// <summary>本段时长。注意状态体内<b>额外再自增一次</b> aiTimer,所以实际只跑 71 帧</summary>
        public const int TransDuration = 140;

        //==================== SpiritSlicing:十字斩,终局循环态 ====================

        /// <summary>一个斩击周期 = 90 + 42 帧</summary>
        public const int SlicingBeat = 90;
        public const int SlicingGap = 42;
        public const int SlicingPeriod = SlicingBeat + SlicingGap;
        /// <summary>两柱各自的行程端点,正负由权威端骰</summary>
        public const float SlicingReach = 1400f;
        public const float SlicingOffsetLerp = 0.056f;
        /// <summary>周期里第 90 帧翻向</summary>
        public const int SlicingFlipPhase = SlicingBeat;
        /// <summary>跑满这么久之后,在周期开头 40 帧的窗口里把计时归零重开</summary>
        public const int SlicingLoopAfter = 800;
        public const int SlicingLoopWindow = 40;
        /// <summary>初始化那一拍</summary>
        public const int SlicingInitFrame = 1;
    }
}
