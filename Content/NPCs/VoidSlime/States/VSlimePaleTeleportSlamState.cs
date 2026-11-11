using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 传送凝胶球:压扁沉下 → 闪到玩家头顶悬停,下方亮出航道 → 快速砸落 → 落地弹出两圈灰色凝胶球,连做两轮
    /// 每轮总长固定,拍点都由 Timer 算出;航道在闪现那一拍锁死,悬停期间不再跟玩家
    /// 下落前半段不碰物块,过了锁定高度才开碰撞,头顶有天花板时不会卡在里面
    /// </summary>
    [VaultState((int)VSlimeStateIndex.PaleTeleportSlam, typeof(VSlimeStateContext))]
    public class VSlimePaleTeleportSlamState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.PaleTeleportSlam;

        private const int HoverStart = VSlimeDirector.SlamSinkFrames + VSlimeDirector.BlinkDuration;
        private const int FallStart = HoverStart + VSlimeDirector.SlamHoverFrames;
        private const int FallEnd = FallStart + VSlimeDirector.SlamFallWindow;

        /// <summary>本轮已经落地,各端按自己的碰撞结果记</summary>
        private bool landed;
        private int lastRep = -1;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            landed = false;
            lastRep = -1;
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int rep = Timer / VSlimeDirector.SlamCycle;
            if (rep >= VSlimeDirector.SlamReps) {
                Ground(ctx);
                npc.velocity.X *= 0.8f;
                return EndAttack(ctx);
            }
            if (rep != lastRep) {
                lastRep = rep;
                landed = false;
            }
            int cycleStart = rep * VSlimeDirector.SlamCycle;
            int local = Timer - cycleStart;

            if (local < VSlimeDirector.SlamSinkFrames) {
                Sink(ctx, cycleStart, local);
            }
            else if (local < FallStart) {
                HoverAbove(ctx, local);
            }
            else {
                Fall(ctx, cycleStart, local);
            }
            return null;
        }

        private void Sink(VSlimeStateContext ctx, int cycleStart, int local) {
            NPC npc = ctx.Npc;
            Ground(ctx);
            npc.velocity.X *= 0.8f;
            float p = VSlimeDirector.Progress(local, 0, VSlimeDirector.SlamSinkFrames);
            ctx.Squash = 0.55f * p;
            ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.3f * p);
            ctx.BodyGlow = 0.35f * p;
            if (At(cycleStart + 2)) {
                VSlimeVfx.Sound(SoundID.Item8 with { Pitch = -0.4f }, npc.Center);
            }
            //沉到底的那一拍锁定落点并起闪现
            if (At(cycleStart + VSlimeDirector.SlamSinkFrames - 1) && IsServer) {
                ctx.LockedPoint = ctx.Target.Center;
                ctx.Owner.StartBlink(ctx.Target.Center + new Vector2(0f, -VSlimeDirector.SlamHoverHeight));
            }
        }

        private static void HoverAbove(VSlimeStateContext ctx, int local) {
            NPC npc = ctx.Npc;
            Direct(ctx);
            npc.velocity = Vector2.Zero;
            ctx.NoContact = true;
            float p = VSlimeDirector.Progress(local, HoverStart, VSlimeDirector.SlamHoverFrames);
            ctx.Squash = -0.2f * p;
            if (ctx.BlinkTimer <= VSlimeDirector.BlinkDuration / 2) {
                ctx.LaneStart = npc.Center;
                ctx.LaneEnd = npc.Center + new Vector2(0f, VSlimeDirector.SlamLaneLength);
                ctx.LaneWidth = VSlimeDirector.SlamLaneWidth;
                ctx.LaneStrength = 0.35f + 0.65f * p;
            }
        }

        private void Fall(VSlimeStateContext ctx, int cycleStart, int local) {
            NPC npc = ctx.Npc;
            if (landed) {
                Ground(ctx);
                npc.velocity.X *= 0.8f;
                return;
            }
            if (At(cycleStart + FallStart)) {
                VSlimeVfx.Sound(SoundID.Item154 with { Pitch = -0.5f, Volume = 0.7f }, npc.Center);
            }
            //过了锁定高度才认物块
            bool collide = npc.Bottom.Y >= ctx.LockedPoint.Y - 24f;
            Direct(ctx);
            ctx.GroundPhysics = collide;
            ctx.Squash = -0.3f;
            ctx.LaneStart = npc.Center;
            ctx.LaneEnd = npc.Center + new Vector2(0f, VSlimeDirector.SlamLaneLength * 0.5f);
            ctx.LaneWidth = VSlimeDirector.SlamLaneWidth;
            ctx.LaneStrength = 0.5f;

            bool hitGround = collide && local > FallStart + 2 && npc.velocity.Y == 0f;
            if (hitGround || local >= FallEnd - 1) {
                Land(ctx);
                return;
            }
            npc.velocity = new Vector2(0f, VSlimeDirector.SlamFallSpeed);
        }

        private void Land(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            landed = true;
            npc.velocity = Vector2.Zero;
            ctx.ShakeStrength = 0.7f;
            ctx.Squash = 0.5f;
            ctx.Pulse = 0.6f;
            VSlimeVfx.PaleShards(npc.Bottom, 20, 2f, 9f);
            VSlimeVfx.Shake(npc.Center, 8f, 2400f);
            VSlimeVfx.Sound(SoundID.Item167, npc.Center);
            if (!IsServer) {
                return;
            }
            MarkNetUpdate(ctx);
            Vector2 origin = npc.Bottom + new Vector2(0f, -28f);
            FireRing(ctx, origin, VSlimeDirector.SlamRingInner, VSlimeDirector.SlamRingInnerSpeed);
            FireRing(ctx, origin, VSlimeDirector.SlamRingOuter, VSlimeDirector.SlamRingOuterSpeed);
        }

        /// <summary>在正上方两侧的扇面里等角摊开,两圈颗数不同,方位自然错开</summary>
        private static void FireRing(VSlimeStateContext ctx, Vector2 origin, int count, float speed) {
            float half = VSlimeDirector.SlamRingHalfAngle;
            for (int i = 0; i < count; i++) {
                float t = (i + 0.5f) / count;
                float angle = -MathHelper.PiOver2 + MathHelper.Lerp(-half, half, t);
                Shoot<VSlimeGreyGel>(ctx, origin, angle.ToRotationVector2() * speed, VSlimeDirector.DmgGreyGel);
            }
        }
    }
}
