using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 超新星:红巨星压缩成耀眼的白色超新星,本体没有接触伤害;1.5 秒后十字星从背景不断涌向玩家身边,持续 8 秒
    /// 十字星的选点和判定与超星系轰炸相同,间隔从 8 帧缩到 5 帧,密度高 50%
    /// </summary>
    [VaultState((int)VSlimeStateIndex.Supernova, typeof(VSlimeStateContext))]
    public class VSlimeSupernovaState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.Supernova;

        private const int BombardStart = VSlimeDirector.NovaCompressFrames;
        private const int BombardEnd = BombardStart + VSlimeDirector.NovaFrames;

        public override int TimeoutFrames => BombardEnd + VSlimeDirector.NovaTail + 120;
        public override bool RequiresTarget => false;

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            float compress = VSlimeDirector.Smooth(VSlimeDirector.Progress(Timer, 0, VSlimeDirector.NovaCompressFrames));
            float radius = MathHelper.Lerp(VSlimeDirector.GiantRadius, VSlimeDirector.NovaRadius, compress);
            ctx.Shape = VSlimeShape.Star;
            ctx.StarRadius = radius * (1f + 0.04f * MathF.Sin(Timer * 0.3f));
            ctx.StarHeat = 1f + compress;
            ctx.NoContact = true;
            ctx.HitboxTarget = new Vector2(Math.Max(VSlimeDirector.BodySize.X, radius * 1.4f));
            if (compress < 1f) {
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.6f * compress);
            }

            if (At(1)) {
                VSlimeVfx.Cue("blackholeCharge", npc.Center, 1.4f, 0.9f, 2);
            }
            if (At(BombardStart)) {
                ctx.Pulse = 1f;
                VSlimeVfx.StarBurst(npc.Center, 5f);
                VSlimeVfx.Shake(npc.Center, 12f);
                VSlimeVfx.Flash(npc.Center, 0.35f);
                VSlimeVfx.SkyFlash(1f);
                VSlimeVfx.Cue("explosionbig", npc.Center, 1.1f, 0.9f, 2);
            }
            if (IsServer && ctx.TargetValid && Timer >= BombardStart && Timer < BombardEnd && (Timer - BombardStart) % VSlimeDirector.NovaInterval == 0) {
                VSlimeSuperclusterState.SpawnCross(ctx, (Timer - BombardStart) / VSlimeDirector.NovaInterval);
            }
            return Timer >= BombardEnd + VSlimeDirector.NovaTail ? new VSlimeSingularityState() : null;
        }
    }
}
