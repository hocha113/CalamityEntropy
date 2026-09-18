using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 深空投送:本体上飘,14 帧在玩家脚下地面开门(门即落点标记),同帧 2/3/4 个投送舱(GFB ×3)在 Z 2.5 的高空出现,
    /// 30 帧越来越大地坠向门,44 帧落地:舱体半径 60 一记接触伤害,教徒从舱里出来。场上上限 8。
    /// 门槛(有地面、未满员)在 <see cref="VDRotation.Substitute"/> 里判,这里只管投;教徒由舱在落地帧放出
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
            ctx.RimCharge = MathHelper.Clamp(Timer / (float)VDDirector.ReinforceSpawnFrame, 0f, 1f);
            //开门 + 放舱(14 帧),舱坠落 30 帧到门上(44 帧):门在地上亮着、舱在背景里越来越大,两样都是预告
            if (Timer == VDDirector.ReinforcePortalFrame) {
                VDVfx.Sound("VoidAnticipation", 0.9f, ctx.Target.Center, 3, 0.8f);
                ctx.WingPulse = Math.Max(ctx.WingPulse, 0.7f);
                if (IsServer) {
                    RollDropPoints(ctx);
                    ctx.Npc.netUpdate = true;
                }
            }
            if (Timer == VDDirector.ReinforceSpawnFrame) {
                //落地拍:本体只做表现,教徒由舱放出
                ctx.WingPulse = 1f;
                ctx.CoreGlow = 1f;
                ctx.RimFlash = 1f;
                VDVfx.Sound("VoidAttack", 0.7f, ctx.Npc.Center, 3, 0.8f);
            }
            if (Timer > VDDirector.ReinforceSpawnFrame) {
                ctx.CoreGlow = 0f;
            }
            if (Timer >= VDDirector.ReinforceDuration) {
                return EndAttack(ctx);
            }
            return null;
        }

        /// <summary>服务端掷落点、开门、放舱:RolledPoints[i] 为第 i 个落点,RandCount 为数量;舱的 Z 速度按坠落帧数配好,落地帧 = 开门帧 + 30</summary>
        private static void RollDropPoints(VDStateContext ctx) {
            int type = ModContent.NPCType<VoidVanguardCultist>();
            int alive = NPC.CountNPCS(type);
            int count = Math.Clamp(Math.Min(VDDirector.ReinforceCount(), VDDirector.MaxVanguards - alive), 0, ctx.RolledPoints.Length);
            ctx.RandCount = count;
            int portalLife = VDDirector.ReinforcePortalLife + (VDDirector.ReinforceSpawnFrame - VDDirector.ReinforcePortalFrame);
            float zVel = -VDDirector.ReinforcePodDepth / VDDirector.ReinforcePodFrames;
            for (int i = 0; i < count; i++) {
                float xOff = Main.rand.NextFloat(-VDDirector.ReinforceSpreadX, VDDirector.ReinforceSpreadX);
                Vector2 spawn = VDVfx.FindGround(ctx.Target.Center + new Vector2(xOff, 0f), ctx.Target.Bottom.Y);
                ctx.RolledPoints[i] = spawn;
                SpawnVisual<VDPortal>(ctx, spawn + new Vector2(0, -44f), Vector2.UnitX, VDPortal.ModeReinforce, portalLife);
                //舱的平面坐标就是落点(略抬高让教徒落在地面上而不是嵌进去),从 Z 2.5 只沿 Z 坠落
                ShootDepth<VDDropPod>(ctx, spawn + new Vector2(0, -30f), Vector2.Zero, VDDirector.DmgDropPod, VDDirector.ReinforcePodDepth, zVel, 0f, ctx.Npc.target, ctx.Npc.whoAmI);
            }
        }
    }
}
