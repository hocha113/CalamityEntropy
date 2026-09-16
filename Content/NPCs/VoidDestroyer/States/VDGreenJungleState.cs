using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 绿色丛林(全息三模式之一):本体头顶悬停,30 帧后放出全息丛林陆龟,陆龟传送到玩家移动方向一侧横冲六次(P3 七次,FTW 双龟)。
    /// 节拍由陆龟弹幕自管(出现 12 帧即预告),本体只在这里陪跑到陆龟收尾
    /// </summary>
    [VaultState((int)VDStateIndex.GreenJungle, typeof(VDStateContext))]
    public class VDGreenJungleState : VDStateBase
    {
        public override string StateName => "GreenJungle";
        public override VDStateIndex StateIndex => VDStateIndex.GreenJungle;
        public override bool NeedsRepositionBlink => true;

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            ctx.CoreColorTarget = VDVfx.JungleGreen;
            DeclareHoldRelative(ctx, VDDirector.JungleHoverOffset, 0.1f, 0.3f, 36f);
            int dashes = VDDirector.JungleDashes(ctx.Phase);
            int duration = VDDirector.JungleSpawnFrame + dashes * (VDHoloTortoise.AppearFrames + VDHoloTortoise.DashFrames) + VDDirector.JungleTail;

            ctx.CoreGlow = Math.Max(ctx.CoreGlow, MathHelper.Clamp(Timer / (float)VDDirector.JungleSpawnFrame, 0f, 1f));
            if (Timer < VDDirector.JungleSpawnFrame && Timer % 3 == 0) {
                ConvergeSparks(ctx, VDVfx.JungleGreen, 80f, 170f, 0.1f);
            }
            if (Timer == VDDirector.JungleSpawnFrame) {
                ctx.CoreGlow = 1f;
                ctx.WingPulse = 1f;
                VDVfx.Sound("VoidAnticipation", 0.8f, ctx.Npc.Center, 3, 0.9f);
                if (IsServer) {
                    if (Main.getGoodWorld) {
                        Shoot<VDHoloTortoise>(ctx, ctx.Npc.Center, Vector2.Zero, VDDirector.DmgHoloBeast, ctx.Npc.whoAmI, ctx.Npc.target, -1);
                        Shoot<VDHoloTortoise>(ctx, ctx.Npc.Center, Vector2.Zero, VDDirector.DmgHoloBeast, ctx.Npc.whoAmI, ctx.Npc.target, 1);
                    }
                    else {
                        Shoot<VDHoloTortoise>(ctx, ctx.Npc.Center, Vector2.Zero, VDDirector.DmgHoloBeast, ctx.Npc.whoAmI, ctx.Npc.target, 0);
                    }
                }
            }
            if (Timer >= duration) {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
