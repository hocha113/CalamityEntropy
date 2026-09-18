using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 纵深环门(Zone,P1 起):本体退到 Z 1.3(hub 落定拍已把它送到位)→ 30 帧起势 → 每 22 帧从 Z 3.2 推出一个 14 弹、缺 2 弹的环
    /// (环心 = 玩家预测点,整环同一 Z 以 -0.07/帧逼近,46 帧到平面,到达那几帧才有判定)→ 相邻环的缺口转 75°(P3 交替正负)→
    /// 末环到达后本体 20 帧俯冲归位(落点大环 + 落地接触窗)。
    /// 公平阀:环的平面脚印从到达前 30 帧起淡淡画出、缺口一目了然;环心用预测点不用实时位置,玩家跑起来环不会追着套
    /// </summary>
    [VaultState((int)VDStateIndex.DepthGates, typeof(VDStateContext))]
    public class VDDepthGatesState : VDStateBase
    {
        public override string StateName => "DepthGates";
        public override VDStateIndex StateIndex => VDStateIndex.DepthGates;
        public override bool ContactByDefault => false;
        public override float StartDepth(VDStateContext ctx) => VDDirector.GateDepth;
        public override Vector2 AnchorFor(VDStateContext ctx)
            => DepthAnchor(ctx, new Vector2(ctx.SideDir * VDDirector.GateHoverOffset.X, VDDirector.GateHoverOffset.Y), VDDirector.GateDepth);

        private enum Beat { Windup, Push, Dive }
        private Beat beat;
        private int ringsPushed;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Windup;
            ringsPushed = 0;
            if (IsServer) {
                ctx.RolledAngles[0] = Main.rand.NextFloat(MathHelper.TwoPi);
                MarkNetUpdate(ctx);
            }
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            int rings = VDDirector.GateRings(ctx.Phase);
            Vector2 apparent = new Vector2(ctx.SideDir * VDDirector.GateHoverOffset.X, VDDirector.GateHoverOffset.Y);
            switch (beat) {
                case Beat.Windup: {
                    ctx.Depth = VDDirector.GateDepth;
                    DeclareHoldRelativeDepth(ctx, apparent, VDDirector.GateDepth, 0.08f, 0.25f, 30f);
                    float p = Timer / (float)VDDirector.GateWindup;
                    ctx.CoreGlow = Math.Max(ctx.CoreGlow, p);
                    ctx.WingPulse = Math.Max(ctx.WingPulse, p);
                    ctx.RimCharge = p;
                    if (Timer == 1) {
                        VDVfx.Sound("VoidAnticipation", 0.8f, ctx.Owner.ProjectedCenter, 3, 0.9f);
                    }
                    if (Timer % 3 == 0) {
                        ConvergeSparks(ctx, VDDirector.RimZoneCyan, 120f, 260f, 0.1f);
                    }
                    if (Timer >= VDDirector.GateWindup) {
                        SwitchBeat(Beat.Push);
                    }
                    break;
                }
                case Beat.Push: {
                    ctx.Depth = VDDirector.GateDepth;
                    DeclareHoldRelativeDepth(ctx, apparent, VDDirector.GateDepth, 0.08f, 0.25f, 30f);
                    ctx.RimCharge = 0.6f;
                    if (ringsPushed < rings && Timer == 1 + VDDirector.GateInterval * ringsPushed) {
                        PushRing(ctx, ringsPushed);
                        ringsPushed++;
                    }
                    //末环到达平面(46 帧)再静默几帧,才俯冲
                    int lastArrive = 1 + VDDirector.GateInterval * (rings - 1) + (int)(VDDirector.GateStartDepth / -VDDirector.GateZVel);
                    if (ringsPushed >= rings && Timer >= lastArrive + VDDirector.GateTail) {
                        SwitchBeat(Beat.Dive);
                        MarkNetUpdate(ctx);
                    }
                    break;
                }
                default: {
                    Vector2 dest = ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.ConnectorDefaultAnchor.X, VDDirector.ConnectorDefaultAnchor.Y);
                    DeclareHoverTo(ctx, dest, 50f, 0.2f, 100f);
                    DeclareDive(ctx, VDDirector.GateDepth, Timer, VDDirector.DiveFrames, dest);
                    if (Timer >= VDDirector.DiveFrames + VDDirector.DiveContactFrames) {
                        return EndAttack(ctx);
                    }
                    break;
                }
            }
            return null;
        }

        /// <summary>推出第 k 环:环心 = 玩家预测点;缺口角 = 掷骰基角 + k × 75°(P3 交替方向);整环同 Z 同速</summary>
        private void PushRing(VDStateContext ctx, int k) {
            NPC npc = ctx.Npc;
            MuzzleCue(ctx, Vector2.UnitY, 3f, "CruiserSpit", 0.8f, 0.9f);
            ctx.WingPulse = 1f;
            if (!IsServer) {
                return;
            }
            Vector2 center = PredictTarget(ctx, VDDirector.GateCenterLead);
            float step = MathHelper.ToRadians(VDDirector.GateGapStepDeg);
            float gapAngle = ctx.RolledAngles[0] + (ctx.Phase >= 3 && k % 2 == 1 ? -step : step) * k;
            int gapStart = (int)Math.Round(MathHelper.WrapAngle(gapAngle) / MathHelper.TwoPi * VDDirector.GateBolts);
            int drawn = 0;
            for (int i = 0; i < VDDirector.GateBolts; i++) {
                int rel = ((i - gapStart) % VDDirector.GateBolts + VDDirector.GateBolts) % VDDirector.GateBolts;
                if (rel < VDDirector.GateGapBolts) {
                    continue;
                }
                float angle = MathHelper.TwoPi * i / VDDirector.GateBolts;
                Vector2 pos = center + angle.ToRotationVector2() * VDDirector.GateRadius;
                ShootDepth<VDGateBolt>(ctx, pos, Vector2.Zero, VDDirector.DmgGateBolt, VDDirector.GateStartDepth, VDDirector.GateZVel, 0f, angle, drawn, VDDirector.GateRadius);
                drawn++;
            }
            npc.netUpdate = true;
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
