using CalamityEntropy.Core.AI;
using System.Collections.Generic;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.Core
{
    /// <summary>
    /// Luminaris 状态上下文。
    /// <para>
    /// 「事实」区是参与判定或需要两端一致的量,随 <c>SendExtraAI</c> 过线;
    /// 「每帧重算」区各端同算所以不过线;
    /// 「表现」区是本地推导的绘制量,靠已过线的状态号与计时自然收敛。
    /// </para>
    /// <para>
    /// 本 Boss <b>没有</b>「每帧回落」的声明通道:原代码里 <see cref="AfterImageTime"/> 与
    /// <see cref="MegaTrail"/> 都是状态在窗口内逐帧写满、宿主在 AI 开头统一衰减的持久量,
    /// 每帧清回默认值反而会把衰减尾巴掐断。所以 <see cref="BeginFrameDefaults"/> 在这里是空的
    /// </para>
    /// </summary>
    public class LuminarisStateContext : CEBossStateContext
    {
        #region 核心引用
        public Luminaris Owner { get; set; }
        #endregion

        #region 事实:过线
        /// <summary>
        /// 出招倒计时,对应原代码的 <c>AIChangeCounter</c>。<b>本 Boss 的主时钟</b>。
        /// <para>
        /// 原代码所有节拍都是对这个<b>递减绝对值</b>的判断(<c>== 200</c>、<c>&gt; 160 &amp;&amp; &lt; 200</c>、
        /// <c>Utils.Remap(C, 230, 190, 0, 1)</c> 等),而 <c>VaultState.Timer</c> 是递增的。
        /// 把几十条阈值手工翻成递增写法,一个符号错就是静默的手感改动,所以倒计时本身被保留下来,
        /// 节拍照原样对它判断;<c>Timer</c> / <c>Counter</c> 仍走基类,只充当计时收养与超时兜底通道。
        /// </para>
        /// <para>
        /// 它由 <see cref="LuminarisRotation.Pick"/> 在选招时置成本招时长,之后每帧由状态体末尾的
        /// <c>Tick</c> 自减一次;<b>状态进入时不置值</b>——生成首帧的初值就是靠这一点保留下来的。
        /// </para>
        /// <para>
        /// <b>相位换算</b>:本字段存的是「本帧招式体读到的那个值」,而原代码的 <c>AIChangeCounter</c>
        /// 存的是「本帧自减<b>之前</b>的值」(原代码每帧先 <c>AIChangeCounter--</c> 再跑招式体)。
        /// 所以从原字段搬过来要减 1:初值 <b>-1</b> 对应原字段的初值 0,
        /// 天顶分身的 210~270 同样减 1(见宿主 <c>SetSpawnCountdown</c>)。
        /// 选招之后的稳态两边逐帧完全一致——原代码在选招那一帧跑的就是新招的体、读到时长值,
        /// 而这里换态由返回值驱动、新招的体下一帧读到时长值,中间没有多出或少掉任何一帧。
        /// </para>
        /// <para>逐帧积分且驱动全部节拍,所以必须过线,口径与 <c>Timer</c> 一致(±2 容差内不动本地值)。</para>
        /// <para>
        /// 覆写基类的同名槽(而不是另声明一个),这样共用层按 <see cref="CEBossStateContext"/>
        /// 读到的就是本 Boss 真正在用的那个值;本 Boss 的相位换算与初值 -1 留在这里说明
        /// </para>
        /// </summary>
        public override int Countdown { get; set; } = -1;

        /// <summary>
        /// 位置锚点一号,对应原代码的 <c>vec1</c>。几乎每个状态都用它当
        /// <c>Vector2.Lerp</c> 的起点,两端取值不同位置会直接跳,所以必须过线
        /// </summary>
        public Vector2 Vec1 { get; set; }

        /// <summary>
        /// 位置锚点二号,对应原代码的 <c>vec2</c>。Subduction / SmashDown / RoundAndDash 的落点,
        /// ShootTriangle 里改当「上一帧位置」用。同样必须过线
        /// </summary>
        public Vector2 Vec2 { get; set; }

        /// <summary>
        /// 通用标量一号(原 <c>num1</c>)。RoundShooting / ShootTriangle 是绕转半径,
        /// Dashing 是锁向起始角,RoundAndDash 是穿场半径
        /// </summary>
        public float Num1 { get; set; }

        /// <summary>
        /// 通用标量二号(原 <c>num2</c>)。RoundShooting / ShootTriangle 是当前绕转角,
        /// Dashing 是锁向目标角,RoundAndDash 是绕场起始角
        /// </summary>
        public float Num2 { get; set; }

        /// <summary>
        /// 通用标量三号(原 <c>num3</c>)。RoundShooting 是绕转方向 ±1,
        /// RoundAndDash 是绕场终止角;AboveMovingShooting 与 ShootTriangle 也会骰它但<b>从不读</b>
        /// </summary>
        public float Num3 { get; set; }
        #endregion

        #region 事实:每帧重算(各端同算,不过线)
        /// <summary>
        /// 难度系数,每帧在状态机之前由宿主重算。七个来源全是世界级已同步量,所以各端同值。
        /// 它直接乘进速度与射速,一旦某端算出不同值位置误差会无界增长——改动它的任何来源都要先确认同步性
        /// </summary>
        public float Enrange { get; set; } = 1f;
        #endregion

        #region 表现:本地推导
        /// <summary>
        /// 上一帧的本体中心(原 <c>oldPos</c>),宿主在每帧末尾写入。
        /// 多个状态用 <c>(Center - OldPos)</c> 算朝向,但那些状态的位置是被硬写的,
        /// 朝向不回头影响航向;<b>唯一让朝向驱动速度的 Dashing 不读它</b>,所以留作本地推导量
        /// </summary>
        public Vector2 OldPos { get; set; }

        /// <summary>
        /// 尾迹采样点(原 <c>odp</c>)。纯绘制,但 RoundShooting 会整条跟着玩家平移、
        /// RoundAndDash / SmashDown 会在起冲那一拍清空重采,所以留在上下文里由状态改写
        /// </summary>
        public List<Vector2> Trail { get; } = new List<Vector2>();

        /// <summary>
        /// 大尾迹强度(原 <c>MegaTrail</c>)。状态在窗口内每帧写满,宿主每帧衰减 0.05。
        /// <b>它同时参与判定</b>:SmashDown 期间 <c>MegaTrail &lt;= 0</c> 就不接触伤害(见宿主 <c>CanHitPlayer</c>),
        /// 但它完全由「状态号 + 倒计时」确定性推导,两者都已过线,所以自身不必过线
        /// </summary>
        public float MegaTrail { get; set; }

        /// <summary>残影窗口(原 <c>AfterImageTime</c>)。状态每帧写满 16,宿主每帧减 1。纯绘制</summary>
        public int AfterImageTime { get; set; }
        #endregion

        /// <summary>
        /// 每帧默认值。本 Boss 没有需要回落的声明通道,见类注释;
        /// 保留覆写是为了把「这里故意是空的」这件事写在代码里,而不是留给下一个人猜
        /// </summary>
        public override void BeginFrameDefaults() {
            base.BeginFrameDefaults();
        }
    }
}
