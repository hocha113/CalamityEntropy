using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 深空跃迁出场(260 帧):0-40 在 Z 6 的深空里开一枚极小的跃迁门(天幕闪光 + 冲击环);40-130 本体从门里出来、
    /// 立方缓入地朝镜头飞来(Z 6 → -0.45,先是一颗星,最后几帧猛地放大到擦着屏幕上缘飞过);130 掠过镜头(呼啸 + 震屏 + 径向拖影);
    /// 130-170 从镜头后方拉回平面(立方缓出硬刹,落定一记冲击环);170-260 静止威压并交还相机。
    /// 全程无敌无接触、限制圈不生效;世界坐标钉在锚点,深度变化让投影位置自己从消失点滑到锚点再越过屏幕边缘再回来;
    /// 相机前 30 帧滑向锚点,200 帧后不再赋值,交给 EModPlayer 的自然衰减滑回玩家
    /// </summary>
    [VaultState((int)VDStateIndex.Entrance, typeof(VDStateContext))]
    public class VDEntranceState : VDStateBase
    {
        public override string StateName => "Entrance";
        public override VDStateIndex StateIndex => VDStateIndex.Entrance;
        public override bool ContactByDefault => false;
        public override bool RunsDuringBlink => true;
        public override int TimeoutFrames => int.MaxValue;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            ctx.Npc.velocity = Vector2.Zero;
            //从深空起手:深度视觉直接钉在起点,不能从平面缩过去
            ctx.Owner.SnapDepth(VDDirector.EntranceStartDepth);
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            DeclareDirect(ctx);
            npc.velocity = Vector2.Zero;
            npc.Center = ctx.AnchorPos;
            ctx.ArenaActive = false;
            ctx.WingsVisible = false;

            int t = Timer;
            if (t <= VDDirector.EntranceWarpFrames) {
                //跃迁门在深空里张开:本体还没出来
                ctx.Depth = VDDirector.EntranceStartDepth;
                DeclareAlpha(ctx, 0f, 1f);
                ctx.PortalDepth = VDDirector.EntranceStartDepth;
                ctx.PortalOpenness = VDVfx.EaseOut(t / (float)VDDirector.EntranceWarpFrames);
                if (t == 1) {
                    VDVfx.Sound("portal_emerge", 1.3f, ctx.Owner.ProjectedCenter, 2, 0.8f);
                }
                if (t == VDDirector.EntranceWarpFrames / 2) {
                    //天幕:跃迁闪光够强,从门的位置放一圈冲击环
                    VDSkyDrive.PushFlash(0.8f);
                }
            }
            else if (t <= VDDirector.EntranceApproachEnd) {
                float p = (t - VDDirector.EntranceWarpFrames) / (float)(VDDirector.EntranceApproachEnd - VDDirector.EntranceWarpFrames);
                //立方缓入:大半时间是一颗慢慢变大的星,最后十几帧猛扑过来
                ctx.Depth = MathHelper.Lerp(VDDirector.EntranceStartDepth, VDDirector.EntrancePassDepth, VDDepth.DiveCurve(p));
                DeclareAlpha(ctx, MathHelper.Clamp((t - VDDirector.EntranceWarpFrames) / 20f, 0f, 1f), 1f);
                //门在本体出来后 30 帧内关上
                ctx.PortalDepth = VDDirector.EntranceStartDepth;
                ctx.PortalOpenness = 1f - MathHelper.Clamp((t - VDDirector.EntranceWarpFrames) / 30f, 0f, 1f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, p * 0.8f);
                ctx.RimCharge = p;
                //引擎音逐级升调:越近越尖
                int local = t - VDDirector.EntranceWarpFrames;
                if (local == 20 || local == 50 || local == 75) {
                    VDVfx.Sound("VoidAnticipation", 0.6f + local / 190f, ctx.Owner.ProjectedCenter, 3, 0.9f);
                }
                if (t == VDDirector.EntranceApproachEnd) {
                    //掠过镜头的一瞬
                    VDVfx.PassBy(ctx.Owner.ProjectedCenter, 1f);
                    VDVfx.Shake(npc.Center, 8f);
                    ctx.RimFlash = 1f;
                    ctx.ShakeStrength = 0.5f;
                }
            }
            else if (t <= VDDirector.EntranceArriveFrame) {
                //从镜头后方拉回平面:立方缓出 = 硬刹
                float p = (t - VDDirector.EntranceApproachEnd) / (float)(VDDirector.EntranceArriveFrame - VDDirector.EntranceApproachEnd);
                ctx.Depth = VDDirector.EntrancePassDepth * (1f - VDDepth.RetreatCurve(p));
                DeclareAlpha(ctx, 1f, 1f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, 0.5f);
                if (t == VDDirector.EntranceArriveFrame) {
                    //落定的一记:冲击环 + 震屏 + 核心亮 + 描边爆闪
                    VDVfx.DiveShock(npc.Center, 1.2f);
                    ctx.CoreGlow = 1f;
                    ctx.RimFlash = 1f;
                    ctx.ShakeStrength = 0.6f;
                }
            }
            else {
                ctx.Depth = 0f;
                DeclareAlpha(ctx, 1f, 1f);
            }

            if (t <= VDDirector.EntranceCameraFrames) {
                ctx.CameraFocus = ctx.AnchorPos + new Vector2(0, 80);
                ctx.CameraShift = t < 30 ? t / 30f * 0.12f : 0.12f;
            }

            if (t >= VDDirector.EntranceDuration) {
                return new VDHubState();
            }
            return null;
        }
    }
}
