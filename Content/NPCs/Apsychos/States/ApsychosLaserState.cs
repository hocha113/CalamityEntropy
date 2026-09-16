using CalamityEntropy.Content.NPCs.Apsychos.Core;
using CalamityEntropy.Content.Projectiles.ApsychosProjs;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.States
{
    /// <summary>
    /// 激光:开局那一帧发射,尾巴指向本体正前方 800,350 帧慢速扫场。
    /// 后坐是运动所以各端都写;弹幕只在权威端。中途加入越过宽限窗就不再补后坐
    /// </summary>
    [VaultState((int)ApsychosStateIndex.Laser, typeof(ApsychosStateContext))]
    public class ApsychosLaserState : ApsychosStateBase
    {
        public override ApsychosStateIndex StateIndex => ApsychosStateIndex.Laser;

        /// <summary>本地一次性拍锁存,不过线。中途加入在宽限窗内仍补后坐,弹幕由权威端生成</summary>
        private bool laserCued;

        public override void OnEnter(ApsychosStateContext ctx) {
            base.OnEnter(ctx);
            laserCued = false;
        }

        public override IVaultState<ApsychosStateContext> OnUpdate(ApsychosStateContext ctx) {
            NPC npc = ctx.Npc;
            NPC tail = ctx.Tail;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;

            ctx.TailStyle = ApsychosTailStyle.TwoPoint;
            Vector2 tpos = npc.Center + new Vector2(ApsychosDirector.LaserTailSwayReach, (float)Math.Sin(Timer * ApsychosDirector.LaserSwayFreq) * ApsychosDirector.LaserSwayAmp).RotatedBy(npc.rotation);
            tail.Center = Vector2.Lerp(tail.Center, tpos, ApsychosDirector.LaserTailLerp);
            tail.rotation = (npc.Center + npc.rotation.ToRotationVector2() * ApsychosDirector.LaserAimReach - tail.Center).ToRotation();
            ctx.TailLight += ApsychosDirector.LaserTailLightRise;
            if (Timer < ApsychosDirector.LaserTrackFrames) {
                float targetRot = (player.Center - npc.Center).ToRotation();
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.LaserTrackRotate, false);
                npc.velocity *= ApsychosDirector.LaserTrackDrag;
                npc.velocity += npc.rotation.ToRotationVector2() * ApsychosDirector.LaserTrackThrust;
            }
            else {
                if (Timer > ApsychosDirector.LaserSlowTrackFrame) {
                    float targetRot = (player.Center - npc.Center).ToRotation();
                    npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.LaserSlowRotateLerp, false);
                    npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.LaserSlowRotateFixed, true);
                }
                npc.velocity *= ApsychosDirector.LaserDrag;
                npc.velocity += npc.rotation.ToRotationVector2() * ApsychosDirector.LaserBackThrust;
            }
            tail.Center = Vector2.Lerp(tail.Center, npc.Center + npc.rotation.ToRotationVector2() * ApsychosDirector.LaserTailReach * npc.scale, ApsychosDirector.LaserTailHomeLerp * enrange);
            if (!laserCued) {
                laserCued = true;
                if (!CuePassed(0)) {
                    tail.velocity += tail.rotation.ToRotationVector2() * ApsychosDirector.LaserRecoil;
                    Shoot<ApsychosLaser>(ctx, tail.Center + tail.rotation.ToRotationVector2() * ApsychosDirector.LaserMuzzleOffset,
                        tail.rotation.ToRotationVector2() * ApsychosDirector.LaserSpeed,
                        ApsychosDirector.LaserDamageMult, tail.whoAmI);
                    MarkNetUpdate(ctx);
                }
            }
            if (Timer > ApsychosDirector.LaserDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
