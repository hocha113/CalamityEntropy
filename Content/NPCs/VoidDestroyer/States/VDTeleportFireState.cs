using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 传送火弹:依次闪现到玩家的角(P2 三角、P3 四角,左上 → 左下 → 右下 → 右上),每到一角落地 18 帧核心蓄力再朝玩家扇形射 5 发慢速直飞虚空弹,
    /// 每角停 42 帧。闪现由服务端发起、经 BlinkTimer 过线,闪现期间本状态计时暂停。自带传送,不走 hub 闪
    /// </summary>
    [VaultState((int)VDStateIndex.TeleportFire, typeof(VDStateContext))]
    public class VDTeleportFireState : VDStateBase
    {
        public override string StateName => "TeleportFire";
        public override VDStateIndex StateIndex => VDStateIndex.TeleportFire;

        private int cornerStep;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            cornerStep = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            int corners = VDDirector.TeleportFireCorners(ctx.Phase);
            if (cornerStep < corners) {
                if (Timer == 1 && IsServer) {
                    ctx.CornerIndex = VDDirector.TeleportFireOrder[cornerStep % VDDirector.TeleportFireOrder.Length];
                    ctx.Owner.StartBlink(ctx.Target.Center + VDVfx.CornerDirs[ctx.CornerIndex] * VDDirector.TeleportFireOffset);
                }
                //落地后 18 帧核心蓄力 + 汇聚流再出手:落地即预告
                float charge = MathHelper.Clamp(Timer / (float)VDDirector.TeleportFireShotFrame, 0f, 1f);
                ctx.CoreGlow = System.Math.Max(ctx.CoreGlow, charge);
                if (Timer < VDDirector.TeleportFireShotFrame && Timer % 2 == 0) {
                    ConvergeSparks(ctx, VDVfx.VoidPurple, 50f, 110f, 0.14f);
                }
                if (Timer == VDDirector.TeleportFireShotFrame) {
                    Vector2 core = ctx.Owner.CorePos;
                    Vector2 dir = (ctx.Target.Center - core).SafeNormalize(Vector2.UnitY);
                    MuzzleCue(ctx, dir, 4f, "CruiserSpit", 0.85f);
                    int half = VDDirector.TeleportFireBolts / 2;
                    for (int i = -half; i <= half; i++) {
                        Shoot<VDVoidBolt>(ctx, core, dir.RotatedBy(MathHelper.ToRadians(VDDirector.TeleportFireSpreadDeg * i)) * VDDirector.TeleportFireBoltSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeStraight);
                    }
                }
                if (Timer > VDDirector.TeleportFireShotFrame) {
                    ctx.CoreGlow = 0f;
                }
                if (Timer >= VDDirector.TeleportFireCornerFrames) {
                    cornerStep++;
                    ResetTimer();
                    MarkNetUpdate(ctx);
                }
                return null;
            }
            //收招:停在最后一角
            ctx.CoreGlow = 0f;
            if (Timer >= VDDirector.TeleportFireTail) {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
