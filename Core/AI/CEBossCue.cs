using System;

namespace CalamityEntropy.Core.AI
{
    /// <summary>
    /// 一次性拍门锁:某个<b>过线计数器</b>走到某一拍时要做一件「客户端也必须做」的事
    /// (锁服向、硬刹、清零、本地演出),而带容差的收养可能正好把那一帧跨过去。
    /// <para>
    /// <b>三条语义缺一不可</b>:宽限窗内首次命中要真的触发;越窗才算是真的中途加入,
    /// 静默记账不补演出;任何情况下都不许触发第二次。
    /// 少了第一条,慢半拍的客户端会被硬按「已经过了拍点就算放过了」判掉,
    /// 每个落后一两帧的客户端都听不到演出;少了第三条,收养一回退就重放。
    /// </para>
    /// <para>
    /// <b>只管「各端都要做的那一半」。</b>弹幕、骰点、世界写入本来就锁在权威端,
    /// 权威端的计数器严格单调、从不收养,不在风险线上,那半边照旧写在 <c>IsServer</c> 里。
    /// </para>
    /// <para>
    /// <b>为什么是 class 不是 struct</b>:可变结构体一旦被放进属性而不是字段,
    /// <c>ctx.Cue.TryFire(...)</c> 会在副本上改标志位,编译通过、运行静默失效 ——
    /// 这正是本件要消灭的那类 bug,不能自己再造一个。
    /// </para>
    /// </summary>
    /// <example>
    /// 递增计数 + 一次性拍的标准写法:
    /// <code>
    /// private readonly CEBossCue burstCue = new();
    ///
    /// public override void OnEnter(FooStateContext ctx) {
    ///     base.OnEnter(ctx);
    ///     //状态实例会被复用,进状态必须复位,否则第二次进来这一拍直接哑掉
    ///     burstCue.Reset();
    /// }
    ///
    /// public override IVaultState&lt;FooStateContext&gt; OnUpdate(FooStateContext ctx) {
    ///     ctx.ChangeCounter++;
    ///     if (burstCue.TryFire(ctx.ChangeCounter, FooDirector.BurstFrame)) {
    ///         //各端都要做的那一半
    ///         ctx.Npc.velocity *= 0f;
    ///         CEUtils.PlaySound("VoidBomb", 1.1f, ctx.Npc.Center);
    ///         //权威端独占的那一半照旧自己带门
    ///         if (IsServer) {
    ///             Shoot(ctx, ...);
    ///             MarkNetUpdate(ctx);
    ///         }
    ///     }
    ///     return null;
    /// }
    /// </code>
    /// 倒计时 Boss 把 <c>TryFire</c> 换成 <see cref="TryFireDescending"/>,判据整个反过来,其余一字不改。
    /// </example>
    public sealed class CEBossCue
    {
        /// <summary>
        /// 一次性拍的宽限窗(帧)。越过拍点<b>超过</b>这个窗口才算真的中途加入。
        /// <para>
        /// 取 20 帧的依据:收养容差是 ±2 帧,而快照由玩家命中率驱动、Boss 节流后约每 4~5 帧一包,
        /// 加上两端天然的 ±1 帧相位抖动,落后十来帧仍属正常范围。
        /// 窗口再小就会开始误伤慢客户端,再大就会给真的中途加入者补一串陈年演出
        /// </para>
        /// </summary>
        public const int CatchUpGrace = 20;

        private bool fired;
        private int periodicFired;

        /// <summary>本拍是否已经记过账(无论是真触发还是越窗静默置位)</summary>
        public bool Fired => fired;

        /// <summary>进状态时复位。状态实例会被状态机复用,漏掉这一句等于第二次进来整拍哑掉</summary>
        public void Reset() {
            fired = false;
            periodicFired = 0;
        }

        /// <summary>
        /// <b>递增</b>计数上的一次性拍。返回 true 表示「本地现在就该把这一拍演出来」。
        /// <para>
        /// 三段判据:还没到拍点 → 不记账不触发;落在 <c>[beat, beat + grace]</c> → 记账并触发;
        /// 已经越窗 → 只记账,静默。
        /// </para>
        /// </summary>
        /// <param name="counter">过线的帧计数(状态 <c>Timer</c> 或自己的计数槽)</param>
        /// <param name="beat">拍点帧号</param>
        /// <param name="grace">宽限窗,默认 <see cref="CatchUpGrace"/></param>
        public bool TryFire(float counter, int beat, int grace = CatchUpGrace) {
            if (fired || counter < beat) {
                return false;
            }
            fired = true;
            return counter <= beat + grace;
        }

        /// <summary>
        /// <b>递减</b>计数上的一次性拍(倒计时型 Boss)。判据整个反过来:
        /// 还在拍点上方 → 不记账;落在 <c>[beat - grace, beat]</c> → 记账并触发;掉到窗下 → 只记账。
        /// </summary>
        /// <param name="countdown">过线的倒计时(<see cref="CEBossStateContext.Countdown"/> 或同型的槽)</param>
        /// <param name="beat">拍点帧号</param>
        /// <param name="grace">宽限窗,默认 <see cref="CatchUpGrace"/></param>
        public bool TryFireDescending(float countdown, int beat, int grace = CatchUpGrace) {
            if (fired || countdown > beat) {
                return false;
            }
            fired = true;
            return countdown >= beat - grace;
        }

        /// <summary>
        /// <b>周期</b>拍,对应原生的 <c>counter % N == 0</c>。
        /// <para>
        /// 判据是<b>单调应发次数</b> <c>due = floor(counter / interval) + 1</c> 而不是取模:
        /// 收养把计数往前推时,取模写法会整段跳过被越过的那一拍;往回拉时又会把同一拍重放一遍。
        /// 记下已发到第几次之后,前推最多补发一次(周期拍差一两帧相位无害,少放一发才是可见缺陷),
        /// <b>回退一律不重发</b>。
        /// </para>
        /// <para>
        /// <b>没有递减版。</b>递减周期拍要靠「起点」才能算出单调次数,而起点本身又是一个得过线的量。
        /// 倒计时 Boss 要么把它折成递增量(<c>elapsed = 时长 - 倒计时</c>)再用本方法,
        /// 要么确认这一拍的副作用全在 <c>IsServer</c> 里 —— 权威端从不收养、计数严格单调,
        /// 那种情况下原样的 <c>% N == 0</c> 本来就是安全的
        /// </para>
        /// </summary>
        /// <param name="counter">过线的递增帧计数</param>
        /// <param name="interval">周期(帧)。非正数直接返回 false</param>
        public bool TryFirePeriodic(float counter, int interval) {
            if (interval <= 0) {
                return false;
            }
            int due = (int)Math.Floor(counter / interval) + 1;
            if (due <= periodicFired) {
                return false;
            }
            periodicFired = due;
            return true;
        }

        /// <summary>
        /// 无状态判据:递增计数上「这一拍是不是真的错过了」(而不是本地慢了一两帧)。
        /// 手写闩锁的老代码用它,新代码直接用 <see cref="TryFire"/>
        /// </summary>
        public static bool Passed(float counter, int beat, int grace = CatchUpGrace)
            => counter > beat + grace;

        /// <summary>
        /// 无状态判据的递减版:掉到拍点下方超过宽限窗才算真的错过。
        /// 倒计时 Boss 的手写闩锁用它
        /// </summary>
        public static bool PassedDescending(float countdown, int beat, int grace = CatchUpGrace)
            => countdown < beat - grace;
    }
}
