using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 正常跳跃:像史莱姆王那样连跳逼近 8 秒,起跳前压扁蓄势,两小跳接一大跳
    /// 接触伤害全程开,起跳前的压扁就是预告;跳跃次序存 Combo 过线
    /// </summary>
    [VaultState((int)VSlimeStateIndex.PaleHop, typeof(VSlimeStateContext))]
    public class VSlimePaleHopState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.PaleHop;

        /// <summary>连续着地的帧数,各端按自己的碰撞结果数</summary>
        private int groundedFrames;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            groundedFrames = 0;
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            Ground(ctx);
            bool grounded = npc.velocity.Y == 0f;
            if (!grounded) {
                groundedFrames = 0;
                //上升拉长,下落略扁
                ctx.Squash = npc.velocity.Y < 0f ? -0.2f : -0.08f;
                return null;
            }

            groundedFrames++;
            npc.velocity.X *= 0.8f;
            if (groundedFrames == 1 && Timer > 2) {
                ctx.ShakeStrength = 0.25f;
                VSlimeVfx.PaleShards(npc.Bottom, 6, 1f, 4f);
                VSlimeVfx.Cue("ksLand", npc.Center, 1.1f, 0.5f, 2);
            }
            ctx.Squash = 0.34f * VSlimeDirector.Progress(groundedFrames, 0, VSlimeDirector.HopWait);

            if (Timer >= VSlimeDirector.HopDuration) {
                return groundedFrames >= 8 ? EndAttack(ctx) : null;
            }
            if (groundedFrames >= VSlimeDirector.HopWait) {
                int dir = ctx.Target.Center.X >= npc.Center.X ? 1 : -1;
                npc.velocity = new Vector2(dir * VSlimeDirector.HopSpeedX, VSlimeDirector.HopSpeedY(ctx.Combo));
                ctx.Combo++;
                groundedFrames = 0;
                MarkNetUpdate(ctx);
                VSlimeVfx.Sound(SoundID.NPCHit1 with { Volume = 0.55f, Pitch = -0.5f, MaxInstances = 2 }, npc.Center);
            }
            return null;
        }
    }
}
