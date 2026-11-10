using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States;
using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core
{
    /// <summary>写入 ai[3],0~9 演出与连接段,10 起是招式</summary>
    public enum SageStateIndex
    {
        /// <summary>星门登场</summary>
        Entrance = 0,
        /// <summary>选招连接段</summary>
        Hub = 1,
        /// <summary>转阶段,66% 与 33% 各一次</summary>
        PhaseShift = 2,
        /// <summary>死亡演出</summary>
        Death = 4,
        /// <summary>脱战离场</summary>
        Despawn = 5,

        /// <summary>星流扇:锁向后沿几道航线连发交织摆动的星魂</summary>
        StarTorrent = 10,
        /// <summary>彗星冲:沿冻结的航道穿过玩家,星遁换位后再冲</summary>
        CometCharge = 11,
        /// <summary>星环:绕身的星符逐颗脱环锁向射出</summary>
        StarRing = 12,
        /// <summary>新月斩:沿回旋轨迹飞出又折回的月刃</summary>
        CrescentSlash = 13,
        /// <summary>十字星爆:十字标记亮起后沿四向爆开</summary>
        CrossStarburst = 14,
        /// <summary>流星雨:成排斜向航线逐道落下流星</summary>
        MeteorShower = 15,
        /// <summary>满月光炮:巨型光柱按预告的扇面扫掠</summary>
        FullMoonBeam = 16,
    }

    /// <summary>
    /// 拍点都是 Timer 的区间,不设会清零 Timer 的拍子枚举,客户端只靠过线的 Timer 就能重放
    /// 弹幕和锁向只在权威端做,运动各端都跑
    /// </summary>
    public abstract class SageStateBase : CEBossStateBase<SageStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract SageStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();
        public override int TimeoutFrames => SageDirector.AttackTimeoutFrames;

        /// <summary>本招起手时本体想待的位置,连接段飞向它</summary>
        public virtual Vector2 AnchorFor(SageStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SageDirector.DefaultAnchor.X, SageDirector.DefaultAnchor.Y);

        /// <summary>连接段选中本招那一帧在权威端调一次,要按当前站位定方位的招覆写它</summary>
        public virtual void OnPicked(SageStateContext ctx) {
        }

        /// <summary>上一帧的 Timer,拍点按越过判定,客户端收养计时跳帧也不漏拍</summary>
        private int lastTimer = -1;

        public override void OnEnter(SageStateContext ctx) {
            base.OnEnter(ctx);
            lastTimer = -1;
        }

        public sealed override IVaultState<SageStateContext> OnUpdate(SageStateContext ctx) {
            IVaultState<SageStateContext> next = Run(ctx);
            lastTimer = Timer;
            return next;
        }

        /// <summary>状态本体,返回非 null 就换态</summary>
        protected abstract IVaultState<SageStateContext> Run(SageStateContext ctx);

        protected override IVaultState<SageStateContext> OnTimeout(SageStateContext ctx) {
            if (!IsServer) {
                return null;
            }
            ctx.Npc.velocity *= 0.5f;
            return EndAttack(ctx);
        }

        /// <summary>本帧 Timer 刚越过 beat</summary>
        protected bool At(int beat) => lastTimer < beat && Timer >= beat;

        protected static IVaultState<SageStateContext> EndAttack(SageStateContext ctx) => new SageHubState();

        /// <summary>服务端生成敌对弹幕,伤害传普通档基值,客户端返回 -1</summary>
        protected static int Shoot<T>(SageStateContext ctx, Vector2 pos, Vector2 vel, int normalBase, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
            where T : ModProjectile {
            if (!IsServer) {
                return -1;
            }
            return Projectile.NewProjectile(ctx.Npc.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<T>(), ctx.Owner.ProjDamage(normalBase), 0f,
                Main.myPlayer, ai0, ai1, ai2);
        }

        protected static void Hover(SageStateContext ctx, Vector2 dest, float maxSpeed, float accel = 0.1f, float slowRadius = 120f) {
            ctx.Mode = SageMoveMode.HoverTo;
            ctx.MoveTarget = dest;
            ctx.MoveSpeed = maxSpeed;
            ctx.Accel = accel;
            ctx.SlowRadius = slowRadius;
        }

        protected static void HoldAt(SageStateContext ctx, Vector2 offset, float stiffness = 0.12f, float lerp = 0.3f, float maxSpeed = 30f) {
            ctx.Mode = SageMoveMode.HoldRelative;
            ctx.HoldOffset = offset;
            ctx.Stiffness = stiffness;
            ctx.Accel = lerp;
            ctx.MoveSpeed = maxSpeed;
        }

        protected static void Direct(SageStateContext ctx) {
            ctx.Mode = SageMoveMode.Direct;
        }

        protected static void FaceTo(SageStateContext ctx, float angle) {
            ctx.Face = SageFaceMode.Angle;
            ctx.FaceAngle = angle;
        }

        /// <summary>嘴部的世界坐标,弹幕从这里出</summary>
        protected static Vector2 Muzzle(SageStateContext ctx)
            => ctx.Npc.Center + ctx.Npc.rotation.ToRotationVector2() * SageDirector.MuzzleDistance;
    }
}