using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States;
using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core
{
    /// <summary>写入 ai[3],0~9 演出与连接段,10 起是招式</summary>
    public enum SunStateIndex
    {
        /// <summary>日出登场</summary>
        Entrance = 0,
        /// <summary>选招连接段</summary>
        Hub = 1,
        /// <summary>转阶段,66% 与 33% 各一次</summary>
        PhaseShift = 2,
        /// <summary>死亡演出</summary>
        Death = 4,
        /// <summary>脱战离场</summary>
        Despawn = 5,

        /// <summary>金乌齐飞:一串先散开再回头追击的太阳神鸟</summary>
        BirdFlock = 10,
        /// <summary>曜日冲:沿冻结的航道横冲,二阶段起连冲并撒火羽</summary>
        SolarDash = 11,
        /// <summary>日冕之舞:以本体为心的多波旋转光芒</summary>
        CoronaDance = 12,
        /// <summary>日矛·追猎:环绕玩家逐支锁向的长枪</summary>
        LanceHunt = 13,
        /// <summary>日珥绽放:沿固定圆弧展开的火带花瓣</summary>
        FlareBloom = 14,
        /// <summary>日矛·天罗:成排平行长枪从各方向扫过</summary>
        LanceWall = 15,
        /// <summary>坠日:头顶蓄出日球砸向锁定落点</summary>
        Sunfall = 16,
        /// <summary>炎阳天轮:四道光芒持续旋转并向外撒火羽环</summary>
        SolarStorm = 17,
    }

    /// <summary>
    /// 拍点都是 Timer 的区间,不设会清零 Timer 的拍子枚举,客户端只靠过线的 Timer 就能重放
    /// 弹幕和锁向只在权威端做,运动各端都跑
    /// </summary>
    public abstract class SunStateBase : CEBossStateBase<SunStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract SunStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();
        public override int TimeoutFrames => SunDirector.AttackTimeoutFrames;

        /// <summary>本招起手时本体想待的位置,连接段飞向它</summary>
        public virtual Vector2 AnchorFor(SunStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SunDirector.DefaultAnchor.X, SunDirector.DefaultAnchor.Y);

        /// <summary>连接段选中本招那一帧在权威端调一次,要按当前站位定侧向的招覆写它</summary>
        public virtual void OnPicked(SunStateContext ctx) {
        }

        /// <summary>上一帧的 Timer,拍点按越过判定,客户端收养计时跳帧也不漏拍</summary>
        private int lastTimer = -1;

        public override void OnEnter(SunStateContext ctx) {
            base.OnEnter(ctx);
            lastTimer = -1;
        }

        public sealed override IVaultState<SunStateContext> OnUpdate(SunStateContext ctx) {
            IVaultState<SunStateContext> next = Run(ctx);
            lastTimer = Timer;
            return next;
        }

        /// <summary>状态本体,返回非 null 就换态</summary>
        protected abstract IVaultState<SunStateContext> Run(SunStateContext ctx);

        protected override IVaultState<SunStateContext> OnTimeout(SunStateContext ctx) {
            if (!IsServer) {
                return null;
            }
            ctx.Npc.velocity *= 0.5f;
            return EndAttack(ctx);
        }

        /// <summary>本帧 Timer 刚越过 beat</summary>
        protected bool At(int beat) => lastTimer < beat && Timer >= beat;

        protected static IVaultState<SunStateContext> EndAttack(SunStateContext ctx) => new SunHubState();

        /// <summary>服务端生成敌对弹幕,伤害传普通档基值,客户端返回 -1</summary>
        protected static int Shoot<T>(SunStateContext ctx, Vector2 pos, Vector2 vel, int normalBase, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
            where T : ModProjectile {
            if (!IsServer) {
                return -1;
            }
            return Projectile.NewProjectile(ctx.Npc.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<T>(), ctx.Owner.ProjDamage(normalBase), 0f,
                Main.myPlayer, ai0, ai1, ai2);
        }

        protected static void Hover(SunStateContext ctx, Vector2 dest, float maxSpeed, float accel = 0.1f, float slowRadius = 120f) {
            ctx.Mode = SunMoveMode.HoverTo;
            ctx.MoveTarget = dest;
            ctx.MoveSpeed = maxSpeed;
            ctx.Accel = accel;
            ctx.SlowRadius = slowRadius;
        }

        protected static void HoldAt(SunStateContext ctx, Vector2 offset, float stiffness = 0.12f, float lerp = 0.3f, float maxSpeed = 30f) {
            ctx.Mode = SunMoveMode.HoldRelative;
            ctx.HoldOffset = offset;
            ctx.Stiffness = stiffness;
            ctx.Accel = lerp;
            ctx.MoveSpeed = maxSpeed;
        }

        protected static void Direct(SunStateContext ctx) {
            ctx.Mode = SunMoveMode.Direct;
        }

        /// <summary>施法手臂帧:起收各 5 帧过渡帧,中间是平举帧,区间外垂手</summary>
        protected static int ArmCast(int timer, int start, int end) {
            if (timer < start || timer >= end) {
                return 0;
            }
            return timer < start + 5 || timer >= end - 5 ? 2 : 3;
        }

        /// <summary>平举施法时手掌的世界坐标,side 为 -1 左手 +1 右手</summary>
        protected static Vector2 Hand(SunStateContext ctx, int side)
            => ctx.Npc.Center + new Vector2(side * SunDirector.HandCastOffset.X, SunDirector.HandCastOffset.Y);
    }
}