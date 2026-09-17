using InnoVault;
using InnoVault.StateMachines;
using Terraria;
using Terraria.Utilities;

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
        /// <para>
        /// <b>还有第三类,本件没有为它提供通路。</b>「挂架时被宿主直写位置、脱架后自己按速度飞」的部件
        /// (卫城机器的鱼叉是第一例)在一条命里会横跨两种模型:挂架段属锚定型,脱架段属本体型。
        /// 这类部件不能整条命固定走一条通路 —— 挂架段跑预测器会被直写位置打架,
        /// 脱架段只清平滑又等于放弃纠偏。正确做法是按当前段切换:挂架段调本方法,
        /// 脱架段改走 <c>BeginFrame</c> / <c>EndFrame</c>,并在<b>切换的那一帧</b>调
        /// <see cref="PlaceEntity"/> 或 <see cref="CEBossNetMotion.ForgetPrediction"/> 作废旧预测。
        /// 切换点本身是决策,必须过线
        /// </para>
        /// </summary>
        public static void RunAnchoredPartFrame(NPC npc) {
            if (npc != null) {
                CEBossNetMotion.ClearSmoothing(npc);
            }
        }

        /// <summary>
        /// 直写某个实体的位置(瞬移、闪现定位、对撞对齐、焊接到锚点),顺手作废它自己的预测器。
        /// <para>
        /// 少了作废这一步,下一包会把「直写造成的位移」当成失步:纠偏器拿上一帧按
        /// <c>position + velocity</c> 推出来的预测位置去比包里的位置,差值正好是整段瞬移距离,
        /// 于是要么被判成瞬移硬拽、要么被当作误差慢慢消化,两种都会在贴图上留下一段拖影。
        /// </para>
        /// <para>
        /// <b>预测器必须是被写实体自己的那一个。</b>宿主直写部件位置时,要作废的是<b>部件</b>的
        /// <see cref="CEBossNetMotion"/>,不是宿主的;两只 Boss 各自实现的
        /// <c>PlaceCell</c> / <c>TeleportBody</c> / <c>TeleportTo</c> 都是这个形状,
        /// 部件侧为此对外开一个转发方法即可。
        /// </para>
        /// </summary>
        /// <param name="npc">被直写位置的实体</param>
        /// <param name="center">新的中心点</param>
        /// <param name="motion">该实体<b>自己</b>的纠偏器</param>
        public static void PlaceEntity(NPC npc, Vector2 center, CEBossNetMotion motion) {
            if (npc == null) {
                return;
            }
            npc.Center = center;
            motion?.ForgetPrediction();
        }

        /// <summary>
        /// 按实体身份播种的确定性随机。<b>凡是「位置累加吃随机」的地方都必须用它</b>,
        /// 不能用 <c>Main.rand</c>。
        /// <para>
        /// <c>Main.rand</c> 在每端各滚各的:同一帧同一个搜索循环,两端摇出不同的落点,
        /// 而落点又被累加进位置,误差从此不再收敛 —— 这类偏差纠偏器修不了,
        /// 它只会把两个都「正确」的积分结果来回硬拽。
        /// </para>
        /// <para>
        /// <b>三个种子源必须全都是两端一致的量</b>:<c>whoAmI</c> 由服务端分配并随生成包过线;
        /// <paramref name="partIndex"/> 是编队里的固定序号;<paramref name="sequence"/> 是
        /// 「这是第几次摇」的计数,它<b>本身也得过线</b>,否则中途加入的客户端会从别的序号开始摇。
        /// 卫城机器的腿部落点搜索是第一例(<c>StepSeed</c> 就是那个 sequence,在腿的定长块里过线)。
        /// </para>
        /// </summary>
        /// <param name="npc">身份来源,取 <c>whoAmI</c></param>
        /// <param name="partIndex">编队序号,单体传 0</param>
        /// <param name="sequence">第几次摇,必须过线</param>
        public static UnifiedRandom SeededRandom(NPC npc, int partIndex, int sequence)
            => new UnifiedRandom((npc == null ? 0 : npc.whoAmI * 7919) + partIndex * 131 + sequence);
    }
}
