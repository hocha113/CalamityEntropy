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

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;

            //自增后的值参与第二个判据,所以这里必须先自增再比较(原 localAI[2]++ 的写法)
            int aimBefore = ctx.LaserAim;
            ctx.LaserAim++;
            if (aimBefore < CruiserDirector.LaserAimFrames)
            {
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation,
                    (player.Center + player.velocity * CruiserDirector.LaserAimLeadFrames * CruiserDirector.LaserLeadFactor - npc.Center).ToRotation(),
                    CruiserDirector.LaserAimRotateRate, false);
                npc.velocity *= CruiserDirector.LaserAimDrag;
                npc.velocity += npc.rotation.ToRotationVector2() * CruiserDirector.LaserAimBackThrust;
            }
            if (ctx.LaserAim > CruiserDirector.LaserActiveFrom)
            {
                int u = (int)Utils.Remap(
                    CruiserDirector.LaserCycle * (int)(ctx.ChangeCounter / (float)CruiserDirector.LaserCycle),
                    0, CruiserDirector.LaserCycles * CruiserDirector.LaserCycle,
                    CruiserDirector.LaserLeadHigh, CruiserDirector.LaserLeadLow);

                if (ctx.ChangeCounter % CruiserDirector.LaserCycle == 0)
                {
                    if (ctx.ChangeCounter > 1)
                    {
                        npc.rotation = (player.Center + player.velocity * u * CruiserDirector.LaserLeadFactor - npc.Center).ToRotation();
                    }
                    npc.velocity = npc.rotation.ToRotationVector2();
                    MarkNetUpdate(ctx);
                    if (!Main.dedServ)
                    {
                        //每轮双层预告粒子,lifetime 传 -1 靠手动删,跟 46 tick 的激光帧对齐
                        PRTLoader.NewParticle<PRT_CruiserWarn>(npc.Center, Vector2.Zero, Color.White, CruiserDirector.LaserWarnScaleBig)
                            .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, npc.rotation, -1);
                        PRTLoader.NewParticle<PRT_CruiserWarn>(npc.Center, Vector2.Zero, Color.White, CruiserDirector.LaserWarnScaleSmall)
                            .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, npc.rotation, -1);
                    }
                }
                if (ctx.ChangeCounter % CruiserDirector.LaserCycle == u)
                {
                    Shoot(ctx, ModContent.ProjectileType<CruiserLaser2>(), npc.Center,
                        npc.rotation.ToRotationVector2() * CruiserDirector.LaserBeamSpeed, ai0: npc.whoAmI);
                    //冲刺是运动,各端都跑;上面的弹幕生成只在权威端
                    npc.velocity = npc.rotation.ToRotationVector2()
                        * ((CEUtils.getDistance(npc.Center, player.Center) + CruiserDirector.LaserDashDistanceBonus)
                            / (CruiserDirector.LaserDashDivisorBase - u));
                }
                if (ctx.ChangeCounter % CruiserDirector.LaserCycle == CruiserDirector.LaserCycleBrakeFrame)
                {
                    npc.velocity = npc.velocity.normalize() * CruiserDirector.LaserCycleBrakeSpeed;
                }
                ctx.ChangeCounter++;
                if (ctx.ChangeCounter >= CruiserDirector.LaserCycles * CruiserDirector.LaserCycle)
                {
                    //瞄准窗计时在收招这一处清零(原代码同一位置),所以下一次激光又从瞄准窗起跑
                    ctx.LaserAim = 0;
                    return NextAttack(ctx);
                }
            }
            return null;
        }

        /// <summary>超时兜底也要把瞄准窗计时清掉,否则下一次激光会跳过瞄准窗直接开火</summary>
        protected override IVaultState<CruiserStateContext> OnTimeout(CruiserStateContext ctx)
        {
            ctx.LaserAim = 0;
            return base.OnTimeout(ctx);
        }
    }
}
