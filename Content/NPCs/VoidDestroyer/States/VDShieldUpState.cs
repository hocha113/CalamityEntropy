using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 30% 护盾展开连接段(45 帧):清弹、护盾亮起、承伤减免升到 25%、全身颤抖收束一记爆闪。
    /// 不无敌(玩家的输出节奏不该被一段过场打断),收尾把签名首招钉为湮灭主炮
    /// </summary>
    [VaultState((int)VDStateIndex.ShieldUp, typeof(VDStateContext))]
    public class VDShieldUpState : VDStateBase
    {
        public override string StateName => "ShieldUp";
        public override VDStateIndex StateIndex => VDStateIndex.ShieldUp;
        public override bool ContactByDefault => false;
        public override bool RunsDuringBlink => true;
        public override int TimeoutFrames => int.MaxValue;

        public override void OnEnter(VDStateContext ctx)
        {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            ctx.QueuedChainState = -1;
            ctx.Phase = 3;
            ctx.DamageReduction = VDDirector.DRPhase3;
            VDVfx.ClearOwnProjectiles();
            MarkNetUpdate(ctx);
        }

        public override IVDState OnUpdate(VDStateContext ctx)
        {
            Timer++;
            NPC npc = ctx.Npc;
            if (ctx.TargetValid)
            {
                DeclareHoldRelative(ctx, new Vector2(0f, -300f), 0.06f, 0.2f, 18f);
            }
            float progress = MathHelper.Clamp(Timer / (float)VDDirector.ShieldUpDuration, 0f, 1f);
            ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, progress * 0.7f);
            ctx.CoreGlow = Math.Max(ctx.CoreGlow, progress);
            VDScreenFx.ReportVignette(0.35f * progress);

            if (Timer == 1)
            {
                VDVfx.Sound("VoidAnticipation", 1.2f, npc.Center, 2);
            }
            if (Timer % 3 == 0 && Timer < VDDirector.ShieldUpDuration - 6)
            {
                ConvergeSparks(ctx, new Color(220, 170, 255), 120f, 260f, 0.1f);
            }
            if (Timer == VDDirector.ShieldUpDuration - 4)
            {
                VDVfx.Sound("VoidAttack", 1.1f, npc.Center, 2);
                VDVfx.Shake(npc.Center, 8f);
                VDVfx.SparkBurst(npc.Center, new Color(220, 170, 255), 40, 6f, 16f, 40);
            }

            if (Timer >= VDDirector.ShieldUpDuration)
            {
                ctx.ForcedNextState = (int)VDStateIndex.AnnihilationCannon;
                ctx.AttackIndex = 1;
                ctx.AttackCooldown = 0;
                MarkNetUpdate(ctx);
                return new VDHubState();
            }
            return null;
        }
    }
}
