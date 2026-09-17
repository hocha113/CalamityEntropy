using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 传送门出场:0-60 门在锚点展开;60-180 本体自门内下滑、淡入放大;180-220 门关闭;220-260 停顿并交还相机。
    /// 全程无敌无接触、限制圈不生效;相机前 30 帧滑向门,200 帧后不再赋值,交给 EModPlayer 的自然衰减滑回玩家
    /// </summary>
    [VaultState((int)VDStateIndex.Entrance, typeof(VDStateContext))]
    public class VDEntranceState : VDStateBase
    {
        public override string StateName => "Entrance";
        public override VDStateIndex StateIndex => VDStateIndex.Entrance;
        public override bool ContactByDefault => false;
        public override bool RunsDuringBlink => true;
        public override int TimeoutFrames => int.MaxValue;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            ctx.Npc.velocity = Vector2.Zero;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            DeclareDirect(ctx);
            npc.velocity = Vector2.Zero;
            ctx.ArenaActive = false;
            ctx.WingsVisible = false;

            if (Timer == 1) {
                VDVfx.Sound("portal_emerge", 1f, ctx.AnchorPos, 2);
            }

            int t = Timer;
            if (t <= VDDirector.EntrancePortalOpen) {
                DeclareAlpha(ctx, 0f, 0.6f);
                npc.Center = ctx.AnchorPos;
                ctx.PortalOpenness = VDVfx.EaseOut(t / (float)VDDirector.EntrancePortalOpen);
            }
            else if (t <= VDDirector.EntranceDescendEnd) {
                float p = (t - VDDirector.EntrancePortalOpen) / (float)(VDDirector.EntranceDescendEnd - VDDirector.EntrancePortalOpen);
                float eased = VDVfx.EaseOut(p);
                DeclareAlpha(ctx, eased, MathHelper.Lerp(0.6f, 1f, eased));
                npc.Center = ctx.AnchorPos + new Vector2(0, 150f * eased);
                ctx.PortalOpenness = 1f;
                if (!Main.dedServ && t % 6 == 0) {
                    Vector2 v = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(1f, 4f));
                    VDVfx.VoidPuff(ctx.AnchorPos + new Vector2(Main.rand.NextFloat(-80f, 80f), 0), v, 1.4f, 0.7f);
                }
            }
            else {
                DeclareAlpha(ctx, 1f, 1f);
                ctx.PortalOpenness = t <= VDDirector.EntrancePortalClose
                    ? 1f - VDVfx.EaseOut((t - VDDirector.EntranceDescendEnd) / (float)(VDDirector.EntrancePortalClose - VDDirector.EntranceDescendEnd))
                    : 0f;
                if (t == VDDirector.EntranceDescendEnd + 1) {
                    //落定的一记:声音 + 震屏 + 核心亮 + 描边爆闪
                    VDVfx.Sound("VoidAttack", 0.9f, npc.Center, 2);
                    VDVfx.Shake(npc.Center, 6f);
                    ctx.CoreGlow = 1f;
                    ctx.RimFlash = 1f;
                    ctx.ShakeStrength = 0.6f;
                }
            }

            if (t <= VDDirector.EntranceCameraFrames) {
                ctx.CameraFocus = ctx.AnchorPos + new Vector2(0, 80);
                ctx.CameraShift = t < 30 ? t / 30f * 0.12f : 0.12f;
            }

            if (t >= VDDirector.EntranceDuration) {
                return new VDHubState();
            }
            return null;
        }
    }
}
