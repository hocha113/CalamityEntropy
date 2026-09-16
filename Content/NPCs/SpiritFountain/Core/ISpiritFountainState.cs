using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.Core
{
    /// <summary>
    /// 状态索引,写入 <c>npc.ai[3]</c> 网络同步。
    /// <b>序号与重构前的 <c>SpiritFountain.AIStyle</c> 逐个对齐</b>,方便对照旧代码与旧日志。
    /// 原枚举只被 <see cref="SpiritRing"/> 引用过(全仓 grep 确认),迁移时整体换成本枚举
    /// </summary>
    public enum SpiritFountainStateIndex
    {
        /// <summary>出场演出:300 帧聚魂 → 显形 → 放出一号柱魂环</summary>
        SpawnAnimation = 0,
        /// <summary>横扫:一号柱左右大幅摆动,按阶段叠加三套弹幕</summary>
        Moving = 1,
        /// <summary>回旋:柱子收回中线,魂环按 Index 依次脱柱扑人</summary>
        Boomerang = 2,
        /// <summary>激光:本体只收柱子,火力全在魂环的扫射上</summary>
        Lasers = 3,
        /// <summary>落环喷泉:本体只收柱子,魂环落地聚成冲击波</summary>
        RingFountains = 4,
        /// <summary>转阶段演出:二号柱亮起,全场免伤 71 帧</summary>
        PhaseTranse1 = 5,
        /// <summary>十字斩:双柱交替横扫竖扫,终局循环态,不再退出</summary>
        SpiritSlicing = 6,
    }

    /// <summary>
    /// 冥魂泉状态基类。收三样公共小件:线性推进(原代码的顺序 if 链)、出手(伤害折算)、
    /// 以及部件要读的「状态体本帧看到的计时」。
    /// <para>
    /// 拍子一律用 <see cref="VaultState{TContext}.Timer"/> 的<b>区间判断</b>表达,
    /// 不引入会归零 Timer 的 beat 枚举:原代码每个状态的进度都是 <c>aiTimer</c> 这一个标量的纯函数,
    /// 加一个锁存的拍号就等于多一个必须过线的量。
    /// </para>
    /// <para>
    /// <b>不设目标门槛</b>:原 AI() 从头到尾没有任何「没目标就不跑」的判断,
    /// 出场演出与十字斩都必须在无目标时照常推进,所以 <see cref="RequiresTarget"/> 整族关掉
    /// </para>
    /// </summary>
    public abstract class SpiritFountainStateBase : CEBossStateBase<SpiritFountainStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract SpiritFountainStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();

        /// <summary>原代码整条 AI 没有目标门槛,照搬</summary>
        public override bool RequiresTarget => false;

        /// <summary>原代码没有超时兜底,这里统一挂一个到不了的安全网,见 Director 的说明</summary>
        public override int TimeoutFrames => SpiritFountainDirector.StateTimeoutFrames;

        /// <summary>
        /// 状态体本帧实际读到的 <c>aiTimer</c>。
        /// <para>
        /// 基类把 <c>Timer++</c> 放在状态体之后,而原代码里 <see cref="SpiritRing"/> 是在同一帧的稍后
        /// 读 <c>fountain.aiTimer</c> 的,读到的正是状态体看到的那个值(而不是自增后的值)。
        /// 部件通路一律读这个量,读 <see cref="VaultState{TContext}.Timer"/> 会整体错一帧。
        /// </para>
        /// </summary>
        public int BodyTimer { get; private set; }

        public override void OnEnter(SpiritFountainStateContext ctx)
        {
            base.OnEnter(ctx);
            BodyTimer = 0;
        }

        public sealed override IVaultState<SpiritFountainStateContext> OnUpdate(SpiritFountainStateContext ctx)
        {
            IVaultState<SpiritFountainStateContext> next = RunBody(ctx);
            //状态体自己可能动过 Timer(转阶段的额外自增、十字斩的原地归零),所以在它跑完之后取值
            BodyTimer = Timer;
            return next;
        }

        /// <summary>状态主体。返回非 null 请求换态,返回 null 留在本状态</summary>
        protected abstract IVaultState<SpiritFountainStateContext> RunBody(SpiritFountainStateContext ctx);

        /// <summary>超时的去处:当成正常收招,走线性链的下一手</summary>
        protected override IVaultState<SpiritFountainStateContext> OnTimeout(SpiritFountainStateContext ctx)
            => Advance(ctx, StateIndex);

        /// <summary>
        /// 延后一帧才执行的换态:补上原代码下一帧开头那次 <c>aiTimer++</c>。
        /// <para>
        /// 原 AI() 里七个状态块是顺序 <c>if</c>,换到序号更靠<b>前</b>的状态(落环喷泉 → 横扫)时,
        /// 新块要等下一帧才跑,而那一帧开头的统一自增已经把 aiTimer 推到 1,
        /// 所以新状态的首个执行帧读到的是 1 而不是 0。宿主的续跑链在判定为「延后」时调它
        /// </para>
        /// </summary>
        public void AdoptDeferredEntry()
        {
            Timer++;
        }

        /// <summary>
        /// 线性推进到下一手。<b>只有权威端真的换态</b>:
        /// <see cref="VaultStateMachine{TContext}"/> 在客户端照常跑状态体但会丢弃返回值,
        /// 客户端安静等换态包(ai[3]),运动数学照跑,只有决策被收归权威端
        /// </summary>
        protected static IVaultState<SpiritFountainStateContext> Advance(SpiritFountainStateContext ctx, SpiritFountainStateIndex from)
            => IsServer ? SpiritFountainRotation.Pick(ctx, from) : null;

        /// <summary>
        /// 生成敌对弹幕。伤害是 <c>NPC.damage / 6</c> 的<b>整数除法</b>再乘倍率,击退 3,owner 传 -1。
        /// 客户端不生成(守卫在 <see cref="SpiritFountain.Shoot"/> 里,与原代码同一处)
        /// </summary>
        protected static void Shoot<T>(SpiritFountainStateContext ctx, Vector2 pos, Vector2 velocity,
            float damageMult = 1f, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile
        {
            ctx.Owner?.Shoot(ModContent.ProjectileType<T>(), pos, velocity, damageMult, ai0, ai1, ai2);
        }

        /// <summary>本地端(含单机)。粒子、音效、滤镜只在这里做</summary>
        protected static bool IsLocal => !Main.dedServ;
    }
}
