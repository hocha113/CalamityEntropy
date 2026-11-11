using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 星体爆炸:虫洞悬在玩家头顶并保持相对静止,四颗大辉星环绕本体,每 100 帧扔出一颗
    /// 落点在出手那一帧玩家左右 38 格处,左右交替;落点锁死后不再追,大辉星自己带落点标记和爆开
    /// 奇点形态悬停高度降到 30 格,爆开时多 4 颗蓝星和 4 颗弱追踪的紫星
    /// </summary>
    [VaultState((int)VSlimeStateIndex.StellarBlast, typeof(VSlimeStateContext))]
    public class VSlimeStellarBlastState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.StellarBlast;

        private static float Height(VSlimeStateContext ctx)
            => ctx.Phase >= VSlimeForm.Singularity ? VSlimeDirector.OverheadHeightEx : VSlimeDirector.OverheadHeight;

        public override Vector2 AnchorFor(VSlimeStateContext ctx) => OverheadAnchor(ctx, Height(ctx));

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.Shape = VSlimeShape.Wormhole;
            HoldAt(ctx, new Vector2(0f, -Height(ctx)), 0.1f, 0.25f, 26f);

            int thrown = 0;
            for (int i = 0; i < VSlimeDirector.BlastStars; i++) {
                int beat = VSlimeDirector.BlastFirstThrow + i * VSlimeDirector.BlastThrowInterval;
                if (Timer >= beat) {
                    thrown++;
                }
                if (At(beat)) {
                    Throw(ctx, i);
                }
                //出手前 20 帧描边亮起
                if (Timer >= beat - 20 && Timer < beat) {
                    ctx.RimGlow = Math.Max(ctx.RimGlow, VSlimeDirector.Progress(Timer, beat - 20, 20));
                }
            }
            //大辉星在开头 30 帧里逐颗成形
            int formed = Math.Min(VSlimeDirector.BlastStars, 1 + Timer / 8);
            ctx.OrbitStars = Math.Max(0, formed - thrown);
            if (At(1)) {
                VSlimeVfx.Cue("LunarStar", npc.Center, 1.2f, 0.6f, 2);
            }

            int lastThrow = VSlimeDirector.BlastFirstThrow + (VSlimeDirector.BlastStars - 1) * VSlimeDirector.BlastThrowInterval;
            return Timer >= lastThrow + VSlimeDirector.BigStarFlight + VSlimeDirector.BlastTail ? EndAttack(ctx) : null;
        }

        private void Throw(VSlimeStateContext ctx, int index) {
            NPC npc = ctx.Npc;
            ctx.Pulse = 0.7f;
            VSlimeVfx.Sound(SoundID.Item9 with { Pitch = -0.3f }, npc.Center);
            if (!IsServer) {
                return;
            }
            int side = index % 2 == 0 ? ctx.SideDir : -ctx.SideDir;
            Vector2 target = ctx.Target.Center + new Vector2(side * VSlimeDirector.BlastSideDistance, 0f);
            Vector2 origin = npc.Center + VSlimeDirector.OrbitStarOffset(index, Timer);
            Shoot<VSlimeBigStar>(ctx, origin, Vector2.Zero, VSlimeDirector.DmgBigStar, target.X, target.Y, ctx.Phase >= VSlimeForm.Singularity ? 1f : 0f);
        }
    }
}
