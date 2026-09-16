using CalamityEntropy.Core.AI;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.Core
{
    /// <summary>
    /// 冥魂泉状态上下文。
    /// <para>
    /// 「事实」区是参与判定或需要两端一致的量,随 <c>SendExtraAI</c> 过线;
    /// 「声明」区每帧由 <see cref="BeginFrameDefaults"/> 回落;
    /// 「表现」区是纯本地推导的绘制量,不过线。
    /// </para>
    /// <para>
    /// 两根柱子(<see cref="SpiritFountain.column1"/> / <see cref="SpiritFountain.column2"/>)
    /// 仍挂在宿主上:它们是魂环读取的对外可达字段,挪走会连带改掉部件的访问路径。
    /// 柱子里参与判定的四个量(offset / rotation / alpha / Num)由宿主写进 ExtraAI。
    /// </para>
    /// </summary>
    public class SpiritFountainStateContext : CEBossStateContext
    {
        #region 核心引用
        /// <summary>宿主。状态要读柱子、要调 <c>Shoot</c>,都从这里走</summary>
        public SpiritFountain Owner { get; set; }
        #endregion

        #region 事实:过线
        /// <summary>
        /// 全局帧计数,对应原 <c>Counter</c>。<b>永不归零</b>,状态换来换去都照涨,
        /// 三个阶段的射速节拍与魂环的驻点摆动都拿它取模
        /// </summary>
        public float GlobalCounter { get; set; }

        /// <summary>
        /// 回旋段的进度量,对应原 <c>num1</c>。魂环按自己的 Index 和它比大小决定什么时候脱柱,
        /// 所以它是<b>部件也要读的判定量</b>,必须过线
        /// </summary>
        public float Num1 { get; set; }

        /// <summary>横扫段的摇摆相位,对应原 <c>mCounter</c>。逐帧积分出来又直接决定柱子横坐标,典型的持久累加量</summary>
        public float MCounter { get; set; }

        /// <summary>横扫段的摇摆幅度,对应原 <c>mAmp</c>。同上,从 0 慢慢涨到 1</summary>
        public float MAmp { get; set; }

        /// <summary>
        /// 聚魂倒计时,对应原 <c>GatheringAnimation</c>。它锁着出场演出的硬时序,
        /// 中途加入的客户端要靠它对上演出进度
        /// </summary>
        public int GatheringAnimation { get; set; } = SpiritFountainDirector.GatheringFrames;

        /// <summary>一号柱魂环的一次性生成闸(出场演出结束时用掉)</summary>
        public bool SpawnSpirits { get; set; } = true;

        /// <summary>二号柱魂环的一次性生成闸(血量跌破 66% 时用掉)</summary>
        public bool SpawnSpirits2 { get; set; } = true;
        #endregion

        #region 事实:每帧重算(各端同算,不过线)
        /// <summary>
        /// 难度系数,每帧在状态机之前由宿主重算。六个来源全是世界级已同步量,所以各端同值。
        /// 它除进射速间隔又乘进摇摆推进,改动它的任何来源都要先确认同步性
        /// </summary>
        public float Enrage { get; set; } = 1f;
        #endregion

        #region 事实:跨帧闸(由已过线的状态号确定性推导)
        /// <summary>
        /// 魂环的免伤开关,对应原 <c>DontTakeDmg</c>。只有 PhaseTranse1 会把它打开,
        /// 等价于「当前状态是不是转阶段演出」,而状态号本身走 ai[3],所以不必单独过线
        /// </summary>
        public bool DontTakeDmg { get; set; }

        /// <summary>落点只写一次的闸,对应原 <c>SetPos</c>。各端各自落一次,随后由原版位置同步对账</summary>
        public bool SetPos { get; set; }
        #endregion

        #region 声明:每帧回落
        /// <summary>
        /// 本帧眼睛透明度的目标值,对应原 <c>EyeAlphaT</c>。默认 0.6,
        /// 出场演出改成跟随自身、三阶段横扫与十字斩顶到 1
        /// </summary>
        public float EyeAlphaTarget { get; set; } = SpiritFountainDirector.EyeAlphaIdle;

        /// <summary>
        /// 本帧瞳孔是否直接钉在本地玩家身上。对应原代码挂在「当前不是出场演出」上的那条 else,
        /// 所以只有出场演出会关掉它(它自己用带插值的追踪)
        /// </summary>
        public bool StareAtLocalPlayer { get; set; } = true;

        /// <summary>
        /// 本帧是否保留横扫摇摆量。对应原代码挂在 Moving 块上的 <c>else { mCounter = 0; mAmp = 0; }</c>,
        /// 所以只有 Moving 会打开它,其余状态一律把摇摆清零
        /// </summary>
        public bool KeepMovingSway { get; set; }

        /// <summary>
        /// 本帧提前收工:对应原代码聚魂期间那个 <c>return</c>。它同时吃掉了尾声的脱战判定、
        /// 眼睛插值与摇摆清零,少一样都会让出场演出的观感对不上
        /// </summary>
        public bool HaltFrame { get; set; }
        #endregion

        #region 表现:纯本地
        /// <summary>眼睛当前透明度,对应原 <c>EyeAlpha</c>。只读于绘制,收敛型插值,不过线</summary>
        public float EyeAlpha { get; set; }

        /// <summary>喷流贴图滚动速度,对应原 <c>FountainSpeed</c>。只喂 <c>trailDrawOffset</c>,不过线</summary>
        public float FountainSpeed { get; set; } = 20f;

        /// <summary>瞳孔注视点,对应原 <c>starePoint</c>。读的是 <c>Main.LocalPlayer</c>,天然是本地量</summary>
        public Vector2 StarePoint { get; set; }

        /// <summary>上一帧的一号柱横坐标,对应原 <c>c1LastPos</c>。同帧内写完即读,用来求柱子的倾斜角</summary>
        public float C1LastPos { get; set; }

        /// <summary>原 <c>CenterRing</c>。原代码写它但从不读,照搬保留</summary>
        public int CenterRing { get; set; }
        #endregion

        /// <summary>
        /// 每帧默认值。四个声明通道全部回落;<see cref="DontTakeDmg"/> 与 <see cref="SetPos"/> 是跨帧闸,
        /// 三个表现累加量由状态自己推进,都不在这里动
        /// </summary>
        public override void BeginFrameDefaults()
        {
            base.BeginFrameDefaults();
            EyeAlphaTarget = SpiritFountainDirector.EyeAlphaIdle;
            StareAtLocalPlayer = true;
            KeepMovingSway = false;
            HaltFrame = false;
        }
    }
}
