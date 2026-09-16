using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.Core
{
    /// <summary>
    /// 选招:原 <c>Luminaris.SetAISyyle()</c>(方法名的拼写错误在原代码里)连同它前后的
    /// 清理、序号推进、时长赋值,一并搬到这一处。
    /// <para>
    /// <b>不引入防复读阀。</b>两张表都是作者手排的确定性序列(一处随机都没有),
    /// 加查重窗只会改变出招序列,那是手感改动,不在无损迁移的范围内。
    /// </para>
    /// <para>
    /// 三条容易被「顺手修掉」的隐式行为,都必须保留:
    /// <list type="number">
    /// <item>选招读的是<b>自增前</b>的序号,自增在选完之后才做,所以开局第一手固定是 0 号槽</item>
    /// <item>序号<b>先</b>参与选招、再判越界归零,所以 6 号槽正常出招、下一轮才回到 0</item>
    /// <item>转二阶段<b>不重置</b>序号,跌破半血那一刻沿用当前序号直接进二阶段表</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class LuminarisRotation
    {
        /// <summary>由注册表创建状态实例(未注册返回 null,框架已打日志)</summary>
        public static IVaultState<LuminarisStateContext> Create(LuminarisStateIndex state)
            => VaultStateRegistry<LuminarisStateContext>.Create((int)state);

        /// <summary>
        /// 选下一手。<b>只该由权威端调用</b>:它带副作用(清锚点与标量、推进序号、重置倒计时、发包),
        /// 而 <see cref="VaultStateMachine{TContext}"/> 在客户端会照常跑 <c>OnUpdate</c> 却丢弃返回值。
        /// 门开在 <see cref="LuminarisStateBase.NextAttack"/> 上
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="current">
        /// 当前状态。二阶段那一串 <c>if (AIRound == n)</c> 在序号落到表外时<b>不给 ai 赋值</b>,
        /// 效果是沿用上一手;那条路径在现有序号范围里到不了,但形状照搬,所以要把当前值传进来
        /// </param>
        public static IVaultState<LuminarisStateContext> Pick(LuminarisStateContext ctx, LuminarisStateIndex current) {
            //对齐原代码的清理顺序:两个锚点与三个标量先无条件清零,再裁决
            ctx.Vec1 = Vector2.Zero;
            ctx.Vec2 = Vector2.Zero;
            ctx.Num1 = 0f;
            ctx.Num2 = 0f;
            ctx.Num3 = 0f;

            LuminarisStateIndex next = SetAISyyle(ctx, current);
            //原代码 SetAISyyle() 返回后紧跟的 AIRound++
            ctx.AttackIndex++;
            ctx.Countdown = LuminarisDirector.DurationOf(next);
            if (ctx.Npc != null) {
                //原 pick 块末尾的 netUpdate。决策点:换招
                ctx.Npc.netUpdate = true;
            }
            return Create(next);
        }

        /// <summary>
        /// 原 <c>SetAISyyle()</c> 的本体(拼写错误保留在注释里,类型名按仓库命名规范改成了状态索引)。
        /// <para>
        /// 一阶段是直接把序号当枚举值强转,所以顺序就是 <see cref="LuminarisStateIndex"/> 的 0~6;
        /// 二阶段是一串独立的 <c>if</c> 重新映射同样的 0~6 槽位。两个分支末尾各有一份
        /// 「序号 ≥ 6 就置 -1」的越界处理,合并成这里的一句(等价:两条路径都会执行到)
        /// </para>
        /// </summary>
        private static LuminarisStateIndex SetAISyyle(LuminarisStateContext ctx, LuminarisStateIndex current) {
            int round = ctx.AttackIndex;
            LuminarisStateIndex next = current;

            if (ctx.Phase == 1) {
                //原 `ai = (AIStyle)AIRound;`——一阶段的出招序列就是枚举的前七项
                next = (LuminarisStateIndex)round;
            }
            else {
                if (round == 0) {
                    next = LuminarisStateIndex.Shoot360;
                }
                if (round == 1) {
                    next = LuminarisStateIndex.SmashDown;
                }
                if (round == 2) {
                    next = LuminarisStateIndex.RoundAndDash;
                }
                if (round == 3) {
                    next = LuminarisStateIndex.Subduction;
                }
                if (round == 4) {
                    next = LuminarisStateIndex.ShootTriangle;
                }
                if (round == 5) {
                    next = LuminarisStateIndex.AstralSpike;
                }
                if (round == 6) {
                    next = LuminarisStateIndex.Dashing;
                }
            }

            //越界处理:置 -1 而非 0,因为紧接着还有一次自增
            if (round >= 6) {
                ctx.AttackIndex = -1;
            }
            return next;
        }
    }
}
