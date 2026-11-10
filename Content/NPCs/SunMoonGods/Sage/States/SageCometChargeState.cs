using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Sage;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 彗星冲,对应先知的冲刺:每冲都是 站位瞄准 → 飞行 → 刹车 → 星遁换位 四段,按 Timer 循环
    /// 瞄准段航道跟着玩家转,最后 ChargeLockLead 帧本体刹停、航向写入 LockedAngle,出手沿冻结的线飞,不再追瞄
    /// 站位方位从 BaseAngle 起按固定步进换,各端自己能算;接触伤害窗是速度门槛
    /// </summary>
    [VaultState((int)SageStateIndex.CometCharge, typeof(SageStateContext))]
    public class SageCometChargeState : SageStateBase
    {
        private const int FlyStart = SageDirector.ChargeAimFrames;
        private const int BrakeStart = FlyStart + SageDirector.ChargeFlyFrames;
        private const int RepositionStart = BrakeStart + SageDirector.ChargeBrakeFrames;

        public override SageStateIndex StateIndex => SageStateIndex.CometCharge;

        public override Vector2 AnchorFor(SageStateContext ctx)
            => ctx.Target.Center + ctx.BaseAngle.ToRotationVector2() * SageDirector.ChargeStandoff;

        /// <summary>第一冲从本体当前所在的方位起手</summary>
        public override void OnPicked(SageStateContext ctx) {
            ctx.BaseAngle = (ctx.Npc.Center - ctx.Target.Center).ToRotation();
        }

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            NPC npc = ctx.Npc;
            int reps = SageDirector.ChargeReps(ctx.Phase);
            int rep = Math.Min(Timer / SageDirector.ChargeCycle, reps - 1);
            int cycleStart = rep * SageDirector.ChargeCycle;
            int local = Timer - cycleStart;
            bool last = rep == reps - 1;

            if (local < FlyStart) {
                Aim(ctx, cycleStart, local, rep);
            }
            else if (local < BrakeStart) {
                Fly(ctx, cycleStart, local - FlyStart);
            }
            else if (local < RepositionStart || last) {
                Direct(ctx);
                npc.velocity *= SageDirector.ChargeBrakeDrag;
                FaceTo(ctx, ctx.LockedAngle);
                ctx.ContactWindow = npc.velocity.Length() > SageDirector.ChargeContactSpeed;
                ctx.WingSpread = -0.6f;
                if (last && local >= RepositionStart + SageDirector.ChargeTail) {
                    return EndAttack(ctx);
                }
            }
            else {
                //星遁到下一个站位,落点标记从这一拍起就在
                if (At(cycleStart + RepositionStart) && IsServer) {
                    ctx.Owner.StartBlink(ctx.Target.Center + Bearing(ctx, rep + 1).ToRotationVector2() * SageDirector.ChargeStandoff);
                }
                Direct(ctx);
                npc.velocity *= 0.8f;
            }
            return null;
        }

        /// <summary>第 rep 冲的站位方位</summary>
        private static float Bearing(SageStateContext ctx, int rep)
            => ctx.BaseAngle + rep * SageDirector.ChargeAngleStep * ctx.SideDir;

        private void Aim(SageStateContext ctx, int cycleStart, int local, int rep) {
            NPC npc = ctx.Npc;
            int lockAt = SageDirector.ChargeAimFrames - SageDirector.ChargeLockLead;
            bool locked = local >= lockAt;
            float p = local / (float)SageDirector.ChargeAimFrames;

            if (!locked) {
                HoldAt(ctx, Bearing(ctx, rep).ToRotationVector2() * SageDirector.ChargeStandoff, 0.14f, 0.3f, 36f);
            }
            else {
                Direct(ctx);
                npc.velocity *= 0.6f;
            }

            if (At(cycleStart + 2)) {
                SageVfx.Cue("bell", npc.Center, 1.3f, 0.7f);
            }
            if (At(cycleStart + lockAt)) {
                //客户端先按本地位置算一个,权威端的值随包覆盖
                ctx.LockedAngle = (ctx.Target.Center - npc.Center).ToRotation();
                ctx.CrestPulse = 0.6f;
                MarkNetUpdate(ctx);
            }

            Vector2 dir = locked ? ctx.LockedAngle.ToRotationVector2() : (ctx.Target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
            FaceTo(ctx, dir.ToRotation());
            ctx.LaneStart = npc.Center;
            ctx.LaneDir = dir;
            ctx.LaneLength = SageDirector.ChargeLaneLength;
            ctx.LaneStrength = locked ? 0.7f + 0.3f * p : 0.15f + 0.45f * p;
            ctx.CrestGlow = Math.Max(ctx.CrestGlow, 0.5f + 0.5f * p);
            ctx.WingSpread = -0.8f * p;
            ctx.BodyGlow = 0.3f * p;
            if (Timer % 2 == 0) {
                SageVfx.Converge(npc.Center, 90f, 200f);
            }
        }

        private void Fly(SageStateContext ctx, int cycleStart, int fly) {
            NPC npc = ctx.Npc;
            Vector2 dir = ctx.LockedAngle.ToRotationVector2();
            float speed = SageDirector.ChargeSpeed * MathHelper.Clamp((fly + 1f) / SageDirector.ChargeRampFrames, 0f, 1f);
            Direct(ctx);
            npc.velocity = dir * speed;
            FaceTo(ctx, ctx.LockedAngle);
            ctx.ContactWindow = speed > SageDirector.ChargeContactSpeed;
            ctx.WingSpread = -1f;
            ctx.CrestGlow = 1f;
            ctx.BodyGlow = 0.4f;
            ctx.LaneStart = npc.Center;
            ctx.LaneDir = dir;
            ctx.LaneLength = SageDirector.ChargeLaneLength;
            ctx.LaneStrength = 1f - fly / 8f;

            if (At(cycleStart + FlyStart)) {
                ctx.CrestPulse = 1f;
                SageVfx.Burst(npc.Center, 1.6f);
                SageVfx.Shake(npc.Center, 5f, 2000f);
                SageVfx.Cue("RuneDash", npc.Center, 1f, 0.9f);
            }
            SageVfx.Stardust(npc.Center - dir * 40f, 2, 1f, 4f);

            if (ctx.Phase >= 2 && fly % SageDirector.ChargeBoltInterval == 2) {
                Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);
                Shoot<SageStarWisp>(ctx, npc.Center, normal * SageDirector.ChargeBoltSpeed, SageDirector.DmgWisp, 0f, SageDirector.ChargeBoltLife);
                Shoot<SageStarWisp>(ctx, npc.Center, -normal * SageDirector.ChargeBoltSpeed, SageDirector.DmgWisp, 0f, SageDirector.ChargeBoltLife);
            }
        }
    }
}