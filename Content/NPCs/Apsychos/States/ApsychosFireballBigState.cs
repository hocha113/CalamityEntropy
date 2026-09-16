using CalamityEntropy.Content.NPCs.Apsychos.Core;
using CalamityEntropy.Content.Projectiles.ApsychosProjs;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.States
{
    /// <summary>
    /// 巨型火球:蓄力帧数 = 65 - 阶段×15,蓄满即发,num2 超过 3 收招(实际 4 发)。
    /// 发射是 <c>num1 &gt;= 阈值</c> 后立刻把 num1 归零,所以拍点用宽限窗防中途加入补放音效
    /// </summary>
    [VaultState((int)ApsychosStateIndex.FireballBig, typeof(ApsychosStateContext))]
    public class ApsychosFireballBigState : ApsychosStateBase
    {
        public override ApsychosStateIndex StateIndex => ApsychosStateIndex.FireballBig;

        public override IVaultState<ApsychosStateContext> OnUpdate(ApsychosStateContext ctx) {
            NPC npc = ctx.Npc;
            NPC tail = ctx.Tail;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;

            ctx.TailStyle = ApsychosTailStyle.OnePoint;
            float targetRot = (player.Center - npc.Center).ToRotation();
            npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.BigRotate, false);
            npc.velocity *= ApsychosDirector.BigDrag;
            npc.velocity += npc.rotation.ToRotationVector2() * ApsychosDirector.BigThrust;

            ctx.Num1++;
            float charge = ApsychosDirector.BigChargeBase - ctx.Phase * ApsychosDirector.BigChargePerPhase;
            if (ctx.Num1 < charge) {
                tail.Center = Vector2.Lerp(tail.Center, npc.Center + npc.rotation.ToRotationVector2() * ApsychosDirector.BigTailReach * npc.scale, ApsychosDirector.BigTailLerp * enrange);
                ctx.TailLight += ApsychosDirector.BigTailLightRise;
            }
            if (ctx.Num1 >= charge) {
                bool replay = CuePassed(ctx.Num1, (int)charge);
                ctx.Num1 = 0f;
                ctx.TailLight = 0f;
                ctx.Num2++;
                //后坐各端都写;音效中途加入静默;弹幕只在权威端
                tail.velocity -= tail.rotation.ToRotationVector2() * ApsychosDirector.BigRecoil;
                if (!replay) {
                    CEUtils.PlaySound("YharonFireball1", 0.9f, npc.Center);
                    CEUtils.PlaySound("YharonFireball1", 0.9f, npc.Center);
                }
                Shoot<ApsychosFireballBig>(ctx, tail.Center, tail.rotation.ToRotationVector2() * (ApsychosDirector.BigSpeedBase + ApsychosDirector.BigSpeedPerPhase * ctx.Phase) * enrange,
                    ApsychosDirector.BigDamageMult, 0f, ctx.Phase);
                MarkNetUpdate(ctx);
            }
            if (ctx.Num2 > ApsychosDirector.BigShotCount) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
