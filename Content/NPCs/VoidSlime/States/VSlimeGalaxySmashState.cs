using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 星河重击,连做四轮:悬在玩家头顶 35 格并保持相对静止,1.5 秒内横向排出 14 颗间隔 12 格的凝胶球
    /// → 停住发白光,正下方亮出航道 → 下砸 70 格,横排凝胶球同时松开缓慢下落 → 砸完放 3 颗弱追踪紫星,停 1 秒后闪回玩家头顶
    /// 每轮总长固定,拍点都由 Timer 算出;航道在发白光那一拍锁死,下砸沿锁死的竖线
    /// </summary>
    [VaultState((int)VSlimeStateIndex.GalaxySmash, typeof(VSlimeStateContext))]
    public class VSlimeGalaxySmashState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.GalaxySmash;

        private const int LockStart = VSlimeDirector.GalaxySummonFrames;
        private const int FallStart = LockStart + VSlimeDirector.GalaxyLockFrames;
        private const int FallEnd = FallStart + VSlimeDirector.GalaxyFallFrames;
        private const int RestEnd = FallEnd + VSlimeDirector.GalaxyRestFrames;
        /// <summary>相邻两颗凝胶球的生成间隔,14 颗正好排满召唤段</summary>
        private const int GelInterval = VSlimeDirector.GalaxySummonFrames / VSlimeDirector.GalaxyGels;

        public override int TimeoutFrames => VSlimeDirector.GalaxyReps * VSlimeDirector.GalaxyCycle + 120;

        public override Vector2 AnchorFor(VSlimeStateContext ctx) => OverheadAnchor(ctx, VSlimeDirector.OverheadHeight);

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int rep = Timer / VSlimeDirector.GalaxyCycle;
            if (rep >= VSlimeDirector.GalaxyReps) {
                return EndAttack(ctx);
            }
            int cycleStart = rep * VSlimeDirector.GalaxyCycle;
            int local = Timer - cycleStart;
            bool lastRep = rep == VSlimeDirector.GalaxyReps - 1;

            if (local < LockStart) {
                HoldAt(ctx, new Vector2(0f, -VSlimeDirector.OverheadHeight), 0.12f, 0.3f, 30f);
                ctx.RowHeld = true;
                ctx.RimGlow = 0.5f;
                if (IsServer && local % GelInterval == 0 && local / GelInterval < VSlimeDirector.GalaxyGels) {
                    SpawnRowGel(ctx, local / GelInterval);
                }
            }
            else if (local < FallStart) {
                float p = VSlimeDirector.Progress(local, LockStart, VSlimeDirector.GalaxyLockFrames);
                Direct(ctx);
                npc.velocity *= 0.7f;
                ctx.RowHeld = true;
                ctx.BodyGlow = p;
                ctx.RimGlow = 1f;
                ctx.Squash = 0.3f * p;
                Lane(ctx, 0.4f + 0.6f * p);
                if (At(cycleStart + LockStart)) {
                    ctx.Pulse = 0.8f;
                    VSlimeVfx.Cue("charge", npc.Center, 1.3f, 0.7f, 2);
                }
            }
            else if (local < FallEnd) {
                Direct(ctx);
                npc.velocity = new Vector2(0f, VSlimeDirector.GalaxyFallDistance / VSlimeDirector.GalaxyFallFrames);
                ctx.BodyGlow = 0.7f;
                ctx.Squash = -0.32f;
                Lane(ctx, 0.5f);
                if (At(cycleStart + FallStart)) {
                    VSlimeVfx.Cue("Dash2", npc.Center, 0.8f, 0.9f, 2);
                }
            }
            else {
                Direct(ctx);
                npc.velocity *= 0.5f;
                float settle = VSlimeDirector.Progress(local, FallEnd, 20);
                ctx.Squash = 0.45f * (1f - settle) * MathF.Cos(settle * MathHelper.Pi * 1.5f);
                if (At(cycleStart + FallEnd)) {
                    Impact(ctx);
                }
                //最后一轮不再闪回头顶,停完就收招
                if (lastRep) {
                    return local >= RestEnd ? EndAttack(ctx) : null;
                }
                if (At(cycleStart + RestEnd) && IsServer) {
                    ctx.Owner.StartBlink(OverheadAnchor(ctx, VSlimeDirector.OverheadHeight));
                }
            }
            return null;
        }

        private static void Lane(VSlimeStateContext ctx, float strength) {
            NPC npc = ctx.Npc;
            ctx.LaneStart = npc.Center;
            ctx.LaneEnd = npc.Center + new Vector2(0f, VSlimeDirector.GalaxyFallDistance);
            ctx.LaneWidth = VSlimeDirector.GalaxyLaneWidth;
            ctx.LaneStrength = strength;
        }

        /// <summary>从中间向两侧交替排开,本体正下方不放球,留给下砸的航道</summary>
        private static void SpawnRowGel(VSlimeStateContext ctx, int index) {
            NPC npc = ctx.Npc;
            int side = index % 2 == 0 ? 1 : -1;
            float offsetX = side * (index / 2 + 0.5f) * VSlimeDirector.GalaxyGelSpacing;
            Shoot<VSlimeDarkGel>(ctx, npc.Center + new Vector2(offsetX, 0f), Vector2.Zero, VSlimeDirector.DmgDarkGel, VSlimeDarkGel.ModeRow, offsetX, npc.whoAmI);
        }

        private static void Impact(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.Pulse = 1f;
            ctx.ShakeStrength = 0.9f;
            VSlimeVfx.VoidBurst(npc.Center, 3f);
            VSlimeVfx.Stardust(npc.Center, 30, 3f, 13f, 1.2f);
            VSlimeVfx.Shake(npc.Center, 11f);
            VSlimeVfx.Sound(SoundID.Item167 with { Pitch = -0.3f }, npc.Center);
            if (!IsServer) {
                return;
            }
            //三颗紫星朝上方的扇面散开,再弱追踪
            for (int i = 0; i < VSlimeDirector.GalaxyStarlets; i++) {
                float angle = -MathHelper.PiOver2 + (i - (VSlimeDirector.GalaxyStarlets - 1) * 0.5f) * 0.9f;
                Shoot<VSlimeStarlet>(ctx, npc.Center, angle.ToRotationVector2() * VSlimeDirector.GalaxyStarletSpeed, VSlimeDirector.DmgStarlet,
                    VSlimeStarlet.ModeHoming, VSlimeDirector.HomingStarletLife, VSlimeStarlet.ColorPurple);
            }
        }
    }
}
