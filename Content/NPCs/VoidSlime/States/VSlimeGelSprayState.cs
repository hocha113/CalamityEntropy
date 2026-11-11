using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 凝胶喷射:虫洞悬在玩家头顶 35 格并保持相对静止,向上喷 6 秒暗物质凝胶球
    /// 凝胶球受逐渐增大的重力缓慢落下,喷射结束 1 秒后全部消失
    /// 公平阀:终端速度 6.5,从本体高度落到玩家身边至少要 86 帧;同一竖列上下两颗相距约 150 像素
    /// </summary>
    [VaultState((int)VSlimeStateIndex.GelSpray, typeof(VSlimeStateContext))]
    public class VSlimeGelSprayState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.GelSpray;

        private const int SprayEnd = VSlimeDirector.SprayWindup + VSlimeDirector.SprayFrames;
        private const int ClearBeat = SprayEnd + VSlimeDirector.SprayLinger;

        public override Vector2 AnchorFor(VSlimeStateContext ctx) => OverheadAnchor(ctx, VSlimeDirector.OverheadHeight);

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.Shape = VSlimeShape.Wormhole;
            HoldAt(ctx, new Vector2(0f, -VSlimeDirector.OverheadHeight), 0.1f, 0.25f, 26f);

            if (Timer < VSlimeDirector.SprayWindup) {
                float p = VSlimeDirector.Progress(Timer, 0, VSlimeDirector.SprayWindup);
                ctx.RimGlow = p;
                ctx.Swell = 0.1f * p;
            }
            else if (Timer < SprayEnd) {
                int local = Timer - VSlimeDirector.SprayWindup;
                ctx.RimGlow = 0.6f;
                ctx.Swell = 0.06f * MathF.Sin(local * 0.6f);
                if (local % VSlimeDirector.SprayInterval == 0) {
                    Spray(ctx, local / VSlimeDirector.SprayInterval);
                }
                if (local % 8 == 0) {
                    VSlimeVfx.Sound(SoundID.Item155 with { Volume = 0.4f, PitchVariance = 0.3f, MaxInstances = 3 }, npc.Center);
                }
            }

            if (At(1)) {
                VSlimeVfx.Cue("VortexSpawn", npc.Center, 1.2f, 0.6f, 2);
            }
            return Timer >= ClearBeat + VSlimeDirector.SprayTail ? EndAttack(ctx) : null;
        }

        /// <summary>寿命算到清场拍为止,所有凝胶球同一帧消失</summary>
        private void Spray(VSlimeStateContext ctx, int sequence) {
            if (!IsServer) {
                return;
            }
            NPC npc = ctx.Npc;
            UnifiedRandom rng = Rng(ctx, sequence);
            Vector2 velocity = new Vector2(
                rng.NextFloat(-1f, 1f) * VSlimeDirector.SpraySpeedX,
                -rng.NextFloat(VSlimeDirector.SpraySpeedYMin, VSlimeDirector.SpraySpeedYMax));
            Shoot<VSlimeDarkGel>(ctx, npc.Center + new Vector2(0f, -30f), velocity, VSlimeDirector.DmgDarkGel, VSlimeDarkGel.ModeFall, ClearBeat - Timer);
        }
    }
}
