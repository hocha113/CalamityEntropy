using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 幻影舰队:本体淡出 → 玩家周围 520px 十字(第二波 X 形)开四门,三门出全息幻影舰、真身占第四门
    /// (真身门环更亮 + 核心拉满 + 翼张 = 可读的破绽)→ 40 帧同步瞄准 → 四舰齐冲(真身接触伤害,幻影 60%)→ 幻影碎成全息碎片,
    /// 两波之间全员静止 20 帧。
    /// P2 起两波;P3 幻影沿路留冲刺尾弹。门开 30 帧才出手,伤害窗按速度门槛
    /// </summary>
    [VaultState((int)VDStateIndex.PhantomFleet, typeof(VDStateContext))]
    public class VDPhantomFleetState : VDStateBase
    {
        public override string StateName => "PhantomFleet";
        public override VDStateIndex StateIndex => VDStateIndex.PhantomFleet;
        public override bool ContactByDefault => false;

        private enum Beat { FadeOut, Aim, Dash, WaveHold, End }
        private Beat beat;
        private int wave;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.FadeOut;
            wave = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            DeclareDirect(ctx);
            switch (beat) {
                case Beat.FadeOut:
                    npc.velocity *= 0.6f;
                    DeclareAlpha(ctx, MathHelper.Clamp(1f - Timer / (float)VDDirector.FleetFadeFrames, 0f, 1f), 1f);
                    if (Timer == 1 && IsServer) {
                        //首帧就掷:门位与真身位要在淡出的 8 帧里过线到客户端
                        RollFormation(ctx);
                        npc.netUpdate = true;
                    }
                    if (Timer >= VDDirector.FleetFadeFrames) {
                        OpenPortals(ctx);
                        SwitchBeat(Beat.Aim);
                    }
                    break;
                case Beat.Aim: {
                    Vector2 realPos = ctx.RolledPoints[ctx.RandCount];
                    npc.Center = realPos;
                    npc.velocity = Vector2.Zero;
                    float p = MathHelper.Clamp(Timer / 8f, 0f, 1f);
                    DeclareAlpha(ctx, p, 1f);
                    //真身破绽:核心拉满、翼随瞄准进度张开(幻影没有这两样)
                    float aimP = MathHelper.Clamp(Timer / (float)VDDirector.FleetAimFrames, 0f, 1f);
                    ctx.CoreGlow = 1f;
                    ctx.WingPulse = Math.Max(ctx.WingPulse, aimP);
                    //真身的第三个破绽:描边随瞄准进度烧起来,幻影舰没有
                    ctx.RimCharge = aimP;
                    if (Timer % 3 == 0) {
                        ConvergeSparks(ctx, VDVfx.VoidWhite, 50f, 120f, 0.14f);
                    }
                    if (Timer >= VDDirector.FleetAimFrames) {
                        Vector2 dir = (ctx.Target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                        npc.velocity = dir * VDDirector.FleetDashSpeed;
                        ctx.WingPulse = 1f;
                        ctx.RimFlash = 1f;
                        VDVfx.Sound("CruiserDash", 0.9f, npc.Center, 3);
                        VDVfx.Shake(npc.Center, 5f, 1800f);
                        SwitchBeat(Beat.Dash);
                        MarkNetUpdate(ctx);
                    }
                    break;
                }
                case Beat.Dash:
                    DeclareAlpha(ctx, 1f, 1f);
                    ctx.ContactWindow = npc.velocity.Length() > VDDirector.PhantomContactSpeed;
                    ctx.RimCharge = 1f;
                    if (ctx.Phase >= 3 && Timer % VDDirector.FleetTrailInterval == 3) {
                        Shoot<VDVoidBolt>(ctx, npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY) * VDDirector.PhantomTrailSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeDashTrail);
                    }
                    if (Timer >= VDDirector.FleetDashFrames) {
                        wave++;
                        SwitchBeat(wave >= VDDirector.FleetWaves(ctx.Phase) ? Beat.End : Beat.WaveHold);
                        MarkNetUpdate(ctx);
                    }
                    break;
                case Beat.WaveHold:
                    //两波之间全员静止:刹停、核心熄、可见,段落之间的一口气
                    DeclareAlpha(ctx, 1f, 1f);
                    npc.velocity *= 0.75f;
                    ctx.CoreGlow = 0f;
                    if (Timer >= VDDirector.FleetWaveHold) {
                        SwitchBeat(Beat.FadeOut);
                    }
                    break;
                default:
                    npc.velocity *= 0.85f;
                    DeclareAlpha(ctx, MathHelper.Clamp(1f - Timer / (float)VDDirector.FleetEndFade, 0f, 1f), 1f);
                    if (Timer >= VDDirector.FleetEndFade) {
                        npc.velocity *= 0.5f;
                        return EndAttack(ctx);
                    }
                    break;
            }
            return null;
        }

        /// <summary>掷出编队:第一波十字、第二波 X;RolledPoints[0..3] 为门位,RandCount 为真身占的门</summary>
        private void RollFormation(VDStateContext ctx) {
            float baseAng = wave % 2 == 0 ? 0f : MathHelper.PiOver4;
            ctx.RolledAngles[1] = baseAng;
            ctx.RandCount = Main.rand.Next(VDDirector.FleetShipCount);
            for (int i = 0; i < VDDirector.FleetShipCount; i++) {
                float ang = baseAng + MathHelper.TwoPi * i / VDDirector.FleetShipCount;
                ctx.RolledPoints[i] = ctx.Target.Center + ang.ToRotationVector2() * VDDirector.FleetPortalRadius;
            }
        }

        /// <summary>开门 + 放幻影:门的朝向指向玩家;真身门带更亮的环(ai[2] 亮度倍率)</summary>
        private void OpenPortals(VDStateContext ctx) {
            if (!IsServer) {
                return;
            }
            for (int i = 0; i < VDDirector.FleetShipCount; i++) {
                Vector2 pos = ctx.RolledPoints[i];
                Vector2 dir = (ctx.Target.Center - pos).SafeNormalize(Vector2.UnitY);
                bool real = i == ctx.RandCount;
                SpawnVisual<VDPortal>(ctx, pos, dir, VDPortal.ModeDash, VDDirector.FleetPortalLife, real ? VDDirector.FleetRealPortalGlow : 1f);
                if (!real) {
                    Shoot<VDPhantomShip>(ctx, pos, Vector2.Zero, VDDirector.DmgPhantomShip, ctx.Npc.whoAmI, VDDirector.FleetAimFrames, ctx.Npc.target);
                }
            }
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
