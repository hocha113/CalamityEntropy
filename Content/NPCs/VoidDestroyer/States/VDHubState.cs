using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 连接段 hub(48/42/36 帧,P1→P3):每一手之间可读的一口气,分三拍。
    /// 拍一「落定」:连接段开头就按 <see cref="VDRotation"/> 选好下一招(权威端),若新招几何上要求换位、
    /// 或本体离它的锚点太远,闪现在这一拍里做完;否则朝锚点飞。拍二「重瞄」:减速对准玩家,核心暗下去。
    /// 拍三「起势」(最后 12 帧):能量翼张开 + 核心亮起 + 低音,这是全招通用的「要出手了」信号。
    /// 杂波阀:连接段末尾场上敌对弹幕仍多就再等最多 60 帧,新招不在上一招的弹雨里起手。
    /// 选招结果写 <see cref="VDStateContext.PendingState"/> 过线,客户端拿它做起势表现与飞行预测;
    /// 阶段签名首招(变形 → 轨道轰炸,护盾 → 湮灭主炮)由转阶段状态写 ForcedNextState,Pick 照单执行
    /// </summary>
    [VaultState((int)VDStateIndex.Hub, typeof(VDStateContext))]
    public class VDHubState : VDStateBase
    {
        public override string StateName => "Hub";
        public override VDStateIndex StateIndex => VDStateIndex.Hub;
        /// <summary>闪现是连接段拍一的一部分,计时不暂停</summary>
        public override bool RunsDuringBlink => true;
        /// <summary>hub 卡死只可能是目标失效,那由全局转移接走;这里给个宽松上限回到自己</summary>
        public override int TimeoutFrames => 60 * 10;

        /// <summary>按 PendingState 缓存的下一招实例(只用来问锚点/是否必须闪现,不进状态机)</summary>
        private VDStateBase pendingProbe;
        private int pendingProbeId = -1;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            pendingProbe = null;
            pendingProbeId = -1;
            //只有权威端清:客户端进 hub 往往晚于携带 PendingState 的那个包,清掉就丢了起势表现的依据
            if (IsServer) {
                ctx.PendingState = -1;
            }
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            if (!ctx.TargetValid) {
                return null;
            }
            NPC npc = ctx.Npc;
            int total = VDDirector.ConnectorFrames(ctx.Phase);
            int postureStart = total - VDDirector.ConnectorPostureFrames;

            //拍一开头:权威端选招、决定闪现还是飞
            if (Timer == 1 && IsServer) {
                PickAndPosition(ctx);
            }

            VDStateBase probe = Probe(ctx.PendingState);
            Vector2 anchor = probe != null ? probe.AnchorFor(ctx) : DefaultAnchor(ctx);

            if (Timer <= VDDirector.ConnectorSettleFrames) {
                //落定:刚闪现的话宿主正在换位,这里声明的运动被闪现接管;没闪现就开始飞
                DeclareHoverTo(ctx, anchor, VDDirector.ConnectorFlySpeed, 0.12f, 140f);
            }
            else if (Timer <= postureStart) {
                //重瞄:减速逼近锚点,核心暗
                DeclareHoverTo(ctx, anchor, VDDirector.ConnectorFlySpeed * 0.75f, 0.1f, 160f);
                ctx.CoreGlow = Math.Min(ctx.CoreGlow, 0.15f);
            }
            else {
                //起势:几乎停住,翼张核心亮
                DeclareHoverTo(ctx, anchor, 8f, 0.1f, 160f);
                float p = MathHelper.Clamp((Timer - postureStart) / (float)VDDirector.ConnectorPostureFrames, 0f, 1f);
                ctx.WingPulse = Math.Max(ctx.WingPulse, p);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, 0.3f + 0.7f * p);
                if (Timer == postureStart + 1) {
                    VDVfx.Sound("VoidAnticipation", 0.7f, npc.Center, 3, 0.7f);
                    //天幕力场跟着起势轻脉冲一下(不出冲击环)
                    VDSkyDrive.PushFlash(VDDirector.SkyFlashPosture);
                }
            }

            if (!IsServer || Timer < total) {
                return null;
            }

            //杂波阀:上一招的弹雨还密就再等一会,起势姿态保持
            if (CountHostileProjectiles() > VDDirector.ClutterThreshold && Timer < total + VDDirector.ClutterWaitMax) {
                return null;
            }

            if (ctx.PendingState < 0) {
                //选招失败(注册表缺项,框架已打日志):重选
                PickAndPosition(ctx);
                return null;
            }
            IVDState next = VDRotation.Create((VDStateIndex)ctx.PendingState);
            ctx.PendingState = -1;
            MarkNetUpdate(ctx);
            return next;
        }

        /// <summary>权威端:选招,写 PendingState,按新招的锚点决定闪现或飞行</summary>
        private void PickAndPosition(VDStateContext ctx) {
            VDStateIndex pick = VDRotation.Pick(ctx);
            //每手换一次侧向:同一招左右出场不同
            ctx.SideDir = Main.rand.NextBool() ? -1 : 1;
            ctx.RandCount = 0;
            ctx.PendingState = (int)pick;

            VDStateBase probe = Probe(ctx.PendingState);
            if (probe == null) {
                ctx.PendingState = -1;
                return;
            }
            Vector2 anchor = probe.AnchorFor(ctx);
            float dist = Vector2.Distance(ctx.Npc.Center, anchor);
            if (probe.NeedsRepositionBlink || dist > VDDirector.ConnectorBlinkDistance) {
                ctx.Owner.StartBlink(anchor);
            }
            ctx.Npc.netUpdate = true;
        }

        /// <summary>取下一招的探针实例(缓存,PendingState 变了才重建)</summary>
        private VDStateBase Probe(int pendingState) {
            if (pendingState < 0) {
                pendingProbe = null;
                pendingProbeId = -1;
                return null;
            }
            if (pendingProbe == null || pendingProbeId != pendingState) {
                pendingProbe = VDRotation.Create((VDStateIndex)pendingState) as VDStateBase;
                pendingProbeId = pendingState;
            }
            return pendingProbe;
        }

        private static Vector2 DefaultAnchor(VDStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.ConnectorDefaultAnchor.X, VDDirector.ConnectorDefaultAnchor.Y);
    }
}
