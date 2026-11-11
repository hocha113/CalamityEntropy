using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 史莱姆猛击:压缩蓄力 → 高高跃起 → 弧顶悬停并锁定落点,航道亮起 → 沿锁定的线砸下 → 落地出 15 格冲击波
    /// 拍长都固定,拍点由 Timer 算出;俯冲方向在悬停起始帧写入 LockedAngle,之后不再追瞄
    /// </summary>
    [VaultState((int)VSlimeStateIndex.PaleSmash, typeof(VSlimeStateContext))]
    public class VSlimePaleSmashState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.PaleSmash;

        private const int LeapStart = VSlimeDirector.SmashChargeFrames;
        private const int HoldStart = LeapStart + VSlimeDirector.SmashLeapFrames;
        private const int DiveStart = HoldStart + VSlimeDirector.SmashHoldFrames;
        private const int DiveEnd = DiveStart + VSlimeDirector.SmashDiveWindow;

        /// <summary>已经落地,各端按自己的碰撞结果记</summary>
        private bool landed;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            landed = false;
            //零向量当作还没锁定,客户端收到锁定包之前不画航道
            if (IsServer) {
                ctx.LockedPoint = Vector2.Zero;
                MarkNetUpdate(ctx);
            }
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            if (Timer < LeapStart) {
                Charge(ctx);
            }
            else if (Timer < HoldStart) {
                Leap(ctx);
            }
            else if (Timer < DiveStart) {
                Hold(ctx);
            }
            else if (!landed) {
                Dive(ctx);
            }
            else {
                Ground(ctx);
                npc.velocity.X *= 0.8f;
            }
            return Timer >= VSlimeDirector.SmashDuration ? EndAttack(ctx) : null;
        }

        private void Charge(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            Ground(ctx);
            npc.velocity.X *= 0.8f;
            float p = VSlimeDirector.Progress(Timer, 0, VSlimeDirector.SmashChargeFrames);
            ctx.Squash = 0.5f * VSlimeDirector.EaseOut(p);
            ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.45f * p);
            ctx.BodyGlow = 0.3f * p;
            if (Timer % 3 == 0) {
                VSlimeVfx.Converge(npc.Center, 90f, 220f, DustID.Silver);
            }
            if (At(1)) {
                VSlimeVfx.Cue("charge", npc.Center, 0.8f, 0.6f, 2);
            }
        }

        private void Leap(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            Ground(ctx);
            ctx.Squash = -0.3f;
            if (At(LeapStart)) {
                float vx = (ctx.Target.Center.X - npc.Center.X) / VSlimeDirector.SmashLeapFrames;
                vx = MathHelper.Clamp(vx, -VSlimeDirector.SmashLeapMaxSpeedX, VSlimeDirector.SmashLeapMaxSpeedX);
                npc.velocity = new Vector2(vx, -VSlimeDirector.SmashLeapSpeed);
                ctx.Pulse = 0.5f;
                MarkNetUpdate(ctx);
                VSlimeVfx.PaleShards(npc.Bottom, 12, 2f, 7f);
                VSlimeVfx.Sound(SoundID.NPCHit1 with { Volume = 0.8f, Pitch = -0.7f }, npc.Center);
            }
        }

        private void Hold(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            Direct(ctx);
            npc.velocity *= 0.6f;
            if (At(HoldStart)) {
                if (IsServer) {
                    ctx.LockedPoint = ctx.Target.Center;
                    ctx.LockedAngle = (ctx.Target.Center - npc.Center).SafeNormalize(Vector2.UnitY).ToRotation();
                    MarkNetUpdate(ctx);
                }
                ctx.Pulse = 0.7f;
                VSlimeVfx.Sound(SoundID.Item154 with { Pitch = 0.2f, Volume = 0.7f }, npc.Center);
            }
            float p = VSlimeDirector.Progress(Timer, HoldStart, VSlimeDirector.SmashHoldFrames);
            ctx.Squash = 0.2f * p;
            ctx.BodyGlow = 0.3f + 0.4f * p;
            if (ctx.LockedPoint == Vector2.Zero) {
                return;
            }
            Vector2 dir = ctx.LockedAngle.ToRotationVector2();
            ctx.LaneStart = npc.Center;
            ctx.LaneEnd = npc.Center + dir * (Vector2.Distance(npc.Center, ctx.LockedPoint) + 320f);
            ctx.LaneWidth = VSlimeDirector.SmashLaneWidth;
            ctx.LaneStrength = 0.4f + 0.6f * p;
        }

        /// <summary>锁定包还没到的客户端先按正下方算</summary>
        private static Vector2 DiveDir(VSlimeStateContext ctx)
            => ctx.LockedPoint == Vector2.Zero ? Vector2.UnitY : ctx.LockedAngle.ToRotationVector2();

        private void Dive(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            Ground(ctx);
            ctx.ContactBase = VSlimeDirector.PaleSmashContact;
            ctx.Squash = -0.32f;
            ctx.BodyGlow = 0.5f;
            Vector2 dir = DiveDir(ctx);
            ctx.LaneStart = npc.Center;
            ctx.LaneEnd = npc.Center + dir * 420f;
            ctx.LaneWidth = VSlimeDirector.SmashLaneWidth;
            ctx.LaneStrength = 0.5f;

            bool blocked = Timer > DiveStart + 2 && (npc.velocity.Y == 0f || npc.collideX || npc.collideY);
            if (blocked || Timer >= DiveEnd - 1) {
                Land(ctx);
                return;
            }
            npc.velocity = dir * VSlimeDirector.SmashDiveSpeed;
        }

        private void Land(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            landed = true;
            npc.velocity = Vector2.Zero;
            ctx.ShakeStrength = 1f;
            ctx.Squash = 0.6f;
            ctx.Pulse = 1f;
            VSlimeVfx.PaleShards(npc.Bottom, 30, 3f, 12f);
            VSlimeVfx.Shake(npc.Center, 12f, 2600f);
            VSlimeVfx.Sound(SoundID.Item167 with { Pitch = -0.2f }, npc.Center);
            VSlimeVfx.Cue("SmashToGroundHeavy", npc.Center, 1f, 0.8f, 2);
            if (IsServer) {
                MarkNetUpdate(ctx);
                Shoot<VSlimeShockwave>(ctx, npc.Bottom + new Vector2(0f, -16f), Vector2.Zero, VSlimeDirector.DmgPaleShock, VSlimeShockwave.ModeSmash, npc.whoAmI);
            }
        }
    }
}
