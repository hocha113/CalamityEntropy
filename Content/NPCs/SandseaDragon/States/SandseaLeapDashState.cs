using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 跳跃冲刺(A):战吼后从沙里竖直上浮,沿四分之一圆压到与玩家同高的航道上,在 26 格外与玩家保持相对静止,
    /// 前半身亮起黄色屏障,水平冲刺 60 格;共三冲,间隔 100 帧
    /// 每冲之后向上兜一个 e 字形的圈折返,水平落回航道再对齐;没追上玩家(玩家还在前方)就不掉头,直接在身后对齐
    /// 公平阀:出手前 14 帧航道冻结并画出,冲刺只沿冻结的水平线飞,上下离开航道即可躲开
    /// 末冲沿途留 12 发毒针,各自在出生时锁向;二阶段每冲上下各带三只沙鲨,锁向拍就排好队形,航道随之画宽
    /// </summary>
    [VaultState((int)SandseaStateIndex.LeapDash, typeof(SandseaStateContext))]
    public class SandseaLeapDashState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.LeapDash;
        public override int TimeoutFrames => 60 * 14;

        #region 纯本地闩锁,各端按自己看到的位置推导
        /// <summary>首冲前起转四分之一圆的帧,-1 为还在上浮</summary>
        private int turnAt = -1;
        /// <summary>已经压上航道,改为与玩家保持相对静止</summary>
        private bool aligned;
        #endregion

        /// <summary>上浮线的横坐标:对齐位外侧一个转弯半径,从这里竖直上浮后四分之一圆正好落在对齐位</summary>
        private static float RiseX(SandseaStateContext ctx)
            => ctx.Target.Center.X + ctx.SideDir * (SandseaDirector.DashStandoff + SandseaDirector.DashArcRadius);

        public override Vector2 AnchorFor(SandseaStateContext ctx) {
            float x = RiseX(ctx);
            return new Vector2(x, SurfaceAt(ctx, x) + SandseaDirector.RiseDeepDepth);
        }

        private static float LaneHalf(int phase)
            => phase >= 2 ? SandseaDirector.EscortPerSide * SandseaDirector.EscortSpacingY + 24f : SandseaDirector.DashLaneHalfWidth;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            turnAt = -1;
            aligned = false;
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            if (Timer < SandseaDirector.DashLockAt(0)) {
                Opening(ctx);
                return null;
            }
            int index = Math.Min((Timer - SandseaDirector.DashLockAt(0)) / SandseaDirector.DashCycle, SandseaDirector.DashCount - 1);
            int local = Timer - SandseaDirector.DashLockAt(index);
            int afterFly = local - SandseaDirector.DashLockFrames - SandseaDirector.DashFlyFrames;

            if (local < SandseaDirector.DashLockFrames) {
                Lock(ctx, index, local);
            }
            else if (afterFly < 0) {
                Fly(ctx, index, local - SandseaDirector.DashLockFrames);
            }
            else if (index < SandseaDirector.DashCount - 1) {
                Reposition(ctx, index, afterFly);
            }
            else {
                //末冲之后下潜入沙
                DiveIntoSand(ctx);
                if (IsServer && (afterFly >= SandseaDirector.DashExitFrames || (afterFly > 12 && InSand(ctx)))) {
                    return EndAttack(ctx);
                }
            }
            return null;
        }

        /// <summary>潜到上浮线正下方,竖直上浮,四分之一圆压上与玩家同高的航道,然后悬停在对齐位</summary>
        private void Opening(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            int side = ctx.SideDir;
            if (At(6)) {
                SandseaVfx.Roar(npc.Center, true);
            }
            if (Timer < SandseaDirector.DashSubmergeFrames) {
                float x = RiseX(ctx);
                RiseLine(ctx, x, SandseaDirector.DashSubmergeFrames);
                if (Timer > 14) {
                    DeclareOmen(ctx, x, SandseaDirector.Progress(Timer, 14, SandseaDirector.DashSubmergeFrames - 14));
                }
                return;
            }
            ctx.Barrier = SandseaDirector.Progress(Timer, SandseaDirector.DashLockAt(0) - 16, 16);
            if (aligned) {
                HoldAt(ctx, new Vector2(side * SandseaDirector.DashStandoff, 0f), 0.12f, 0.3f);
                return;
            }

            //上浮和转弧都在随玩家平移的参考系里做,玩家跑动时终点仍在对齐位
            float top = target.Center.Y + SandseaDirector.DashArcRadius;
            DropCarry(ctx);
            if (turnAt < 0 && (npc.Center.Y <= top || Timer >= SandseaDirector.DashLockAt(0) - SandseaDirector.DashForceTurnLead)) {
                turnAt = Timer;
            }
            if (turnAt < 0) {
                RiseTo(ctx, top, SandseaDirector.DashRiseBy);
                AddCarry(ctx, new Vector2(target.velocity.X, 0f));
                return;
            }
            //头朝上起转,朝玩家一侧转四分之一圆,转完时水平对着玩家
            float turned = Math.Min(MathHelper.PiOver2, SandseaDirector.DashArcSpeed / SandseaDirector.DashArcRadius * (Timer - turnAt + 1));
            float heading = -MathHelper.PiOver2 - side * turned;
            npc.velocity = heading.ToRotationVector2() * SandseaDirector.DashArcSpeed;
            ctx.Mode = SandseaMoveMode.Direct;
            AddCarry(ctx, target.velocity);
            if (turned >= MathHelper.PiOver2) {
                aligned = true;
            }
        }

        /// <summary>锁向:冲刺方向取头此刻的朝向,航道冻结成过本体的水平线,期间只沿航道慢滑</summary>
        private void Lock(SandseaStateContext ctx, int index, int local) {
            NPC npc = ctx.Npc;
            if (At(SandseaDirector.DashLockAt(index))) {
                //悬停位本来就在朝向的背后一侧,不按左右重算,否则玩家穿到身后时会朝着尾巴冲
                ctx.LockedAngle = MathF.Cos(npc.rotation) >= 0f ? 0f : MathHelper.Pi;
                ctx.LockedPoint = npc.Center;
                if (IsServer) {
                    if (ctx.Phase >= 2) {
                        SpawnEscorts(ctx);
                    }
                    MarkNetUpdate(ctx);
                }
                SandseaVfx.Sound(SoundID.Zombie7 with { Pitch = -0.2f, MaxInstances = 2 }, npc.Center);
            }
            Vector2 dir = ctx.LockedAngle.ToRotationVector2();
            SteerHeading(ctx, ctx.LockedAngle, SandseaDirector.DashGlideSpeed, 2f);
            ctx.Barrier = 1f;
            ctx.Effort = 0.3f;
            ctx.JawOpen = 0.5f;
            ctx.EscortHold = true;
            DeclareLane(ctx, npc.Center, dir, SandseaDirector.DashLaneLength, LaneHalf(ctx.Phase), 0.55f + 0.45f * local / SandseaDirector.DashLockFrames);
        }

        private void Fly(SandseaStateContext ctx, int index, int frame) {
            NPC npc = ctx.Npc;
            int flyAt = SandseaDirector.DashFlyAt(index);
            Vector2 dir = ctx.LockedAngle.ToRotationVector2();
            float speed = SandseaDirector.DashSpeed(ctx.Phase) * MathHelper.Clamp((frame + 1f) / SandseaDirector.DashRampFrames, 0f, 1f);
            npc.velocity = dir * speed;
            ctx.Mode = SandseaMoveMode.Direct;
            ctx.Barrier = 1f;
            ctx.Effort = 0.25f;
            ctx.JawOpen = 0.9f;
            ctx.EscortHold = true;
            DeclareLane(ctx, npc.Center, dir, SandseaDirector.DashLaneLength, LaneHalf(ctx.Phase), Math.Max(0f, 1f - frame / 10f));

            if (At(flyAt)) {
                ctx.ShakeStrength = 0.5f;
                SandseaVfx.Sound(SoundID.DD2_WyvernDiveDown with { Volume = 1f, Pitch = -0.3f, MaxInstances = 2 }, npc.Center);
                SandseaVfx.Shake(npc.Center, 5f, 2000f);
                MarkNetUpdate(ctx);
            }

            if (index == SandseaDirector.DashCount - 1 && IsServer) {
                for (int k = 0; k < SandseaDirector.NeedleCount; k++) {
                    int needleFrame = (int)((k + 0.5f) * SandseaDirector.DashFlyFrames / SandseaDirector.NeedleCount);
                    if (At(flyAt + needleFrame)) {
                        SpawnNeedle(ctx, k);
                    }
                }
            }
        }

        /// <summary>
        /// 两冲之间:收速 8 帧;冲过了玩家就向上兜 270 度再反向转 90 度,水平落回原航道、朝向玩家;之后悬停在对齐位等下一次锁向
        /// 悬停在哪一侧由权威端在冲刺结束那一帧按玩家到下次锁向时的预计位置定下,写进 SideDir 过线;要不要掉头由 SideDir 和冲刺方向推出
        /// 转向固定朝上,各端不用骰点也一致
        /// </summary>
        private void Reposition(SandseaStateContext ctx, int index, int frame) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            if (At(SandseaDirector.DashEndAt(index)) && IsServer) {
                float ahead = target.Center.X + MathHelper.Clamp(target.velocity.X, -SandseaDirector.CarryMax, SandseaDirector.CarryMax)
                    * (SandseaDirector.DashCycle - SandseaDirector.DashFlyFrames - SandseaDirector.DashLockFrames);
                ctx.SideDir = npc.Center.X >= ahead ? 1 : -1;
                MarkNetUpdate(ctx);
            }
            //冲刺方向朝右且要停在玩家右侧,说明玩家将在身后,要掉头;反之玩家还在前方
            bool loop = MathF.Cos(ctx.LockedAngle) * ctx.SideDir > 0f;
            float rate = SandseaDirector.DashLoopSpeed / SandseaDirector.DashLoopRadius;
            int recover = SandseaDirector.DashRecoverFrames;
            int overFrames = (int)MathF.Ceiling(1.5f * MathHelper.Pi / rate);
            int backFrames = (int)MathF.Ceiling(MathHelper.PiOver2 / rate);
            float sign = MathF.Cos(ctx.LockedAngle) > 0f ? -1f : 1f;

            if (frame < recover) {
                SteerHeading(ctx, ctx.LockedAngle, SandseaDirector.DashLoopSpeed, 1.4f);
                ctx.Barrier = 1f - frame / (float)recover;
                return;
            }
            if (loop && frame < recover + overFrames + backFrames) {
                float heading;
                if (frame < recover + overFrames) {
                    heading = ctx.LockedAngle + sign * rate * (frame - recover + 1);
                }
                else {
                    float back = Math.Min(MathHelper.PiOver2, rate * (frame - recover - overFrames + 1));
                    heading = ctx.LockedAngle + sign * (1.5f * MathHelper.Pi - back);
                }
                npc.velocity = heading.ToRotationVector2() * SandseaDirector.DashLoopSpeed;
                ctx.Mode = SandseaMoveMode.Direct;
                return;
            }
            HoldAt(ctx, new Vector2(ctx.SideDir * SandseaDirector.DashStandoff, 0f), 0.12f, 0.3f);
            ctx.Barrier = SandseaDirector.Progress(Timer, SandseaDirector.DashLockAt(index + 1) - 16, 16);
        }

        /// <summary>双数针瞄玩家此刻的位置,单数针瞄提前点,站着不动和直线跑都会被其中一种打到</summary>
        private static void SpawnNeedle(SandseaStateContext ctx, int k) {
            Player target = ctx.Target;
            Vector2 pos = ctx.Owner.Joints[1];
            Vector2 aim = target.Center;
            if (k % 2 == 1) {
                aim += target.velocity * SandseaDirector.NeedleLeadFrames;
            }
            Vector2 dir = (aim - pos).SafeNormalize(Vector2.UnitY);
            Shoot<SandseaVenomNeedle>(ctx, pos, dir * SandseaDirector.NeedleStartSpeed, SandseaDirector.DmgNeedle);
        }

        /// <summary>上下各一列沙鲨,ai[2] 是带符号的队形位,种类各自随机</summary>
        private static void SpawnEscorts(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            for (int k = 1; k <= SandseaDirector.EscortPerSide; k++) {
                for (int s = -1; s <= 1; s += 2) {
                    Shoot<SandseaEscortShark>(ctx, npc.Center, Vector2.Zero, SandseaDirector.DmgShark, Main.rand.Next(4), npc.whoAmI, s * k);
                }
            }
        }
    }
}
