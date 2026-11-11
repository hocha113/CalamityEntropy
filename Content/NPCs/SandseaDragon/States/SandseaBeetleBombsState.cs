using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 甲虫炸弹(D):战吼后从玩家一侧 35 格处陡直破沙,跃到玩家上方,上升途中抛出 5 个甲虫炸弹(二阶段 7 个),3 秒后爆炸
    /// 弧顶弓身,转一段弧朝玩家俯冲扎进地面,砸地时向上方扇面溅起 24 到 28 发沙弹
    /// 公平阀:破沙前沙面喷涌 30 帧,破沙点就在喷涌处;俯冲线在弧顶前 14 帧锁死并画出,画的是转向弧之后的那条直线;
    /// 炸弹悬停带有上下界,带外总是安全;沙弹只在正上方左右各 50 度的扇面里按分格撒开,不追踪
    /// LockedPoint 先存破沙点的沙面,锁俯冲后改存预告航道的起点
    /// </summary>
    [VaultState((int)SandseaStateIndex.BeetleBombs, typeof(SandseaStateContext))]
    public class SandseaBeetleBombsState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.BeetleBombs;
        public override int TimeoutFrames => 60 * 10;

        /// <summary>砸地只结算一次,各端按自己的入沙帧触发表现</summary>
        private bool slammed;
        private int slamAt;
        private bool sawAir;
        /// <summary>转向弧已经转完,之后沿锁定方向直飞</summary>
        private bool straight;
        /// <summary>权威端骰好的炸弹悬停点</summary>
        private List<Vector2> spots;

        public override Vector2 AnchorFor(SandseaStateContext ctx) {
            float x = ctx.Target.Center.X + ctx.SideDir * SandseaDirector.BombBreachDist;
            return new Vector2(x, SurfaceAt(ctx, x) + SandseaDirector.BombLaunchDepth);
        }

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            slammed = false;
            slamAt = 0;
            sawAir = false;
            straight = false;
            spots = null;
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            int submerge = SandseaDirector.BombSubmergeFrames;
            int launchAt = SandseaDirector.BombLaunchAt;
            if (At(6)) {
                SandseaVfx.Roar(npc.Center, true);
            }

            if (Timer < submerge) {
                Swim(ctx, AnchorFor(ctx), SandseaDirector.SwimSpeed);
                return null;
            }
            if (At(submerge)) {
                //破沙点在前摇开始时定死,LockedPoint 存破沙点的沙面
                float x = ctx.Target.Center.X + ctx.SideDir * SandseaDirector.BombBreachDist;
                ctx.LockedPoint = new Vector2(x, SurfaceAt(ctx, x));
                MarkNetUpdate(ctx);
            }
            if (Timer < launchAt) {
                //停在破沙点正下方,喷涌在哪里就从哪里出来
                Swim(ctx, ctx.LockedPoint + new Vector2(0f, SandseaDirector.BombLaunchDepth), 8f);
                DeclareOmen(ctx, ctx.LockedPoint.X, SandseaDirector.Progress(Timer, submerge, SandseaDirector.BombWindup));
                ctx.JawOpen = 0.3f;
                return null;
            }

            if (At(launchAt)) {
                Launch(ctx);
            }
            int apexAt = launchAt + (int)MathF.Ceiling(Math.Abs(ctx.LockedVelocity.Y) / SandseaDirector.Gravity);
            int lockAt = apexAt - SandseaDirector.DiveLockLead;

            if (Timer < apexAt) {
                Ballistic(ctx);
                ctx.Effort = 0.5f;
                //升到高处像鱼一样弓起身
                ctx.Curl = 0.9f * SandseaDirector.Progress(Timer, launchAt + 16, 30);
                TossBombs(ctx, launchAt);
                if (At(lockAt)) {
                    LockDive(ctx, apexAt - Timer);
                }
                if (Timer >= lockAt) {
                    float strength = 0.5f + 0.5f * SandseaDirector.Progress(Timer, lockAt, SandseaDirector.DiveLockLead);
                    DeclareLane(ctx, ctx.LockedPoint, ctx.LockedAngle.ToRotationVector2(), SandseaDirector.DiveLaneLength, SandseaDirector.DashLaneHalfWidth, strength);
                    ctx.JawOpen = 0.7f;
                }
                return null;
            }

            if (!slammed) {
                int frame = Timer - apexAt;
                Vector2 dir = ctx.LockedAngle.ToRotationVector2();
                if (!straight) {
                    //弧顶的速度是水平的,按固定半径转向俯冲方向并加速,转完后正好跑在预告航道上
                    SteerHeading(ctx, ctx.LockedAngle, SandseaDirector.DiveSpeed, SandseaDirector.DiveAccel, SandseaDirector.DiveTurnRadius);
                    if (Math.Abs(MathHelper.WrapAngle(npc.velocity.ToRotation() - ctx.LockedAngle)) < 0.01f) {
                        straight = true;
                    }
                }
                else {
                    npc.velocity = dir * SandseaDirector.DiveSpeed;
                    ctx.Mode = SandseaMoveMode.Direct;
                }
                ctx.Effort = 0.25f;
                ctx.JawOpen = 1f;
                DeclareLane(ctx, ctx.LockedPoint, dir, SandseaDirector.DiveLaneLength, SandseaDirector.DashLaneHalfWidth, Math.Max(0f, 1f - frame / 16f));
                if (At(apexAt)) {
                    SandseaVfx.Sound(SoundID.DD2_WyvernDiveDown with { Volume = 1f, Pitch = -0.3f, MaxInstances = 2 }, npc.Center);
                    MarkNetUpdate(ctx);
                }
                //洞穴里弧顶可能埋在顶壁中,先穿出到空中再认砸地
                bool buried = InSand(ctx);
                sawAir |= !buried;
                if (buried && (sawAir ? frame > 3 : frame > 24)) {
                    slammed = true;
                    slamAt = Timer;
                    Slam(ctx);
                }
                else if (frame > SandseaDirector.DiveMaxFrames && IsServer) {
                    return EndAttack(ctx);
                }
                return null;
            }

            //砸地后顺势往沙里钻,收速
            SteerHeading(ctx, Heading(ctx), 8f, 0.9f);
            return IsServer && Timer - slamAt >= SandseaDirector.BombExitFrames ? EndAttack(ctx) : null;
        }

        /// <summary>
        /// 陡直破沙:弧顶高出玩家 260 像素,按沙面算最高 70 格;横向速度让弧顶停在玩家近侧 170 像素处,给俯冲的转向弧留出地方
        /// 初速存进 LockedVelocity,各端据此算出弧顶帧
        /// </summary>
        private static void Launch(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            float surface = ctx.LockedPoint.Y;
            float depth = Math.Max(0f, npc.Center.Y - surface);
            float apex = MathHelper.Clamp(surface - ctx.Target.Center.Y + SandseaDirector.BombApexAbovePlayer, SandseaDirector.BombMinApex, SandseaDirector.BombMaxApex);
            float vy = MathF.Sqrt(2f * SandseaDirector.Gravity * (apex + depth));
            float riseFrames = vy / SandseaDirector.Gravity;
            float dx = ctx.Target.Center.X - npc.Center.X;
            float sign = Math.Abs(dx) > 8f ? Math.Sign(dx) : -ctx.SideDir;
            float vx = sign * MathHelper.Clamp((Math.Abs(dx) - SandseaDirector.BombApexShort) / riseFrames,
                SandseaDirector.BombMinHorizontalSpeed, SandseaDirector.BombMaxHorizontalSpeed);
            npc.velocity = new Vector2(vx, -vy);
            ctx.LockedVelocity = npc.velocity;
            SandseaVfx.BreachSound(npc.Center, 1f);
            SandseaVfx.Shake(npc.Center, 5f, 2000f);
            MarkNetUpdate(ctx);
        }

        /// <summary>玩家在下方就朝玩家,与水平至少夹 35 度;玩家不在下方就朝玩家那一侧斜向下 45 度</summary>
        private static float DiveAngle(Vector2 from, Vector2 target) {
            Vector2 to = target - from;
            if (to.Y > 40f) {
                return MathHelper.Clamp(to.ToRotation(), SandseaDirector.DiveMinAngle, MathHelper.Pi - SandseaDirector.DiveMinAngle);
            }
            return to.X >= 0f ? MathHelper.PiOver4 : MathHelper.Pi - MathHelper.PiOver4;
        }

        /// <summary>
        /// 锁俯冲:按弹道算出弧顶的位置,从那里以固定半径转向俯冲方向,转完的那一点是预告航道的起点
        /// 俯冲方向要让航道穿过玩家此刻的位置,而航道起点又取决于方向,迭代几次就收敛;方向和起点锁定后不再改
        /// </summary>
        /// <param name="framesToApex">离弧顶还有多少帧,调用时本帧的重力已经加进速度、位置还没动</param>
        private static void LockDive(SandseaStateContext ctx, int framesToApex) {
            NPC npc = ctx.Npc;
            int n = framesToApex;
            Vector2 apex = npc.Center + npc.velocity * n + new Vector2(0f, 0.5f * SandseaDirector.Gravity * n * (n - 1));
            //弧顶时的航向是水平的
            float level = npc.velocity.X >= 0f ? 0f : MathHelper.Pi;
            float radius = SandseaDirector.DiveTurnRadius;
            Vector2 target = ctx.Target.Center;
            float angle = DiveAngle(apex, target);
            Vector2 laneStart = apex;
            for (int i = 0; i < 6; i++) {
                float turn = MathHelper.WrapAngle(angle - level) >= 0f ? 1f : -1f;
                Vector2 center = apex + (level + turn * MathHelper.PiOver2).ToRotationVector2() * radius;
                laneStart = center + (angle - turn * MathHelper.PiOver2).ToRotationVector2() * radius;
                if (i < 5) {
                    angle = DiveAngle(laneStart, target);
                }
            }
            ctx.LockedAngle = angle;
            ctx.LockedPoint = laneStart;
            MarkNetUpdate(ctx);
        }

        private void TossBombs(SandseaStateContext ctx, int launchAt) {
            if (!IsServer) {
                return;
            }
            int count = SandseaDirector.BombCount(ctx.Phase);
            for (int j = 0; j < count; j++) {
                if (At(launchAt + SandseaDirector.BombTossDelay + j * SandseaDirector.BombTossInterval)) {
                    spots ??= RollSpots(ctx, count);
                    Vector2 dest = spots[j];
                    Shoot<SandseaBeetleBomb>(ctx, ctx.Owner.SnoutWorld(40f), Vector2.Zero, SandseaDirector.DmgBomb(ctx.Phase), dest.X, dest.Y, SandseaDirector.BombRadius);
                }
            }
        }

        /// <summary>
        /// 悬停点按横向分格散开,纵向限在玩家上方 14 格到下方 4 格的带内,圆心两两相距不小于 1.5 个爆炸半径
        /// 带的上下两侧和横向 40 格之外都炸不到
        /// </summary>
        private static List<Vector2> RollSpots(SandseaStateContext ctx, int count) {
            List<Vector2> list = new List<Vector2>(count);
            Player target = ctx.Target;
            float minDist = SandseaDirector.BombRadius * SandseaDirector.BombMinSpacing;
            float left = Math.Max(target.Center.X - SandseaDirector.BombSpreadX, ctx.ArenaCenterX - ctx.ArenaHalfWidth + 160f);
            float right = Math.Min(target.Center.X + SandseaDirector.BombSpreadX, ctx.ArenaCenterX + ctx.ArenaHalfWidth - 160f);
            if (right - left < 400f) {
                left = target.Center.X - 200f;
                right = target.Center.X + 200f;
            }
            float slot = (right - left) / count;
            for (int j = 0; j < count; j++) {
                Vector2 spot = Vector2.Zero;
                for (int attempt = 0; attempt < 24; attempt++) {
                    float x = left + (j + Main.rand.NextFloat()) * slot;
                    float y = target.Center.Y + Main.rand.NextFloat(-SandseaDirector.BombBandUp, SandseaDirector.BombBandDown);
                    y = Math.Min(y, SurfaceAt(ctx, x) - SandseaDirector.BombGroundClear);
                    spot = new Vector2(x, y);
                    bool clear = true;
                    foreach (Vector2 other in list) {
                        if (Vector2.Distance(other, spot) < minDist) {
                            clear = false;
                            break;
                        }
                    }
                    if (clear) {
                        break;
                    }
                }
                list.Add(spot);
            }
            return list;
        }

        /// <summary>砸地:沙浪和震屏各端都放,沙弹只由权威端按分格撒开</summary>
        private static void Slam(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            Vector2 impact = new Vector2(npc.Center.X, SandseaTerrain.Surface(npc.Center.X, npc.Center.Y));
            SandseaVfx.Slam(impact, 1.3f);
            ctx.ShakeStrength = 0.7f;
            if (!IsServer) {
                return;
            }
            int count = Main.rand.Next(SandseaDirector.SlamBlastsMin, SandseaDirector.SlamBlastsMax + 1);
            for (int j = 0; j < count; j++) {
                float t = (j + Main.rand.NextFloat(0.15f, 0.85f)) / count;
                float angle = -MathHelper.PiOver2 + MathHelper.Lerp(-SandseaDirector.SlamFanHalfAngle, SandseaDirector.SlamFanHalfAngle, t);
                float speed = Main.rand.NextFloat(SandseaDirector.SlamSpeedMin, SandseaDirector.SlamSpeedMax);
                Shoot<SandseaSandBlast>(ctx, impact - new Vector2(0f, 12f), angle.ToRotationVector2() * speed, SandseaDirector.DmgSandBlast, 0f, SandseaDirector.SandBlastRiseFrames);
            }
            MarkNetUpdate(ctx);
        }
    }
}
