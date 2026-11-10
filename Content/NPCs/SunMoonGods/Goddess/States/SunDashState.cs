using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 曜日冲,对应原版冲刺:每冲都是 站位瞄准 → 飞行 → 刹车 三段,按 Timer 循环
    /// 最后 DashLockLead 帧本体刹停、航道冻结,出手沿冻结的线飞;接触伤害窗是速度门槛
    /// 横冲航向只由站位侧决定,各端自己能算;三阶段第三冲从斜上方来,航向在锁向拍写入 LockedAngle
    /// </summary>
    [VaultState((int)SunStateIndex.SolarDash, typeof(SunStateContext))]
    public class SunDashState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.SolarDash;

        public override Vector2 AnchorFor(SunStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SunDirector.DashStandoff, 0f);

        /// <summary>第一冲从本体当前所在的一侧起手,不越过玩家头顶去换边</summary>
        public override void OnPicked(SunStateContext ctx) {
            ctx.SideDir = ctx.Npc.Center.X >= ctx.Target.Center.X ? 1 : -1;
        }

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            NPC npc = ctx.Npc;
            int reps = SunDirector.DashReps(ctx.Phase);
            int rep = Timer / SunDirector.DashCycle;
            if (rep >= reps) {
                Direct(ctx);
                npc.velocity *= 0.85f;
                return Timer >= reps * SunDirector.DashCycle + SunDirector.DashTail ? EndAttack(ctx) : null;
            }

            int cycleStart = rep * SunDirector.DashCycle;
            int local = Timer - cycleStart;
            //上一冲穿过玩家后本体已在另一侧,每冲换边
            int side = rep % 2 == 0 ? ctx.SideDir : -ctx.SideDir;
            bool diagonal = ctx.Phase >= 3 && rep == 2;

            if (local < SunDirector.DashAimFrames) {
                Aim(ctx, cycleStart, local, side, diagonal);
            }
            else if (local < SunDirector.DashAimFrames + SunDirector.DashFlyFrames) {
                Fly(ctx, cycleStart, local - SunDirector.DashAimFrames, side, diagonal);
            }
            else {
                Direct(ctx);
                npc.velocity *= SunDirector.DashBrakeDrag;
                ctx.ContactWindow = npc.velocity.Length() > SunDirector.DashContactSpeed;
            }
            return null;
        }

        private void Aim(SunStateContext ctx, int cycleStart, int local, int side, bool diagonal) {
            NPC npc = ctx.Npc;
            int lockAt = SunDirector.DashAimFrames - SunDirector.DashLockLead;
            bool locked = local >= lockAt;
            float p = local / (float)SunDirector.DashAimFrames;

            if (!locked) {
                Vector2 standoff = diagonal
                    ? new Vector2(side * SunDirector.DashDiagonalStandoff.X, SunDirector.DashDiagonalStandoff.Y)
                    : new Vector2(side * SunDirector.DashStandoff, 0f);
                HoldAt(ctx, standoff, 0.14f, 0.3f, 36f);
            }
            else {
                Direct(ctx);
                npc.velocity *= 0.6f;
            }

            if (At(cycleStart + 2)) {
                SunVfx.Sound(SoundID.Item160, npc.Center);
            }
            if (At(cycleStart + lockAt)) {
                if (diagonal) {
                    ctx.LockedAngle = (ctx.Target.Center - npc.Center).ToRotation();
                }
                ctx.HaloPulse = 0.6f;
                MarkNetUpdate(ctx);
            }

            Vector2 dir = LaneDir(ctx, side, diagonal, locked);
            ctx.LaneStart = npc.Center;
            ctx.LaneDir = dir;
            ctx.LaneLength = SunDirector.DashLaneLength;
            ctx.LaneStrength = locked ? 0.7f + 0.3f * p : 0.15f + 0.45f * p;
            //起手前向后仰
            ctx.TiltOverride = -0.15f * dir.X;
            ctx.ArmLeft = 2;
            ctx.ArmRight = 2;
            ctx.HaloGlow = Math.Max(ctx.HaloGlow, 0.5f + 0.5f * p);
            ctx.WingPose = -0.8f * p;
        }

        private void Fly(SunStateContext ctx, int cycleStart, int fly, int side, bool diagonal) {
            NPC npc = ctx.Npc;
            Vector2 dir = LaneDir(ctx, side, diagonal, true);
            float speed = SunDirector.DashSpeed * MathHelper.Clamp((fly + 1f) / SunDirector.DashRampFrames, 0f, 1f);
            Direct(ctx);
            npc.velocity = dir * speed;
            ctx.ContactWindow = speed > SunDirector.DashContactSpeed;
            ctx.TiltOverride = 0.3f * dir.X;
            ctx.WingPose = 0.45f;
            ctx.HaloGlow = 1f;
            //帧 5 横过身前,帧 6 向外伸直,都指向冲刺方向
            ctx.ArmLeft = dir.X >= 0f ? 5 : 6;
            ctx.ArmRight = dir.X >= 0f ? 6 : 5;
            ctx.LaneStart = npc.Center;
            ctx.LaneDir = dir;
            ctx.LaneLength = SunDirector.DashLaneLength;
            ctx.LaneStrength = 1f - fly / 8f;

            if (At(cycleStart + SunDirector.DashAimFrames)) {
                ctx.HaloPulse = 1f;
                SunVfx.Burst(npc.Center, 1.6f);
                SunVfx.Shake(npc.Center, 5f, 2000f);
                SunVfx.Sound(SoundID.DD2_PhantomPhoenixShot with { Volume = 0.9f, Pitch = -0.3f }, npc.Center);
            }
            SunVfx.Embers(npc.Center - dir * 30f, 2, 1f, 4f);

            if (ctx.Phase >= 2 && fly % SunDirector.DashFeatherInterval == 2) {
                Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);
                Shoot<SunFlareBolt>(ctx, npc.Center, normal * SunDirector.DashFeatherSpeed, SunDirector.DmgFlare, SunDirector.DashFeatherLife);
                Shoot<SunFlareBolt>(ctx, npc.Center, -normal * SunDirector.DashFeatherSpeed, SunDirector.DmgFlare, SunDirector.DashFeatherLife);
            }
        }

        /// <summary>横冲恒为朝玩家一侧的水平方向;斜冲锁向前跟着玩家,锁向后读 LockedAngle</summary>
        private static Vector2 LaneDir(SunStateContext ctx, int side, bool diagonal, bool locked) {
            Vector2 horizontal = new Vector2(-side, 0f);
            if (!diagonal) {
                return horizontal;
            }
            return locked ? ctx.LockedAngle.ToRotationVector2() : (ctx.Target.Center - ctx.Npc.Center).SafeNormalize(horizontal);
        }
    }
}