using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 75% 变形(132 帧):清弹;0-50 颤抖并后撤到 Z 1.2(反冲远离玩家,越来越小、雾化);50-84 在远处逐帧推进六帧变形图;
    /// 84 一记爆闪换二阶段贴图(天幕闪光 + 远处巨闪);84-118 以新形态俯冲回平面(落地冲击环);118-132 落地展翼。
    /// 「它退到远处重构,再以新形态扑回来」。收尾把阶段写 2,签名首招钉为轨道轰炸(再次退入深处开炮 = 新阶段的宣言)
    /// </summary>
    [VaultState((int)VDStateIndex.Transform, typeof(VDStateContext))]
    public class VDTransformState : VDStateBase
    {
        public override string StateName => "Transform";
        public override VDStateIndex StateIndex => VDStateIndex.Transform;
        public override bool ContactByDefault => false;
        public override bool RunsDuringBlink => true;
        public override int TimeoutFrames => int.MaxValue;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            ctx.QueuedChainState = -1;
            ctx.Npc.velocity = Vector2.Zero;
            VDVfx.ClearOwnProjectiles();
            MarkNetUpdate(ctx);
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            int t = Timer;
            DeclareDirect(ctx);
            npc.velocity *= 0.8f;
            DeclareAlpha(ctx, 1f, 1f);
            ctx.WingsVisible = t > VDDirector.TransformDiveEnd;

            if (t <= VDDirector.TransformRetreatEnd) {
                //后撤:颤抖加剧、核心亮起、退入深处
                float p = t / (float)VDDirector.TransformRetreatEnd;
                ctx.Depth = VDDirector.TransformRetreatDepth * VDDepth.RetreatCurve(p);
                ctx.TransformFrame = 0;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, p * p * 0.6f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, p * 0.6f);
                ctx.RimCharge = p * 0.6f;
                if (t == 1) {
                    VDVfx.Sound("VoidAnticipation", 0.9f, ctx.Owner.ProjectedCenter, 2);
                    if (ctx.TargetValid) {
                        npc.velocity += (npc.Center - ctx.Target.Center).SafeNormalize(-Vector2.UnitY) * 5f;
                    }
                }
            }
            else if (t <= VDDirector.TransformBurstFrame) {
                //远处重构:六帧变形图在 34 帧里推完,汇聚流越来越密
                ctx.Depth = VDDirector.TransformRetreatDepth;
                float p = (t - VDDirector.TransformRetreatEnd) / (float)(VDDirector.TransformBurstFrame - VDDirector.TransformRetreatEnd);
                ctx.TransformFrame = (int)MathHelper.Clamp(p * 6f, 0, 5);
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.4f + p * p * 0.5f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, 0.6f + 0.4f * p);
                ctx.RimCharge = 0.6f + 0.4f * p;
                if (!Main.dedServ && t % 3 == 0) {
                    ConvergeSparks(ctx, VDVfx.VoidPurple, 90f, 200f, 0.08f);
                    Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(1f, 3f);
                    VDVfx.VoidPuff(ctx.Owner.ProjectedCenter + CEUtils.randomPointInCircle(30f), v, 0.8f, 0.6f);
                }
                if (t == VDDirector.TransformBurstFrame) {
                    ctx.RimFlash = 1f;
                    if (!Main.dedServ) {
                        Vector2 shown = ctx.Owner.ProjectedCenter;
                        VDVfx.Sound("VoidAttack", 1f, shown, 2);
                        VDVfx.Shake(npc.Center, 10f);
                        VDVfx.SparkBurst(shown, VDVfx.VoidPurple, 40, 3f, 10f, 36, 0.5f, 1f);
                        VDVfx.Explosion(shown, 0.7f, 26);
                        //天幕:封锁力场整面亮起,冲击环从本体扩散
                        VDSkyDrive.PushFlash(VDDirector.SkyFlashBeat);
                    }
                }
            }
            else if (t <= VDDirector.TransformDiveEnd) {
                //以新形态俯冲回平面:落点就是自己现在的位置(变形期间不追玩家)
                ctx.TransformFrame = 5;
                DeclareDive(ctx, VDDirector.TransformRetreatDepth, t - VDDirector.TransformBurstFrame, VDDirector.TransformDiveEnd - VDDirector.TransformBurstFrame, npc.Center);
                ctx.RimCharge = 1f;
            }
            else {
                //落地展翼(阶段号要到收尾才写 2,这里继续钉在变形图末帧,免得闪回一阶段贴图)
                ctx.TransformFrame = 5;
                ctx.Depth = 0f;
                ctx.WingPulse = Math.Max(ctx.WingPulse, 1f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, 0.8f);
                if (t == VDDirector.TransformDiveEnd + 1) {
                    VDVfx.Sound("VoidAnticipation", 1.3f, npc.Center, 2, 0.8f);
                }
            }

            if (t >= VDDirector.TransformDuration) {
                ctx.Phase = 2;
                //签名首招:轨道轰炸;表指针跳过 0 号槽(那正是轨道轰炸,历史闸也会拦,这里只是把意图写明)
                ctx.ForcedNextState = (int)VDStateIndex.OrbitalStrike;
                ctx.AttackIndex = 1;
                MarkNetUpdate(ctx);
                return new VDHubState();
            }
            return null;
        }
    }
}
