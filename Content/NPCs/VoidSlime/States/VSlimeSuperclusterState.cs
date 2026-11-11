using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 超星系轰炸:蓝光一闪跃入背景化作黑洞,7 秒内不断往玩家周围投十字星
    /// 十字星先在落点亮标记并从深处逼近,到达玩家层才爆开并有判定;本体在背景里没有接触伤害,但仍然能被攻击
    /// 公平阀:每颗有 56 帧标记;每三颗里一颗瞄预判点,其余散布在周围,停着和直飞都会被点到,换向就能躲
    /// </summary>
    [VaultState((int)VSlimeStateIndex.Supercluster, typeof(VSlimeStateContext))]
    public class VSlimeSuperclusterState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.Supercluster;

        private const int BombardStart = VSlimeDirector.BombardDiveFrames;
        private const int BombardEnd = BombardStart + VSlimeDirector.BombardFrames;
        /// <summary>背景里黑洞的半径,绘制时还要乘退入背景的倍率</summary>
        private const float BackHoleRadius = 150f;
        private static readonly List<Player> candidates = new();

        public override Vector2 AnchorFor(VSlimeStateContext ctx)
            => VSlimeDirector.ClampToArena(ctx, ctx.ArenaCenter + new Vector2(0f, -220f));

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.NoContact = true;
            ctx.HoleTint = 1f;

            if (Timer < BombardStart) {
                float p = VSlimeDirector.Progress(Timer, 0, BombardStart);
                ctx.Depth = VSlimeDirector.EaseOut(p);
                ctx.BodyGlow = 1f - p;
                ctx.Shape = p > 0.5f ? VSlimeShape.BlackHole : VSlimeShape.Slime;
                ctx.HoleRadius = BackHoleRadius * p;
            }
            else if (Timer < BombardEnd) {
                ctx.Depth = 1f;
                ctx.Shape = VSlimeShape.BlackHole;
                ctx.HoleRadius = BackHoleRadius;
                int local = Timer - BombardStart;
                if (IsServer && local % VSlimeDirector.BombardInterval == 0) {
                    SpawnCross(ctx, local / VSlimeDirector.BombardInterval);
                }
            }
            else {
                float p = VSlimeDirector.Progress(Timer, BombardEnd, VSlimeDirector.BombardReturnFrames);
                ctx.Depth = 1f - VSlimeDirector.EaseIn(p);
                ctx.Shape = p > 0.5f ? VSlimeShape.Slime : VSlimeShape.BlackHole;
                ctx.HoleRadius = BackHoleRadius * (1f - p);
            }

            if (At(1)) {
                ctx.Pulse = 1f;
                VSlimeVfx.StarBurst(npc.Center, 2.5f);
                VSlimeVfx.Flash(npc.Center, 0.2f);
                VSlimeVfx.SkyFlash(0.7f);
                VSlimeVfx.Cue("teleport", npc.Center, 0.8f, 0.9f, 2);
            }
            return Timer >= BombardEnd + VSlimeDirector.BombardReturnFrames ? EndAttack(ctx) : null;
        }

        /// <summary>
        /// 只在权威端调:场内的玩家轮流当目标,每三颗里一颗瞄预判点,其余散布在目标周围
        /// 超新星阶段也走这里
        /// </summary>
        public static void SpawnCross(VSlimeStateContext ctx, int sequence) {
            NPC npc = ctx.Npc;
            candidates.Clear();
            foreach (Player player in Main.ActivePlayers) {
                if (!player.dead && player.Distance(npc.Center) < VSlimeDirector.MaxFindDistance) {
                    candidates.Add(player);
                }
            }
            Player target = candidates.Count > 0 ? candidates[sequence % candidates.Count] : ctx.Target;
            if (target == null) {
                return;
            }
            UnifiedRandom rng = Rng(ctx, 100 + sequence);
            Vector2 pos;
            if (sequence % 3 == 0) {
                Vector2 lead = target.velocity * VSlimeDirector.CrossApproachFrames;
                float length = lead.Length();
                if (length > VSlimeDirector.BombardLeadMax) {
                    lead *= VSlimeDirector.BombardLeadMax / length;
                }
                pos = target.Center + lead + rng.NextVector2Circular(40f, 40f);
            }
            else {
                pos = target.Center + rng.NextVector2Circular(VSlimeDirector.BombardScatter, VSlimeDirector.BombardScatter);
            }
            Shoot<VSlimeCrossStar>(ctx, pos, Vector2.Zero, VSlimeDirector.DmgCrossStar, rng.NextFloat(MathHelper.PiOver2));
        }
    }
}
