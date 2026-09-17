using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.PRT;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 虚空激光:瞄准窗 + 六轮定点扫射。
    /// <para>
    /// 本状态有<b>两个</b>计时:瞄准窗用 <see cref="CruiserStateContext.LaserAim"/>(原 <c>NPC.localAI[2]</c>),
    /// 六轮扫射用 <see cref="CruiserStateContext.ChangeCounter"/>,而且 ChangeCounter <b>在瞄准窗期间完全不推进</b>。
    /// 原代码的判据是 <c>LaserAim++ &lt; 35</c> 与自增后的 <c>&gt; 36</c>,
    /// 于是第 36 帧(0 基)两边都不进,是一个空帧。三条都照搬。
    /// </para>
    /// <para>
    /// 每轮 46 帧:第 0 帧重锁朝向并放两颗预告粒子(速度归一化到 1,近乎停住,预告很好读);
    /// 第 u 帧开火,同时本体顺着光束冲出 <c>(距离 + 1400) / (45 - u)</c>;第 45 帧刹回速度 4。
    /// u 按轮次从 42 收到 18,所以越往后预告越短
    /// </para>
    /// <para>本状态期间宿主<b>不</b>把 rotation 覆写成速度朝向,朝向是自管量(原代码的 <c>ai != VoidLaser</c> 门)</para>
    /// </summary>
    [VaultState((int)CruiserStateIndex.VoidLaser, typeof(CruiserStateContext))]
    public class CruiserVoidLaserState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.VoidLaser;

        /// <summary>
        /// 本地一次性拍锁存,不过线。
        /// <para>
        /// <b>为什么不能直接写 <c>ChangeCounter % 46 == K</c></b>:ChangeCounter 现在随快照过线并带
        /// ±2 容差收养,收养会让本地值一步跳到权威端的值,于是任何等值判定都可能被跨过或重放。
        /// 这三拍每一拍都<b>直写 velocity</b>(归一化到 1 / 冲刺 / 刹回 4),是各端都要跑的运动数学,
        /// 漏一拍就是几十到几百像素的当帧分叉。所以改成「轮次单调 + 拍内闩锁 + 区间判定」:
        /// 轮次由 <c>ChangeCounter / 46</c> 推出(单调不回退),拍由 <c>beat &gt;= 阈值</c> 加闩锁保证只放一次。
        /// 权威端 ChangeCounter 每帧 +1,<c>轮次</c>恰在 <c>cc % 46 == 0</c> 跳变、<c>beat</c> 恰在阈值那一帧
        /// 首次成立,与原等值判定逐帧等价
        /// </para>
        /// </summary>
        private int cycleStarted = -1;
        private bool firedThisCycle;
        private bool brakedThisCycle;

        public override void OnEnter(CruiserStateContext ctx) {
            base.OnEnter(ctx);
            cycleStarted = -1;
            firedThisCycle = false;
            brakedThisCycle = false;
        }

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;

            //自增后的值参与第二个判据,所以这里必须先自增再比较(原 localAI[2]++ 的写法)
            int aimBefore = ctx.LaserAim;
            ctx.LaserAim++;
            if (aimBefore < CruiserDirector.LaserAimFrames) {
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation,
                    (player.Center + player.velocity * CruiserDirector.LaserAimLeadFrames * CruiserDirector.LaserLeadFactor - npc.Center).ToRotation(),
                    CruiserDirector.LaserAimRotateRate, false);
                npc.velocity *= CruiserDirector.LaserAimDrag;
                npc.velocity += npc.rotation.ToRotationVector2() * CruiserDirector.LaserAimBackThrust;
            }
            if (ctx.LaserAim > CruiserDirector.LaserActiveFrom) {
                //轮次单调推出,不用等值判定;beat 是轮内帧号
                int cycle = ctx.ChangeCounter / CruiserDirector.LaserCycle;
                int beat = ctx.ChangeCounter % CruiserDirector.LaserCycle;
                int u = (int)Utils.Remap(
                    CruiserDirector.LaserCycle * cycle,
                    0, CruiserDirector.LaserCycles * CruiserDirector.LaserCycle,
                    CruiserDirector.LaserLeadHigh, CruiserDirector.LaserLeadLow);

                //原 cc % 46 == 0:重锁朝向 + 速度归一化到 1(近乎停住,预告好读)
                if (cycle > cycleStarted) {
                    cycleStarted = cycle;
                    firedThisCycle = false;
                    brakedThisCycle = false;
                    //中途加入且已越过本轮起拍很久:静默吞掉这一拍的演出,只认闩锁
                    if (!CuePassed(beat, 0)) {
                        if (ctx.ChangeCounter > 1) {
                            npc.rotation = (player.Center + player.velocity * u * CruiserDirector.LaserLeadFactor - npc.Center).ToRotation();
                        }
                        npc.velocity = npc.rotation.ToRotationVector2();
                        MarkNetUpdate(ctx);
                        if (!Main.dedServ) {
                            //每轮双层预告粒子,lifetime 传 -1 靠手动删,跟 46 tick 的激光帧对齐
                            PRTLoader.NewParticle<PRT_CruiserWarn>(npc.Center, Vector2.Zero, Color.White, CruiserDirector.LaserWarnScaleBig)
                                .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, npc.rotation, -1);
                            PRTLoader.NewParticle<PRT_CruiserWarn>(npc.Center, Vector2.Zero, Color.White, CruiserDirector.LaserWarnScaleSmall)
                                .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, npc.rotation, -1);
                        }
                    }
                }
                //原 cc % 46 == u:开火并顺着光束冲出去
                if (!firedThisCycle && beat >= u) {
                    firedThisCycle = true;
                    if (!CuePassed(beat, u)) {
                        Shoot(ctx, ModContent.ProjectileType<CruiserLaser2>(), npc.Center,
                            npc.rotation.ToRotationVector2() * CruiserDirector.LaserBeamSpeed, ai0: npc.whoAmI);
                        //冲刺是运动,各端都跑;上面的弹幕生成只在权威端
                        npc.velocity = npc.rotation.ToRotationVector2()
                            * ((CEUtils.getDistance(npc.Center, player.Center) + CruiserDirector.LaserDashDistanceBonus)
                                / (CruiserDirector.LaserDashDivisorBase - u));
                    }
                }
                //原 cc % 46 == 45:刹回速度 4。beat 上限就是 45,所以这里只需要闩锁
                if (!brakedThisCycle && beat >= CruiserDirector.LaserCycleBrakeFrame) {
                    brakedThisCycle = true;
                    npc.velocity = npc.velocity.normalize() * CruiserDirector.LaserCycleBrakeSpeed;
                }
                ctx.ChangeCounter++;
                if (ctx.ChangeCounter >= CruiserDirector.LaserCycles * CruiserDirector.LaserCycle) {
                    ResetAim(ctx);
                    return NextAttack(ctx);
                }
            }
            return null;
        }

        /// <summary>超时兜底也要把瞄准窗计时清掉,否则下一次激光会跳过瞄准窗直接开火</summary>
        protected override IVaultState<CruiserStateContext> OnTimeout(CruiserStateContext ctx) {
            ResetAim(ctx);
            return base.OnTimeout(ctx);
        }

        /// <summary>
        /// 瞄准窗计时清零(原代码在收招那一处清)。<b>只在权威端清</b>:
        /// 客户端的换态要等包,若本地先把 LaserAim 归零,在等包的那几帧它会重新落进瞄准窗分支,
        /// 对着已经收招的招式继续跑「追瞄 + 刹速 + 后退推力」,那是客户端独有的一段运动分叉。
        /// LaserAim 随 ExtraAI 过线且差值远超容差,客户端会在换态同包里直接采用权威端的 0
        /// </summary>
        private static void ResetAim(CruiserStateContext ctx) {
            if (IsServer) {
                ctx.LaserAim = 0;
            }
        }
    }
}
