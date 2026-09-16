using CalamityEntropy.Content.NPCs.Apsychos.Core;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.Apsychos.States
{
    /// <summary>
    /// 冲刺:前 30 帧刹速涨描边,点火后 20 帧推进(双侧喷口尘埃 + 闪屏),再 20 帧收尾。
    /// 描边自管,关掉默认衰减。喷火音只在 num1 落到 1 的那一帧响,中途加入不会补放
    /// </summary>
    [VaultState((int)ApsychosStateIndex.Dash, typeof(ApsychosStateContext))]
    public class ApsychosDashState : ApsychosStateBase
    {
        public override ApsychosStateIndex StateIndex => ApsychosStateIndex.Dash;

        public override IVaultState<ApsychosStateContext> OnUpdate(ApsychosStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;

            ctx.DecayOutline = false;
            ctx.TailStyle = ApsychosTailStyle.Follow;

            if (Timer < ApsychosDirector.DashWindupFrames) {
                npc.velocity *= ApsychosDirector.DashWindupDrag;
                float targetRot = (player.Center - npc.Center).ToRotation();
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.DashWindupRotate, false);
                ctx.Outline += ApsychosDirector.DashOutlineRise;
                if (ctx.Outline > 1f) {
                    ctx.Outline = 1f;
                }
            }

            float ignite = ApsychosDirector.DashIgniteBase / enrange;
            if (Timer > ignite) {
                if (Timer < ignite + ApsychosDirector.DashThrustFrames) {
                    if (ctx.Num1 <= ApsychosDirector.DashPlumeFrames) {
                        ctx.Num1 += 1f;
                        if (!Main.dedServ) {
                            //一次性拍:num1 落到 1 才响。中途加入时 num1 已经大于 1,不会补放
                            if (ctx.Num1 == 1f) {
                                CEUtils.PlaySound("flamethrower end", 1, npc.Center);
                            }
                            CalamityEntropy.FlashEffectStrength = ApsychosDirector.DashFlash;
                            if (Main.LocalPlayer.ZoneUnderworldHeight) {
                                CalamityEntropy.FlashEffectStrength = ApsychosDirector.DashFlashUnderworld;
                            }
                        }
                    }
                    npc.velocity *= ApsychosDirector.DashThrustDrag;
                    npc.velocity += npc.rotation.ToRotationVector2() * ApsychosDirector.DashThrust * enrange;
                    SpawnTrail(ctx, npc);
                }
            }
            if (Timer > ignite + ApsychosDirector.DashThrustFrames) {
                npc.velocity *= ApsychosDirector.DashRecoverDrag;
                ctx.Outline -= ApsychosDirector.DashOutlineFall;
                if (ctx.Outline < 0f) {
                    ctx.Outline = 0f;
                }
            }
            if (Timer > ignite + ApsychosDirector.DashRecoverFrames) {
                return NextAttack(ctx);
            }
            return null;
        }

        /// <summary>
        /// 双侧喷口:FlameBurst 尘埃 + PRT_Smoke 双通道,isp 从 0 到 1 步长 0.05。
        /// 一阶段暖橙,二阶段冷蓝。纯演出,各端各自生成
        /// </summary>
        private static void SpawnTrail(ApsychosStateContext ctx, NPC npc) {
            if (Main.dedServ) {
                return;
            }
            Color smoke = ctx.Phase == 1 ? new Color(255, 160, 140) : new Color(120, 120, 255);
            for (float isp = 0f; isp < 1f; isp += ApsychosDirector.DashTrailStep) {
                Vector2 sample = npc.Center + npc.velocity * isp;
                SpawnNozzleDust(npc, sample, new Vector2(ApsychosDirector.DashNozzleBack, ApsychosDirector.DashNozzleSide));
                SpawnNozzleDust(npc, sample, new Vector2(ApsychosDirector.DashNozzleBack, -ApsychosDirector.DashNozzleSide));
                var p = PRTLoader.NewParticle<PRT_Smoke>(sample, Vector2.Zero, smoke, ApsychosDirector.DashSmokeScale);
                p.scaleStart = ApsychosDirector.DashSmokeScaleStart;
                p.scaleEnd = 0f;
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), ApsychosDirector.DashSmokeLife);
            }
        }

        private static void SpawnNozzleDust(NPC npc, Vector2 sample, Vector2 localOffset) {
            var d = Dust.NewDustDirect(npc.Center, 0, 0, DustID.FlameBurst);
            d.position = sample + localOffset.RotatedBy(npc.rotation) * npc.scale;
            d.noGravity = true;
            d.scale = ApsychosDirector.DashDustScale;
            d.velocity = npc.velocity * ApsychosDirector.DashDustVelFactor + CEUtils.randomPointInCircle(ApsychosDirector.DashDustScatter);
        }
    }
}
