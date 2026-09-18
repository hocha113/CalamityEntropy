using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 立体舰队:本体淡出 → 玩家周围表观 520px 的十字(第二波 X 形)开四门:两远(Z 1.4 的背景里两枚小门)、一平面、一近(Z -0.45,屏幕边缘的半透明巨门),
    /// 三门出全息幻影舰、真身占第四门(真身门环更亮 + 核心拉满 + 翼张 + 描边烧起 = 可读的破绽)→ 40 帧同步瞄准 →
    /// 四舰沿各自的三维直线齐冲,第 20 帧同帧穿过玩家预测点(远舰放大着来、近舰缩小着来、平面舰不变),各自只在穿过平面那几帧有判定,
    /// 再飞 16 帧收尾(远舰越过镜头淡出、近舰遁回平面后方)→ 幻影碎成全息碎片,两波之间全员静止 20 帧(真身此时已拉回平面)。
    /// P2 起两波,第二波门的深度整体轮转一位;P3 幻影在平面附近沿路留冲刺尾弹。穿越点从瞄准起就画大环
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
        /// <summary>冲刺结束时的深度,收尾拍从它拉回平面</summary>
        private float endDepth;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.FadeOut;
            wave = 0;
            endDepth = 0f;
        }

        /// <summary>真身这一波所在门的深度</summary>
        private static float RealDepth(VDStateContext ctx) => ctx.RolledDepths[Math.Clamp(ctx.RandCount, 0, ctx.RolledDepths.Length - 1)];

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            DeclareDirect(ctx);
            switch (beat) {
                case Beat.FadeOut:
                    npc.velocity *= 0.6f;
                    DeclareAlpha(ctx, MathHelper.Clamp(1f - Timer / (float)VDDirector.FleetFadeFrames, 0f, 1f), 1f);
                    if (Timer == 1 && IsServer) {
                        //首帧就掷:门位、门深与真身位要在淡出的 8 帧里过线到客户端
                        RollFormation(ctx);
                        npc.netUpdate = true;
                    }
                    if (Timer >= VDDirector.FleetFadeFrames) {
                        OpenPortals(ctx);
                        SwitchBeat(Beat.Aim);
                    }
                    break;
                case Beat.Aim: {
                    float depth = RealDepth(ctx);
                    npc.Center = ctx.RolledPoints[ctx.RandCount];
                    npc.velocity = Vector2.Zero;
                    ctx.Depth = depth;
                    float p = MathHelper.Clamp(Timer / 8f, 0f, 1f);
                    DeclareAlpha(ctx, p, 1f);
                    //真身破绽:核心拉满、翼随瞄准进度张开、描边烧起(幻影没有这三样)
                    float aimP = MathHelper.Clamp(Timer / (float)VDDirector.FleetAimFrames, 0f, 1f);
                    ctx.CoreGlow = 1f;
                    ctx.WingPulse = Math.Max(ctx.WingPulse, aimP);
                    ctx.RimCharge = aimP;
                    if (Timer % 3 == 0) {
                        ConvergeSparks(ctx, VDVfx.VoidWhite, 50f, 120f, 0.14f);
                    }
                    //穿越点大环:四舰会在这里同帧交汇
                    ctx.DiveMarkerPos = PredictTarget(ctx, VDDirector.FleetCrossLead);
                    ctx.DiveMarkerProgress = aimP;
                    if (Timer >= VDDirector.FleetAimFrames) {
                        Vector2 cross = PredictTarget(ctx, VDDirector.FleetCrossLead);
                        if (IsServer) {
                            ctx.RolledPoints[4] = cross;
                        }
                        npc.velocity = (cross - npc.Center) / VDDirector.FleetCrossFrame;
                        ctx.WingPulse = 1f;
                        ctx.RimFlash = 1f;
                        VDVfx.Sound("CruiserDash", 0.9f, ctx.Owner.ProjectedCenter, 3);
                        VDVfx.Shake(npc.Center, 5f, 1800f);
                        SwitchBeat(Beat.Dash);
                        MarkNetUpdate(ctx);
                    }
                    break;
                }
                case Beat.Dash: {
                    float z0 = RealDepth(ctx);
                    //三维直线:Z 线性,第 FleetCrossFrame 帧恰好为 0,之后继续(远门的真身越过镜头,近门的真身遁回平面后方)
                    ctx.Depth = z0 * (1f - Timer / (float)VDDirector.FleetCrossFrame);
                    DeclareAlpha(ctx, 1f, 1f);
                    //平面舰按速度门槛开窗,深度舰全程开窗由判定带把关
                    ctx.ContactWindow = z0 == 0f ? npc.velocity.Length() > VDDirector.PhantomContactSpeed : true;
                    ctx.RimCharge = 1f;
                    if (Timer <= VDDirector.FleetCrossFrame) {
                        ctx.DiveMarkerPos = ctx.RolledPoints[4];
                        ctx.DiveMarkerProgress = Timer / (float)VDDirector.FleetCrossFrame;
                    }
                    if (Timer == VDDirector.FleetCrossFrame && Math.Abs(z0) > 0.01f) {
                        //穿过平面的一瞬
                        VDVfx.Shake(npc.Center, 4f, 1600f);
                        VDVfx.SparkBurst(npc.Center, VDVfx.VoidWhite, 16, 4f, 10f, 20, 0.5f, 1f);
                    }
                    //远门的真身越过镜头那一瞬:呼啸 + 径向拖影
                    float prevDepth = z0 * (1f - (Timer - 1) / (float)VDDirector.FleetCrossFrame);
                    if (z0 > 0f && prevDepth > VDDirector.DepthWhooshZ && ctx.Depth <= VDDirector.DepthWhooshZ) {
                        VDVfx.PassBy(ctx.Owner.ProjectedCenter, 0.8f);
                    }
                    if (ctx.Phase >= 3 && Math.Abs(ctx.Depth) < 0.3f && Timer % VDDirector.FleetTrailInterval == 3) {
                        Shoot<VDVoidBolt>(ctx, npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY) * VDDirector.PhantomTrailSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeDashTrail);
                    }
                    if (Timer >= VDDirector.FleetDashFrames) {
                        wave++;
                        endDepth = ctx.Depth;
                        SwitchBeat(wave >= VDDirector.FleetWaves(ctx.Phase) ? Beat.End : Beat.WaveHold);
                        MarkNetUpdate(ctx);
                    }
                    break;
                }
                case Beat.WaveHold: {
                    //两波之间全员静止:刹停、核心熄、拉回平面(真身若在镜头后或深处,这 20 帧里滑回来)
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.FleetWaveHold, 0f, 1f);
                    ctx.Depth = endDepth * (1f - VDDepth.RetreatCurve(p));
                    DeclareAlpha(ctx, 1f, 1f);
                    npc.velocity *= 0.75f;
                    ctx.CoreGlow = 0f;
                    if (Timer >= VDDirector.FleetWaveHold) {
                        SwitchBeat(Beat.FadeOut);
                    }
                    break;
                }
                default: {
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.FleetEndFade, 0f, 1f);
                    ctx.Depth = endDepth * (1f - p);
                    npc.velocity *= 0.85f;
                    DeclareAlpha(ctx, 1f - p, 1f);
                    if (Timer >= VDDirector.FleetEndFade) {
                        npc.velocity *= 0.5f;
                        return EndAttack(ctx);
                    }
                    break;
                }
            }
            return null;
        }

        /// <summary>
        /// 掷出编队:第一波十字、第二波 X;门 i 的表观位置在玩家周围 520px,深度按 FleetDepths 轮转(第二波整体错一位);
        /// RolledPoints[0..3] 为门的平面位置(表观偏移按深度换算),RolledDepths[0..3] 为门深,RandCount 为真身占的门
        /// </summary>
        private void RollFormation(VDStateContext ctx) {
            float baseAng = wave % 2 == 0 ? 0f : MathHelper.PiOver4;
            ctx.RolledAngles[1] = baseAng;
            ctx.RandCount = Main.rand.Next(VDDirector.FleetShipCount);
            for (int i = 0; i < VDDirector.FleetShipCount; i++) {
                float ang = baseAng + MathHelper.TwoPi * i / VDDirector.FleetShipCount;
                float depth = VDDirector.FleetDepths[(i + wave) % VDDirector.FleetDepths.Length];
                Vector2 apparent = ang.ToRotationVector2() * VDDirector.FleetPortalRadius;
                ctx.RolledPoints[i] = VDDepth.WorldFromApparent(ctx.Target.Center, apparent, depth);
                ctx.RolledDepths[i] = depth;
            }
        }

        /// <summary>开门 + 放幻影:门与舰都带各自的深度;门的朝向指向玩家;真身门带更亮的环(ai[2] 亮度倍率)</summary>
        private void OpenPortals(VDStateContext ctx) {
            if (!IsServer) {
                return;
            }
            for (int i = 0; i < VDDirector.FleetShipCount; i++) {
                Vector2 pos = ctx.RolledPoints[i];
                float depth = ctx.RolledDepths[i];
                Vector2 dir = (ctx.Target.Center - pos).SafeNormalize(Vector2.UnitY);
                bool real = i == ctx.RandCount;
                SpawnVisualDepth<VDPortal>(ctx, pos, dir, depth, 0f, 0f, VDPortal.ModeDash, VDDirector.FleetPortalLife, real ? VDDirector.FleetRealPortalGlow : 1f);
                if (!real) {
                    ShootDepth<VDPhantomShip>(ctx, pos, Vector2.Zero, VDDirector.DmgPhantomShip, depth, 0f, 0f, ctx.Npc.whoAmI, VDDirector.FleetAimFrames, ctx.Npc.target);
                }
            }
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
