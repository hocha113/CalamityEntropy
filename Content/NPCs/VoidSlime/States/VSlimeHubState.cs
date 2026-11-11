using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 虚空与奇点形态的连接段,共 2 秒:开头权威端按表选招并定落点 → 压缩起跳,团成球沿弧线跃迁过去 → 落定 → 起势
    /// 跃迁轨迹由起点、落点和 Timer 解析算出;整段没有接触伤害,起势拍的描边亮起是全招通用的出手信号
    /// 场上弹幕还密就再等一会;落点太远改用闪现
    /// </summary>
    [VaultState((int)VSlimeStateIndex.Hub, typeof(VSlimeStateContext))]
    public class VSlimeHubState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.Hub;
        public override int TimeoutFrames => 60 * 8;

        private const int FlyStart = VSlimeDirector.LeapCrouchFrames;
        private const int FlyEnd = FlyStart + VSlimeDirector.LeapFlyFrames;
        /// <summary>落地后压扁回弹的帧数</summary>
        private const int LandFrames = 14;

        /// <summary>按 PendingState 缓存的下一招实例,只用来问锚点,不进状态机</summary>
        private VSlimeStateBase probe;
        private int probeId = -1;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            probe = null;
            probeId = -1;
            //客户端往往晚于带 PendingState 的包,只权威端清
            if (IsServer) {
                ctx.PendingState = -1;
                ctx.LeapFrom = ctx.Npc.Center;
                ctx.AnchorPos = ctx.Npc.Center;
            }
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int total = VSlimeDirector.ConnectorFrames;
            int postureStart = total - VSlimeDirector.ConnectorPostureFrames;

            if (At(0) && IsServer) {
                PickAndPosition(ctx);
            }

            float leapLength = Vector2.Distance(ctx.LeapFrom, ctx.AnchorPos);
            bool leaps = leapLength >= VSlimeDirector.LeapMinDistance;
            if (Timer < FlyStart) {
                float p = VSlimeDirector.Progress(Timer, 0, FlyStart);
                ctx.Squash = leaps ? 0.36f * p : 0f;
                ctx.NoContact = leaps;
            }
            else if (Timer < FlyEnd && leaps) {
                Fly(ctx, leapLength);
            }
            else if (Timer < FlyEnd + LandFrames && leaps) {
                float t = VSlimeDirector.Progress(Timer, FlyEnd, LandFrames);
                ctx.Squash = 0.4f * (1f - t) * MathF.Cos(t * MathHelper.Pi * 1.5f);
                ctx.NoContact = true;
            }
            if (At(FlyEnd) && leaps) {
                ctx.ShakeStrength = 0.35f;
                VSlimeVfx.Stardust(npc.Center + new Vector2(0f, 40f), 14, 2f, 7f);
                VSlimeVfx.Cue("ksLand", npc.Center, 1.2f, 0.5f, 2);
            }

            if (Timer >= postureStart) {
                float p = VSlimeDirector.Progress(Timer, postureStart, VSlimeDirector.ConnectorPostureFrames);
                ctx.RimGlow = Math.Max(ctx.RimGlow, 0.3f + 0.7f * p);
                ctx.Squash += 0.12f * MathF.Sin(p * MathHelper.Pi);
            }
            if (At(postureStart)) {
                ctx.Pulse = 0.6f;
                VSlimeVfx.SkyFlash(0.25f);
                VSlimeVfx.Cue("VoidAnticipation", npc.Center, 1.1f, 0.45f, 2);
            }

            if (!IsServer || Timer < total || ctx.BlinkTimer > 0) {
                return null;
            }
            //杂波阀:上一招的弹幕还密就再等一会
            if (VSlimeHostileProjectile.CountAlive() > VSlimeDirector.ClutterThreshold && Timer < total + VSlimeDirector.ClutterWaitMax) {
                return null;
            }
            if (ctx.PendingState < 0) {
                PickAndPosition(ctx);
                return null;
            }
            VSlimeStateBase state = VSlimeRotation.Create((VSlimeStateIndex)ctx.PendingState);
            ctx.PendingState = -1;
            MarkNetUpdate(ctx);
            return state;
        }

        /// <summary>团成球沿弧线飞,位置按进度解析,速度取下一点减当前点,落后的客户端会自己收拢</summary>
        private void Fly(VSlimeStateContext ctx, float leapLength) {
            NPC npc = ctx.Npc;
            float t = (Timer - FlyStart + 1f) / VSlimeDirector.LeapFlyFrames;
            float arcHeight = MathHelper.Clamp(leapLength * VSlimeDirector.LeapArcRatio, VSlimeDirector.LeapArcMin, VSlimeDirector.LeapArcMax);
            Vector2 next = Vector2.Lerp(ctx.LeapFrom, ctx.AnchorPos, VSlimeDirector.Smooth(t));
            next.Y -= MathF.Sin(t * MathHelper.Pi) * arcHeight;
            Direct(ctx);
            npc.velocity = next - npc.Center;
            ctx.NoContact = true;
            ctx.Shape = t > 0.08f && t < 0.92f ? VSlimeShape.Orb : VSlimeShape.Slime;
            ctx.Squash = -0.2f * MathF.Sin(t * MathHelper.Pi);
            if (At(FlyStart)) {
                VSlimeVfx.Sound(SoundID.NPCHit1 with { Volume = 0.6f, Pitch = -0.6f }, npc.Center);
                VSlimeVfx.Stardust(npc.Center, 10, 2f, 6f);
            }
        }

        /// <summary>权威端选招、定侧向、换随机种子,落点近就原地等,太远就闪过去</summary>
        private void PickAndPosition(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            VSlimeStateIndex pick = VSlimeRotation.Pick(ctx);
            ctx.SideDir = ChooseSide(ctx);
            ctx.RollSeed = Main.rand.Next(1, 1 << 24);
            ctx.Combo = 0;
            ctx.PendingState = (int)pick;
            VSlimeStateBase next = Probe(ctx.PendingState);
            if (next == null) {
                ctx.PendingState = -1;
                return;
            }
            next.OnPicked(ctx);
            Vector2 anchor = next.AnchorFor(ctx);
            float dist = Vector2.Distance(anchor, npc.Center);
            ctx.LeapFrom = npc.Center;
            ctx.AnchorPos = anchor;
            if (dist < VSlimeDirector.LeapMinDistance) {
                ctx.AnchorPos = npc.Center;
            }
            else if (dist > VSlimeDirector.LeapMaxDistance) {
                ctx.LeapFrom = anchor;
                ctx.Owner.StartBlink(anchor);
            }
            npc.netUpdate = true;
        }

        /// <summary>选落点离圈心更近的一侧,两侧差不多时按上一手翻转</summary>
        private static int ChooseSide(VSlimeStateContext ctx) {
            if (!ctx.ArenaActive) {
                return -ctx.SideDir;
            }
            Vector2 target = ctx.Target.Center;
            float right = Vector2.Distance(target + new Vector2(VSlimeDirector.SideAnchorDistance, 0f), ctx.ArenaCenter);
            float left = Vector2.Distance(target - new Vector2(VSlimeDirector.SideAnchorDistance, 0f), ctx.ArenaCenter);
            if (Math.Abs(right - left) < 120f) {
                return -ctx.SideDir;
            }
            return right < left ? 1 : -1;
        }

        private VSlimeStateBase Probe(int pendingState) {
            if (pendingState < 0) {
                probe = null;
                probeId = -1;
                return null;
            }
            if (probe == null || probeId != pendingState) {
                probe = VSlimeRotation.Create((VSlimeStateIndex)pendingState);
                probeId = pendingState;
            }
            return probe;
        }
    }
}
