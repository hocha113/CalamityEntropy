using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 奇点:超新星中心形成扭曲背景的蓝色黑洞,半径每秒涨 2.5 格,涨到 100 格封顶
    /// 血量已经打到 1 时,黑洞涨到 12 格就进终幕;否则一直涨,直到血量打空
    /// 黑洞内的玩家持续受接触伤害并获得衰变;没有超时,这是整场战斗的收束
    /// </summary>
    [VaultState((int)VSlimeStateIndex.Singularity, typeof(VSlimeStateContext))]
    public class VSlimeSingularityState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.Singularity;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        /// <summary>黑洞张开前的预警帧数,这段没有判定</summary>
        private const int OpenGrace = 40;

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            float radius = Math.Min(VSlimeDirector.SingularityMaxRadius, VSlimeDirector.SingularityStartRadius + VSlimeDirector.SingularityGrowth * Timer);
            ctx.Shape = VSlimeShape.BlackHole;
            ctx.HoleTint = 1f;
            ctx.HoleRadius = radius;
            ctx.HitboxTarget = new Vector2(MathHelper.Clamp(radius * 1.4f, VSlimeDirector.BodySize.X, VSlimeDirector.SingularityHitboxMax));
            if (Timer >= OpenGrace) {
                ctx.HazardRadius = radius * 0.92f;
                ctx.ContactBase = VSlimeDirector.SingularityContact;
                ctx.HazardDecays = true;
            }
            else {
                ctx.NoContact = true;
                ctx.WarnRadius = VSlimeDirector.SingularityEndRadius;
                ctx.WarnStrength = 1f - Timer / (float)OpenGrace;
            }

            if (At(1)) {
                ctx.Pulse = 1f;
                VSlimeVfx.Cue("BlackholeSpawn", npc.Center, 0.6f, 1f, 2);
                VSlimeVfx.Shake(npc.Center, 8f);
            }
            if (IsServer && npc.life <= 1 && radius >= VSlimeDirector.SingularityEndRadius) {
                ctx.HeldRadius = radius;
                MarkNetUpdate(ctx);
                return new VSlimeDeathState();
            }
            return null;
        }
    }
}
