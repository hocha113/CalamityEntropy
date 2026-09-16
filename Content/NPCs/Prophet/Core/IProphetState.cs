using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Prophet.Core
{
    /// <summary>
    /// 状态索引,写入 <c>npc.ai[3]</c> 网络同步。
    /// <b>序号与迁移前的 <c>TheProphet.AIStyle</c> 逐个对齐</b>,方便对照旧代码与旧日志。
    /// <c>CanHitPlayer</c> 与 <c>ModifyIncomingHit</c> 都还按这套编号读 <c>ai[3]</c>
    /// </summary>
    public enum ProphetStateIndex
    {
        /// <summary>四轮符文弹:侧向绕行,定期闪到远处再打一把扇形洪流</summary>
        RuneVolley = 0,
        /// <summary>冲刺:三次锁向反冲后突进,第三次是重击并沿途撒侧弹</summary>
        Dash = 1,
        /// <summary>符文晶簇:闪现后贴脸冲刺,半拍吐出高速晶体</summary>
        RuneCluster = 2,
        /// <summary>双层符文洪流:闪现后一次性铺满两层扇面</summary>
        RuneTorrentFan = 3,
        /// <summary>速射符文洪流:慢段 6 帧一发转快段 2 帧一发</summary>
        RapidTorrent = 4,
        /// <summary>环状符文:每三四帧闪一次并吐一发慢弹,收尾转普通追击</summary>
        RingBlink = 5,
        /// <summary>符文光球:在自身周围布一圈静止符文,二阶段连布三圈</summary>
        RuneOrb = 6,
        /// <summary>符文冲击:绕行与刹车放电交替,每 40 帧一次扇形闪电</summary>
        RuneImpact = 7,
        /// <summary>大激光:清场定位,把范围内玩家拖到炮口前,再放出眼球</summary>
        GrandLaser = 8,
        /// <summary>符文飞匕:闪到中距离后每 5 帧撒一把匕首</summary>
        RuneDagger = 9,
        /// <summary>虚空触手:慢速贴近,定期放出一整圈尖刺</summary>
        VoidSpike = 10,
        /// <summary>异形符文冲锋:先布一圈异形符文,再三次直线冲锋</summary>
        AltRuneCharge = 11,
    }

    /// <summary>
    /// 先知状态基类。收四样公共小件:倒计时门、出手(伤害折算 + 权威端守卫)、瞬移、超时兜底。
    /// <para>
    /// 拍子一律用 <see cref="ProphetStateContext.Countdown"/> 的<b>原样判定</b>表达
    /// (原代码就是对这个倒计时做 <c>==</c> / <c>%</c> / <c>&gt;</c>),
    /// 不引入会归零 <see cref="VaultState{TContext}.Timer"/> 的 beat 枚举:
    /// 多一个锁存的拍号就等于多一个必须过线的量,白送一个失步来源。
    /// </para>
    /// <para>
    /// <b>收招不由状态发起。</b>原代码是在每帧开头先看倒计时是否耗尽、耗尽就当场选招并
    /// <b>同帧</b>跑新招的状态体,所以选招口放在宿主的 <c>RunAttackFrame</c> 里,
    /// 状态的 <c>OnUpdate</c> 恒返回 null。只有超时兜底这条安全网会从状态里请求换态
    /// </para>
    /// </summary>
    public abstract class ProphetStateBase : CEBossStateBase<ProphetStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract ProphetStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();

        /// <summary>原代码没有超时兜底,这里统一挂一个到不了的安全网,见 Director 的说明</summary>
        public override int TimeoutFrames => ProphetDirector.StateTimeoutFrames;

        /// <summary>超时的去处:当成正常收招,走轮换表选下一手(只有权威端真的选)</summary>
        protected override IVaultState<ProphetStateContext> OnTimeout(ProphetStateContext ctx)
            => IsServer ? ProphetRotation.Pick(ctx) : null;

        /// <summary>
        /// 原 <c>if (AIChangeDelay &gt; 0) { 招式 } else { 惯性漂移 }</c> 的外壳。
        /// 权威端选招总把倒计时赋成正数且同帧就跑状态体,所以 else 支<b>永远进不来</b>;
        /// 客户端在「本地倒计时已归零、换态包还没到」的一两帧里会短暂走到。照搬保留
        /// </summary>
        public sealed override IVaultState<ProphetStateContext> OnUpdate(ProphetStateContext ctx) {
            if (ctx.Countdown > 0) {
                RunAttack(ctx);
            }
            else {
                IdleDrift(ctx);
            }
            return null;
        }

        /// <summary>招式本体。倒计时仍为正时每帧调用一次</summary>
        protected abstract void RunAttack(ProphetStateContext ctx);

        /// <summary>原 else 支:朝向跟速度走,轻微阻尼,再朝玩家加一个单位推力</summary>
        private static void IdleDrift(ProphetStateContext ctx) {
            NPC npc = ctx.Npc;
            npc.rotation = npc.velocity.ToRotation();
            npc.velocity *= ProphetDirector.IdleDrag;
            npc.velocity += (ctx.Target.Center - npc.Center).SafeNormalize(Vector2.Zero) * ProphetDirector.IdleThrust;
        }

        /// <summary>常规弹幕伤害:原代码一律 <c>NPC.damage / 6</c>(整数除法)</summary>
        protected static int ProjDamage(ProphetStateContext ctx)
            => ctx.Npc.damage / ProphetDirector.ProjDamageDivisor;

        /// <summary>符文结晶起手音。原代码在 0 / 1 / 3 / 6 号招里逐字重复了四遍,这里合成一处</summary>
        protected static void CrystalCue(NPC npc) {
            if (!Main.dedServ) {
                SoundEngine.PlaySound(SoundID.Item9 with { Pitch = 0.2f, Volume = 0.85f, MaxInstances = 8 }, npc.Center);
                CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), npc.Center);
            }
        }

        /// <summary>
        /// 生成敌对弹幕,owner 固定 -1。客户端不生成
        /// (<c>IsServer</c> 就是原代码的 <c>Main.netMode != NetmodeID.MultiplayerClient</c>)
        /// </summary>
        protected static void Shoot<T>(ProphetStateContext ctx, Vector2 pos, Vector2 velocity, int damage,
            float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile {
            if (!IsServer) {
                return;
            }
            NPC npc = ctx.Npc;
            Projectile.NewProjectile(npc.GetSource_FromAI(), pos, velocity, ModContent.ProjectileType<T>(),
                damage, knockback, -1, ai0, ai1, ai2);
        }

        /// <summary>
        /// 瞬移。原代码在各端各自掷骰各自落位,于是每端落在不同地方、全靠每帧快照硬拽回来。
        /// <para>
        /// 现在<b>只有权威端掷骰并落位</b>,落点写进 <see cref="ProphetStateContext.TeleportPos"/>
        /// 并把流水号 +1,与位置/速度在同一个快照里原子过线;客户端收到流水号推进后
        /// 补放两端火花、清空尾迹、丢掉本地预测(见 <c>TheProphet.ReceiveExtraAI</c>)。
        /// 瞬移是决策点,当场 netUpdate
        /// </para>
        /// </summary>
        protected static void Teleport(ProphetStateContext ctx, Vector2 pos) {
            if (!IsServer) {
                return;
            }
            ctx.TeleportSeq++;
            ctx.TeleportPos = pos;
            ctx.Owner.TeleportTo(pos);
            ctx.Npc.netUpdate = true;
        }
    }
}
