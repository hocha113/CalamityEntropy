using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.Core
{
    /// <summary>
    /// 状态索引,写入 <c>npc.ai[3]</c> 网络同步。
    /// <b>序号与迁移前的 <c>CruiserHead.AIStyle</c> 逐项对齐</b>,方便对照旧代码与旧日志
    /// </summary>
    public enum CruiserStateIndex
    {
        /// <summary>直扑:一路加速撞向玩家,进到 700 + 当前速度就交棒</summary>
        TryToClosePlayer = 0,
        /// <summary>拉开:先散开再回身,第 90 帧甩一次尾鞭(尾部新星)</summary>
        StayAwayAndShootVoidStar = 1,
        /// <summary>绕飞:贴着 600 半径侧向绕圈,每 40 帧甩一次尾鞭</summary>
        AroundPlayerAndShootVoidStar = 2,
        /// <summary>能量球:开局放出一颗挂在本体上的能量球,自己慢速贴近</summary>
        EnergyBall = 3,
        /// <summary>虚空残渣:张嘴蓄 80 帧,一口喷出 80 发残渣,再顺势冲一段</summary>
        VoidResidue = 4,
        /// <summary>转阶段:122 帧演出,计数器是 <c>phaseTrans</c> 而不是状态计时</summary>
        PhaseTransing = 5,
        /// <summary>虚空尖刺:高速盘旋,四次全向 12 发尖刺</summary>
        VoidSpike = 6,
        /// <summary>咬击:咬住玩家拖 20 帧,甩出并沿航线布下刀光阵</summary>
        BiteAndDash = 7,
        /// <summary>巡航:稳速追瞄,100 帧后每帧 1/150 概率收招</summary>
        Cruise = 8,
        /// <summary>裂空吐星:蓄 100 帧一口喷出 80 发虚空星</summary>
        SplittingVoidStar = 9,
        /// <summary>短冲:锁向直冲 38 帧,之后追瞄</summary>
        QuickDash = 10,
        /// <summary>巡游布雷:缓转弯,前 180 帧每 7 帧撒一颗虚空炸弹</summary>
        AroundSpawnVoidBomb = 11,
        /// <summary>虚空激光:瞄准窗后 6 轮定点扫射,每轮自身也顺着光束冲一次</summary>
        VoidLaser = 12,
    }

    /// <summary>
    /// 巡游者状态基类。收三样公共小件:收招(走轮换裁决)、出手(伤害折算)、原 changeCounter 的读写约定。
    /// <para>
    /// 拍子一律用 <see cref="CruiserStateContext.ChangeCounter"/> 的<b>区间判断</b>表达,
    /// 不引入会归零 <see cref="VaultState{TContext}.Timer"/> 的 beat 枚举——原代码每个状态的进度
    /// 都是 <c>changeCounter</c>(外加激光的 <c>localAI[2]</c>)的纯函数,没有任何锁存的子拍。
    /// 加一个锁存的拍号就等于多一个必须过线的量,白送一个失步来源。
    /// </para>
    /// <para>
    /// <b>自增点的口径</b>:原代码在状态体中间某一处写 <c>changeCounter++</c>,前后的比较用的是
    /// 不同的值。迁移后一律保留这个位置:自增<b>之前</b>读 <c>ctx.ChangeCounter</c>,
    /// 自增<b>之后</b>再读一次,阈值数字全部照抄原文。
    /// </para>
    /// </summary>
    public abstract class CruiserStateBase : CEBossStateBase<CruiserStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract CruiserStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();

        /// <summary>原代码没有超时兜底,这里统一挂一个到不了的安全网,见 Director 的说明</summary>
        public override int TimeoutFrames => CruiserDirector.StateTimeoutFrames;

        /// <summary>超时的去处:当成正常收招,走轮换裁决选下一手</summary>
        protected override IVaultState<CruiserStateContext> OnTimeout(CruiserStateContext ctx)
            => NextAttack(ctx);

        /// <summary>
        /// 收招:对应原代码的 <c>changeAi()</c>。清 <c>changeCounter</c>、推进序号、选下一手。
        /// <para>
        /// <b>只有权威端真的选招。</b><see cref="VaultStateMachine{TContext}"/> 在客户端照常跑
        /// <c>OnUpdate</c> 但会丢弃返回值,所以客户端若也走一遍 <see cref="CruiserRotation.Pick"/>,
        /// 状态换不掉、<c>ChangeCounter</c> 却被提前清零,剩下几帧会拿着 0 继续跑旧状态。
        /// 客户端在这里返回 null,安静等换态包——运动数学照跑,只有决策被收归权威端。
        /// </para>
        /// </summary>
        protected IVaultState<CruiserStateContext> NextAttack(CruiserStateContext ctx)
            => IsServer ? CruiserRotation.Pick(ctx, StateIndex) : null;

        /// <summary>
        /// 生成敌对弹幕。伤害 <c>NPC.damage / 6.9 × 倍率</c>,击退 3,owner 传 -1(全部照搬原 <c>Shoot</c>)。
        /// 客户端不生成
        /// </summary>
        protected static void Shoot(CruiserStateContext ctx, int type, Vector2 pos, Vector2 velocity,
            float damageMult = 1f, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) {
            if (!IsServer) {
                return;
            }
            NPC npc = ctx.Npc;
            Projectile.NewProjectile(npc.GetSource_FromAI(), pos, velocity, type,
                (int)(npc.damage / CruiserDirector.ProjDamageDivisor * damageMult),
                CruiserDirector.ProjKnockback, -1, ai0, ai1, ai2);
        }
    }
}
