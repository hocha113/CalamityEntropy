using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 传送火弹:依次闪现到玩家四角(左上 → 左下 → 右下 → 右上),每到一角朝玩家扇形射 5 发慢速直飞虚空弹。
    /// 闪现由服务端发起、经 BlinkTimer 过线,闪现期间本状态计时暂停,落地 8 帧后出手(落地即预告)。自带传送,不走 hub 闪
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
            if (cornerStep < VDDirector.TeleportFireOrder.Length) {
                if (Timer == 1 && IsServer) {
                    ctx.CornerIndex = VDDirector.TeleportFireOrder[cornerStep];
                    ctx.Owner.StartBlink(ctx.Target.Center + VDVfx.CornerDirs[ctx.CornerIndex] * VDDirector.TeleportFireOffset);
                }
                //落地后核心亮起到出手
                ctx.CoreGlow = System.Math.Max(ctx.CoreGlow, MathHelper.Clamp(Timer / (float)VDDirector.TeleportFireShotFrame, 0f, 1f));
                if (Timer == VDDirector.TeleportFireShotFrame) {
                    Vector2 core = ctx.Owner.CorePos;
                    Vector2 dir = (ctx.Target.Center - core).SafeNormalize(Vector2.UnitY);
                    MuzzleCue(ctx, dir, 4f, "CruiserSpit", 0.85f);
                    int half = VDDirector.TeleportFireBolts / 2;
                    for (int i = -half; i <= half; i++) {
                        Shoot<VDVoidBolt>(ctx, core, dir.RotatedBy(MathHelper.ToRadians(VDDirector.TeleportFireSpreadDeg * i)) * VDDirector.TeleportFireBoltSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeStraight);
                    }
                }
                if (Timer >= VDDirector.TeleportFireCornerFrames) {
                    cornerStep++;
                    ResetTimer();
                    MarkNetUpdate(ctx);
                }
                return null;
            }
            if (Timer >= VDDirector.TeleportFireTail) {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
