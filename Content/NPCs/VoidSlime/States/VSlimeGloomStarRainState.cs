using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 晦暗星雨:本体站定蓄力 1 秒,向天发射紫色伽玛射线;随后蓝色小辉星从玩家斜上方的画面外慢慢斜落,6 秒后整片消失
    /// 整招本体没有接触伤害;射线自带 1 秒细线预告;星雨从画面外进场,方向整招不变,来向在本体所在的一侧
    /// </summary>
    [VaultState((int)VSlimeStateIndex.GloomStarRain, typeof(VSlimeStateContext))]
    public class VSlimeGloomStarRainState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.GloomStarRain;

        private const int FireBeat = VSlimeDirector.RainChargeFrames;
        private const int SpawnStart = FireBeat + VSlimeDirector.RainSpawnDelay;
        private const int RainEnd = FireBeat + VSlimeDirector.RainStartDelay + VSlimeDirector.RainFrames;
        private const int SpawnEnd = RainEnd - VSlimeDirector.RainSpawnCutoff;

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.NoContact = true;

            if (At(0)) {
                Shoot<VSlimeGammaRay>(ctx, npc.Center, Vector2.Zero, VSlimeDirector.DmgPurpleRay, VSlimeGammaRay.ModeSky, npc.whoAmI, -MathHelper.PiOver2);
                VSlimeVfx.Cue("lasercharge", npc.Center, 1f, 0.7f, 2);
            }
            if (Timer < FireBeat) {
                float p = VSlimeDirector.Progress(Timer, 0, FireBeat);
                ctx.Squash = 0.3f * p;
                ctx.RimGlow = p;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.4f * p);
                if (Timer % 2 == 0) {
                    VSlimeVfx.Converge(npc.Center, 100f, 260f);
                }
            }
            else if (Timer < FireBeat + VSlimeDirector.SkyRayActiveFrames) {
                ctx.Squash = -0.28f;
                ctx.RimGlow = 1f;
                ctx.BodyGlow = 0.4f;
            }
            if (At(FireBeat)) {
                ctx.Pulse = 1f;
                VSlimeVfx.SkyFlash(0.8f);
            }

            if (IsServer && Timer >= SpawnStart && Timer < SpawnEnd && (Timer - SpawnStart) % VSlimeDirector.RainInterval == 0) {
                SpawnRain(ctx, (Timer - SpawnStart) / VSlimeDirector.RainInterval);
            }
            return Timer >= RainEnd + VSlimeDirector.RainTail ? EndAttack(ctx) : null;
        }

        /// <summary>生成线垂直于落向,摆在玩家上游的画面外;寿命算到消失拍为止</summary>
        private void SpawnRain(VSlimeStateContext ctx, int sequence) {
            float slant = VSlimeDirector.RainSlant;
            Vector2 dir = new Vector2(-ctx.SideDir * MathF.Sin(slant), MathF.Cos(slant));
            Vector2 lateral = dir.RotatedBy(MathHelper.PiOver2);
            Vector2 lineCenter = ctx.Target.Center - dir * VSlimeDirector.RainUpstream;
            UnifiedRandom rng = Rng(ctx, sequence);
            int count = VSlimeDirector.RainPerSpawn;
            for (int i = 0; i < count; i++) {
                //每颗占一段横向区间再在段内抖动,同一批不会挤在一起
                float slot = (i + rng.NextFloat()) / count * 2f - 1f;
                Vector2 pos = lineCenter + lateral * (slot * VSlimeDirector.RainHalfWidth);
                Shoot<VSlimeStarlet>(ctx, pos, dir * VSlimeDirector.RainSpeed, VSlimeDirector.DmgStarlet, VSlimeStarlet.ModeStraight, RainEnd - Timer, VSlimeStarlet.ColorBlue);
            }
        }
    }
}
