using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 跳跃追逐(C):像沙鲨一样在沙面下浅层朝玩家游,背鳍露在沙面上;每 2.5 秒扑跳一次,共三次,第三跳更高更急
    /// 路线:每跳落地后顺势往外游到起跳点外侧的深处,在沙里掉头,再沿浅层航道朝玩家游回来起跳,于是一来一回地跳
    /// 公平阀:起跳前 30 帧收速下沉、沙面喷涌;出手前 12 帧落点锁死并画出弹道,弧线只受重力,不再追瞄;
    /// 没锁过落点的跳不起跳;前两跳最高 30 格,高于此的玩家够不着
    /// 二阶段追加:跳完后潜到玩家正下方深处再竖直探头并跟随,放三轮从两侧 20 格处合拢的小龙卷,探头前脚下沙面先喷涌 70 帧
    /// </summary>
    [VaultState((int)SandseaStateIndex.LeapChase, typeof(SandseaStateContext))]
    public class SandseaLeapChaseState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.LeapChase;
        public override int TimeoutFrames => 60 * 24;

        #region 纯本地闩锁,各端按自己看到的位置推导
        /// <summary>已经锁过落点的那一跳的序号,-1 为无</summary>
        private int armed = -1;
        /// <summary>已经游到起跳点外侧、可以回头上航道</summary>
        private bool turned;
        /// <summary>这一跳已经出手且还没落回沙里</summary>
        private bool leaping;
        /// <summary>这一跳出手后头已经离开过沙子</summary>
        private bool leftSand;
        #endregion

        /// <summary>起跳点外侧的深处:从这里向上兜半圈正好落在浅层航道上,头朝玩家</summary>
        private static Vector2 OuterPoint(SandseaStateContext ctx) {
            float x = ctx.Target.Center.X + ctx.SideDir * (SandseaDirector.LeapLaunchDist + SandseaDirector.LeapOuterMargin);
            return new Vector2(x, SurfaceAt(ctx, x) + SandseaDirector.ShallowDepth + 2f * SandseaDirector.BuriedTurnRadius);
        }

        public override Vector2 AnchorFor(SandseaStateContext ctx) => OuterPoint(ctx);

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            armed = -1;
            turned = false;
            leaping = false;
            leftSand = false;
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            int launched = 0;
            while (launched < SandseaDirector.LeapCount && Timer >= SandseaDirector.LeapLaunchAt(launched)) {
                launched++;
            }

            if (launched > 0) {
                int last = launched - 1;
                int launchAt = SandseaDirector.LeapLaunchAt(last);
                //上一跳落得太晚、这一跳没来得及锁落点画弹道时,这一跳作废,不做没有预告的空中二段跳
                if (At(launchAt) && armed == last && InSand(ctx)) {
                    npc.velocity = LeapArc(ctx, last);
                    leaping = true;
                    leftSand = false;
                    SandseaVfx.BreachSound(npc.Center, 0.9f);
                    SandseaVfx.Sound(SoundID.Zombie7 with { Pitch = -0.25f, MaxInstances = 2 }, npc.Center);
                    MarkNetUpdate(ctx);
                }
                if (leaping) {
                    bool buried = InSand(ctx);
                    leftSand |= !buried;
                    //出过沙再入沙算落地;深处起跳迟迟没出沙就作罢
                    bool landed = leftSand ? buried : Timer - launchAt > SandseaDirector.LeapAbortFrames;
                    if (!landed) {
                        Ballistic(ctx, SandseaDirector.LeapGravity(last));
                        ctx.Effort = 0.5f;
                        //下落段张嘴扑下
                        ctx.JawOpen = npc.velocity.Y > -2f ? 0.9f : 0.3f;
                        return null;
                    }
                    leaping = false;
                    turned = false;
                    //落在玩家哪一侧,下一跳就从哪一侧回头
                    if (IsServer) {
                        ctx.SideDir = SideOfTarget(ctx);
                        MarkNetUpdate(ctx);
                    }
                }
            }

            if (launched < SandseaDirector.LeapCount) {
                Stalk(ctx, launched);
                return null;
            }

            if (ctx.Phase < 2) {
                Swim(ctx, base.AnchorFor(ctx), SandseaDirector.SwimSpeed);
                bool done = Timer >= SandseaDirector.LeapLaunchAt(SandseaDirector.LeapCount - 1) + SandseaDirector.LeapTailFrames;
                return IsServer && done ? EndAttack(ctx) : null;
            }
            return Perch(ctx);
        }

        /// <summary>游到外侧掉头,沿浅层航道游向起跳点,前摇期收速下沉并锁定落点</summary>
        private void Stalk(SandseaStateContext ctx, int index) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            int launchAt = SandseaDirector.LeapLaunchAt(index);
            int windupAt = launchAt - SandseaDirector.LeapWindup;
            int lockAt = launchAt - SandseaDirector.LeapLockLead;
            int side = ctx.SideDir;
            Vector2 dir = new Vector2(-side, 0f);
            float surface = SurfaceAt(ctx, npc.Center.X);
            float laneY = surface + SandseaDirector.ShallowDepth;
            Vector2 stop = new Vector2(target.Center.X + side * SandseaDirector.LeapLaunchDist, laneY);
            //沿航道方向超过起跳点的距离,在外侧是负的
            float along = (npc.Center.X - stop.X) * dir.X;
            int left = windupAt - Timer;
            Vector2 outer = OuterPoint(ctx);

            if (!turned) {
                //已经在起跳点外侧,或者来不及绕到外侧:就地转向航道
                float need = Vector2.Distance(npc.Center, outer) / SandseaDirector.LeapStalkSpeed + 24f;
                if (along < -40f || need > left) {
                    turned = true;
                }
            }

            if (Timer < windupAt) {
                if (!turned || (along < -40f && left > SandseaDirector.LeapApproachFrames)) {
                    //往外游,或者已经到了外侧但还早:在外侧深处待机
                    Swim(ctx, outer, SandseaDirector.LeapStalkSpeed);
                    return;
                }
                //算好速度,前摇开始时正好游到起跳距离
                float lateral = Math.Abs(npc.Center.Y - laneY);
                float pace = MathHelper.Clamp((Math.Max(0f, -along) + lateral) / Math.Max(1f, left), 4f, SandseaDirector.LeapStalkSpeed);
                ApproachLane(ctx, stop, dir, pace, 4f, SandseaDirector.TightTurnRadius, 1f, SandseaDirector.TightLookahead);
                return;
            }

            if (At(windupAt)) {
                SandseaVfx.Roar(npc.Center, false);
            }
            DeclareOmen(ctx, npc.Center.X, SandseaDirector.Progress(Timer, windupAt, SandseaDirector.LeapWindup));
            ctx.JawOpen = 0.4f;

            if (At(lockAt)) {
                ctx.LockedPoint = target.Center;
                armed = index;
                MarkNetUpdate(ctx);
            }
            if (Timer >= lockAt && armed == index) {
                Vector2 arc = LeapArc(ctx, index);
                float strength = 0.5f + 0.5f * SandseaDirector.Progress(Timer, lockAt, SandseaDirector.LeapLockLead);
                DeclareArc(ctx, npc.Center, arc, SandseaDirector.LeapGravity(index), strength);
                //锁点后把头朝起跳方向抬起来,出手时不折角;太浅时不再抬,免得提前冒头
                if (npc.Center.Y - surface > 70f) {
                    SteerHeading(ctx, arc.ToRotation(), SandseaDirector.LeapLiftSpeed, 1.5f, SandseaDirector.BuriedTurnRadius);
                    return;
                }
            }
            //前摇:收速,沿航道慢滑并下沉 40 像素
            float sinkY = laneY + 40f;
            SteerHeading(ctx, MathF.Atan2(sinkY - npc.Center.Y, -side * 200f), SandseaDirector.LeapWindupSpeed, 1.5f, SandseaDirector.BuriedTurnRadius);
        }

        /// <summary>
        /// 从当前位置起跳、下落段穿过锁定点的初速,弧顶高度按沙面算上下限
        /// 横向速度有下限,离锁定点太近时弧线会越过锁定点;正下方起跳就朝本招的出手方向跳
        /// </summary>
        private static Vector2 LeapArc(SandseaStateContext ctx, int index) {
            NPC npc = ctx.Npc;
            float depth = Math.Max(0f, npc.Center.Y - SurfaceAt(ctx, npc.Center.X));
            float rise = Math.Max(0f, npc.Center.Y - ctx.LockedPoint.Y);
            float apex = MathHelper.Clamp(rise + SandseaDirector.LeapExtraRise, SandseaDirector.LeapMinApex + depth, SandseaDirector.LeapMaxApex(index) + depth);
            Vector2 velocity = LeapVelocity(npc.Center, ctx.LockedPoint, apex, SandseaDirector.LeapGravity(index), SandseaDirector.LeapMaxHorizontalSpeed);
            if (Math.Abs(velocity.X) < SandseaDirector.LeapMinHorizontalSpeed) {
                float dx = ctx.LockedPoint.X - npc.Center.X;
                float sign = Math.Abs(dx) > 24f ? Math.Sign(dx) : -ctx.SideDir;
                velocity.X = sign * SandseaDirector.LeapMinHorizontalSpeed;
            }
            return velocity;
        }

        /// <summary>二阶段:潜到玩家正下方深处,沿竖直线上来探头,之后跟着玩家平移,按拍放三轮小龙卷</summary>
        private IVaultState<SandseaStateContext> Perch(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            float px = target.Center.X;
            float surface = SurfaceAt(ctx, px);
            Vector2 perch = new Vector2(px, surface - SandseaDirector.PerchHeadRise);
            int local = Timer - SandseaDirector.PerchStartAt;

            if (local < 0) {
                Swim(ctx, new Vector2(px, surface + SandseaDirector.PerchDeepDepth), SandseaDirector.SwimSpeed);
                return null;
            }
            if (local < SandseaDirector.PerchSettleFrames) {
                DeclareOmen(ctx, px, SandseaDirector.Progress(local, 0, SandseaDirector.PerchSettleFrames));
                //竖直航道跟着玩家横移,在随玩家平移的参考系里上航道,玩家跑动时也对得准
                DropCarry(ctx);
                ApproachLane(ctx, perch, -Vector2.UnitY, SandseaDirector.PerchSpeed, 1.2f, SandseaDirector.BuriedTurnRadius, 120f, 90f);
                AddCarry(ctx, new Vector2(target.velocity.X, 0f));
            }
            else {
                //只跟玩家的横向移动,整条身体随头部平移
                Vector2 want = new Vector2(target.velocity.X, 0f) + (perch - npc.Center) * 0.14f;
                if (want.Length() > 14f) {
                    want = want.SafeNormalize(Vector2.Zero) * 14f;
                }
                npc.velocity = Vector2.Lerp(npc.velocity, want, 0.3f);
                ctx.Mode = SandseaMoveMode.Direct;
                ctx.FrameVelocity = npc.velocity;
                ctx.RigidFollow = true;
                ctx.Effort = 0.5f;
                ctx.JawOpen = 0.5f;
            }

            if (IsServer) {
                for (int cycle = 0; cycle < SandseaDirector.TwisterCycles; cycle++) {
                    if (At(SandseaDirector.TwisterSpawnAt(cycle))) {
                        SpawnTwisters(ctx);
                    }
                }
            }
            return IsServer && Timer >= SandseaDirector.PerchEndAt ? EndAttack(ctx) : null;
        }

        /// <summary>两道小龙卷生在玩家左右各 20 格、与玩家脚下同层的物块或平台上,朝玩家此刻的横坐标合拢</summary>
        private static void SpawnTwisters(SandseaStateContext ctx) {
            Player target = ctx.Target;
            float mergeX = target.Center.X;
            for (int side = -1; side <= 1; side += 2) {
                float x = mergeX + side * SandseaDirector.TwisterStartOffset;
                float baseY = SandseaTerrain.Surface(x, target.Bottom.Y - 24f, true);
                Shoot<SandseaTwister>(ctx, new Vector2(x, baseY), Vector2.Zero, SandseaDirector.DmgTwister, side, mergeX, baseY);
            }
        }
    }
}
