using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 深空掠袭(Barrage,P2 起):hub 把本体闪到 Z 2 的一侧背景 → 36 帧起势(两艘护航幻影舰从后方跟上成梯队,引擎亮起)→
    /// 90 帧横越背景(表观 11.5px/帧),每 12 帧一轮:本体一对、护航各一发纵深贯穿弹朝玩家预测点飞来(42 帧到平面,越来越大,落点标记提前 30 帧)→
    /// 到对侧后 20 帧俯冲归位。「玻璃后面的轰炸航线」:三艘船在背景里掠过,弹从远处一颗颗放大着落到你脚边
    /// </summary>
    [VaultState((int)VDStateIndex.DeepStrafe, typeof(VDStateContext))]
    public class VDDeepStrafeState : VDStateBase
    {
        public override string StateName => "DeepStrafe";
        public override VDStateIndex StateIndex => VDStateIndex.DeepStrafe;
        public override bool ContactByDefault => false;
        public override bool NeedsRepositionBlink => true;
        public override float StartDepth(VDStateContext ctx) => VDDirector.StrafeDepth;
        public override Vector2 AnchorFor(VDStateContext ctx) => DepthAnchor(ctx, ApparentAt(ctx, 0f), VDDirector.StrafeDepth);

        private enum Beat { Windup, Strafe, Dive }
        private Beat beat;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Windup;
        }

        /// <summary>航线上的表观位置:t 0..1 从起侧飞到对侧</summary>
        private static Vector2 ApparentAt(VDStateContext ctx, float t) {
            float x = -ctx.SideDir * MathHelper.Lerp(-VDDirector.StrafeApparentHalfSpan, VDDirector.StrafeApparentHalfSpan, t);
            return new Vector2(x, VDDirector.StrafeApparentY);
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            switch (beat) {
                case Beat.Windup: {
                    ctx.Depth = VDDirector.StrafeDepth;
                    DeclareHoldRelativeDepth(ctx, ApparentAt(ctx, 0f), VDDirector.StrafeDepth, 0.1f, 0.3f, 40f);
                    float p = Timer / (float)VDDirector.StrafeWindup;
                    ctx.CoreGlow = Math.Max(ctx.CoreGlow, p);
                    ctx.WingPulse = Math.Max(ctx.WingPulse, p * 0.6f);
                    ctx.RimCharge = p;
                    if (Timer == 1) {
                        VDVfx.Sound("VoidAnticipation", 0.75f, ctx.Owner.ProjectedCenter, 3, 0.9f);
                        SpawnEscorts(ctx);
                    }
                    if (Timer % 3 == 0) {
                        ConvergeSparks(ctx, VDVfx.VoidPink, 100f, 220f, 0.1f);
                    }
                    if (Timer >= VDDirector.StrafeWindup) {
                        SwitchBeat(Beat.Strafe);
                    }
                    break;
                }
                case Beat.Strafe: {
                    ctx.Depth = VDDirector.StrafeDepth;
                    float t = MathHelper.Clamp(Timer / (float)VDDirector.StrafeFrames, 0f, 1f);
                    DeclareHoldRelativeDepth(ctx, ApparentAt(ctx, t), VDDirector.StrafeDepth, 0.25f, 0.45f, 70f);
                    ctx.RimCharge = 0.7f;
                    ctx.CoreGlow = Math.Max(ctx.CoreGlow, 0.6f);
                    if (Timer % VDDirector.StrafeBoltInterval == 1) {
                        Volley(ctx);
                    }
                    if (Timer >= VDDirector.StrafeFrames) {
                        SwitchBeat(Beat.Dive);
                        MarkNetUpdate(ctx);
                    }
                    break;
                }
                default: {
                    //俯冲归位到玩家对侧斜上方(航线终点那一侧)
                    Vector2 dest = ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.ConnectorDefaultAnchor.X, VDDirector.ConnectorDefaultAnchor.Y);
                    DeclareHoverTo(ctx, dest, 60f, 0.22f, 100f);
                    DeclareDive(ctx, VDDirector.StrafeDepth, Timer, VDDirector.DiveFrames, dest);
                    if (Timer >= VDDirector.DiveFrames + VDDirector.DiveContactFrames) {
                        return EndAttack(ctx);
                    }
                    break;
                }
            }
            return null;
        }

        /// <summary>护航舰拖在本体后方斜下(x 按航向取反)</summary>
        private void SpawnEscorts(VDStateContext ctx) {
            foreach (Vector2 ofs in VDDirector.StrafeEscortOffsets) {
                Vector2 apparent = new Vector2(-ctx.SideDir * ofs.X, ofs.Y);
                Vector2 pos = ctx.Npc.Center + VDDepth.WorldOffset(apparent, VDDirector.StrafeDepth);
                SpawnVisualDepth<VDEscortShip>(ctx, pos, Vector2.Zero, VDDirector.StrafeDepth, 0f, 0f, ctx.Npc.whoAmI, apparent.X, apparent.Y);
            }
        }

        /// <summary>一轮:本体一对(落点左右散开)、每艘护航一发,全部纵深贯穿,42 帧到平面</summary>
        private void Volley(VDStateContext ctx) {
            NPC npc = ctx.Npc;
            MuzzleCue(ctx, Vector2.UnitY, 2f, "CruiserSpit", 1.05f, 0.75f);
            if (!IsServer) {
                return;
            }
            Vector2 landing = PredictTarget(ctx, VDDirector.StrafeBoltLead);
            for (int i = -1; i <= 1; i += 2) {
                Vector2 land = landing + new Vector2(i * VDDirector.StrafeBoltSpread, 0f);
                (Vector2 vel, float zVel) = AimThroughPlane(npc.Center, VDDirector.StrafeDepth, land, VDDirector.StrafeBoltFrames);
                ShootDepth<VDVoidBolt>(ctx, npc.Center, vel, VDDirector.DmgVoidBolt, VDDirector.StrafeDepth, zVel, 0f, VDVoidBolt.ModeZPierce);
            }
            foreach (Vector2 ofs in VDDirector.StrafeEscortOffsets) {
                Vector2 apparent = new Vector2(-ctx.SideDir * ofs.X, ofs.Y);
                Vector2 from = npc.Center + VDDepth.WorldOffset(apparent, VDDirector.StrafeDepth);
                Vector2 land = landing + new Vector2(-ctx.SideDir * ofs.X * 0.4f, 0f);
                (Vector2 vel, float zVel) = AimThroughPlane(from, VDDirector.StrafeDepth, land, VDDirector.StrafeBoltFrames);
                ShootDepth<VDVoidBolt>(ctx, from, vel, VDDirector.DmgVoidBolt, VDDirector.StrafeDepth, zVel, 0f, VDVoidBolt.ModeZPierce);
            }
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
