using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 引力融合:本体分成两个半径 6 格的红色小黑洞并消失,随后连做四轮:
    /// 两个黑洞出现在玩家两侧各 35 格处并亮出连线 1 秒 → 沿连线对撞 → 融成大黑洞并放出一圈 6 颗慢速凝胶球 → 消失 1 秒
    /// 本体就是其中一个黑洞,另一个是它关于锁定点的镜像;连线在出现那一拍锁死,偏离水平不超过 60 度
    /// 整招引力场不移动;消失期间不可受伤;最后一轮融合后直接重聚成史莱姆
    /// </summary>
    [VaultState((int)VSlimeStateIndex.GravityFusion, typeof(VSlimeStateContext))]
    public class VSlimeGravityFusionState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.GravityFusion;

        private const int Split = VSlimeDirector.FusionSplitFrames;
        private const int DashStart = VSlimeDirector.FusionTelegraphFrames;
        private const int MergeStart = DashStart + VSlimeDirector.FusionDashFrames;
        private const int VanishStart = MergeStart + VSlimeDirector.FusionMergeFrames;
        /// <summary>最后一轮融合停留结束就重聚,不再消失</summary>
        private const int ReformStart = Split + (VSlimeDirector.FusionCycles - 1) * VSlimeDirector.FusionCycle + VanishStart;
        /// <summary>出现后这么多帧才有判定,黑洞在这段时间淡入</summary>
        private const int AppearGrace = 16;
        /// <summary>融合后大黑洞的半径倍率</summary>
        private const float MergedScale = 1.5f;

        public override Vector2 AnchorFor(VSlimeStateContext ctx) => ctx.Npc.Center;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            //零向量当作还没锁定
            if (IsServer) {
                ctx.LockedPoint = Vector2.Zero;
                MarkNetUpdate(ctx);
            }
        }

        public override int TimeoutFrames => ReformStart + VSlimeDirector.FusionReformFrames + 120;

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.ArenaFollow = false;
            ctx.Shape = VSlimeShape.BlackHole;
            Direct(ctx);
            float radius = VSlimeDirector.FusionHoleRadius;

            if (Timer < Split) {
                //起手:缩成小黑洞并淡出
                float p = VSlimeDirector.Progress(Timer, 0, Split);
                npc.velocity *= 0.85f;
                ctx.HoleRadius = radius * VSlimeDirector.EaseOut(Math.Min(1f, p * 2f));
                ctx.AlphaDeclared = 1f - VSlimeDirector.Progress(Timer, Split / 2, Split / 2);
                ctx.NoContact = true;
                ctx.Invulnerable = p > 0.5f;
                if (At(1)) {
                    VSlimeVfx.Cue("blackholeCharge", npc.Center, 1.3f, 0.8f, 2);
                }
                return null;
            }

            if (Timer >= ReformStart) {
                //收尾:大黑洞重聚成史莱姆
                float p = VSlimeDirector.Progress(Timer, ReformStart, VSlimeDirector.FusionReformFrames);
                npc.velocity = Vector2.Zero;
                ctx.HoleRadius = radius * MergedScale * (1f - VSlimeDirector.EaseIn(p));
                ctx.NoContact = true;
                if (p > 0.6f) {
                    ctx.Shape = VSlimeShape.Slime;
                }
                return Timer >= ReformStart + VSlimeDirector.FusionReformFrames ? EndAttack(ctx) : null;
            }

            int t = Timer - Split;
            int cycle = t / VSlimeDirector.FusionCycle;
            int cycleStart = Split + cycle * VSlimeDirector.FusionCycle;
            int local = t - cycle * VSlimeDirector.FusionCycle;
            bool last = cycle == VSlimeDirector.FusionCycles - 1;

            if (At(cycleStart) && IsServer) {
                Lock(ctx, cycle);
            }
            bool locked = ctx.LockedPoint != Vector2.Zero;
            Vector2 mid = ctx.LockedPoint;
            Vector2 axis = ctx.LockedAngle.ToRotationVector2();
            Vector2 start = mid + axis * VSlimeDirector.FusionDistance;

            if (local < DashStart) {
                //出现并亮出连线
                npc.velocity = Vector2.Zero;
                float p = VSlimeDirector.Progress(local, 0, DashStart);
                ctx.HoleRadius = radius;
                ctx.AlphaDeclared = locked ? VSlimeDirector.Progress(local, 0, AppearGrace) : 0f;
                ctx.Invulnerable = !locked;
                if (locked && local >= AppearGrace) {
                    DeclareHoles(ctx, radius, mid);
                }
                else {
                    ctx.NoContact = true;
                }
                if (locked) {
                    ctx.LaneStart = npc.Center;
                    ctx.LaneEnd = 2f * mid - npc.Center;
                    ctx.LaneWidth = radius * 2f;
                    ctx.LaneStrength = 0.3f + 0.7f * p;
                }
            }
            else if (local < MergeStart) {
                //对撞:匀速沿连线跑完锁定的距离
                if (locked) {
                    npc.velocity = (mid - start) / VSlimeDirector.FusionDashFrames;
                    DeclareHoles(ctx, radius, mid);
                    ctx.LaneStart = npc.Center;
                    ctx.LaneEnd = 2f * mid - npc.Center;
                    ctx.LaneWidth = radius * 2f;
                    ctx.LaneStrength = 1f - (local - DashStart) / (float)VSlimeDirector.FusionDashFrames;
                }
                ctx.HoleRadius = radius;
                if (At(cycleStart + DashStart)) {
                    VSlimeVfx.Cue("Dash2", npc.Center, 0.9f, 0.8f, 2);
                }
            }
            else if (local < VanishStart) {
                //融合:大黑洞停在锁定点,前半段有判定,非最后一轮的后半段淡出
                npc.velocity = Vector2.Zero;
                int merged = local - MergeStart;
                ctx.HoleRadius = radius * MergedScale;
                bool solid = merged < VSlimeDirector.FusionMergeFrames / 2;
                if (solid) {
                    ctx.HazardRadius = radius * MergedScale;
                    ctx.ContactBase = VSlimeDirector.FusionContact;
                }
                else {
                    ctx.NoContact = true;
                    if (!last) {
                        ctx.AlphaDeclared = 1f - VSlimeDirector.Progress(merged, VSlimeDirector.FusionMergeFrames / 2, VSlimeDirector.FusionMergeFrames / 2);
                    }
                }
                if (At(cycleStart + MergeStart)) {
                    Merge(ctx, cycle);
                }
            }
            else {
                //消失
                npc.velocity = Vector2.Zero;
                ctx.HoleRadius = radius;
                ctx.AlphaDeclared = 0f;
                ctx.NoContact = true;
                ctx.Invulnerable = true;
            }
            ctx.HitboxTarget = new Vector2(ctx.HoleRadius * 1.3f);
            return null;
        }

        /// <summary>两个黑洞的圆形判定,第二个是本体关于锁定点的镜像</summary>
        private static void DeclareHoles(VSlimeStateContext ctx, float radius, Vector2 mid) {
            ctx.HazardRadius = radius;
            ctx.ContactBase = VSlimeDirector.FusionContact;
            ctx.Hazard2Center = 2f * mid - ctx.Npc.Center;
        }

        /// <summary>权威端锁定玩家位置和连线角度,本体此刻不可见,直接换到连线一端;每轮换一侧起手</summary>
        private void Lock(VSlimeStateContext ctx, int cycle) {
            float tilt = Rng(ctx, cycle).NextFloat(-VSlimeDirector.FusionMaxTilt, VSlimeDirector.FusionMaxTilt);
            int side = cycle % 2 == 0 ? ctx.SideDir : -ctx.SideDir;
            float angle = side > 0 ? tilt : MathHelper.Pi + tilt;
            ctx.LockedPoint = ctx.Target.Center;
            ctx.LockedAngle = angle;
            ctx.Owner.Teleport(ctx.LockedPoint + angle.ToRotationVector2() * VSlimeDirector.FusionDistance);
            MarkNetUpdate(ctx);
        }

        private void Merge(VSlimeStateContext ctx, int cycle) {
            NPC npc = ctx.Npc;
            ctx.Pulse = 1f;
            ctx.ShakeStrength = 0.8f;
            VSlimeVfx.Burst(npc.Center, 3f, VSlimeVfx.HoleRed, VSlimeVfx.HoleEmber);
            VSlimeVfx.Scatter(npc.Center, DustID.RedTorch, 30, 3f, 14f, 1.3f);
            VSlimeVfx.Shake(npc.Center, 10f);
            VSlimeVfx.SkyFlash(0.6f);
            VSlimeVfx.Cue("energyImpact", npc.Center, 0.8f, 1f, 2);
            if (!IsServer) {
                return;
            }
            int count = VSlimeDirector.FusionGels;
            float baseAngle = cycle * MathHelper.Pi / count;
            for (int i = 0; i < count; i++) {
                Vector2 dir = (baseAngle + MathHelper.TwoPi * i / count).ToRotationVector2();
                Shoot<VSlimeDarkGel>(ctx, npc.Center + dir * 40f, dir * VSlimeDirector.FusionGelSpeed, VSlimeDirector.DmgDarkGel,
                    VSlimeDarkGel.ModeStraight, VSlimeDirector.FusionGelLife);
            }
        }
    }
}
