using CalamityEntropy.Content.NPCs.Apsychos.Core;
using CalamityEntropy.Content.Projectiles.ApsychosProjs;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.States
{
    /// <summary>
    /// 喷火:尾巴摆动瞄人,60 到 140 帧持续喷射。
    /// 原代码有一支「150 帧后渐弱」,但喷射窗 140 帧就结束了,那一支到不了,照搬保留
    /// </summary>
    [VaultState((int)ApsychosStateIndex.FlameThrow, typeof(ApsychosStateContext))]
    public class ApsychosFlameThrowState : ApsychosStateBase
    {
        public override ApsychosStateIndex StateIndex => ApsychosStateIndex.FlameThrow;

        public override IVaultState<ApsychosStateContext> OnUpdate(ApsychosStateContext ctx) {
            NPC npc = ctx.Npc;
            NPC tail = ctx.Tail;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;

            ctx.TailStyle = ApsychosTailStyle.TwoPoint;
            Vector2 tpos = npc.Center + new Vector2(ApsychosDirector.FlameTailSwayReach, (float)Math.Sin(Timer * ApsychosDirector.FlameSwayFreq) * ApsychosDirector.FlameSwayAmp).RotatedBy(npc.rotation);
            tail.Center = Vector2.Lerp(tail.Center, tpos, ApsychosDirector.FlameTailLerp);
            tail.rotation = CEUtils.RotateTowardsAngle(tail.rotation, (player.Center - tail.Center).ToRotation(), ApsychosDirector.FlameTailAimRate, false);
            ctx.TailLight += ApsychosDirector.FlameTailLightRise;
            float targetRot = (player.Center - npc.Center).ToRotation();
            npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.FlameRotate * enrange, false);
            npc.velocity *= ApsychosDirector.FlameDrag;
            npc.velocity += npc.rotation.ToRotationVector2() * ApsychosDirector.FlameThrust;
            tail.Center = Vector2.Lerp(tail.Center, npc.Center + npc.rotation.ToRotationVector2() * ApsychosDirector.FlameTailReach * npc.scale, ApsychosDirector.FlameTailHomeLerp * enrange);

            if (Timer > ApsychosDirector.FlameStartFrame && Timer < ApsychosDirector.FlameEndFrame) {
                tail.velocity += tail.rotation.ToRotationVector2() * ApsychosDirector.FlameTailRecoilPerFrame;
                float v = 1f;
                if (Timer < ApsychosDirector.FlameRampFrame) {
                    v = (Timer - ApsychosDirector.FlameStartFrame) / ApsychosDirector.FlameRampSpan;
                }
                if (Timer > ApsychosDirector.FlameFadeFrame) {
                    v = 1f - (Timer - ApsychosDirector.FlameFadeFrame) / ApsychosDirector.FlameRampSpan;
                }
                Shoot<ApsychosFire>(ctx, tail.Center + tail.rotation.ToRotationVector2() * ApsychosDirector.FlameMuzzleOffset,
                    tail.rotation.ToRotationVector2() * ApsychosDirector.FlameSpeed * v,
                    ApsychosDirector.FlameDamageMult,
                    ApsychosDirector.FlameLifeBase + (ctx.Phase - 1) * ApsychosDirector.FlameLifePerPhase,
                    ctx.Phase);
            }
            if (Timer > ApsychosDirector.FlameDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
