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
    /// 支援投送:本体上飘,20 帧先在玩家脚下地面开门(门即预告),44 帧才投下 2/3/4 个前卫教徒(GFB ×3),场上上限 8。
    /// 门槛(有地面、未满员)在 <see cref="VDRotation.Substitute"/> 里判,这里只管投
    /// </summary>
    [VaultState((int)VDStateIndex.Reinforcement, typeof(VDStateContext))]
    public class VDReinforcementState : VDStateBase
    {
        public override string StateName => "Reinforcement";
        public override VDStateIndex StateIndex => VDStateIndex.Reinforcement;
        public override Vector2 AnchorFor(VDStateContext ctx) => ctx.Target.Center + VDDirector.ReinforceHoverOffset;

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            DeclareHoldRelative(ctx, VDDirector.ReinforceHoverOffset, VDDirector.ReinforceHoldStiffness, VDDirector.ReinforceHoldLerp, VDDirector.ReinforceHoldMaxSpeed);
            ctx.CoreGlow = Math.Max(ctx.CoreGlow, MathHelper.Clamp(Timer / (float)VDDirector.ReinforceSpawnFrame, 0f, 1f));
            //先开门(20 帧)再投人(44 帧):门在地上亮着的那一秒就是预告
            if (Timer == VDDirector.ReinforcePortalFrame) {
                VDVfx.Sound("VoidAnticipation", 0.9f, ctx.Target.Center, 3, 0.8f);
                if (IsServer) {
                    RollDropPoints(ctx);
                    ctx.Npc.netUpdate = true;
                }
            }
            if (Timer == VDDirector.ReinforceSpawnFrame) {
                ctx.WingPulse = 1f;
                ctx.CoreGlow = 1f;
                VDVfx.Sound("VoidAttack", 0.7f, ctx.Npc.Center, 3, 0.8f);
                if (IsServer) {
                    SpawnReinforcements(ctx);
                }
            }
            if (Timer > VDDirector.ReinforceSpawnFrame) {
                ctx.CoreGlow = 0f;
            }
            if (Timer >= VDDirector.ReinforceDuration) {
                return EndAttack(ctx);
            }
            return null;
        }

        /// <summary>服务端掷落点并先开门:RolledPoints[i] 为第 i 个落点,RandCount 为数量</summary>
        private static void RollDropPoints(VDStateContext ctx) {
            int type = ModContent.NPCType<VoidVanguardCultist>();
            int alive = NPC.CountNPCS(type);
            int count = Math.Clamp(Math.Min(VDDirector.ReinforceCount(), VDDirector.MaxVanguards - alive), 0, ctx.RolledPoints.Length);
            ctx.RandCount = count;
            int portalLife = VDDirector.ReinforcePortalLife + (VDDirector.ReinforceSpawnFrame - VDDirector.ReinforcePortalFrame);
            for (int i = 0; i < count; i++) {
                float xOff = Main.rand.NextFloat(-VDDirector.ReinforceSpreadX, VDDirector.ReinforceSpreadX);
                Vector2 spawn = VDVfx.FindGround(ctx.Target.Center + new Vector2(xOff, 0f), ctx.Target.Bottom.Y);
                ctx.RolledPoints[i] = spawn;
                SpawnVisual<VDPortal>(ctx, spawn + new Vector2(0, -44f), Vector2.UnitX, VDPortal.ModeReinforce, portalLife);
            }
        }

        private static void SpawnReinforcements(VDStateContext ctx) {
            int type = ModContent.NPCType<VoidVanguardCultist>();
            for (int i = 0; i < ctx.RandCount && i < ctx.RolledPoints.Length; i++) {
                Vector2 spawn = ctx.RolledPoints[i];
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
