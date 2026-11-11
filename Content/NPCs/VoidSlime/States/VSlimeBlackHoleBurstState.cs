using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 黑洞爆发:蓄力变成半径 10 格的红色大黑洞,慢速漂向玩家并施加吸力,持续 6 秒
    /// 每 80 帧从引力场边沿生成一圈 12 个星体残骸飞向黑洞,相邻两圈错半格
    /// 公平阀:蓄力 70 帧期间预警圈标出黑洞的实际大小;漂移速度和吸力加起来远低于玩家飞行速度;残骸圈在边沿时弧距约 628 像素
    /// </summary>
    [VaultState((int)VSlimeStateIndex.BlackHoleBurst, typeof(VSlimeStateContext))]
    public class VSlimeBlackHoleBurstState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.BlackHoleBurst;

        private const int ActiveStart = VSlimeDirector.HoleChargeFrames;
        private const int ActiveEnd = ActiveStart + VSlimeDirector.HoleActiveFrames;
        /// <summary>离玩家不到这个距离就先跃迁到旁边再变黑洞,不在玩家脸上成形</summary>
        private const float MinStartDistance = 460f;

        public override Vector2 AnchorFor(VSlimeStateContext ctx)
            => Vector2.Distance(ctx.Npc.Center, ctx.Target.Center) >= MinStartDistance ? ctx.Npc.Center : SideAnchor(ctx);

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.Shape = VSlimeShape.BlackHole;
            float full = VSlimeDirector.HoleRadius;

            if (Timer < ActiveStart) {
                float p = VSlimeDirector.Progress(Timer, 0, ActiveStart);
                ctx.HoleRadius = full * VSlimeDirector.EaseOut(p);
                ctx.ChargeGlow = p;
                ctx.WarnRadius = full;
                ctx.WarnStrength = 0.3f + 0.7f * p;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f * p);
                if (Timer % 2 == 0) {
                    VSlimeVfx.Converge(npc.Center, 140f, 360f, DustID.RedTorch);
                }
            }
            else if (Timer < ActiveEnd) {
                Direct(ctx);
                Vector2 toTarget = (ctx.Target.Center - npc.Center).SafeNormalize(Vector2.Zero);
                npc.velocity = Vector2.Lerp(npc.velocity, toTarget * VSlimeDirector.HoleDriftSpeed, 0.06f);
                ctx.HoleRadius = full;
                ctx.HazardRadius = full;
                ctx.ContactBase = VSlimeDirector.BlackHoleContact;
                ctx.HitboxTarget = new Vector2(full * 1.3f);
                ctx.PullStrength = VSlimeDirector.HolePullSpeed;
                ctx.PullRange = VSlimeDirector.HolePullRange;

                int local = Timer - ActiveStart;
                if (IsServer && local % VSlimeDirector.DebrisInterval == 0 && local <= VSlimeDirector.HoleActiveFrames - VSlimeDirector.DebrisInterval / 2) {
                    SpawnDebrisRing(ctx, local / VSlimeDirector.DebrisInterval);
                }
            }
            else {
                float p = VSlimeDirector.Progress(Timer, ActiveEnd, VSlimeDirector.HoleDissolveFrames);
                ctx.HoleRadius = full * (1f - VSlimeDirector.EaseIn(p));
                ctx.NoContact = true;
                if (p > 0.6f) {
                    ctx.Shape = VSlimeShape.Slime;
                }
            }

            if (At(1)) {
                VSlimeVfx.Cue("blackholeCharge", npc.Center, 1f, 0.9f, 2);
            }
            if (At(ActiveStart)) {
                ctx.Pulse = 1f;
                VSlimeVfx.Shake(npc.Center, 8f);
                VSlimeVfx.SkyFlash(0.5f);
                VSlimeVfx.Cue("BlackholeSpawn", npc.Center, 0.9f, 1f, 2);
            }
            if (At(ActiveEnd)) {
                ctx.Pulse = 0.6f;
                VSlimeVfx.Cue("blackholeEnd", npc.Center, 1f, 0.9f, 2);
            }
            return Timer >= ActiveEnd + VSlimeDirector.HoleDissolveFrames ? EndAttack(ctx) : null;
        }

        private static void SpawnDebrisRing(VSlimeStateContext ctx, int ring) {
            NPC npc = ctx.Npc;
            int count = VSlimeDirector.DebrisPerRing;
            float baseAngle = Rng(ctx, 1).NextFloat(MathHelper.TwoPi) + ring * MathHelper.Pi / count;
            float radius = Math.Max(400f, ctx.ArenaRadius - 24f);
            for (int j = 0; j < count; j++) {
                Vector2 pos = ctx.ArenaCenter + (baseAngle + MathHelper.TwoPi * j / count).ToRotationVector2() * radius;
                Shoot<VSlimeDebris>(ctx, pos, Vector2.Zero, VSlimeDirector.DmgDebris, npc.whoAmI, (j + ring) % 3);
            }
        }
    }
}
