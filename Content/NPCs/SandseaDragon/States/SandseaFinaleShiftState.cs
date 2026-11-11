using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 转三阶段:血量锁在 10%,浮到玩家侧上方喷涌白色粒子 70 帧,然后快速赶到竞技场中线、玩家所在的高度
    /// 到位后切阶段、天气强制转为沙尘暴并进入鲨卷风,全程不可受伤也没有接触伤害
    /// </summary>
    [VaultState((int)SandseaStateIndex.FinaleShift, typeof(SandseaStateContext))]
    public class SandseaFinaleShiftState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.FinaleShift;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            SandseaHostileProjectile.ClearAll();
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            int burst = SandseaDirector.FinaleBurstFrames;
            ctx.Invulnerable = true;
            ctx.ContactActive = false;
            ctx.GlowBoost = 1f;

            if (At(4)) {
                SandseaVfx.Roar(npc.Center, true);
                SandseaVfx.Shake(npc.Center, 8f);
            }

            if (Timer < burst) {
                //转阶段可能发生在沙下深处,先浮到玩家侧上方的展示位再喷,锁血的演出要让人看见
                float x = ctx.Target.Center.X + SideOfTarget(ctx) * 420f;
                Vector2 display = new Vector2(x, SurfaceAt(ctx, x) - SandseaDirector.ShiftDisplayHeight);
                Steer(ctx, display, 18f, 1f, 200f);
                float p = SandseaDirector.Progress(Timer, 0, burst);
                ctx.WhiteBurst = 0.4f + 0.6f * p;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.3f + 0.5f * p);
                ctx.JawOpen = 1f;
                ctx.Effort = 0.3f;
                return null;
            }

            float floor = ctx.ArenaFloorY;
            Vector2 center = new Vector2(ctx.ArenaCenterX,
                MathHelper.Clamp(ctx.Target.Center.Y, floor - SandseaDirector.MaelstromHeight + 180f, floor - 190f));
            Steer(ctx, center, SandseaDirector.FinaleTravelSpeed, 1.2f, 220f);
            ctx.WhiteBurst = 0.35f;

            bool arrived = Vector2.Distance(npc.Center, center) < 150f;
            if (IsServer && (arrived || Timer >= burst + SandseaDirector.FinaleTravelMax)) {
                ctx.Phase = 3;
                SandseaWeather.Start();
                MarkNetUpdate(ctx);
                return new SandseaSharknadoState();
            }
            return null;
        }
    }
}
