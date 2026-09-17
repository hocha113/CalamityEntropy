using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 轨道轰炸(P2 阶段签名):本体 30 帧退入背景纵深(假 Z:缩小、冷色、无接触)→ 沿玩家移动方向标 4/5 个落点
    /// (标记 40 帧收缩,标记就是承诺)→ 虚空光柱按 8 帧错拍从屏顶砸落(P3 缓慢横扫)→ 立方曲线 20 帧俯冲回前景 + 冲击波。
    /// 深度只改绘制与接触窗,判定位置不动
    /// </summary>
    [VaultState((int)VDStateIndex.OrbitalStrike, typeof(VDStateContext))]
    public class VDOrbitalStrikeState : VDStateBase
    {
        public override string StateName => "OrbitalStrike";
        public override VDStateIndex StateIndex => VDStateIndex.OrbitalStrike;

        private enum Beat { Ascend, Mark, Fire, Return }
        private Beat beat;
        private int pillarsFired;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Ascend;
            pillarsFired = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            int count = VDDirector.OrbitalPillars(ctx.Phase);
            switch (beat) {
                case Beat.Ascend: {
                    DeclareHoverTo(ctx, ctx.Target.Center + VDDirector.OrbitalHoverOffset, 16f, 0.1f, 160f);
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.OrbitalAscendFrames, 0f, 1f);
                    ctx.FakeZ = 1f - MathF.Pow(1f - p, 3f);
                    if (Timer == 1) {
                        //起手后仰:朝远离玩家的方向一记反冲,再退入纵深
                        VDVfx.Sound("vbdisapear", 0.7f, npc.Center, 3, 0.9f);
                        ctx.WingPulse = 1f;
                        Vector2 away = (npc.Center - ctx.Target.Center).SafeNormalize(-Vector2.UnitY);
                        npc.velocity += away * VDDirector.OrbitalAscendRecoil;
                        ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.4f);
                    }
                    ctx.CoreGlow = Math.Max(ctx.CoreGlow, p * 0.6f);
                    if (Timer >= VDDirector.OrbitalAscendFrames) {
                        SwitchBeat(Beat.Mark);
                    }
                    break;
                }
                case Beat.Mark:
                    ctx.FakeZ = 1f;
                    DeclareHoldRelative(ctx, VDDirector.OrbitalHoverOffset, 0.05f, 0.2f, 16f);
                    if (Timer == 1) {
                        if (IsServer) {
                            RollTargets(ctx, count);
                            for (int i = 0; i < count; i++) {
                                SpawnVisual<VDTargetReticle>(ctx, ctx.RolledPoints[i], Vector2.Zero, VDDirector.OrbitalMarkFrames + VDDirector.OrbitalPillarStagger * i);
                            }
                            npc.netUpdate = true;
                        }
                        VDVfx.Sound("VoidAnticipation", 1.1f, ctx.Target.Center, 3, 0.9f);
                    }
                    ctx.CoreGlow = Math.Max(ctx.CoreGlow, Timer / (float)VDDirector.OrbitalMarkFrames);
                    if (Timer >= VDDirector.OrbitalMarkFrames) {
                        SwitchBeat(Beat.Fire);
                    }
                    break;
                case Beat.Fire: {
                    ctx.FakeZ = 1f;
                    DeclareHoldRelative(ctx, VDDirector.OrbitalHoverOffset, 0.05f, 0.2f, 16f);
                    if (pillarsFired < count && Timer == 1 + VDDirector.OrbitalPillarStagger * pillarsFired) {
                        Vector2 point = ctx.RolledPoints[pillarsFired];
                        float sweep = VDDirector.OrbitalPillarSweep(ctx.Phase) * Math.Sign(ctx.Target.Center.X - point.X + 0.01f);
                        Shoot<VDVoidPillar>(ctx, point, new Vector2(sweep, 0f), VDDirector.DmgVoidPillar, VDDirector.OrbitalPillarLife, VDDirector.OrbitalPillarWidth);
                        ctx.WingPulse = 1f;
                        ctx.CoreGlow = 1f;
                        ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f);
                        pillarsFired++;
                    }
                    if (pillarsFired >= count && Timer >= 1 + VDDirector.OrbitalPillarStagger * count + 10) {
                        SwitchBeat(Beat.Return);
                        MarkNetUpdate(ctx);
                    }
                    break;
                }
                default: {
                    //俯冲归位:立方曲线,朝镜头飞来,落定一记震屏
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.OrbitalReturnFrames, 0f, 1f);
                    ctx.FakeZ = 1f - MathF.Pow(p, 3f);
                    Vector2 dest = ctx.Target.Center + new Vector2(ctx.SideDir * 200f, -300f);
                    DeclareHoverTo(ctx, dest, 30f, 0.18f, 100f);
                    if (Timer == VDDirector.OrbitalReturnFrames) {
                        VDVfx.Sound("VoidAttack", 0.9f, npc.Center, 2);
                        VDVfx.Shake(npc.Center, VDDirector.OrbitalReturnShake);
                        VDVfx.SparkBurst(npc.Center, VDVfx.VoidPurple, 30, 5f, 16f, 30);
                        ctx.ShakeStrength = 0.7f;
                    }
                    if (Timer >= VDDirector.OrbitalReturnFrames + 6) {
                        return EndAttack(ctx);
                    }
                    break;
                }
            }
            return null;
        }

        /// <summary>落点沿玩家移动方向排布(静止时按 SideDir 横排),相邻间距 160,整体以玩家为中心</summary>
        private static void RollTargets(VDStateContext ctx, int count) {
            Player target = ctx.Target;
            Vector2 dir = target.velocity.LengthSquared() > 1f ? target.velocity.SafeNormalize(Vector2.UnitX) : new Vector2(ctx.SideDir, 0f);
            for (int i = 0; i < count; i++) {
                float offset = (i - (count - 1) * 0.5f) * VDDirector.OrbitalMarkSpacing;
                ctx.RolledPoints[i] = target.Center + dir * offset;
            }
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
