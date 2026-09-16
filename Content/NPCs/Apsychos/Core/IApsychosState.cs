using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Apsychos.Core
{
    /// <summary>
    /// 状态索引,写入 <c>npc.ai[3]</c> 网络同步。
    /// <b>序号与重构前的 <c>Apsychos.AIStyle</c> 逐个对齐</b>,方便对照旧代码与旧日志
    /// </summary>
    public enum ApsychosStateIndex
    {
        /// <summary>接近:转向玩家并按距离推进,轮换表里每隔一手就垫一次</summary>
        MoveToTarget = 0,
        /// <summary>冲刺:蓄力刹速涨描边,点火后 20 帧推进,再 20 帧收尾</summary>
        Dash = 1,
        /// <summary>三连火球:尾巴前伸当炮口,五轮三发扇形</summary>
        FireballShooting = 2,
        /// <summary>喷火:尾巴摆动瞄人,60 到 140 帧持续喷射</summary>
        FlameThrow = 3,
        /// <summary>巨型火球:蓄满即发,共四发</summary>
        FireballBig = 4,
        /// <summary>转阶段:白化涨满换配色,80 帧置阶段,120 帧收</summary>
        PhaseTrans = 5,
        /// <summary>甩尾:尾巴内收蓄力,60 帧定向突进,尾巴弹出打尾刺,按血量循环 3 或 6 次</summary>
        TailDash = 6,
        /// <summary>激光:尾巴指向本体正前方,开局即发,350 帧慢速扫场</summary>
        Laser = 7,
    }

    /// <summary>
    /// Apsychos 状态基类。收三样公共小件:收招(走轮换表)、出手(伤害折算)、难度系数。
    /// <para>
    /// 拍子一律用 <see cref="VaultState{TContext}.Timer"/> 的<b>区间判断</b>表达,
    /// 不引入会归零 Timer 的 beat 枚举——原代码每个状态的进度都是
    /// <c>{AIChangeCounter, num1, num2, num3}</c> 这几个标量的纯函数,没有任何锁存的子拍。
    /// 加一个锁存的拍号就等于多一个必须过线的量,白送一个失步来源。
    /// </para>
    /// </summary>
    public abstract class ApsychosStateBase : CEBossStateBase<ApsychosStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract ApsychosStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();

        /// <summary>原代码没有超时兜底,这里统一挂一个到不了的安全网,见 Director 的说明</summary>
        public override int TimeoutFrames => ApsychosDirector.StateTimeoutFrames;

        /// <summary>超时的去处:当成正常收招,走轮换表选下一手</summary>
        protected override IVaultState<ApsychosStateContext> OnTimeout(ApsychosStateContext ctx)
            => NextAttack(ctx);

        /// <summary>
        /// 收招:对应原代码的 <c>SetAIStyle()</c>。清标量、选下一手。
        /// <para>
        /// <b>只有权威端真的选招。</b><see cref="VaultStateMachine{TContext}"/> 在客户端照常跑
        /// <c>OnUpdate</c> 但会丢弃返回值,所以客户端若也走一遍 <see cref="ApsychosRotation.Pick"/>,
        /// 状态换不掉、标量却被提前清零,剩下几帧会拿着一堆 0 继续跑旧状态(尾巴瞬间弹回本体那类)。
        /// 客户端在这里返回 null,安静等换态包——运动数学照跑,只有决策被收归权威端。
        /// </para>
        /// <para>
        /// 注意<b>不</b>清 <see cref="ApsychosStateContext.TailDashReps"/>——原代码的 <c>NPC.ai[2]</c>
        /// 也不在 <c>SetAIStyle</c> 里清,是由 TailDash 自己在收招前清的
        /// </para>
        /// </summary>
        protected static IVaultState<ApsychosStateContext> NextAttack(ApsychosStateContext ctx)
            => IsServer ? ApsychosRotation.Pick(ctx) : null;

        /// <summary>
        /// 生成敌对弹幕。伤害按阶段折算(一阶段 <c>damage/6.5</c>,二阶段 <c>damage/5.4</c>),
        /// 击退固定 4,owner 传 -1。客户端不生成
        /// </summary>
        protected static void Shoot<T>(ApsychosStateContext ctx, Vector2 pos, Vector2 velocity,
            float damageMult = 1f, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            NPC npc = ctx.Npc;
            float divisor = ctx.Phase == 1 ? ApsychosDirector.ProjDamageDivisorPhase1 : ApsychosDirector.ProjDamageDivisorPhase2;
            int baseDamage = (int)(npc.damage / divisor);
            Projectile.NewProjectile(npc.GetSource_FromAI(), pos, velocity, ModContent.ProjectileType<T>(),
                (int)(baseDamage * damageMult), ApsychosDirector.ProjKnockback, -1, ai0, ai1, ai2);
        }
    }
}
