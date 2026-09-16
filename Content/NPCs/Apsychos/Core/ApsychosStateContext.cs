using CalamityEntropy.Core.AI;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.Core
{
    /// <summary>尾巴骨架的三种驱动方式。持久量,<b>不</b>每帧回落——转阶段状态不声明它,沿用上一手的档位</summary>
    public enum ApsychosTailStyle
    {
        /// <summary>逐节跟随本体(巡航默认)</summary>
        Follow,
        /// <summary>三点贝塞尔:本体、后方控制点、尾尖</summary>
        OnePoint,
        /// <summary>四点贝塞尔:本体、后方控制点、尾尖后方控制点、尾尖</summary>
        TwoPoint,
    }

    /// <summary>
    /// Apsychos 状态上下文。
    /// <para>
    /// 「事实」区是参与判定或需要两端一致的量,随 <c>SendExtraAI</c> 过线;
    /// 「声明」区每帧由 <see cref="BeginFrameDefaults"/> 回落;
    /// 「表现」区是纯本地推导的绘制量,不过线(状态号与计时已过线,它们自然收敛)。
    /// </para>
    /// </summary>
    public class ApsychosStateContext : CEBossStateContext
    {
        #region 核心引用
        public Apsychos Owner { get; set; }

        /// <summary>尾尖实体。可能为 null(生成失败或已死),状态里读之前宿主已保证非空</summary>
        public NPC Tail => Owner?.tail;
        #endregion

        #region 事实:过线
        /// <summary>
        /// 通用标量一号。各状态含义不同:Dash 是喷口计数、FireballShooting 是齐射数、
        /// FireballBig 是蓄力帧、TailDash 是本次鞭击的帧计时。对应原代码的 <c>num1</c>
        /// </summary>
        public float Num1 { get; set; }

        /// <summary>
        /// 通用标量二号:FireballShooting 是齐射间隔倒计时、FireballBig 是已发射数、
        /// TailDash 是尾巴伸出的速度累加器。对应原代码的 <c>num2</c>
        /// </summary>
        public float Num2 { get; set; }

        /// <summary>
        /// 通用标量三号:FireballShooting 是收尾延迟、TailDash 是尾巴相对本体的伸出距离。
        /// 对应原代码的 <c>num3</c>
        /// </summary>
        public float Num3 { get; set; }

        /// <summary>
        /// 甩尾已完成的鞭击次数。原代码借用 <c>NPC.ai[2]</c> 存它,而 ai[2] 现在归阶段用,
        /// 所以挪成独立字段随包过线。
        /// <b>注意它不由收招清零</b>——原代码的 <c>SetAIStyle</c> 也不清,是 TailDash 自己在收招前清的
        /// </summary>
        public int TailDashReps { get; set; }
        #endregion

        #region 事实:每帧重算(各端同算,不过线)
        /// <summary>
        /// 难度系数,每帧在状态机之前由宿主重算。七个来源全是世界级已同步量,所以各端同值。
        /// 它直接乘进速度,一旦某端算出不同值,位置误差会无界增长——改动它的任何来源都要先确认同步性
        /// </summary>
        public float Enrange { get; set; } = 1f;

        /// <summary>目标距离,每帧重算</summary>
        public float TargetDistance { get; set; }
        #endregion

        #region 声明:每帧回落
        /// <summary>尾巴骨架档位。<b>持久量</b>,不回落(转阶段不声明它,沿用上一手)</summary>
        public ApsychosTailStyle TailStyle { get; set; } = ApsychosTailStyle.Follow;

        /// <summary>本帧是否让描边自然衰减。对应原 <c>OutlineFlag</c>,只有 Dash 关掉它自管</summary>
        public bool DecayOutline { get; set; } = true;

        /// <summary>本帧是否让尾巴速度自然衰减。对应原 <c>TailSpeedMultFlag</c>,只有 FireballShooting 关掉</summary>
        public bool DecayTailSpeed { get; set; } = true;

        /// <summary>本帧是否让尾焰亮度自然衰减。对应原 <c>TailLightFlag</c>,原代码里没有任何状态关掉它</summary>
        public bool DecayTailLight { get; set; } = true;

        /// <summary>
        /// 本帧是否让白化强度自然衰减。原代码把它写成挂在 PhaseTrans 上的 <c>else</c>
        /// (L499),效果就是「除转阶段外每个状态每帧都衰减」,所以只有 PhaseTrans 关掉它
        /// </summary>
        public bool DecayHighLight { get; set; } = true;
        #endregion

        #region 表现:纯本地
        /// <summary>冲刺预告描边 0~1</summary>
        public float Outline { get; set; }

        /// <summary>尾焰亮度,炮口发光与光带都读它</summary>
        public float TailLight { get; set; }

        /// <summary>
        /// 白化强度,喂给 WhiteTrans 着色器。<b>初值 1</b>:原代码字段初值就是 1,
        /// 所以刚生成时有一次白闪,随后按 0.94 衰减掉
        /// </summary>
        public float HighLight { get; set; } = 1f;

        /// <summary>
        /// 转阶段配色插值。原代码只写不读(贴图切换看的是 <see cref="CEBossStateContext.Phase"/>),
        /// 照搬保留以免哪天绘制要用
        /// </summary>
        public float P2Lerp { get; set; }
        #endregion

        /// <summary>
        /// 每帧默认值。只回落四个衰减开关——<see cref="TailStyle"/> 是持久量,
        /// 三个表现累加量由宿主在状态机之后结算,都不在这里动
        /// </summary>
        public override void BeginFrameDefaults()
        {
            base.BeginFrameDefaults();
            DecayOutline = true;
            DecayTailSpeed = true;
            DecayTailLight = true;
            DecayHighLight = true;
        }
    }
}
