using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 沙鲨冲击(B):从悬停点外侧竖直破沙上浮,转一段 135 度的弧压到玩家斜上方 36 格处,头正对着玩家悬住并随玩家平移
    /// 1 秒内在头部周围 15 格的圈上结出 10 只慢转的沙鲨,再过 1 秒眼部闪蓝光,沙鲨逐只朝玩家当时的位置发射;二阶段重复三波,每波间隔 100 帧
    /// 公平阀:沙鲨成圈与发射之间有 1 秒可见等待,蓝光先于发射 14 帧;每只发射时锁向后直飞,速度 14,飞到玩家约 40 帧
    /// 悬停期间本体离玩家 36 格且不主动靠近,是远程输出的窗口
    /// </summary>
    [VaultState((int)SandseaStateIndex.SharkBarrage, typeof(SandseaStateContext))]
    public class SandseaSharkBarrageState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.SharkBarrage;
        public override int TimeoutFrames => 60 * 20;

        #region 纯本地闩锁,各端按自己看到的位置推导
        /// <summary>起转弧线的帧,-1 为还在上浮</summary>
        private int turnAt = -1;
        /// <summary>已经到达悬停点,改为与玩家保持相对静止</summary>
        private bool arrived;
        #endregion

        private static Vector2 HoverOffset(SandseaStateContext ctx)
            => new Vector2(ctx.SideDir * SandseaDirector.BarrageHoverOffset.X, SandseaDirector.BarrageHoverOffset.Y);

        /// <summary>
        /// 弧线起转点相对悬停点的偏移:从这里头朝上、朝玩家一侧转 135 度,终点正好落在悬停点,头沿斜线指向玩家
        /// 圆心在起转点内侧一个半径处,终点在圆心的内上方 45 度,所以偏移是外 R(1+√½)、下 R√½
        /// </summary>
        private static Vector2 TurnInOffset(SandseaStateContext ctx) {
            float r = SandseaDirector.BarrageArcRadius;
            float half = MathF.Sqrt(0.5f);
            return new Vector2(ctx.SideDir * r * (1f + half), r * half);
        }

        private static float RiseX(SandseaStateContext ctx) => ctx.Target.Center.X + HoverOffset(ctx).X + TurnInOffset(ctx).X;

        public override Vector2 AnchorFor(SandseaStateContext ctx) {
            float x = RiseX(ctx);
            return new Vector2(x, SurfaceAt(ctx, x) + SandseaDirector.RiseDeepDepth);
        }

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            turnAt = -1;
            arrived = false;
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            Player target = ctx.Target;
            Vector2 offset = HoverOffset(ctx);
            int volleys = SandseaDirector.BarrageVolleys(ctx.Phase);
            int endAt = SandseaDirector.BarrageVolleyAt(volleys - 1) + SandseaDirector.BarrageVolleyLength;

            if (Timer >= endAt) {
                //离开悬停:从头的朝向起步,弧线转向正下方扎进沙里,落点离玩家约 16 格
                if (InSand(ctx)) {
                    SteerHeading(ctx, MathHelper.PiOver2, SandseaDirector.SwimSpeed);
                }
                else {
                    SteerHeading(ctx, MathHelper.PiOver2, 18f, 0.7f);
                }
                bool done = Timer >= endAt + SandseaDirector.BarrageExitFrames || (Timer > endAt + 12 && InSand(ctx));
                return IsServer && done ? EndAttack(ctx) : null;
            }

            if (Timer < SandseaDirector.BarrageSubmergeFrames) {
                float x = RiseX(ctx);
                RiseLine(ctx, x, SandseaDirector.BarrageSubmergeFrames);
                if (Timer > 10) {
                    DeclareOmen(ctx, x, SandseaDirector.Progress(Timer, 10, SandseaDirector.BarrageSubmergeFrames - 10));
                }
                return null;
            }

            //首波前 4 帧还没到位就直接转悬停,沙鲨的拍点不能漏
            if (!arrived && Timer < SandseaDirector.BarrageFirstVolleyAt - 4) {
                Approach(ctx, target.Center + offset);
                return null;
            }
            arrived = true;

            HoldAt(ctx, offset, 0.1f, 0.25f);
            ctx.Effort = 0.6f;
            for (int v = 0; v < volleys; v++) {
                Volley(ctx, v);
            }
            return null;
        }

        /// <summary>竖直上浮到起转高度,再按固定角速度转 135 度落到悬停点;都在随玩家平移的参考系里做</summary>
        private void Approach(SandseaStateContext ctx, Vector2 hover) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            float top = hover.Y + TurnInOffset(ctx).Y;
            DropCarry(ctx);
            if (turnAt < 0 && (npc.Center.Y <= top || Timer >= SandseaDirector.BarrageForceTurnAt)) {
                turnAt = Timer;
            }
            if (turnAt < 0) {
                RiseTo(ctx, top, SandseaDirector.BarrageRiseBy);
                AddCarry(ctx, new Vector2(target.velocity.X, 0f));
                return;
            }
            const float Sweep = 0.75f * MathHelper.Pi;
            float turned = Math.Min(Sweep, SandseaDirector.BarrageArcSpeed / SandseaDirector.BarrageArcRadius * (Timer - turnAt + 1));
            float heading = -MathHelper.PiOver2 - ctx.SideDir * turned;
            npc.velocity = heading.ToRotationVector2() * SandseaDirector.BarrageArcSpeed;
            ctx.Mode = SandseaMoveMode.Direct;
            AddCarry(ctx, target.velocity);
            if (turned >= Sweep) {
                arrived = true;
            }
        }

        private void Volley(SandseaStateContext ctx, int volley) {
            NPC npc = ctx.Npc;
            int start = SandseaDirector.BarrageVolleyAt(volley);
            int count = SandseaDirector.BarrageSharks;
            if (Timer < start || Timer > start + SandseaDirector.BarrageVolleyLength) {
                return;
            }
            int launchAt = start + SandseaDirector.BarrageLaunchAt;

            if (IsServer) {
                for (int k = 0; k < count; k++) {
                    if (At(start + k * SandseaDirector.BarrageSpawnSpan / count)) {
                        //ai[0] 低两位是种类,其余是圈上序号;ai[2] 是距发射还有多少帧
                        int launchFrame = launchAt + k * SandseaDirector.BarrageLaunchGap;
                        Shoot<SandseaRingShark>(ctx, npc.Center, Vector2.Zero, SandseaDirector.DmgShark, Main.rand.Next(4) + 4 * k, npc.whoAmI, launchFrame - Timer);
                    }
                }
            }

            int flashAt = launchAt - SandseaDirector.BarrageEyeFlashLead;
            if (At(flashAt)) {
                ctx.EyeFlash = 1f;
                SandseaVfx.Sound(SoundID.Item9 with { Pitch = 0.4f, Volume = 0.9f }, ctx.Owner.EyeWorld);
                SandseaVfx.FinSparks(ctx.Owner.EyeWorld, 10, 6f);
            }
            if (Timer >= flashAt && Timer < launchAt + count * SandseaDirector.BarrageLaunchGap) {
                ctx.EyeFlash = Math.Max(ctx.EyeFlash, 0.7f);
                ctx.JawOpen = 0.8f;
            }
        }
    }
}
