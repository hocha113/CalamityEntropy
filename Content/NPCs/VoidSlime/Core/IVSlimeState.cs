using CalamityEntropy.Content.NPCs.VoidSlime.States;
using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.VoidSlime.Core
{
    /// <summary>写入 ai[3],0~9 演出与连接段,10 起按形态分段是招式</summary>
    public enum VSlimeStateIndex
    {
        /// <summary>苍白史莱姆从高空坠落登场</summary>
        Entrance = 0,
        /// <summary>苍白形态的连接段</summary>
        PaleHub = 1,
        /// <summary>苍白形态锁血后膨胀爆开,露出虚空史莱姆</summary>
        PaleBurst = 2,
        /// <summary>虚空与奇点形态的连接段,跃迁到下一招的锚点</summary>
        Hub = 3,
        /// <summary>终幕,黑洞缩成白点后爆闪</summary>
        Death = 4,
        /// <summary>脱战离场</summary>
        Despawn = 5,
        /// <summary>50% 血量转奇点形态</summary>
        SingularityShift = 6,
        /// <summary>20% 血量转死兆星</summary>
        DeathStarShift = 7,

        /// <summary>正常跳跃:连续跳跃逼近 8 秒</summary>
        PaleHop = 10,
        /// <summary>传送凝胶球:闪到头顶砸落,弹出两圈灰色凝胶球</summary>
        PaleTeleportSlam = 11,
        /// <summary>史莱姆猛击:蓄力高跳再砸向锁定落点,落地出冲击波</summary>
        PaleSmash = 12,

        /// <summary>引力星环:四圈暗物质球,三轮瞄准凝胶球,最后整圈爆发</summary>
        GravityRings = 20,
        /// <summary>星体爆炸:虫洞悬顶,四颗大辉星依次扔向玩家两侧爆开</summary>
        StellarBlast = 21,
        /// <summary>黑洞爆发:化作慢速逼近的红色大黑洞,圈沿的残骸被吸向它</summary>
        BlackHoleBurst = 22,
        /// <summary>凝胶喷射:虫洞悬顶向上喷凝胶球,缓慢落下</summary>
        GelSpray = 23,
        /// <summary>晦暗星雨:向天发射伽玛射线,随后斜落星雨</summary>
        GloomStarRain = 24,

        /// <summary>引力融合:分成两个小黑洞夹着玩家对撞,连续四次</summary>
        GravityFusion = 30,
        /// <summary>无尽之噬:画出五角星,圈心黑洞吸入旋臂星群,收尾两道旋转射线</summary>
        EndlessDevour = 31,
        /// <summary>星河重击:头顶排出一行凝胶球后下砸,连续四次</summary>
        GalaxySmash = 32,
        /// <summary>超星系轰炸:退入背景,十字星从深处逼近后在玩家层爆开</summary>
        Supercluster = 33,

        /// <summary>主序星:蓝恒星十轮放射激光</summary>
        MainSequence = 40,
        /// <summary>红巨星:全场爆发冲击,只有图腾内安全</summary>
        RedGiant = 41,
        /// <summary>超新星:更密的十字星轰炸</summary>
        Supernova = 42,
        /// <summary>奇点:蓝色黑洞持续扩大,血量打空后收缩</summary>
        Singularity = 43,
    }

    /// <summary>
    /// 拍点都是 Timer 的区间,不设会清零 Timer 的拍子枚举,客户端只靠过线的 Timer 就能重放
    /// 弹幕、锁向和骰点只在权威端做,运动各端都跑
    /// </summary>
    public abstract class VSlimeStateBase : CEBossStateBase<VSlimeStateContext>
    {
        public override int StateId => (int)StateIndex;
        public abstract VSlimeStateIndex StateIndex { get; }
        public override string StateName => StateIndex.ToString();
        public override int TimeoutFrames => VSlimeDirector.AttackTimeoutFrames;

        /// <summary>本招起手时本体想待的位置,连接段跃迁过去,返回前已钳进引力场</summary>
        public virtual Vector2 AnchorFor(VSlimeStateContext ctx) => SideAnchor(ctx);

        /// <summary>连接段选中本招那一帧在权威端调一次</summary>
        public virtual void OnPicked(VSlimeStateContext ctx) {
        }

        /// <summary>上一帧的 Timer,拍点按越过判定,客户端收养计时跳帧也不漏拍</summary>
        private int lastTimer = -1;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            lastTimer = -1;
        }

        public sealed override IVaultState<VSlimeStateContext> OnUpdate(VSlimeStateContext ctx) {
            IVaultState<VSlimeStateContext> next = Run(ctx);
            lastTimer = Timer;
            return next;
        }

        /// <summary>状态本体,返回非 null 就换态</summary>
        protected abstract IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx);

        protected override IVaultState<VSlimeStateContext> OnTimeout(VSlimeStateContext ctx) {
            if (!IsServer) {
                return null;
            }
            ctx.Npc.velocity *= 0.5f;
            return EndAttack(ctx);
        }

        /// <summary>本帧 Timer 刚越过 beat</summary>
        protected bool At(int beat) => lastTimer < beat && Timer >= beat;

        /// <summary>按形态回到对应的连接段</summary>
        protected static IVaultState<VSlimeStateContext> EndAttack(VSlimeStateContext ctx)
            => ctx.Phase <= VSlimeForm.Pale ? new VSlimePaleHubState() : new VSlimeHubState();

        /// <summary>服务端生成敌对弹幕,伤害传普通档基值,客户端返回 -1</summary>
        protected static int Shoot<T>(VSlimeStateContext ctx, Vector2 pos, Vector2 vel, int normalBase, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
            where T : ModProjectile {
            if (!IsServer) {
                return -1;
            }
            return Projectile.NewProjectile(ctx.Npc.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<T>(), VoidSlime.ProjDamage(normalBase), 0f,
                Main.myPlayer, ai0, ai1, ai2);
        }

        /// <summary>本招第 sequence 次骰点用的随机源,种子过线,各端算出同一组数</summary>
        protected static UnifiedRandom Rng(VSlimeStateContext ctx, int sequence)
            => CEBossHost.SeededRandom(ctx.Npc, 0, ctx.RollSeed + sequence * 7919);

        protected static void Hover(VSlimeStateContext ctx, Vector2 dest, float maxSpeed, float accel = 0.1f, float slowRadius = 120f) {
            ctx.Mode = VSlimeMoveMode.HoverTo;
            ctx.MoveTarget = dest;
            ctx.MoveSpeed = maxSpeed;
            ctx.Accel = accel;
            ctx.SlowRadius = slowRadius;
        }

        protected static void HoldAt(VSlimeStateContext ctx, Vector2 offset, float stiffness = 0.12f, float lerp = 0.3f, float maxSpeed = 30f) {
            ctx.Mode = VSlimeMoveMode.HoldRelative;
            ctx.HoldOffset = offset;
            ctx.Stiffness = stiffness;
            ctx.Accel = lerp;
            ctx.MoveSpeed = maxSpeed;
        }

        protected static void Direct(VSlimeStateContext ctx) {
            ctx.Mode = VSlimeMoveMode.Direct;
        }

        /// <summary>苍白形态的落地物理,速度由状态自己写</summary>
        protected static void Ground(VSlimeStateContext ctx) {
            ctx.Mode = VSlimeMoveMode.Direct;
            ctx.GroundPhysics = true;
        }

        /// <summary>玩家旁边 30 格,侧向跟 SideDir</summary>
        protected static Vector2 SideAnchor(VSlimeStateContext ctx)
            => VSlimeDirector.ClampToArena(ctx, ctx.Target.Center + new Vector2(ctx.SideDir * VSlimeDirector.SideAnchorDistance, 0f));

        /// <summary>玩家头顶 height 像素</summary>
        protected static Vector2 OverheadAnchor(VSlimeStateContext ctx, float height)
            => VSlimeDirector.ClampToArena(ctx, ctx.Target.Center + new Vector2(0f, -height), 120f);

        /// <summary>入态时权威端换一个骰点种子并过线</summary>
        protected static void Reseed(VSlimeStateContext ctx) {
            if (IsServer) {
                ctx.RollSeed = Main.rand.Next(1, 1 << 24);
                MarkNetUpdate(ctx);
            }
        }
    }
}
