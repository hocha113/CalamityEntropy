using CalamityEntropy.Core.CalamityRef;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.Core
{
    /// <summary>
    /// 先知的唯一数字出口。状态里不许出现裸数字(纯局部插值系数与原样照抄的角度步进除外)。
    /// <para>
    /// 本文件是状态机迁移时从原 <c>TheProphet.AI()</c> / <c>AttackPlayer()</c> 逐个搬出来的,
    /// <b>数值一律照搬,没有一处调整</b>。注释写的是「这个数在原代码里干什么」,
    /// 不是「这个数为什么该是这样」,原作者没留依据的地方不替他编理由。
    /// </para>
    /// </summary>
    internal static class ProphetDirector
    {
        //==================== 难度系数 difficult ====================

        /// <summary>
        /// 难度系数。原代码每帧在 <c>AttackPlayer</c> 开头重算一次:
        /// 专家 +0.06、大师 +0.06、复仇 +0.1、死亡 +0.1、getGood +0.15、天顶 +0.15,
        /// 最后整体乘 <c>1 + 当前血量比例 × 0.2</c>(血越满越快,残血反而变慢)。
        /// <para>
        /// 加法项先全部累完再乘那一项,顺序不可换。它直接乘进速度、射速与瞬移半径的分母,
        /// 所以六个开关必须各端一致,全是世界级已同步量;血量比例读的是已同步的 <c>NPC.life</c>。
        /// </para>
        /// <para>装灾厄读复仇/死亡,缺席仍走专家/大师兜底。</para>
        /// <para>
        /// 天顶那一支<b>实战到不了</b>:天顶世界整条 AI 都委派给 <c>OlderCruiserAIGNPC</c>,
        /// 根本走不到这里。原代码就这么写的,照搬保留。
        /// </para>
        /// </summary>
        public static float Difficult(NPC npc) {
            float difficult = 1;
            if (Main.expertMode) {
                difficult += 0.06f;
            }
            if (Main.masterMode) {
                difficult += 0.06f;
            }
            if (CECal.IsRevengeance) {
                difficult += 0.1f;
            }
            if (CECal.IsDeathMode) {
                difficult += 0.1f;
            }
            if (Main.getGoodWorld) {
                difficult += 0.15f;
            }
            if (Main.zenithWorld) {
                difficult += 0.15f;
            }
            difficult *= 1 + ((float)npc.life / npc.lifeMax) * 0.2f;
            return difficult;
        }

        //==================== 全局 ====================

        /// <summary>
        /// 状态总龄上限。原代码<b>没有</b>任何超时兜底,这是迁移时新加的纯安全网:
        /// 最长的一手是大激光 560 帧,1800 帧在正常对局里到不了,
        /// 存在的意义只是不让状态机死在某个状态里、Boss 靠惯性飘走
        /// </summary>
        public const int StateTimeoutFrames = 1800;

        /// <summary>出生演出帧数(原 <c>spawnAnm</c> 字段初值)。这一段 <c>dontTakeDamage</c> 且不出招</summary>
        public const int SpawnAnimFrames = 120;

        /// <summary>出生演出期间强制的朝向:正上方(原 <c>MathHelper.PiOver2 * -1</c>)</summary>
        public const float SpawnAnimRotation = -MathHelper.PiOver2;

        /// <summary>绘制:<c>spawnAnm</c> 小于它才画本体,大于 0 才画光环</summary>
        public const int SpawnAnimDrawFrames = 60;

        /// <summary>无目标时:每帧上浮加速度、速度阻尼、坚持多少帧后消失</summary>
        public const float NoTargetRiseAccel = 0.8f;
        public const float NoTargetDrag = 0.96f;
        public const int NoTargetDespawnFrames = 180;

        /// <summary>
        /// 原 <c>NoEnrange</c>:字段初值 300,目标在地牢里就被重置成 500,其余每帧自减。
        /// <b>整个仓库没有任何地方读它</b>,是彻底的残留量;照搬保留,别当它是狂怒计时
        /// </summary>
        public const int NoEnrageStart = 300;
        public const int NoEnrageDungeon = 500;

        /// <summary>
        /// 转二阶段的血量分界。原代码写的是<b>整数除法</b> <c>NPC.life &lt; NPC.lifeMax / 2</c>,
        /// 这里保持整数除法以免在 lifeMax 为奇数时差一格。转过去之后永不回落
        /// </summary>
        public const int Phase2LifeDivisor = 2;

        //==================== 减伤 ====================

        /// <summary>
        /// 原灾厄全局 DR 字段的本地等效初值(<c>SetDefaults</c> 里写一次)。
        /// 天顶世界整条 AI 提前 return,所以天顶下这个值原样保留不被覆盖
        /// </summary>
        public const float BaseDamageReduction = 0.10f;

        /// <summary>爬升减伤 <c>dr</c> 的初值与每帧衰减量(原 <c>0.5f / (160 * 60)</c>,即 160 秒抹平 0.5)</summary>
        public const float DrRampInitial = 0.26f;
        public const float DrRampDecayPerFrame = 0.5f / (160 * 60);

        /// <summary>大激光期间的基础减伤,以及其余时候的基础减伤(都再加上 <c>dr</c>)</summary>
        public const float DamageReductionLaser = 0.50f;
        public const float DamageReductionNormal = 0.12f;

        /// <summary>原 <c>DamageReduction = (AIStyle == 8 ? 0.50f : 0.12f) + dr</c></summary>
        public static float DamageReductionFor(ProphetStateIndex state, float drRamp)
            => (state == ProphetStateIndex.GrandLaser ? DamageReductionLaser : DamageReductionNormal) + drRamp;

        //==================== 表现:鳍与尾迹 ====================

        /// <summary>绘制朝向 <c>rl</c> 向真实朝向收敛的速率</summary>
        public const float DrawRotateRate = 0.1f;

        /// <summary>鳍摆动相位:每帧加 <c>速度长度 × 0.001 + 0.008</c>,超过 1 就减 1 回绕</summary>
        public const float FinPhaseSpeedFactor = 0.001f;
        public const float FinPhaseBase = 0.008f;

        /// <summary>尾迹质点:存活帧数、每帧阻尼</summary>
        public const int TailPointLife = 20;
        public const float TailPointDrag = 0.96f;

        /// <summary>尾迹新点:挂在本体后方 26,初速沿后方 16,再叠一个 <c>sin</c> 侧摆(频率 0.1,幅度 6)</summary>
        public const float TailSpawnBack = 26f;
        public const float TailSpawnSpeed = -16f;
        public const float TailSwayFreq = 0.1f;
        public const float TailSwayAmp = 6f;

        /// <summary>冲刺尾焰取点:本体前方 <c>速度长度 + 60</c></summary>
        public const float TrailPointForward = 60f;

        /// <summary>冲刺尾焰粒子:生成缩放、最长节数、以及冲刺推进期每帧续的 Lifetime</summary>
        public const float TrailSpawnScale = 7f;
        public const int TrailMaxLength = 14;
        public const int TrailKeepAlive = 13;

        /// <summary>瞬移特效:进出各两枚 SparkleCal 的基准缩放</summary>
        public const float TeleportSparkleScale = 5.6f;

        //==================== 轮换 ====================

        /// <summary>
        /// 轮换序号上限。原代码 <c>AIC++; if (AIC > 7) AIC = 0;</c>,即 8 个槽位循环,
        /// 起始值 -1 所以第一手落在槽 0
        /// </summary>
        public const int AttackIndexMax = 7;
        public const int AttackIndexStart = -1;

        /// <summary>
        /// 原 <c>GetAIType(int r)</c>:八个槽位到招式的映射,其中三个槽位当场掷一次硬币。
        /// <b>掷骰只在权威端</b>,结果经状态号写进 <c>ai[3]</c> 过线
        /// </summary>
        public static ProphetStateIndex AttackFor(int slot) {
            switch (slot) {
                case 0: return ProphetStateIndex.RuneVolley;
                case 1: return ProphetStateIndex.RuneTorrentFan;
                case 2: return ProphetStateIndex.VoidSpike;
                case 3: return ProphetStateIndex.Dash;
                case 4: return Main.rand.NextBool() ? ProphetStateIndex.RuneOrb : ProphetStateIndex.RuneImpact;
                case 5: return ProphetStateIndex.RuneDagger;
                case 6: return Main.rand.NextBool() ? ProphetStateIndex.RuneCluster : ProphetStateIndex.RapidTorrent;
                case 7: return Main.rand.NextBool() ? ProphetStateIndex.GrandLaser : ProphetStateIndex.AltRuneCharge;
            }
            //原代码的兜底 return 0;八槽全覆盖,走不到
            return ProphetStateIndex.RuneVolley;
        }

        /// <summary>
        /// 原选招段落里那十二个并列的 <c>if (AIStyle == n) AIChangeDelay = ...</c>。
        /// 倒计时在选招那一帧被赋上本招时长,状态体<b>当帧</b>就以满值跑一遍,
        /// 帧末再自减一次,所以每一手正好跑「时长」帧
        /// </summary>
        public static int DurationFor(ProphetStateIndex state) {
            switch (state) {
                case ProphetStateIndex.RuneVolley: return 240;
                case ProphetStateIndex.Dash: return 220;
                //原代码写的是 120 + 60 * 4,保留算式形状
                case ProphetStateIndex.RuneCluster: return 120 + 60 * 4;
                case ProphetStateIndex.RuneTorrentFan: return 100;
                case ProphetStateIndex.RapidTorrent: return 150;
                case ProphetStateIndex.RingBlink: return 245;
                case ProphetStateIndex.RuneOrb: return 142;
                case ProphetStateIndex.RuneImpact: return 280;
                case ProphetStateIndex.GrandLaser: return 560;
                case ProphetStateIndex.RuneDagger: return 160;
                case ProphetStateIndex.VoidSpike: return 320;
                case ProphetStateIndex.AltRuneCharge: return 242;
            }
            return 0;
        }

        //==================== 弹幕 ====================

        /// <summary>绝大多数弹幕的伤害除数:<c>NPC.damage / 6</c>(整数除法)</summary>
        public const int ProjDamageDivisor = 6;
        /// <summary>大激光眼球单独用 <c>NPC.damage / 5</c></summary>
        public const int EyeDamageDivisor = 5;
        /// <summary>符文晶簇在 <c>damage / 6</c> 之后再减 5</summary>
        public const int CrystalDamageOffset = -5;

        //==================== 收招后的惯性(原 else 分支)====================

        /// <summary>
        /// 原 <c>if (AIChangeDelay > 0) { 招式 } else { 这里 }</c>。
        /// 选招总把倒计时赋成正数、且当帧就跑状态体,所以权威端<b>永远进不来</b>;
        /// 只有客户端在「本地倒计时已归零、换态包还没到」的一两帧里会短暂走到。照搬保留
        /// </summary>
        public const float IdleDrag = 0.98f;
        public const float IdleThrust = 1f;

        //==================== 0 号 四轮符文弹 ====================

        /// <summary>每帧阻尼,以及绕着玩家侧向切线推进的加速度(本体在玩家左侧取正,右侧取负)</summary>
        public const float VolleyDrag = 0.96f;
        public const float VolleyOrbitAccel = 0.1f;
        /// <summary>转向速率(比例式)</summary>
        public const float VolleyRotateRate = 0.6f;
        /// <summary>倒计时高于它才继续瞬移/开火</summary>
        public const int VolleyActiveUntil = 30;
        /// <summary>瞬移节拍:一阶段每 60 帧、二阶段每 46 帧一次</summary>
        public const int VolleyBlinkPeriodP1 = 60;
        public const int VolleyBlinkPeriodP2 = 46;
        /// <summary>瞬移半径(除以难度系数,越难落点越近)</summary>
        public const float VolleyBlinkRadius = 1250f;
        /// <summary>
        /// 开火节拍:一阶段 <c>倒计时 % 60 == 56</c>,二阶段 <c>% 50 == 46</c>。
        /// 二阶段的周期(50)与瞬移周期(46)<b>不同</b>,所以两拍会互相错开,原代码如此
        /// </summary>
        public const int VolleyFirePeriodP1 = 60;
        public const int VolleyFirePeriodP2 = 50;
        public const int VolleyFirePhaseP1 = 56;
        public const int VolleyFirePhaseP2 = 46;
        /// <summary>扇形层数上限:一阶段 2,二阶段 3(判定是 &lt;=,所以含中弹共 3 / 4 层)</summary>
        public const int VolleyLayersP1 = 2;
        public const int VolleyLayersP2 = 3;
        /// <summary>每层张角</summary>
        public const float VolleySpreadP1 = 0.5f;
        public const float VolleySpreadP2 = 0.36f;
        /// <summary>符文洪流的 ai0(最大速度)与 ai1(开启加速段)</summary>
        public const float VolleyTorrentMaxSpeed = 6f;
        public const float VolleyTorrentAi1 = 1f;
        /// <summary>开火后坐:沿远离玩家方向加 9</summary>
        public const float VolleyRecoil = 9f;

        //==================== 1 号 冲刺 ====================

        /// <summary>三次冲刺的起手帧(倒计时读数)。最后一次是重击</summary>
        public const int DashBeatA = 220;
        public const int DashBeatB = 160;
        public const int DashBeatC = 100;
        /// <summary>锁向时的目标预测提前量(帧)</summary>
        public const float DashLeadFrames = 12f;
        /// <summary>起手反冲:前两次 6,重击 16</summary>
        public const float DashBackstepNormal = 6f;
        public const float DashBackstepHeavy = 16f;
        /// <summary>推进窗长度(写进 <c>ai[1]</c> 的倒计时):前两次 46,重击 80</summary>
        public const float DashThrustFramesNormal = 46f;
        public const float DashThrustFramesHeavy = 80f;
        /// <summary>二阶段起手才附带的扇形符文洪流:层数上限(重击 2,平击 1)、张角、速度倍率</summary>
        public const int DashVolleyLayersHeavy = 2;
        public const int DashVolleyLayersNormal = 1;
        public const float DashVolleySpread = 0.44f;
        public const float DashVolleySpeedMult = 2f;
        /// <summary>推进期:每帧推力(重击段再 ×1.6,二阶段再 ×1.4)与阻尼</summary>
        public const float DashThrust = 1f;
        public const float DashThrustHeavyMult = 1.6f;
        public const float DashThrustPhase2Mult = 1.4f;
        public const float DashDrag = 0.98f;
        /// <summary>推进结束后的硬刹</summary>
        public const float DashBrakeDrag = 0.8f;
        /// <summary>重击途中撒侧弹的窗口:<c>ai[1] &lt; 64</c> 且倒计时整除 <c>(int)(周期 / 难度)</c></summary>
        public const int DashSideBulletWindow = 64;
        public const int DashSideBulletPeriodP1 = 10;
        public const int DashSideBulletPeriodP2 = 8;
        /// <summary>侧弹:沿速度法线 0.15 倍速射出,ai0(最大速度)20</summary>
        public const float DashSideBulletSpeedFactor = 0.15f;
        public const float DashSideBulletMaxSpeed = 20f;
        /// <summary>收招前的撤离瞬移:倒计时 10 时跳到玩家周围 1000</summary>
        public const int DashExitBeat = 10;
        public const float DashExitRadius = 1000f;

        //==================== 2 号 符文晶簇 ====================

        public const float ClusterDrag = 0.98f;
        /// <summary>倒计时低于它开始向玩家侧后方 400 的悬停点回收</summary>
        public const int ClusterHomeBelow = 120;
        public const float ClusterHomeAngle = 0.4f;
        public const float ClusterHomeRadius = 400f;
        public const float ClusterHomeAccel = 1f;
        /// <summary>瞬移/开火只在倒计时高于它时进行</summary>
        public const int ClusterActiveAbove = 110;
        public const int ClusterPeriod = 60;
        /// <summary>瞬移在周期的 0 拍,开火在 30 拍</summary>
        public const int ClusterBlinkPhase = 0;
        public const int ClusterFirePhase = 30;
        public const float ClusterBlinkRadius = 900f;
        /// <summary>瞬移后立刻朝玩家冲 8</summary>
        public const float ClusterLaunchSpeed = 8f;
        /// <summary>晶簇发数:一阶段 1,二阶段 2(第二发左右各一)</summary>
        public const int ClusterShotsP1 = 1;
        public const int ClusterShotsP2 = 2;
        public const float ClusterSpread = 0.4f;
        public const float ClusterSpeed = 20f;

        //==================== 3 号 双层符文洪流 ====================

        /// <summary>瞬移拍与开火拍(整段只有 100 帧)</summary>
        public const int TorrentFanBlinkBeat = 88;
        public const int TorrentFanFireBeat = 77;
        public const float TorrentFanBlinkRadius = 900f;
        /// <summary>外层扇形层数上限:一阶段 4,二阶段 6</summary>
        public const int TorrentFanLayersP1 = 4;
        public const int TorrentFanLayersP2 = 6;
        /// <summary>内插层的层号上限(半整数,从 0.5 起步):一阶段 5.5,二阶段 7.5</summary>
        public const float TorrentFanHalfLayersP1 = 5.5f;
        public const float TorrentFanHalfLayersP2 = 7.5f;
        public const float TorrentFanSpreadP1 = 0.38f;
        public const float TorrentFanSpreadP2 = 0.32f;
        /// <summary>外层速度倍率 0.8,内插层 0.5</summary>
        public const float TorrentFanSpeedOuter = 0.8f;
        public const float TorrentFanSpeedInner = 0.5f;
        /// <summary>ai0(最大速度):正中那一发 6,其余 5</summary>
        public const float TorrentFanMaxSpeedCenter = 6f;
        public const float TorrentFanMaxSpeedSide = 5f;
        public const float TorrentFanAi1 = 1f;

        //==================== 4 号 速射符文洪流 ====================

        /// <summary>起手瞬移拍(整段 150 帧,所以是第二帧)</summary>
        public const int RapidBlinkBeat = 149;
        public const float RapidBlinkRadius = 900f;
        /// <summary>瞬移后朝玩家推 1</summary>
        public const float RapidLaunchSpeed = 1f;
        /// <summary>
        /// 两段射击窗:倒计时 119~100 是慢段(每 6 帧一发),99~81 是快段(每 2 帧一发)。
        /// 150~120 这 31 帧什么都不做,原代码的 <c>&gt; 80</c> / <c>&lt; 100</c> / <c>&lt; 120</c> 三层嵌套如此
        /// </summary>
        public const int RapidWindowLow = 80;
        public const int RapidFastBelow = 100;
        public const int RapidSlowBelow = 120;
        public const int RapidFastPeriod = 2;
        public const int RapidFastSoundPeriod = 4;
        public const int RapidSlowPeriod = 6;
        /// <summary>快段:散布与速度倍率</summary>
        public const float RapidFastScatterP1 = 0.1f;
        public const float RapidFastScatterP2 = 0.16f;
        public const float RapidFastSpeed = 1.8f;
        /// <summary>慢段:散布与速度倍率</summary>
        public const float RapidSlowScatterP1 = 0.16f;
        public const float RapidSlowScatterP2 = 0.4f;
        public const float RapidSlowSpeed = 1.5f;
        /// <summary>两段共用的 ai0(最大速度)</summary>
        public const float RapidMaxSpeed = 12f;

        //==================== 5 号 环状符文(连续闪现)====================

        /// <summary>倒计时高于它是闪现段,低于它转为普通追击</summary>
        public const int RingBlinkUntil = 160;
        /// <summary>闪现周期:一阶段 4 帧一次,二阶段 3 帧一次</summary>
        public const int RingBlinkPeriodP1 = 4;
        public const int RingBlinkPeriodP2 = 3;
        public const float RingBlinkRadius = 1400f;
        /// <summary>每次闪现吐一发慢速洪流,ai0(最大速度)12</summary>
        public const float RingTorrentSpeed = 0.4f;
        public const float RingTorrentMaxSpeed = 12f;
        /// <summary>收尾追击段:阻尼与推力</summary>
        public const float RingChaseDrag = 0.96f;
        public const float RingChaseThrust = 0.5f;

        //==================== 6 号 符文光球 ====================

        /// <summary>起手瞬移拍与三次布阵拍(后两次只在二阶段)</summary>
        public const int OrbBlinkBeat = 140;
        public const int OrbRingBeatA = 130;
        public const int OrbRingBeatB = 120;
        public const int OrbRingBeatC = 110;
        public const float OrbBlinkRadius = 800f;
        /// <summary>环上锚点的角度步进:一阶段 60°(6 个),二阶段 40°(9 个)</summary>
        public const float OrbAngleStepP1 = 60f;
        public const float OrbAngleStepP2 = 40f;
        /// <summary>锚点半径与每个锚点的两枚火花粒子缩放</summary>
        public const float OrbRingRadius = 86f;
        public const float OrbSparkleScale = 4f;
        /// <summary>符文的 ai2 取值范围(贴图变体),原 <c>Main.rand.Next(1, 12)</c></summary>
        public const int OrbRuneVariantMin = 1;
        public const int OrbRuneVariantMax = 12;
        /// <summary>本体每帧朝玩家的推力与阻尼</summary>
        public const float OrbThrust = 0.2f;
        public const float OrbDrag = 0.96f;

        //==================== 7 号 符文冲击 ====================

        /// <summary>倒计时高于它才进「刹车 + 放电」节拍,低于它一直保持绕行</summary>
        public const int ImpactActiveAbove = 50;
        /// <summary>节拍周期 40:余数 0~20 是放电窗(并刹车),21~39 是绕行窗</summary>
        public const int ImpactPeriod = 40;
        public const int ImpactFireRemainder = 20;
        public const float ImpactBrakeDrag = 0.94f;
        /// <summary>闪电层数上限:一阶段 2,二阶段 4</summary>
        public const int ImpactLayersP1 = 2;
        public const int ImpactLayersP2 = 4;
        public const float ImpactSpreadP1 = 0.38f;
        public const float ImpactSpreadP2 = 0.32f;
        public const float ImpactBoltSpeed = 16f;
        /// <summary>绕行窗:保持在玩家外侧 160 的环上,速度按差值 0.08 收敛,转向 0.06</summary>
        public const float ImpactOrbitRadius = 160f;
        public const float ImpactOrbitLerp = 0.08f;
        public const float ImpactOrbitRotate = 0.06f;

        //==================== 8 号 大激光 ====================

        public const float LaserDrag = 0.9f;
        /// <summary>开场拍:清场 + 定位(整段 560 帧,所以是第五帧)</summary>
        public const int LaserSetupBeat = 556;
        /// <summary>禁忌档案馆定位的下移量</summary>
        public const float LaserArchiveOffsetY = 80f;
        /// <summary>兜底:离玩家超过这个距离就改落到玩家附近 300</summary>
        public const float LaserFallbackDistance = 1600f;
        public const float LaserFallbackRadius = 300f;
        /// <summary>拖人段的结束拍(倒计时高于它都在拖人)</summary>
        public const int LaserPullUntil = 480;
        public const float LaserPullDrag = 0.96f;
        /// <summary>只影响这个半径内的玩家</summary>
        public const float LaserAffectRadius = 2400f;
        /// <summary>拖人靶点相对本体的偏移</summary>
        public const float LaserFocusOffsetY = -80f;
        /// <summary>离靶点超过它才会被拖</summary>
        public const float LaserPullRadius = 560f;
        /// <summary>被拖玩家:无敌帧、速度、每帧位移、粒子数</summary>
        public const int LaserPullImmune = 20;
        public const float LaserPullSpeed = 10f;
        public const float LaserPullStep = 36f;
        public const int LaserPullParticles = 8;
        /// <summary>回翅膀的窗口:倒计时 61~479</summary>
        public const int LaserWingWindowLow = 60;
        public const int LaserWingWindowHigh = 480;
        /// <summary>开炮拍;若本体周围 250 见方有实心块则本招作废,倒计时直接压到 30</summary>
        public const int LaserFireBeat = 480;
        public const int LaserBlockedCheckSize = 250;
        public const int LaserAbortCountdown = 30;
        /// <summary>眼球初速</summary>
        public const float LaserEyeSpeed = 0.7f;

        //==================== 9 号 符文飞匕 ====================

        /// <summary>起手瞬移拍(整段 160 帧,即第一帧)与随机半径区间</summary>
        public const int DaggerBlinkBeat = 160;
        public const float DaggerBlinkRadiusMin = 500f;
        public const float DaggerBlinkRadiusMax = 600f;
        /// <summary>倒计时高于它才撒匕首,每 5 帧一把</summary>
        public const int DaggerActiveAbove = 60;
        public const int DaggerPeriod = 5;
        /// <summary>匕首初速:半径 10 的圆内随机一点</summary>
        public const float DaggerScatter = 10f;

        //==================== 10 号 虚空触手 ====================

        /// <summary>起手瞬移拍(整段 320 帧,即第 11 帧)与半径</summary>
        public const int SpikeBlinkBeat = 310;
        public const float SpikeBlinkRadius = 400f;
        /// <summary>倒计时高于它才放环,周期一阶段 46、二阶段 36</summary>
        public const int SpikeActiveAbove = 140;
        public const int SpikePeriodP1 = 46;
        public const int SpikePeriodP2 = 36;
        /// <summary>环上角度步进:一阶段 36°(10 根),二阶段 30°(12 根)</summary>
        public const float SpikeAngleStepP1 = 36f;
        public const float SpikeAngleStepP2 = 30f;
        /// <summary>每根触手的随机抖动与初速</summary>
        public const float SpikeScatter = 0.2f;
        public const float SpikeSpeed = 8f;
        /// <summary>本体每帧按与玩家的差值 ×0.008 直接赋速度(慢速贴近)</summary>
        public const float SpikeDriftLerp = 0.008f;

        //==================== 11 号 异形符文冲锋 ====================

        /// <summary>起手瞬移拍(整段 242 帧,即第四帧)与半径(除以难度系数)</summary>
        public const int AltBlinkBeat = 239;
        public const float AltBlinkRadius = 1400f;
        /// <summary>布阵拍:环上撒静止符文</summary>
        public const int AltRuneBeat = 228;
        /// <summary>角度上限写的是 358 而不是 360,所以一阶段实际只有 5 个锚点(0/72/144/216/288)</summary>
        public const float AltRuneAngleLimit = 358f;
        public const float AltRuneStepP1 = 72f;
        public const float AltRuneStepP2 = 60f;
        /// <summary>三次冲锋的起手拍</summary>
        public const int AltChargeBeatA = 210;
        public const int AltChargeBeatB = 140;
        public const int AltChargeBeatC = 70;
        /// <summary>锁向时的目标预测提前量(帧)</summary>
        public const float AltChargeLeadFrames = 8f;
        /// <summary>冲锋窗长度(写进 <c>ai[1]</c>)</summary>
        public const float AltChargeFrames = 62f;
        /// <summary>冲锋初速:难度系数 × 阶段倍率 × 18</summary>
        public const float AltChargeSpeedP1 = 0.86f;
        public const float AltChargeSpeedP2 = 1.1f;
        public const float AltChargeSpeedBase = 18f;
        /// <summary>冲锋期阻尼与结束后的硬刹</summary>
        public const float AltChargeDrag = 0.99f;
        public const float AltBrakeDrag = 0.8f;
    }
}
