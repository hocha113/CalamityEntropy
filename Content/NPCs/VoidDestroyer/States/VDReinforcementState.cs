using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 支援投送:本体上飘,玩家脚下地面开门投下 2/3/4 个前卫教徒(GFB ×3),场上上限 8。
    /// 门槛(有地面、未满员)在 <see cref="VDRotation.Substitute"/> 里判,这里只管投
    /// </summary>
    [VaultState((int)VDStateIndex.Reinforcement, typeof(VDStateContext))]
    public class VDReinforcementState : VDStateBase
    {
        public override string StateName => "Reinforcement";
        public override VDStateIndex StateIndex => VDStateIndex.Reinforcement;
        public override bool NeedsRepositionBlink => true;

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            DeclareHoldRelative(ctx, VDDirector.ReinforceHoverOffset, VDDirector.ReinforceHoldStiffness, VDDirector.ReinforceHoldLerp, VDDirector.ReinforceHoldMaxSpeed);
            ctx.CoreGlow = Math.Max(ctx.CoreGlow, MathHelper.Clamp(Timer / (float)VDDirector.ReinforceSpawnFrame, 0f, 1f));
            if (Timer == VDDirector.ReinforceSpawnFrame) {
                ctx.WingPulse = 1f;
                ctx.CoreGlow = 1f;
                VDVfx.Sound("VoidAttack", 0.7f, ctx.Npc.Center, 3, 0.8f);
                if (IsServer) {
                    SpawnReinforcements(ctx);
                }
            }
            if (Timer >= VDDirector.ReinforceDuration) {
                return EndAttack(ctx);
            }
            return null;
        }

        private static void SpawnReinforcements(VDStateContext ctx) {
            int type = ModContent.NPCType<VoidVanguardCultist>();
            int alive = NPC.CountNPCS(type);
            int count = Math.Min(VDDirector.ReinforceCount(), VDDirector.MaxVanguards - alive);
            for (int i = 0; i < count; i++) {
                float xOff = Main.rand.NextFloat(-VDDirector.ReinforceSpreadX, VDDirector.ReinforceSpreadX);
                Vector2 spawn = VDVfx.FindGround(ctx.Target.Center + new Vector2(xOff, 0f), ctx.Target.Bottom.Y);
                SpawnVisual<VDPortal>(ctx, spawn + new Vector2(0, -44f), Vector2.UnitX, VDPortal.ModeReinforce, VDDirector.ReinforcePortalLife);
                int n = NPC.NewNPC(ctx.Npc.GetSource_FromAI(), (int)spawn.X, (int)spawn.Y, type);
                if (n < Main.maxNPCs) {
                    Main.npc[n].target = ctx.Npc.target;
                    if (Main.netMode == NetmodeID.Server) {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                    }
                }
            }
        }
    }
}
