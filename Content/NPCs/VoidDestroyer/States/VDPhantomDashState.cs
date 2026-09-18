using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 三维幻影冲刺:淡出 → 开门待机 36 帧(门开即预告,接触档前摇)→ 冲 → 硬刹 → 回到淡出;P1 三冲、P2 起四冲。
    /// P1 全是平面冲刺(玩家某角 480px 外开门,一帧定速 40 冲 30 帧,硬刹 10 帧);P2 起 平面 / 远→近 / 平面 / 近→远 交替:
    /// 远→近从 Z 1.2 的远门出来,冲刺向量穿过 Z 轴,第 24 帧恰好在锁定的预测点穿过平面(约 4 帧接触,「从平面里浮出来的鲨鱼」),继续冲到镜头后 -0.6 淡出;
    /// 近→远从屏幕边缘 Z -0.45 的半透明巨门缩进平面(第 10 帧穿过),再遁入深处。
    /// 公平阀:平面冲刺的伤害窗 = 速度门槛,贯穿冲刺的伤害窗 = 判定带;穿越点从待机起就画大环;P2 起路径上(平面附近)留加速虚空弹
    /// </summary>
    [VaultState((int)VDStateIndex.PhantomDash, typeof(VDStateContext))]
    public class VDPhantomDashState : VDStateBase
    {
        public override string StateName => "PhantomDash";
        public override VDStateIndex StateIndex => VDStateIndex.PhantomDash;
        public override bool ContactByDefault => false;

        private enum Beat { FadeOut, Wait, Dash, Brake, End }
        private Beat beat;
        private int dashes;
        /// <summary>贯穿冲刺结束后持住的深度(刹车 / 淡出期间不让它弹回平面),收招拍再拉回 0</summary>
        private float carryDepth;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.FadeOut;
            dashes = 0;
            carryDepth = 0f;
        }

        /// <summary>本冲的种类(由已完成冲数决定,各端一致)</summary>
        private int Kind(VDStateContext ctx) => VDDirector.PhantomDashKind(ctx.Phase, dashes);

        private static float StartDepthOf(int kind) => kind == 1 ? VDDirector.PhantomFarDepth : kind == 2 ? VDDirector.PhantomNearDepth : 0f;
        private static float EndDepthOf(int kind) => kind == 1 ? VDDirector.PhantomPassDepth : kind == 2 ? VDDirector.PhantomFarDepth : 0f;

        /// <summary>贯穿冲刺穿过平面的帧(Z 线性,从起点深度到终点深度)</summary>
        private static float CrossFrame(int kind) {
            float z0 = StartDepthOf(kind);
            float z1 = EndDepthOf(kind);
            return VDDirector.PhantomPierceFrames * (z0 / (z0 - z1));
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            DeclareDirect(ctx);
            int kind = Kind(ctx);
            switch (beat) {
                case Beat.FadeOut:
                    npc.velocity *= 0.5f;
                    ctx.Depth = carryDepth;
                    DeclareAlpha(ctx, MathHelper.Clamp(1f - Timer / (float)VDDirector.PhantomFadeFrames, 0f, 1f), 1f);
                    if (Timer >= VDDirector.PhantomFadeFrames) {
                        if (IsServer) {
                            RollDash(ctx, kind);
                            npc.netUpdate = true;
                        }
                        SwitchBeat(Beat.Wait);
                    }
                    break;
                case Beat.Wait: {
                    npc.Center = ctx.AnchorPos;
                    npc.velocity = Vector2.Zero;
                    ctx.Depth = StartDepthOf(kind);
                    DeclareAlpha(ctx, 0f, 1f);
                    //门内隐身时描边先蓄满(本体透明看不见,但强度已到位),冲出的第一帧就是白热整圈
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.PhantomWaitFrames, 0f, 1f);
                    ctx.RimCharge = p;
                    if (kind != 0) {
                        //贯穿冲刺:穿越点大环从待机起就画
                        ctx.DiveMarkerPos = ctx.RolledPoints[0];
                        ctx.DiveMarkerProgress = p;
                    }
                    if (Timer >= VDDirector.PhantomWaitFrames) {
                        Launch(ctx, kind);
                        SwitchBeat(Beat.Dash);
                        MarkNetUpdate(ctx);
                    }
                    break;
                }
                case Beat.Dash:
                    if (kind == 0) {
                        UpdatePlaneDash(ctx);
                    }
                    else {
                        UpdatePierceDash(ctx, kind);
                    }
                    break;
                case Beat.Brake:
                    //硬刹:×0.7/帧,伤害窗随速度关掉,刹车火花;贯穿冲刺结束时本体已在镜头后或深处,持住深度、无接触
                    ctx.Depth = carryDepth;
                    DeclareAlpha(ctx, 1f, 1f);
                    npc.velocity *= 0.7f;
                    ctx.ContactWindow = carryDepth == 0f && npc.velocity.Length() > VDDirector.PhantomContactSpeed;
                    if (Timer == 1 && carryDepth == 0f) {
                        VDVfx.SparkBurst(npc.Center, VDVfx.VoidPurple, 12, 3f, 9f, 20, 0.5f, 1f);
                    }
                    if (Timer >= VDDirector.PhantomBrakeFrames) {
                        SwitchBeat(dashes >= VDDirector.PhantomDashes(ctx.Phase) ? Beat.End : Beat.FadeOut);
                    }
                    break;
                default: {
                    //收招:淡出,同时把深度拉回平面,交给 hub 的落定拍时已在 0
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.PhantomEndFade, 0f, 1f);
                    npc.velocity *= 0.85f;
                    ctx.Depth = carryDepth * (1f - p);
                    DeclareAlpha(ctx, 1f - p, 1f);
                    if (Timer >= VDDirector.PhantomEndFade) {
                        npc.velocity *= 0.5f;
                        return EndAttack(ctx);
                    }
                    break;
                }
            }
            return null;
        }

        /// <summary>
        /// 服务端掷本冲:选角;平面冲在角上 480px 开门;贯穿冲先钉穿越点(玩家预测位置,RolledPoints[0]),
        /// 再按「第 CrossFrame 帧穿过穿越点」反推起点的平面位置(AnchorPos),门带起点深度开在那里
        /// </summary>
        private void RollDash(VDStateContext ctx, int kind) {
            ctx.CornerIndex = Main.rand.Next(4);
            Vector2 corner = VDVfx.CornerDirs[ctx.CornerIndex];
            if (kind == 0) {
                ctx.AnchorPos = ctx.Target.Center + corner * VDDirector.PhantomPortalOffset;
                Vector2 dir = (ctx.Target.Center - ctx.AnchorPos).SafeNormalize(Vector2.UnitY);
                SpawnVisual<VDPortal>(ctx, ctx.AnchorPos, dir, VDPortal.ModeDash, VDDirector.PhantomPortalLife);
                return;
            }
            Vector2 cross = PredictTarget(ctx, VDDirector.PhantomPierceLead);
            ctx.RolledPoints[0] = cross;
            Vector2 d = (-corner).SafeNormalize(Vector2.UnitY);
            ctx.AnchorPos = cross - d * VDDirector.PhantomPierceSpeed * CrossFrame(kind);
            SpawnVisualDepth<VDPortal>(ctx, ctx.AnchorPos, d, StartDepthOf(kind), 0f, 0f, VDPortal.ModeDash, VDDirector.PhantomPortalLife);
        }

        /// <summary>出手:平面冲一帧定速 40 朝玩家当前位置;贯穿冲沿起点 → 穿越点的方向定速 28</summary>
        private void Launch(VDStateContext ctx, int kind) {
            NPC npc = ctx.Npc;
            Vector2 dir = kind == 0
                ? (ctx.Target.Center - ctx.AnchorPos).SafeNormalize(Vector2.UnitY)
                : (ctx.RolledPoints[0] - ctx.AnchorPos).SafeNormalize(Vector2.UnitY);
            npc.velocity = dir * (kind == 0 ? VDDirector.PhantomDashSpeed : VDDirector.PhantomPierceSpeed);
            DeclareAlpha(ctx, 1f, 1f);
            ctx.ContactWindow = kind == 0;
            ctx.WingPulse = 1f;
            ctx.RimFlash = 1f;
            VDVfx.Sound("CruiserDash", kind == 1 ? 0.85f : kind == 2 ? 1.15f : 1f, ctx.Owner.ProjectedCenter, 3);
            VDVfx.Shake(npc.Center, 4f, 1600f);
        }

        private void UpdatePlaneDash(VDStateContext ctx) {
            NPC npc = ctx.Npc;
            DeclareAlpha(ctx, 1f, 1f);
            ctx.ContactWindow = npc.velocity.Length() > VDDirector.PhantomContactSpeed;
            //冲刺全程满亮,拖尾风格沿速度反向抹开
            ctx.RimCharge = 1f;
            if (ctx.Phase >= 2 && Timer % VDDirector.PhantomTrailInterval == 3) {
                Shoot<VDVoidBolt>(ctx, npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY) * VDDirector.PhantomTrailSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeDashTrail);
            }
            if (Timer >= VDDirector.PhantomDashFrames) {
                dashes++;
                carryDepth = 0f;
                SwitchBeat(Beat.Brake);
                MarkNetUpdate(ctx);
            }
        }

        /// <summary>贯穿冲:Z 从起点深度线性走到终点深度,穿越帧那一瞬震屏 + 火花;判定由带把关,窗全开</summary>
        private void UpdatePierceDash(VDStateContext ctx, int kind) {
            NPC npc = ctx.Npc;
            float z0 = StartDepthOf(kind);
            float z1 = EndDepthOf(kind);
            float t = MathHelper.Clamp(Timer / (float)VDDirector.PhantomPierceFrames, 0f, 1f);
            ctx.Depth = MathHelper.Lerp(z0, z1, t);
            DeclareAlpha(ctx, 1f, 1f);
            ctx.ContactWindow = true;
            ctx.RimCharge = 1f;
            int crossFrame = (int)Math.Round(CrossFrame(kind));
            if (Timer < crossFrame) {
                ctx.DiveMarkerPos = ctx.RolledPoints[0];
                ctx.DiveMarkerProgress = Timer / (float)crossFrame;
            }
            if (Timer == crossFrame) {
                VDVfx.Shake(npc.Center, 6f, 1800f);
                VDVfx.SparkBurst(npc.Center, VDVfx.VoidWhite, 20, 4f, 12f, 22, 0.5f, 1f);
                VDVfx.Sound("VoidAttack", 1.1f, npc.Center, 3, 0.7f);
                ctx.RimFlash = 1f;
            }
            //远→近的冲刺越过镜头那一瞬:呼啸 + 径向拖影
            float prevDepth = MathHelper.Lerp(z0, z1, MathHelper.Clamp((Timer - 1) / (float)VDDirector.PhantomPierceFrames, 0f, 1f));
            if (prevDepth > VDDirector.DepthWhooshZ && ctx.Depth <= VDDirector.DepthWhooshZ) {
                VDVfx.PassBy(ctx.Owner.ProjectedCenter, 0.8f);
            }
            if (ctx.Phase >= 2 && Math.Abs(ctx.Depth) < 0.3f && Timer % VDDirector.PhantomTrailInterval == 3) {
                Shoot<VDVoidBolt>(ctx, npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY) * VDDirector.PhantomTrailSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeDashTrail);
            }
            if (Timer >= VDDirector.PhantomPierceFrames) {
                dashes++;
                carryDepth = z1;
                SwitchBeat(Beat.Brake);
                MarkNetUpdate(ctx);
            }
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
