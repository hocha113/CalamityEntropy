using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 轨道轰炸(P2 阶段签名):本体 30 帧退入纵深(Z 0 → 2.2:真透视,向消失点收缩、进远景层、雾化、无接触不可攻击)→
    /// 沿玩家移动方向标 4/5 个落点(标记 50 帧收缩,标记就是承诺)→ 虚空光柱按 10 帧错拍从屏顶砸落(P3 缓慢横扫)→
    /// 20 帧俯冲归位(立方曲线「朝镜头飞来」,落点大环从起手就画,落地震屏 + 6 帧接触窗)。
    /// 表观悬停点是玩家头顶 380px,世界坐标按深度换算,退远时世界位置往上飞、投影位置基本不动,读成「越来越远」
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
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.OrbitalAscendFrames, 0f, 1f);
                    ctx.Depth = VDDirector.OrbitalFarDepth * VDDepth.RetreatCurve(p);
                    //世界坐标追着「表观 380px 头顶」在当前深度下的位置飞,越远越高
                    DeclareHoverTo(ctx, ctx.Target.Center + VDDepth.WorldOffset(VDDirector.OrbitalHoverOffset, ctx.Depth), 40f, 0.15f, 160f);
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
                    ctx.Depth = VDDirector.OrbitalFarDepth;
                    DeclareHoldRelativeDepth(ctx, VDDirector.OrbitalHoverOffset, VDDirector.OrbitalFarDepth, 0.05f, 0.2f, 40f);
                    if (Timer == 1) {
                        if (IsServer) {
                            RollTargets(ctx, count);
                            for (int i = 0; i < count; i++) {
                                SpawnVisual<VDTargetReticle>(ctx, ctx.RolledPoints[i], Vector2.Zero, VDDirector.OrbitalMarkFrames + VDDirector.OrbitalPillarStagger * i, npc.whoAmI + 1);
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
                    ctx.Depth = VDDirector.OrbitalFarDepth;
                    DeclareHoldRelativeDepth(ctx, VDDirector.OrbitalHoverOffset, VDDirector.OrbitalFarDepth, 0.05f, 0.2f, 40f);
                    if (pillarsFired < count && Timer == 1 + VDDirector.OrbitalPillarStagger * pillarsFired) {
                        Vector2 point = ctx.RolledPoints[pillarsFired];
                        float sweep = VDDirector.OrbitalPillarSweep(ctx.Phase) * Math.Sign(ctx.Target.Center.X - point.X + 0.01f);
                        Shoot<VDVoidPillar>(ctx, point, new Vector2(sweep, 0f), VDDirector.DmgVoidPillar, VDDirector.OrbitalPillarLife, VDDirector.OrbitalPillarWidth, npc.whoAmI + 1);
                        ctx.WingPulse = 1f;
                        ctx.CoreGlow = 1f;
                        //每根光柱砸落,背景里的本体描边闪一下(退入纵深后描边本就减半,爆闪给足)
                        ctx.RimFlash = 1f;
                        ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f);
                        pillarsFired++;
                    }
                    //P3:柱子的错拍空档里从背景朝玩家预测点射纵深贯穿炮弹(越来越大地飞来,自带落点标记)
                    int shells = VDDirector.OrbitalShells(ctx.Phase);
                    if (shells > 0 && Timer % VDDirector.OrbitalPillarStagger == VDDirector.OrbitalPillarStagger / 2 && Timer < 1 + VDDirector.OrbitalPillarStagger * shells) {
                        Vector2 landing = PredictTarget(ctx, VDDirector.OrbitalShellLead);
                        (Vector2 vel, float zVel) = AimThroughPlane(npc.Center, VDDirector.OrbitalFarDepth, landing, VDDirector.OrbitalShellFrames);
                        ShootDepth<VDVoidBolt>(ctx, npc.Center, vel, VDDirector.DmgVoidBolt, VDDirector.OrbitalFarDepth, zVel, 0f, VDVoidBolt.ModeZPierce);
                        MuzzleCue(ctx, Vector2.UnitY, 2f, "CruiserSpit", 1.1f, 0.7f);
                    }
                    if (pillarsFired >= count && Timer >= 1 + VDDirector.OrbitalPillarStagger * count + 10) {
                        SwitchBeat(Beat.Return);
                        MarkNetUpdate(ctx);
                    }
                    break;
                }
                default: {
                    //俯冲归位:立方曲线朝镜头飞来,落点大环 + 落地震屏 + 接触窗都在 DeclareDive 里
                    Vector2 dest = ctx.Target.Center + new Vector2(ctx.SideDir * 200f, -300f);
                    DeclareHoverTo(ctx, dest, 50f, 0.2f, 100f);
                    DeclareDive(ctx, VDDirector.OrbitalFarDepth, Timer, VDDirector.OrbitalReturnFrames, dest);
                    if (Timer >= VDDirector.OrbitalReturnFrames + VDDirector.DiveContactFrames) {
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
