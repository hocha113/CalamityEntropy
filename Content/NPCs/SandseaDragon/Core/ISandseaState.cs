using CalamityEntropy.Content.NPCs.SandseaDragon.States;
using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.Core
{
    /// <summary>写入 ai[3],0~9 演出与连接段,10 起是招式</summary>
    public enum SandseaStateIndex
    {
        /// <summary>破沙登场</summary>
        Entrance = 0,
        /// <summary>选招连接段,沙中巡航</summary>
        Hub = 1,
        /// <summary>60% 血转二阶段</summary>
        PhaseShift = 2,
        /// <summary>10% 血锁血喷白,赶往场地中央</summary>
        FinaleShift = 3,
        /// <summary>死亡演出</summary>
        Death = 4,
        /// <summary>脱战下潜</summary>
        Despawn = 5,

        /// <summary>跳跃追逐(C):浅沙游弋后三次扑跳,二阶段追加合拢龙卷</summary>
        LeapChase = 10,
        /// <summary>跳跃冲刺(A):空中对齐后三次水平冲刺,末冲撒毒针</summary>
        LeapDash = 11,
        /// <summary>沙鲨冲击(B):悬在斜上方结一圈沙鲨再发射</summary>
        SharkBarrage = 12,
        /// <summary>甲虫炸弹(D):跃起抛炸弹,弧顶俯冲砸地溅起沙弹</summary>
        BeetleBombs = 13,
        /// <summary>鲨卷风:三阶段唯一的招,打到战斗结束</summary>
        Sharknado = 14,
    }

    /// <summary>
    /// 拍点都是 Timer 的区间,不设会清零 Timer 的拍子枚举,客户端只靠过线的 Timer 就能重放
    /// 运动助手各端都跑,弹幕、骰点和锁向只在权威端做
    /// 运动的几何与节拍先在离线模拟里跑过(Doc/plans/2026-11-11-sandsea-dragon/tools/boss/sim.py),改这里的助手要同步改那边再跑一遍
    /// </summary>
    public abstract class SandseaStateBase : CEBossStateBase<SandseaStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract SandseaStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();
        public override int TimeoutFrames => SandseaDirector.AttackTimeoutFrames;

        /// <summary>本招起手时头部想待的沙中位置,连接段朝它游并停在那里</summary>
        public virtual Vector2 AnchorFor(SandseaStateContext ctx) {
            float x = ctx.Target.Center.X + ctx.SideDir * 420f;
            return new Vector2(x, SurfaceAt(ctx, x) + SandseaDirector.CruiseDepth);
        }

        /// <summary>上一帧的 Timer,拍点按越过判定,客户端收养计时跳帧也不漏拍</summary>
        private int lastTimer = -1;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            lastTimer = -1;
        }

        public sealed override IVaultState<SandseaStateContext> OnUpdate(SandseaStateContext ctx) {
            IVaultState<SandseaStateContext> next = Run(ctx);
            lastTimer = Timer;
            return next;
        }

        /// <summary>状态本体,返回非 null 就换态</summary>
        protected abstract IVaultState<SandseaStateContext> Run(SandseaStateContext ctx);

        protected override IVaultState<SandseaStateContext> OnTimeout(SandseaStateContext ctx) {
            if (!IsServer) {
                return null;
            }
            ctx.Npc.velocity *= 0.5f;
            return EndAttack(ctx);
        }

        /// <summary>本帧 Timer 刚越过 beat</summary>
        protected bool At(int beat) => lastTimer < beat && Timer >= beat;

        protected static IVaultState<SandseaStateContext> EndAttack(SandseaStateContext ctx) => new SandseaHubState();

        /// <summary>服务端生成敌对弹幕,伤害传普通档基值,客户端返回 -1</summary>
        protected static int Shoot<T>(SandseaStateContext ctx, Vector2 pos, Vector2 vel, int normalBase, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
            where T : ModProjectile {
            if (!IsServer) {
                return -1;
            }
            return Projectile.NewProjectile(ctx.Npc.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<T>(), SandseaDirector.ProjDamage(normalBase), 0f,
                Main.myPlayer, ai0, ai1, ai2);
        }

        #region 地形
        /// <summary>x 处的沙面,从目标所在高度起探</summary>
        protected static float SurfaceAt(SandseaStateContext ctx, float x) => SandseaTerrain.Surface(x, ctx.Target.Center.Y);

        protected static bool InSand(SandseaStateContext ctx) => SandseaTerrain.Buried(ctx.Npc.Center);

        /// <summary>本体在目标的哪一侧,±1</summary>
        protected static int SideOfTarget(SandseaStateContext ctx) => ctx.Npc.Center.X >= ctx.Target.Center.X ? 1 : -1;
        #endregion

        #region 运动助手
        /// <summary>
        /// 当前航向:平时取速度方向,几乎静止时取头的朝向
        /// 上一帧在悬停或平移跟随时也取朝向:那时的速度只是跟着玩家平移,拿它当航向头会倒着轧过自己的脖子
        /// </summary>
        protected static float Heading(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            if (ctx.Held) {
                return npc.rotation;
            }
            return npc.velocity.LengthSquared() > 0.04f ? npc.velocity.ToRotation() : npc.rotation;
        }

        /// <summary>按转弯半径限速转向期望航向,速率每帧最多变 accel</summary>
        protected static void SteerHeading(SandseaStateContext ctx, float desiredAngle, float speed, float accel = 0.6f, float radius = SandseaDirector.TurnRadius) {
            NPC npc = ctx.Npc;
            //离开悬停的第一帧,速度里只有沿朝向的分量算数
            float v = ctx.Held ? Math.Max(0f, Vector2.Dot(npc.velocity, npc.rotation.ToRotationVector2())) : npc.velocity.Length();
            float next = v + MathHelper.Clamp(speed - v, -accel, accel);
            //低速时也留一点转向能力,否则起步转不动
            float maxTurn = Math.Max(next, 4f) / radius;
            float heading = Heading(ctx).AngleTowards(desiredAngle, maxTurn);
            npc.velocity = heading.ToRotationVector2() * next;
            ctx.Mode = SandseaMoveMode.Direct;
        }

        /// <summary>空中朝目标点游,临近时收速;转弯半径是看得见的那一档</summary>
        protected static void Steer(SandseaStateContext ctx, Vector2 target, float speed, float accel = 0.6f, float arriveRadius = 140f) {
            Vector2 diff = target - ctx.Npc.Center;
            float dist = diff.Length();
            float k = MathHelper.Clamp(dist / arriveRadius, 0.3f, 1f);
            SteerHeading(ctx, dist > 1f ? diff.ToRotation() : Heading(ctx), speed * k, accel);
        }

        /// <summary>
        /// 沙里朝目标点游并停在那里:身体被物块挡着,转弯半径收紧,临近时半径跟着距离收小,螺旋收拢到目标上而不是绕圈
        /// 离沙面太近时不许再往上,巡航不冒头;人在空中(洞穴)时先落回沙里
        /// </summary>
        protected static void Swim(SandseaStateContext ctx, Vector2 target, float speed, float accel = 0.8f) {
            NPC npc = ctx.Npc;
            if (!InSand(ctx)) {
                DiveIntoSand(ctx);
                return;
            }
            Vector2 diff = target - npc.Center;
            float dist = diff.Length();
            float desired = dist > 1f ? diff.ToRotation() : Heading(ctx);
            float depth = npc.Center.Y - SandseaTerrain.Surface(npc.Center.X, npc.Center.Y);
            if (depth < SandseaDirector.MinSwimDepth && MathF.Sin(desired) < 0f) {
                //压成略朝下的水平方向
                desired = MathF.Cos(desired) >= 0f ? 0.25f : MathHelper.Pi - 0.25f;
            }
            float k = MathHelper.Clamp(dist / 110f, 0.08f, 1f);
            float radius = MathHelper.Clamp(dist * 0.5f, 12f, SandseaDirector.BuriedTurnRadius);
            SteerHeading(ctx, desired, speed * k, accel, radius);
        }

        /// <summary>
        /// 沿过 stop、方向 dir 的航道驶入:朝航道上前方 look 远的点追,横向偏差收敛后头就顺着 dir
        /// 离 stop 不到 slow 时从 cruise 收到 glide,过了 stop 只沿航道慢慢前滑,不回头
        /// </summary>
        /// <param name="along">本体沿航道方向超过 stop 的距离,还没到是负的</param>
        /// <param name="lateral">本体到航道的横向距离</param>
        protected static void ApproachLane(SandseaStateContext ctx, Vector2 stop, Vector2 dir, float cruise, float glide, out float along, out float lateral,
            float radius = SandseaDirector.TurnRadius, float slow = 260f, float look = SandseaDirector.LaneLookahead) {
            NPC npc = ctx.Npc;
            Vector2 rel = npc.Center - stop;
            along = Vector2.Dot(rel, dir);
            lateral = Math.Abs(rel.X * dir.Y - rel.Y * dir.X);
            Vector2 carrot = stop + dir * (along + look);
            float far = MathHelper.Clamp(-along / slow, 0f, 1f);
            float off = MathHelper.Clamp(lateral / 160f, 0f, 1f);
            float speed = MathHelper.Lerp(glide, cruise, Math.Max(far, off * 0.7f));
            SteerHeading(ctx, (carrot - npc.Center).ToRotation(), speed, 0.6f, radius);
        }

        protected static void ApproachLane(SandseaStateContext ctx, Vector2 stop, Vector2 dir, float cruise, float glide,
            float radius = SandseaDirector.TurnRadius, float slow = 260f, float look = SandseaDirector.LaneLookahead)
            => ApproachLane(ctx, stop, dir, cruise, glide, out _, out _, radius, slow, look);

        /// <summary>
        /// 破沙上浮前的潜行段:先在 x 正下方的深处就位,最后 RiseLeadFrames 帧沿过 x 的竖直线往上游
        /// 这样出沙时头朝上、横坐标对准 x,后面按固定圆弧转向的招式才能落在预定位置上
        /// </summary>
        /// <param name="until">潜行段结束的 Timer</param>
        protected void RiseLine(SandseaStateContext ctx, float x, int until) {
            float surface = SurfaceAt(ctx, x);
            if (Timer < until - SandseaDirector.RiseLeadFrames) {
                Swim(ctx, new Vector2(x, surface + SandseaDirector.RiseDeepDepth), SandseaDirector.SwimSpeed);
                return;
            }
            ApproachLane(ctx, new Vector2(x, surface + SandseaDirector.RiseStartDepth), -Vector2.UnitY, 10f, 6f,
                SandseaDirector.TightTurnRadius, 120f, SandseaDirector.TightLookahead);
        }

        /// <summary>头朝正上方上浮到 topY,速度按剩余帧数算,保证 riseBy 那一帧前到;沙里用急的转弯半径</summary>
        protected void RiseTo(SandseaStateContext ctx, float topY, int riseBy) {
            float need = (ctx.Npc.Center.Y - topY) / Math.Max(1f, riseBy - Timer);
            SteerHeading(ctx, -MathHelper.PiOver2, MathHelper.Clamp(need, SandseaDirector.RiseSpeedMin, SandseaDirector.RiseSpeedMax), 1.6f,
                InSand(ctx) ? SandseaDirector.BuriedTurnRadius : SandseaDirector.TurnRadius);
        }

        /// <summary>
        /// 在随玩家平移的参考系里操舵的前一半:把上一帧叠上去的牵连速度扣掉,剩下的才是自身速度
        /// 操舵之后用 AddCarry 叠回本帧的牵连速度
        /// </summary>
        protected static void DropCarry(SandseaStateContext ctx) {
            ctx.Npc.velocity -= ctx.CarryPrev;
        }

        /// <summary>把参考系的速度叠到自身速度上,身体轨迹随参考系一起平移,路径形状在玩家看来不变</summary>
        protected static void AddCarry(SandseaStateContext ctx, Vector2 frame) {
            float length = frame.Length();
            if (length > SandseaDirector.CarryMax) {
                frame *= SandseaDirector.CarryMax / length;
            }
            ctx.Npc.velocity += frame;
            ctx.Carry = frame;
            ctx.FrameVelocity = frame;
        }

        /// <summary>只受重力的弹道飞行</summary>
        protected static void Ballistic(SandseaStateContext ctx, float gravity = SandseaDirector.Gravity) {
            NPC npc = ctx.Npc;
            npc.velocity.Y = Math.Min(npc.velocity.Y + gravity, SandseaDirector.MaxFallSpeed);
            ctx.Mode = SandseaMoveMode.Direct;
        }

        /// <summary>朝正下方扎进沙里:空中按弹道落,入沙后转向朝下游</summary>
        protected static void DiveIntoSand(SandseaStateContext ctx, float speed = SandseaDirector.SwimSpeed) {
            NPC npc = ctx.Npc;
            if (InSand(ctx)) {
                SteerHeading(ctx, MathHelper.PiOver2, speed);
                return;
            }
            if (ctx.Held) {
                //刚离开悬停:只留沿身体轴向的速度,横向的跟随速度丢掉,否则头会横着拖过自己的脖子
                Vector2 facing = npc.rotation.ToRotationVector2();
                npc.velocity = facing * Vector2.Dot(npc.velocity, facing);
            }
            Ballistic(ctx);
            //空中把横向速度慢慢收掉,落点不至于飘得太远
            npc.velocity.X *= 0.985f;
        }

        /// <summary>与目标保持相对静止,整条身体随头部平移</summary>
        protected static void HoldAt(SandseaStateContext ctx, Vector2 offset, float stiffness = 0.12f, float lerp = 0.3f, float maxSpeed = SandseaDirector.BarrageHoldSpeed) {
            ctx.Mode = SandseaMoveMode.HoldRelative;
            ctx.HoldOffset = offset;
            ctx.HoldStiffness = stiffness;
            ctx.HoldLerp = lerp;
            ctx.HoldMaxSpeed = maxSpeed;
        }

        /// <summary>从 from 起跳、弧顶高 apex、下落段穿过 through 的弹道初速,横向速度封顶,够不着时落在半路</summary>
        protected static Vector2 LeapVelocity(Vector2 from, Vector2 through, float apex, float gravity, float maxHorizontal) {
            float rise = Math.Max(0f, from.Y - through.Y);
            float vy = MathF.Sqrt(2f * gravity * apex);
            float tUp = vy / gravity;
            float tDown = MathF.Sqrt(2f * Math.Max(0f, apex - rise) / gravity);
            float vx = (through.X - from.X) / Math.Max(1f, tUp + tDown);
            return new Vector2(MathHelper.Clamp(vx, -maxHorizontal, maxHorizontal), -vy);
        }
        #endregion

        #region 预告声明
        protected static void DeclareLane(SandseaStateContext ctx, Vector2 start, Vector2 dir, float length, float halfWidth, float strength) {
            ctx.LaneStart = start;
            ctx.LaneDir = dir;
            ctx.LaneLength = length;
            ctx.LaneHalfWidth = halfWidth;
            ctx.LaneStrength = strength;
        }

        protected static void DeclareArc(SandseaStateContext ctx, Vector2 start, Vector2 velocity, float gravity, float strength) {
            ctx.ArcStart = start;
            ctx.ArcVelocity = velocity;
            ctx.ArcGravity = gravity;
            ctx.ArcStrength = strength;
        }

        /// <summary>破沙预兆:x 处的沙面持续喷涌</summary>
        protected static void DeclareOmen(SandseaStateContext ctx, float x, float strength) {
            ctx.OmenPos = new Vector2(x, SurfaceAt(ctx, x));
            ctx.OmenStrength = strength;
        }
        #endregion
    }
}
