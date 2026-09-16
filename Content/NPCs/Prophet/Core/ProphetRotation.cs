using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.Prophet.Core
{
    /// <summary>
    /// 选招:原 <c>AttackPlayer</c> 里 <c>if (AIChangeDelay &lt;= 0)</c> 那一段的逐条搬运。
    /// <para>
    /// 裁决只有三步:序号自增并在超过 7 时归零、查 <c>GetAIType</c> 得到招式、按招式赋上倒计时。
    /// <b>不引入防复读阀</b>:八个槽位是作者手排的固定顺序(其中三槽当场掷一次硬币),
    /// 加查重窗会改变出招序列,那是手感改动,不在无损迁移范围内。
    /// </para>
    /// <para>
    /// <b>只该由权威端调用</b>:它带副作用(推进序号、掷骰、赋倒计时),
    /// 而 <see cref="VaultStateMachine{TContext}"/> 在客户端会照常跑 <c>OnUpdate</c> 却丢弃返回值。
    /// 客户端若也调一遍,序号与倒计时都会跟服务端分叉。宿主的选招口已经开在 <c>!isClient</c> 上
    /// </para>
    /// </summary>
    public static class ProphetRotation
    {
        /// <summary>由注册表创建状态实例(未注册返回 null,框架已打日志)</summary>
        public static IVaultState<ProphetStateContext> Create(ProphetStateIndex state)
            => VaultStateRegistry<ProphetStateContext>.Create((int)state);

        /// <summary>
        /// 选下一手。原代码在这一段末尾还写了一次 <c>NPC.netUpdate = true</c>,
        /// 现在由状态机写 <c>ai[3]</c> 时的 <c>AiSlotNetSync</c> 自动完成,等价
        /// </summary>
        public static IVaultState<ProphetStateContext> Pick(ProphetStateContext ctx)
        {
            ctx.AttackIndex++;
            if (ctx.AttackIndex > ProphetDirector.AttackIndexMax)
            {
                ctx.AttackIndex = 0;
            }
            ProphetStateIndex next = ProphetDirector.AttackFor(ctx.AttackIndex);
            ctx.Countdown = ProphetDirector.DurationFor(next);
            return Create(next);
        }
    }
}
