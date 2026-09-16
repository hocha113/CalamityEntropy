using InnoVault;
using InnoVault.StateMachines;

namespace CalamityEntropy.Core.AI
{
    /// <summary>
    /// Boss 状态基类:桥接 <see cref="VaultState{TContext}"/> 的双参签名,收公共小件。
    /// <para>
    /// 计时约定:<see cref="VaultState{TContext}.Timer"/> 是本状态内的帧计时,
    /// <see cref="VaultState{TContext}.Counter"/> 是状态总龄(超时兜底用),二者随快照过线。
    /// </para>
    /// <para>
    /// <b>自增时序是硬契约</b>:<c>Timer++</c> 放在子类 <see cref="OnUpdate(TCtx)"/> <b>之后</b>。
    /// 这样才能容纳「状态体内部先自增若干次、再拿自增后的值判阈值」这类原生写法
    /// (Apsychos 的 MoveToTarget 每帧自增 2~3 次,阈值判定夹在中间)。
    /// 若把自增提到前面,所有这类状态的节拍都会整体偏移一帧。
    /// </para>
    /// </summary>
    /// <typeparam name="TCtx">本 Boss 的上下文类型</typeparam>
    public abstract class CEBossStateBase<TCtx> : VaultState<TCtx>, ICEBossNetTiming
        where TCtx : CEBossStateContext
    {
        /// <summary>
        /// 一次性拍的宽限窗(帧)。派生的一次性标志只在越过拍点<b>超过</b>这个窗口时才静默置位
        /// (那是真的中途加入);窗口之内让本地拍照常触发。
        /// <para>
        /// 少了这一条,慢半拍的客户端会在收养计时之后把最大的那一拍直接吞掉——
        /// 硬按「已经过了拍点就算放过了」判定,等于让每个落后一帧的客户端都听不到演出。
        /// </para>
        /// </summary>
        public const int CueCatchUpGrace = 20;

        /// <summary>
        /// 状态总龄上限(帧),超过即走 <see cref="OnTimeout"/> 强制收招。
        /// 默认不限;<b>每个实战状态都该给一个值</b>,否则状态机可能死在这里、Boss 靠惯性飘走
        /// </summary>
        public virtual int TimeoutFrames => int.MaxValue;

        /// <summary>
        /// 没目标时不跑状态体(脱战由宿主接管运动)。演出态若必须在无目标时继续,覆写为 false
        /// </summary>
        public virtual bool RequiresTarget => true;

        /// <summary>进入状态:计时归零。覆写时先调 <c>base.OnEnter(ctx)</c></summary>
        public virtual void OnEnter(TCtx ctx) {
            Timer = 0;
            Counter = 0;
        }

        /// <summary>状态主体。返回非 null 请求换态,返回 null 留在本状态</summary>
        public abstract IVaultState<TCtx> OnUpdate(TCtx ctx);

        /// <summary>离开状态</summary>
        public virtual void OnExit(TCtx ctx) {
        }

        /// <summary>超时兜底的去处。各 Boss 的状态基类覆写它,指回自己的选招口</summary>
        protected virtual IVaultState<TCtx> OnTimeout(TCtx ctx) => null;

        public sealed override void OnEnter(VaultStateMachine<TCtx> machine, TCtx ctx) {
            OnEnter(ctx);
        }

        public sealed override IVaultState<TCtx> OnUpdate(VaultStateMachine<TCtx> machine, TCtx ctx) {
            Counter++;
            IVaultState<TCtx> next = null;
            if (!RequiresTarget || ctx.TargetValid) {
                next = OnUpdate(ctx);
            }
            if (next == null && Counter > TimeoutFrames) {
                next = OnTimeout(ctx);
            }
            //对齐原生「状态体跑完之后统一自增一次」的时序,见类注释
            Timer++;
            return next;
        }

        public sealed override void OnExit(VaultStateMachine<TCtx> machine, TCtx ctx) {
            OnExit(ctx);
        }

        /// <summary>
        /// 收养权威端随快照过线的状态计时(客户端)。接口成员必须是 public,不能收成 internal。
        /// 容差内不动本地值:只差一两帧是网络抖动的常态,硬对齐会让 <c>Timer == N</c> 型一次性拍被跳过或重放
        /// </summary>
        public void AdoptNetTiming(int timer, int counter) {
            Timer = CEBossNetMotion.AdoptTimer(Timer, timer);
            Counter = counter;
        }

        /// <summary>
        /// 某个拍点是否已经真的错过(而不是本地慢了一两帧)。
        /// 用法:一次性标志在收养计时后靠它静默置位,<c>if (CuePassed(BurstFrame)) burstFired = true;</c>
        /// </summary>
        protected bool CuePassed(int beat) => Timer > beat + CueCatchUpGrace;

        /// <summary>同上,但判据是别的已过线的帧计数(例如状态自己的 num 槽)</summary>
        protected static bool CuePassed(float counterValue, int beat) => counterValue > beat + CueCatchUpGrace;

        /// <summary>权威端(服务端或单机)。骰点、生成、世界写入只在这里做;运动数学各端都要跑</summary>
        protected static bool IsServer => !VaultUtils.isClient;

        /// <summary>目标预测点</summary>
        protected static Vector2 PredictTarget(TCtx ctx, float leadFrames)
            => ctx.Target.Center + ctx.Target.velocity * leadFrames;

        /// <summary>决策点同步(权威端)。换态本身由 AiSlotNetSync 自带,这里用于出手锁向之类的额外决策</summary>
        protected static void MarkNetUpdate(TCtx ctx) {
            if (IsServer && ctx.Npc != null) {
                ctx.Npc.netUpdate = true;
            }
        }

        /// <summary>
        /// 宿主在脱战等「计时必须归零」的场合调用。两端都要调:
        /// 原代码脱战期间每帧把攻击计时清零,重新接战时从 0 起跑
        /// </summary>
        public void ResetTiming() {
            Timer = 0;
            Counter = 0;
        }
    }
}
