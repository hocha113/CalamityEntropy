using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 75% 变形:清弹、定住、六帧变形图逐帧推进,84 帧一记爆闪换二阶段贴图,能量翼随后展开。
    /// 收尾把阶段写 2,签名首招钉为轨道轰炸(退入背景开炮 = 新阶段的宣言)
    /// </summary>
    [VaultState((int)VDStateIndex.Transform, typeof(VDStateContext))]
    public class VDTransformState : VDStateBase
    {
        public override string StateName => "Transform";
        public override VDStateIndex StateIndex => VDStateIndex.Transform;
        public override bool ContactByDefault => false;
        public override bool RunsDuringBlink => true;
        public override int TimeoutFrames => int.MaxValue;

        public override void OnEnter(VDStateContext ctx)
        {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            ctx.QueuedChainState = -1;
            ctx.Npc.velocity = Vector2.Zero;
            VDVfx.ClearOwnProjectiles();
            MarkNetUpdate(ctx);
        }

        public override IVDState OnUpdate(VDStateContext ctx)
        {
            Timer++;
            NPC npc = ctx.Npc;
            DeclareDirect(ctx);
            npc.velocity *= 0.8f;
            DeclareAlpha(ctx, 1f, 1f);
            ctx.WingsVisible = false;
            ctx.TransformFrame = (int)MathHelper.Clamp(Timer / 14f, 0, 5);
            //越接近爆闪抖得越厉害(绘制层)
            float progress = MathHelper.Clamp(Timer / (float)VDDirector.TransformBurstFrame, 0f, 1f);
            ctx.ShakeStrength = System.Math.Max(ctx.ShakeStrength, progress * progress * 0.8f);
            ctx.CoreGlow = System.Math.Max(ctx.CoreGlow, progress);

            if (!Main.dedServ)
            {
                if (Timer == 8)
                {
                    VDVfx.Sound("VoidAnticipation", 0.9f, npc.Center, 2);
                }
                if (Timer % 4 == 0 && Timer < VDDirector.TransformBurstFrame)
                {
                    Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 6f);
                    VDVfx.VoidPuff(npc.Center + CEUtils.randomPointInCircle(70f), v, 1.3f, 0.8f);
                    ConvergeSparks(ctx, VDVfx.VoidPurple, 90f, 200f, 0.08f);
                }
                if (Timer == VDDirector.TransformBurstFrame)
                {
                    VDVfx.Sound("VoidAttack", 1f, npc.Center, 2);
                    VDVfx.Shake(npc.Center, 12f);
                    VDVfx.SparkBurst(npc.Center, VDVfx.VoidPurple, 60, 5f, 18f, 36);
                    VDVfx.Explosion(npc.Center, 1.1f, 26);
                }
            }

            if (Timer >= VDDirector.TransformDuration)
            {
                ctx.Phase = 2;
                //签名首招:轨道轰炸;表指针跳过 0 号槽(那正是轨道轰炸,历史闸也会拦,这里只是把意图写明)
                ctx.ForcedNextState = (int)VDStateIndex.OrbitalStrike;
                ctx.AttackIndex = 1;
                ctx.AttackCooldown = 0;
                MarkNetUpdate(ctx);
                return new VDHubState();
            }
            return null;
        }
    }
}
