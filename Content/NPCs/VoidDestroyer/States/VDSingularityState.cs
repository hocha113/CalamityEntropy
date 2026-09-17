using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 虚空奇点:45 帧蓄力(核心汇聚流 + 八次幂迟滞后撤:大半时间几乎不动,最后几帧猛然向后吸满)→ 一帧放出奇点
    /// (反冲 6)→ 奇点飘向本体与玩家的中点停住,150 帧引力 + 透镜 + 螺旋弹,20 帧塌缩后环爆(P3 版塌缩帧 = 整场唯一冲击帧)。
    /// 本体在奇点期间与玩家保持相对静止,不再加压;奇点的节拍全部由弹幕自管
    /// </summary>
    [VaultState((int)VDStateIndex.Singularity, typeof(VDStateContext))]
    public class VDSingularityState : VDStateBase
    {
        public override string StateName => "Singularity";
        public override VDStateIndex StateIndex => VDStateIndex.Singularity;
        public override Vector2 AnchorFor(VDStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.SingHoverOffset.X, VDDirector.SingHoverOffset.Y);

        private enum Beat { Charge, Hold }
        private Beat beat;
        private Vector2 lockedDir = Vector2.UnitX;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Charge;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            Vector2 hover = new Vector2(ctx.SideDir * VDDirector.SingHoverOffset.X, VDDirector.SingHoverOffset.Y);

            if (beat == Beat.Charge) {
                float progress = MathHelper.Clamp(Timer / (float)VDDirector.SingChargeFrames, 0f, 1f);
                //锁向前追瞄,最后 8 帧死向
                if (Timer <= VDDirector.SingChargeFrames - VDDirector.SingLockLead) {
                    lockedDir = (ctx.Target.Center - ctx.Owner.CorePos).SafeNormalize(Vector2.UnitX);
                }
                //迟滞后撤:pow8,几乎不动 → 最后猛吸
                float late = MathF.Pow(progress, 8f);
                DeclareDirect(ctx);
                npc.velocity = Vector2.Lerp(npc.velocity, -lockedDir * (1.5f + 10f * late), 0.25f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, progress);
                ctx.WingPulse = Math.Max(ctx.WingPulse, progress);
                if (Timer == 1) {
                    VDVfx.Sound("VoidAnticipation", 0.7f, npc.Center, 3, 1f);
                }
                //汇聚流密度随蓄力升,72% 处硬切:尖叫前的吸气
                if (progress < 0.72f && Timer % 2 == 0) {
                    ConvergeSparks(ctx, VDVfx.VoidPurple, 120f, 260f, 0.08f);
                    if (progress > 0.4f) {
                        ConvergeSparks(ctx, VDVfx.VoidPink, 200f, 360f, 0.06f);
                    }
                }
                else if (progress >= 0.72f) {
                    ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f);
                }
                if (Timer >= VDDirector.SingChargeFrames) {
                    Launch(ctx);
                    beat = Beat.Hold;
                    ResetTimer();
                    MarkNetUpdate(ctx);
                }
                return null;
            }

            DeclareHoldRelative(ctx, hover, 0.08f, 0.25f, 26f);
            int total = VDSingularity.TravelFrames + VDDirector.SingActiveFrames + VDDirector.SingCollapseFrames + VDDirector.SingTail;
            if (Timer >= total) {
                return EndAttack(ctx);
            }
            return null;
        }

        /// <summary>放出奇点:初速按到玩家一半距离标定(30 帧 ×0.94 衰减的总程),反冲 6</summary>
        private void Launch(VDStateContext ctx) {
            Vector2 core = ctx.Owner.CorePos;
            float dist = Vector2.Distance(core, ctx.Target.Center);
            //总程 = v0·(1-0.94^30)/(1-0.94) ≈ v0·14,目标飘到中点
            float v0 = MathHelper.Clamp(dist * 0.5f / 14f, 6f, 26f);
            MuzzleCue(ctx, lockedDir, 6f, "VoidAttack", 0.6f, 1.1f);
            VDVfx.Shake(ctx.Npc.Center, 6f, 2400f);
            Shoot<VDSingularity>(ctx, core, lockedDir * v0, VDDirector.DmgSingularityCore, ctx.Npc.whoAmI, ctx.Phase, ctx.Owner.ProjDamage(VDDirector.DmgVoidBolt));
        }
    }
}
