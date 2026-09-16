using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 转阶段:低速漂浮 + 整条链每节每帧一颗虚空粒子,把外壳烧掉换成二阶段形态。
    /// <para>
    /// <b>本状态不用自己的计时,也不自己收招。</b>计数器是宿主的 <c>phaseTrans</c>(0~122),
    /// 进入与退出都由宿主的全局转移裁决:原代码是每帧强制 <c>ai = PhaseTransing</c>,
    /// 到 122 帧再直写 <c>ai = VoidSpike</c>,既不清 <c>changeCounter</c> 也不走选招口。
    /// 这里保持同一分工,所以 <c>OnUpdate</c> 永远返回 null。
    /// </para>
    /// <para>
    /// 运动与粒子在原代码里写在全局块(紧挨着 SpaceCenter 那几行),
    /// 功能上是本状态的运动,迁移时搬进状态文件;执行顺序不受影响(同一帧只有一个状态在跑)
    /// </para>
    /// </summary>
    [VaultState((int)CruiserStateIndex.PhaseTransing, typeof(CruiserStateContext))]
    public class CruiserPhaseTransingState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.PhaseTransing;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            if (npc.velocity.Length() < CruiserDirector.PhaseTransSpeedFloor) {
                npc.velocity *= CruiserDirector.PhaseTransAccel;
            }
            else {
                npc.velocity *= CruiserDirector.PhaseTransDrag;
            }

            if (!Main.dedServ && ctx.Owner != null) {
                //每骨节每帧一颗,节数多时能堆几百颗,对齐原转场密度
                foreach (Vector2 p in ctx.Owner.bodies) {
                    PRT_Void vpt = PRTLoader.NewParticle<PRT_Void>(p,
                        CEUtils.randomPointInCircle(CruiserDirector.PhaseTransParticleScatter), Color.White, 1f);
                    vpt.Opacity = Main.rand.NextFloat(CruiserDirector.PhaseTransParticleOpacityMin, CruiserDirector.PhaseTransParticleOpacityMax);
                    vpt.shape = 4;
                }
            }
            return null;
        }
    }
}
