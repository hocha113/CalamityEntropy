using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Sage;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 星环,对应先知的符文球:本体绕着玩家慢转,身周一圈星符逐颗脱环,停住锁向并画出航线,预告走完沿线射出
    /// 偶数颗瞄玩家当前位置,奇数颗瞄预判点,站着不动和匀速直跑都会被点到
    /// 环上还没打出去的星符由宿主画,槽位角只由 Timer 决定,各端一致
    /// </summary>
    [VaultState((int)SageStateIndex.StarRing, typeof(SageStateContext))]
    public class SageStarRingState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.StarRing;

        public override Vector2 AnchorFor(SageStateContext ctx)
            => ctx.Target.Center + ctx.BaseAngle.ToRotationVector2() * SageDirector.RingOrbitRadius;

        /// <summary>从本体当前所在的方位开始绕</summary>
        public override void OnPicked(SageStateContext ctx) {
            ctx.BaseAngle = (ctx.Npc.Center - ctx.Target.Center).ToRotation();
        }

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            int count = SageDirector.RingCount(ctx.Phase);
            int interval = SageDirector.RingInterval(ctx.Phase);
            int fireEnd = SageDirector.RingWindup + count * interval;

            float bearing = ctx.BaseAngle + ctx.SideDir * SageDirector.RingOrbitSpeed * Timer;
            HoldAt(ctx, bearing.ToRotationVector2() * SageDirector.RingOrbitRadius, 0.1f, 0.25f, 22f);

            int since = Timer - SageDirector.RingWindup;
            int fired = since < 0 ? 0 : Math.Min(since / interval + 1, count);
            float form = SageDirector.Progress(Timer, 0, SageDirector.RingWindup);
            ctx.RingSlots = count;
            ctx.RingFired = fired;
            ctx.RingAngle = SlotAngle(ctx, Timer);
            ctx.RingGlow = form;
            ctx.WingSpread = 0.5f * form * (Timer < fireEnd ? 1f : 0f);
            ctx.CrestGlow = Math.Max(ctx.CrestGlow, 0.45f + 0.45f * form);

            if (At(0)) {
                SageVfx.Cue("bell", ctx.Npc.Center, 0.9f, 0.8f);
            }
            if (since >= 0 && Timer < fireEnd && since % interval == 0) {
                FireRune(ctx, since / interval, count);
            }
            return Timer >= fireEnd + SageDirector.RingTail ? EndAttack(ctx) : null;
        }

        private static float SlotAngle(SageStateContext ctx, int timer) => ctx.SideDir * SageDirector.RingSlotSpin * timer;

        private void FireRune(SageStateContext ctx, int index, int count) {
            Player target = ctx.Target;
            Vector2 spawn = ctx.Npc.Center + (SlotAngle(ctx, Timer) + MathHelper.TwoPi * index / count).ToRotationVector2() * SageDirector.RingSlotRadius;
            Vector2 aim = target.Center;
            if (index % 2 == 1) {
                //提前量按预告帧数加飞行时间算,封顶
                float frames = SageDirector.RingTelegraph + Vector2.Distance(spawn, target.Center) / SageDirector.RingSpeed;
                Vector2 lead = target.velocity * frames;
                float length = lead.Length();
                if (length > SageDirector.RingLeadMax) {
                    lead *= SageDirector.RingLeadMax / length;
                }
                aim += lead;
            }
            float rotation = (aim - spawn).SafeNormalize(Vector2.UnitY).ToRotation();
            Shoot<SageStarRune>(ctx, spawn, Vector2.Zero, SageDirector.DmgRune, rotation, SageDirector.RingTelegraph);
            ctx.CrestPulse = Math.Max(ctx.CrestPulse, 0.3f);
            SageVfx.Stardust(spawn, 4, 1f, 4f);
            SageVfx.Cue("LunarStar", spawn, 1.2f, 0.4f, 4);
        }
    }
}