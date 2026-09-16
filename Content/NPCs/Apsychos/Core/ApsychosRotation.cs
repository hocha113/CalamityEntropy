using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.Core
{
    /// <summary>
    /// 选招:原 <c>Apsychos.SetAIStyle()</c> 的裁决顺序逐条搬过来。
    /// <para>
    /// <b>不引入防复读阀。</b>三张表是作者手排的确定性序列(一处随机都没有),
    /// 加查重窗只会改变出招序列——那是手感改动,不在本次无损迁移的范围内。
    /// </para>
    /// <para>
    /// 三条容易被"顺手修掉"的隐式行为,都必须保留:
    /// <list type="number">
    /// <item>远距离强制接近的那一支<b>不动</b>序号,所以拉开距离期间轮换是冻结的</item>
    /// <item>转阶段那一支把序号<b>置 0</b>(不是自增),所以二阶段第一手固定落在新表的 1 号槽</item>
    /// <item>血量跌破四分之一换表时<b>不重置</b>序号,沿用当前值直接进短表;越界才归零</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class ApsychosRotation
    {
        /// <summary>由注册表创建状态实例(未注册返回 null,框架已打日志)</summary>
        public static IVaultState<ApsychosStateContext> Create(ApsychosStateIndex state)
            => VaultStateRegistry<ApsychosStateContext>.Create((int)state);

        /// <summary>
        /// 选下一手。<b>只该由权威端调用</b>:它带副作用(清标量、推进序号),
        /// 而 <see cref="VaultStateMachine{TContext}"/> 在客户端会照常跑 <c>OnUpdate</c> 却丢弃返回值,
        /// 客户端若也调一遍就会把标量提前清零、序号也跟着乱。门开在
        /// <see cref="ApsychosStateBase.NextAttack"/> 上
        /// </summary>
        public static IVaultState<ApsychosStateContext> Pick(ApsychosStateContext ctx)
        {
            //对齐原代码:三个标量先无条件清零,再做裁决(计时由新状态的 OnEnter 归零)
            ctx.Num1 = 0f;
            ctx.Num2 = 0f;
            ctx.Num3 = 0f;

            NPC npc = ctx.Npc;
            ApsychosStateIndex next;

            if (npc.HasValidTarget && npc.target.ToPlayer().Distance(npc.Center) > ApsychosDirector.ForceApproachDistance)
            {
                //隐式行为一:序号不动
                next = ApsychosStateIndex.MoveToTarget;
            }
            else if (ctx.Phase == 1 && npc.life < npc.lifeMax * ApsychosDirector.Phase2LifeRatio)
            {
                //隐式行为二:置 0 而非自增。转阶段结束后的那次 Pick 会把它推到 1
                next = ApsychosStateIndex.PhaseTrans;
                ctx.AttackIndex = 0;
            }
            else
            {
                //隐式行为三:换表不重置序号,只在越界时归零
                ApsychosStateIndex[] table = ApsychosDirector.TableFor(ctx.Phase, npc);
                ctx.AttackIndex++;
                if (ctx.AttackIndex >= table.Length)
                {
                    ctx.AttackIndex = 0;
                }
                next = table[ctx.AttackIndex];
            }

            return Create(next);
        }
    }
}
