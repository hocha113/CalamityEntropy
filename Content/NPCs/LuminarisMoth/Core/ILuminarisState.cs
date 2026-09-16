using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.Core
{
    /// <summary>
    /// 状态索引,写入 <c>npc.ai[3]</c> 网络同步。
    /// <b>序号与迁移前的 <c>Luminaris.AIStyle</c> 逐个对齐</b>,方便对照旧代码与旧日志
    /// </summary>
    public enum LuminarisStateIndex
    {
        /// <summary>定点绕转:入位到玩家侧上方,之后绕着玩家转圈并持续喷涡。这一招不造成接触伤害</summary>
        RoundShooting = 0,
        /// <summary>星刺滑行:朝随机方向滑出 800,滑行途中每 2 帧撒一根红蓝交替的刺</summary>
        AstralSpike = 1,
        /// <summary>高空横移:两段入位到玩家正上方,余弦横移并从场地两侧甩横扫星弹,入位末尾有一记落地拍</summary>
        AboveMovingShooting = 2,
        /// <summary>空拍一秒:只把朝向掰正,速度不动(靠残余速度滑行)</summary>
        Waiting1Sec = 3,
        /// <summary>俯冲:抬升 → 拱形俯冲,重复两轮,末段转为直接追撞玩家</summary>
        Subduction = 4,
        /// <summary>悬停环射:黏在玩家侧上方,每隔一段时间后坐一下并打出一圈星弹</summary>
        StayAboveAndShooting = 5,
        /// <summary>冲撞:锁向 → 40 速直冲 → 再锁向 → 再直冲,冲刺中带极慢的追瞄</summary>
        Dashing = 6,
        /// <summary>环爆:贴到玩家附近,之后每 10 帧炸出一圈八向星弹</summary>
        Shoot360 = 7,
        /// <summary>绕场直冲:先拉开 700 绕场蓄力,再穿过玩家所在点冲到对面,带大尾迹</summary>
        RoundAndDash = 8,
        /// <summary>高空砸落:四轮「抬到玩家上方 → 自由落体」,砸落中左右撒弹,只有砸落段有接触伤害</summary>
        SmashDown = 9,
        /// <summary>三角弹绕圈:12 帧绕玩家一整圈并逐帧射三角弹,之后转为慢速绕圈</summary>
        ShootTriangle = 10,
    }

    /// <summary>
    /// Luminaris 状态基类。收四样公共小件:倒计时推进兼收招、选招入口、出手伤害折算、倒计时版的一次性拍宽限窗。
    /// <para>
    /// 拍子一律用 <see cref="LuminarisStateContext.Countdown"/> 的<b>区间判断</b>表达,
    /// 不引入会归零 <see cref="VaultState{TContext}.Timer"/> 的 beat 枚举——原代码每个状态的进度
    /// 都是 <c>{AIChangeCounter, num1, num2, num3, vec1, vec2}</c> 这几个量的纯函数,没有任何锁存的子拍。
    /// 加一个锁存的拍号就等于多一个必须过线的量,白送一个失步来源。
    /// </para>
    /// </summary>
    public abstract class LuminarisStateBase : CEBossStateBase<LuminarisStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract LuminarisStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();

        /// <summary>原代码没有超时兜底,这里统一挂一个到不了的安全网,见 Director 的说明</summary>
        public override int TimeoutFrames => LuminarisDirector.StateTimeoutFrames;

        /// <summary>超时的去处:当成正常收招,走轮换表选下一手</summary>
        protected override IVaultState<LuminarisStateContext> OnTimeout(LuminarisStateContext ctx)
            => NextAttack(ctx);

        /// <summary>
        /// 原 <c>AttackPlayer</c> 顶部 <c>if (AIChangeCounter-- &lt; 0)</c> 的下半段。
        /// <b>每个状态体跑完都必须调它</b>:倒计时自减一帧,本帧读到的值若已跌破 0 就收招。
        /// <para>
        /// 原代码把自减与选招放在状态体<b>之前</b>,所以「选招那一帧跑的是新招的体」;
        /// 这里放在体<b>之后</b>、换态由返回值驱动、新招的体下一帧才跑。
        /// 逐帧序列完全一致:旧招最后一帧读到 -1,新招第一帧读到本招时长
        /// (<see cref="VaultStateMachine{TContext}.Update"/> 换态后不会在同一帧再跑一次 <c>OnUpdate</c>)。
        /// </para>
        /// <para>
        /// 客户端 <see cref="NextAttack"/> 返回 null,于是倒计时会继续往负数走,直到换态包带来新值。
        /// 各状态在负值区的分支与 -1 帧相同,所以这几帧的运动是连续的
        /// </para>
        /// </summary>
        protected IVaultState<LuminarisStateContext> Tick(LuminarisStateContext ctx, int countdown) {
            ctx.Countdown--;
            return countdown < 0 ? NextAttack(ctx) : null;
        }

        /// <summary>
        /// 收招:对应原代码的 <c>SetAISyyle()</c> 及其前后的清理与序号推进。
        /// <para>
        /// <b>只有权威端真的选招。</b><see cref="VaultStateMachine{TContext}"/> 在客户端照常跑
        /// <c>OnUpdate</c> 但会丢弃返回值,所以客户端若也走一遍 <see cref="LuminarisRotation.Pick"/>,
        /// 状态换不掉、锚点与标量却被提前清零,本体会瞬间弹到 <c>Vector2.Zero</c> 那一类地方去。
        /// 客户端在这里返回 null,安静等换态包——运动数学照跑,只有决策被收归权威端。
        /// </para>
        /// </summary>
        protected IVaultState<LuminarisStateContext> NextAttack(LuminarisStateContext ctx)
            => IsServer ? LuminarisRotation.Pick(ctx, StateIndex) : null;

        /// <summary>
        /// 倒计时版的一次性拍宽限窗。基类的 <c>CuePassed</c> 假定计数递增,而这里的时钟是递减的,
        /// 所以「真的错过」是掉到拍点下方超过 <c>CueCatchUpGrace</c> 帧。
        /// 用法与 Apsychos 的 Laser / TailDash 一致:先置锁存,再用它决定本地要不要真的补这一拍
        /// </summary>
        protected static bool CountdownCuePassed(int countdown, int beat)
            => countdown < beat - CueCatchUpGrace;

        /// <summary>
        /// 生成敌对弹幕。伤害是 <c>(int)(NPC.damage / 6.2f)</c> 再乘倍率,击退固定 4,owner 传 -1。
        /// 客户端不生成(原 <c>Shoot&lt;T&gt;</c> 自带的 netMode 门,原样保留)
        /// </summary>
        protected static void Shoot<T>(LuminarisStateContext ctx, Vector2 pos, Vector2 velocity,
            float damageMult = 1f, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile {
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }
            NPC npc = ctx.Npc;
            int baseDamage = (int)(npc.damage / LuminarisDirector.ProjDamageDivisor);
            Projectile.NewProjectile(npc.GetSource_FromAI(), pos, velocity, ModContent.ProjectileType<T>(),
                (int)(baseDamage * damageMult), LuminarisDirector.ProjKnockback, -1, ai0, ai1, ai2);
        }
    }
}
