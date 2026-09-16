using InnoVault;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Core.AI
{
    /// <summary>
    /// Boss 宿主侧的联机装配件:两处计时收养点、慢频心跳、锚定部件通路。
    /// <para>
    /// <b>线格式的铁律</b>:每只 Boss 的 <c>SendExtraAI</c> / <c>ReceiveExtraAI</c> 必须是
    /// <b>定长</b>块,且读写顺序写在同一处、肉眼可对照。不许出现运行时条件决定写不写某个字段——
    /// 两端字节数不等会把整条共享读取流错位,后面所有订阅者一起读到垃圾。
    /// </para>
    /// <para>
    /// <b>持久累加量必须过线,不能只过计时。</b><see cref="VaultState{TContext}.Timer"/>
    /// 每帧被状态重新声明,所以它自愈;而被逐帧积分出来、又反过来扰动航向的量
    /// (朝向、摆动相位、速度累加器)一旦分叉就永远收不回来,位置误差按它的二重积分增长。
    /// 顺序约定:先 <see cref="CEBossNetMotion.WriteTiming"/>,紧接着写本 Boss 自己的累加量块。
    /// </para>
    /// </summary>
    public static class CEBossHost
    {
        /// <summary>
        /// 客户端 AI 开头:同态包带来的计时收养。
        /// 与 <see cref="HookStateSwapAdoption"/> 两处缺一不可——这一处管「包和本地处在同一状态」,
        /// 那一处管「包本身就是换态包」(新实例的 OnEnter 刚把计时清零)
        /// </summary>
        public static void AdoptTimingAtFrameStart<TCtx>(CEBossNetMotion motion, VaultStateMachine<TCtx> machine)
            where TCtx : CEBossStateContext {
            if (!VaultUtils.isClient || motion == null) {
                return;
            }
            if (machine?.CurrentState is ICEBossNetTiming timed
                && motion.TryTakeTiming(timed.StateId, out int timer, out int counter)) {
                timed.AdoptNetTiming(timer, counter);
            }
        }

        /// <summary>
        /// 装配换态后的收养:挂在 <see cref="VaultStateMachine{TContext}.OnStateChanged"/> 上。
        /// 权威端换态的 netUpdate 由框架在同一服务端帧的末尾发出,发包时机在宿主写完计时<b>之后</b>,
        /// 所以让客户端换态的那一包携带的正是新状态的计时
        /// </summary>
        public static void HookStateSwapAdoption<TCtx>(CEBossNetMotion motion, VaultStateMachine<TCtx> machine)
            where TCtx : CEBossStateContext {
            if (motion == null || machine == null) {
                return;
            }
            machine.OnStateChanged += (_, next, _) => {
                if (VaultUtils.isClient && next is ICEBossNetTiming timed
                    && motion.TryTakeTiming(timed.StateId, out int timer, out int counter)) {
                    timed.AdoptNetTiming(timer, counter);
                }
            };
        }

        /// <summary>
        /// 权威端的慢频兜底心跳。真实快照频率由玩家命中率决定(服务端每收到一次打击就 netUpdate,
        /// Boss 节流后约每 4~5 帧一包),所以这里只是空闲期的对账网,决策点各自 netUpdate。
        /// <para>
        /// <b>放长心跳的前提是客户端确实在预测。</b>对一个把运动写在 <c>!isClient</c> 里的被动客户端,
        /// 包间隔从 N 帧拉到 45 帧会让每包误差按 <c>a·N²/2</c> 涨一个量级——那不是修复,是放大器
        /// </para>
        /// </summary>
        public static void Heartbeat(NPC npc) {
            if (npc != null && !VaultUtils.isClient && Main.GameUpdateCount % CEBossNetMotion.HeartbeatFrames == 0) {
                npc.netUpdate = true;
            }
        }

        /// <summary>
        /// 锚定部件的一帧(手、臂、焊死的头、贴锚点的尾巴)。
        /// <para>
        /// 这类部件靠 <c>Center = Lerp(Center, anchor, k)</c> 向锚点收敛,下一帧位置<b>不是</b>
        /// <c>position + velocity</c>,所以 <see cref="CEBossNetMotion"/> 的预测纠偏会和它打架:
        /// 只清原版平滑、不进预测器。它们本身是自愈的——任何偏移都会被收敛率拉回去。
        /// </para>
        /// <para>本体走两段式的 <c>BeginFrame</c> / <c>EndFrame</c>,锚定部件<b>绝不</b>调 <c>EndFrame</c>。</para>
        /// </summary>
        public static void RunAnchoredPartFrame(NPC npc) {
            if (npc != null) {
                CEBossNetMotion.ClearSmoothing(npc);
            }
        }
    }
}
