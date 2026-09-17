using System;

namespace CalamityEntropy.Core.AI
{
    /// <summary>
    /// 过线标量的收养口。共用层<b>只提供「带容差的帧计数收养」这一个方法</b>。
    /// <para>
    /// <b>直取不提供任何辅助方法</b>,这是刻意的:直取就写 <c>Context.X = reader.ReadSingle();</c>,
    /// 让它成为零成本默认;想要容差就必须亲手打出 <c>FrameCounter</c> 这个词,
    /// 打之前先回答「这个槽真的是帧计数吗」。
    /// </para>
    /// <para>
    /// <b>改名的由来</b>:上一代叫 <c>AdoptScalar</c>,名字太中性,听起来适用于任何标量,
    /// 而适用条件只写在一句<b>会连同代码一起被复制</b>的注释里。于是同一轮里出过两次错:
    /// 魂泉把它用在一个整段只涨到 1.66 的插值系数上;Luminaris 用在 <c>Num3</c> 上,
    /// 而那个槽的整段取值集合就是 <c>{-1, +1}</c>,跳度恰好等于容差,等于永远不纠正,
    /// 客户端按反方向绕飞、或卡在固定方位角不动。两处都已改成无容差直取。
    /// 名字本身就该是那道闸,所以这一版把闸焊在方法名上。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>容差收养只对帧计数型量成立</b>:整段取值范围远大于容差、且每帧 ±1 升降。两个方向都会错。
    /// </para>
    /// <para>
    /// <b>一、容差吞掉纠偏。</b>若字段的<b>整段跳度</b>本身 ≤ 容差(0/1/2 三档状态码、±1 方向标志、
    /// 只涨到 1.66 的插值系数、弧度角 —— ±2 rad 就是 114°),容差等于永远不纠正,字段白过线。
    /// 判据是<b>整段跳度</b>而不是「某两帧之间差多少」:两端积分速率相同时,
    /// 锁存那一刻的差值会永久固化,不会自己收敛。
    /// </para>
    /// <para>
    /// <b>二、无容差直取撞拍点。</b>若字段确实是帧计数,且代码里对它有 <c>== N</c> / <c>% N == 0</c>
    /// 型判定,直取会让客户端每收一包就跳一下,把那一拍跳过或重放。
    /// </para>
    /// <para><b>落地前逐条过:</b></para>
    /// <para>
    /// (1) 去 <c>States/</c> 找全部读写点,写下实测范围与步长。结论必须有行号依据,不许凭字段名猜。
    /// </para>
    /// <para>
    /// (2) 跳度 ≤ 容差 → 直取;跳度 ≫ 容差且步长 ±1 → 容差;落在 2~10 的尴尬区间 → 停下来问人。
    /// </para>
    /// <para>
    /// (3) 检查等值判定落在哪一侧。只在权威端产生副作用的(弹幕、骰点、世界写入)不在风险线上:
    /// <c>MessageBuffer.GetData</c> 的 <c>case 23</c> 开头就 <c>if (Main.netMode != 1) break;</c>,
    /// 权威端从不收养、严格单调。客户端也要执行的那一半(运动数学、锁存服向、清零、本地演出)
    /// 才要靠容差保拍。
    /// </para>
    /// <para>
    /// (4) 单独验「权威端清零」能不能越过容差。若清零只在权威端做,客户端的重置全靠 <c>synced = 0</c>
    /// 那一包;要是字段退出时的残值本身 ≤ 容差,重置会被连带吞掉,残值泄漏进下一手。
    /// </para>
    /// <para>
    /// (5) <b>一个槽只准一种语义。</b>通用槽若在 A 状态是帧计时、在 B 状态是锁存角,
    /// 单一收养口无解,拆成两个过线字段。
    /// </para>
    /// </remarks>
    public static class CEBossNetAdopt
    {
        /// <summary>
        /// 收养一个<b>帧计数型</b>整数字段:差值在容差内就不动本地值。
        /// 适用条件见类注释的五条守则,用之前必须逐条过一遍
        /// </summary>
        /// <param name="local">本地当前值</param>
        /// <param name="synced">包里的权威端值</param>
        /// <param name="tolerance">容差,默认与 <see cref="CEBossNetMotion.TimerTolerance"/> 同口径</param>
        public static int AdoptFrameCounter(int local, int synced, int tolerance = CEBossNetMotion.TimerTolerance)
            => Math.Abs(synced - local) > tolerance ? synced : local;

        /// <summary>
        /// 浮点槽里装的<b>帧计数</b>的收养。口径与整数版完全一致。
        /// <para>
        /// 浮点槽格外危险:通用 <c>num</c> 槽经常被不同状态复用成角度、比例、包络值,
        /// 那些都不是帧计数。守则第 (5) 条对浮点槽是硬约束
        /// </para>
        /// </summary>
        public static float AdoptFrameCounter(float local, float synced, float tolerance = CEBossNetMotion.TimerTolerance)
            => Math.Abs(synced - local) > tolerance ? synced : local;
    }
}
