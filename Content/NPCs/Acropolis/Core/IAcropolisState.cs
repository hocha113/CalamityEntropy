using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Acropolis.Core
{
    /// <summary>
    /// 状态索引,写入 <c>npc.ai[3]</c> 网络同步。
    /// <para>
    /// 原代码没有状态变量,是三个并行的布尔/倒计时开关(<c>CannonUpAtk</c> / <c>JumpAndShoot</c> /
    /// <c>Jumping</c>)。这里把每个开关代表的<b>招式</b>抽成一个互斥状态;
    /// 走路、腿部步态、跨招冷却、鱼叉循环、鱼叉拽拉留在宿主当背景行为,不占状态位
    /// </para>
    /// </summary>
    public enum AcropolisStateIndex
    {
        /// <summary>
        /// 行走(选招口)。地面推进由宿主背景跑,本状态只做两件事:
        /// 冷却归零时骰点选招、单发电球、以及满足条件时请求追高跳
        /// </summary>
        Walk = 0,

        /// <summary>炮击:抬炮 60 帧,之后 140 帧向玩家头顶高抛电球。原 <c>CannonUpAtk = 200</c></summary>
        CannonBarrage = 1,

        /// <summary>跳射:朝玩家方向起跳,滞空期间把炮口压向正下方倾泻电球。原 <c>Jumping + JumpAndShoot = 200</c></summary>
        JumpShoot = 2,

        /// <summary>追高跳:玩家高出本体 200 以上且跳跃冷却透支时,朝玩家弹射上去。原 <c>JumpCD &lt;= -260</c> 那一支</summary>
        Leap = 3,
    }

    /// <summary>
    /// 卫城机器状态基类。收三样公共小件:收招(回行走)、出手(伤害折算)、权威端判定。
    /// <para>
    /// 拍子一律用 <see cref="VaultState{TContext}.Timer"/> 的<b>区间判断</b>表达,
    /// 不引入会归零 Timer 的 beat 枚举——原代码每个开关的进度都是单个倒计时标量的纯函数,
    /// 加一个锁存的拍号就等于多一个必须过线的量,白送一个失步来源。
    /// </para>
    /// </summary>
    public abstract class AcropolisStateBase : CEBossStateBase<AcropolisStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract AcropolisStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();

        /// <summary>原代码没有超时兜底,这里统一挂一个到不了的安全网,见 Director 的说明</summary>
        public override int TimeoutFrames => AcropolisDirector.StateTimeoutFrames;

        /// <summary>超时的去处:当成正常收招,回行走</summary>
        protected override IVaultState<AcropolisStateContext> OnTimeout(AcropolisStateContext ctx)
            => BackToWalk(ctx);

        /// <summary>
        /// 收招:回行走态。
        /// <para>
        /// <b>只有权威端真的换态。</b><see cref="VaultStateMachine{TContext}"/> 在客户端照常跑
        /// <c>OnUpdate</c> 但会丢弃返回值,客户端返回 null 安静等换态包即可——
        /// 运动数学照跑,只有决策被收归权威端
        /// </para>
        /// </summary>
        protected static IVaultState<AcropolisStateContext> BackToWalk(AcropolisStateContext ctx)
            => IsServer ? VaultStateRegistry<AcropolisStateContext>.Create((int)AcropolisStateIndex.Walk) : null;

        /// <summary>由注册表创建状态实例(未注册返回 null,框架已打日志)</summary>
        protected static IVaultState<AcropolisStateContext> Create(AcropolisStateIndex state)
            => VaultStateRegistry<AcropolisStateContext>.Create((int)state);

        /// <summary>
        /// 生成敌对弹幕:伤害 <c>NPC.damage / 6.2</c>,击退 4,owner 传 -1。
        /// 客户端不生成,吃随机数的散布也必须在调用前的权威端分支里摇
        /// </summary>
        protected static void Shoot<T>(AcropolisStateContext ctx, Vector2 pos, Vector2 velocity,
            float damageMult = 1f, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile
            => ctx.Owner.Shoot<T>(pos, velocity, damageMult, ai0, ai1, ai2);
    }
}
