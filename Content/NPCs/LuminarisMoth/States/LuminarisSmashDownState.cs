using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 高空砸落,四轮。轮内进度是 <c>ac = 倒计时 % 100 + 1</c>:
    /// <list type="bullet">
    /// <item>ac 100 → 40:抬到「玩家预判位置的上方 520(再随机 ±80)」,速度清零</item>
    /// <item>ac 39:起砸拍,闪屏并清空尾迹重采</item>
    /// <item>ac 39 → 1:自由落体(每帧竖直加速 2),ac &lt; 34 时每隔几帧向左右各撒一发星弹</item>
    /// <item>ac 39 → 11:开大尾迹。<b>接触伤害只在大尾迹亮着时生效</b>(见宿主 <c>CanHitPlayer</c>),所以只有砸落段能撞人</item>
    /// </list>
    /// <para>
    /// 取模的副作用:第一帧(倒计时 400)折出的是 ac = 1、最后一帧(-1)折出 ac = 0,
    /// 于是首尾各多一帧自由落体,并且 ac = 0 那一帧还会满足 <c>ac % k == 0</c> 再打一次左右撒弹。原代码如此
    /// </para>
    /// </summary>
    [VaultState((int)LuminarisStateIndex.SmashDown, typeof(LuminarisStateContext))]
    public class LuminarisSmashDownState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.SmashDown;

        /// <summary>
        /// 起砸拍的本地锁存,按轮号记(一段里有四轮),不过线。
        /// 慢半拍的客户端在宽限窗内仍补这一拍,越过就静默
        /// </summary>
        private int launchCuedCycle;

        public override void OnEnter(LuminarisStateContext ctx) {
            base.OnEnter(ctx);
            launchCuedCycle = int.MinValue;
        }

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;
            int c = ctx.Countdown;
            int ac = c % LuminarisDirector.SmashDownCycleFrames + 1;
            //轮号只用来给起砸拍的锁存分轮,不参与任何运动数学
            int cycle = c / LuminarisDirector.SmashDownCycleFrames;

            if (ac == LuminarisDirector.SmashDownCycleFrames) {
                ctx.Vec1 = npc.Center;
                if (IsServer) {
                    //抬升落点的高度抖动吃随机数,它整轮都在驱动位置,所以骰点收在权威端、结果靠 Vec2 过线
                    ctx.Vec2 = player.Center + player.velocity * LuminarisDirector.SmashDownLeadFrames
                        + new Vector2(0, LuminarisDirector.SmashDownRiseHeight + Main.rand.NextFloat(-LuminarisDirector.SmashDownRiseJitter, LuminarisDirector.SmashDownRiseJitter));
                    MarkNetUpdate(ctx);
                }
            }
            if (ac >= LuminarisDirector.SmashDownRiseEndFrame) {
                npc.velocity *= 0;
                npc.rotation = 0;
                npc.Center = Vector2.Lerp(ctx.Vec1, ctx.Vec2,
                    CEUtils.GetRepeatedCosFromZeroToOne(Utils.Remap(ac, LuminarisDirector.SmashDownCycleFrames, LuminarisDirector.SmashDownRiseEndFrame, 0, 1), 1));
            }
            else {
                npc.velocity.Y += LuminarisDirector.SmashDownGravity;
                if (launchCuedCycle != cycle && ac <= LuminarisDirector.SmashDownLaunchFrame) {
                    launchCuedCycle = cycle;
                    if (!CountdownCuePassed(ac, LuminarisDirector.SmashDownLaunchFrame)) {
                        if (!Main.dedServ) {
                            CalamityEntropy.FlashEffectStrength = LuminarisDirector.SmashDownLaunchFlash;
                        }
                        CEUtils.PlaySound("flamethrower end", 1, npc.Center);
                        ctx.Trail.Clear();
                        ctx.Trail.Add(npc.Center);
                    }
                }
                if (ac < LuminarisDirector.SmashDownShootStartFrame) {
                    if (ac % (int)(LuminarisDirector.SmashDownShootIntervalBase / enrange) == 0) {
                        Shoot<LuminarisAstralShoot>(ctx, npc.Center, new Vector2(LuminarisDirector.SmashDownShotSpeedX, 0) * enrange,
                            1, (-Vector2.UnitY).ToRotation(), enrange * LuminarisDirector.SmashDownShotGravity, LuminarisDirector.SmashDownShotGravityDelayBase / enrange);
                        Shoot<LuminarisAstralShoot>(ctx, npc.Center, new Vector2(-LuminarisDirector.SmashDownShotSpeedX, 0) * enrange,
                            1, (-Vector2.UnitY).ToRotation(), enrange * LuminarisDirector.SmashDownShotGravity, LuminarisDirector.SmashDownShotGravityDelayBase / enrange);
                    }
                }
                if (ac > LuminarisDirector.SmashDownTrailFrame) {
                    ctx.MegaTrail = LuminarisDirector.SmashDownTrailStrength;
                }
            }

            return Tick(ctx, c);
        }
    }
}
