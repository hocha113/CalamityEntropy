using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 立体四角:依次闪现到玩家的角(P2 三角、P3 四角,左上 → 左下 → 右下 → 右上),角位带深度(远 / 平面 / 近 / 远):
    /// 每到一角落地 18 帧核心蓄力再射 5 发。远角(Z 1.2,背景里的小影)射纵深贯穿弹,落点是垂直于角→玩家方向排开的 5 点,40 帧越来越大地飞来;
    /// 近角(Z -0.4,屏幕边缘的半透明巨影)射越肩弹,由大缩小落到同样的落点;平面角保持 5 发慢速直飞扇。每角停 42 帧。
    /// 三种来向逼玩家读三种运动签名。闪现由服务端发起、经 BlinkTimer 过线,闪现期间本状态计时暂停;自带传送,不走 hub 闪
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

        /// <summary>当前角的深度(收招拍停在最后一角的深度上,由 hub 的落定拍拉回)</summary>
        private float CurrentDepth(VDStateContext ctx) {
            int corners = VDDirector.TeleportFireCorners(ctx.Phase);
            return VDDirector.TeleportFireDepth(Math.Min(cornerStep, corners - 1));
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            int corners = VDDirector.TeleportFireCorners(ctx.Phase);
            float depth = CurrentDepth(ctx);
            ctx.Depth = depth;
            if (cornerStep < corners) {
                if (Timer == 1 && IsServer) {
                    ctx.CornerIndex = VDDirector.TeleportFireOrder[cornerStep % VDDirector.TeleportFireOrder.Length];
                    //角位是表观位置:按本角深度换算成世界坐标,远角的世界位置在更远处、近角在更近处,画出来都在玩家 480px 外的角上
                    Vector2 anchor = DepthAnchor(ctx, VDVfx.CornerDirs[ctx.CornerIndex] * VDDirector.TeleportFireOffset, depth);
                    ctx.Owner.StartBlink(anchor);
                }
                //落地后 18 帧核心蓄力 + 汇聚流再出手:落地即预告
                float charge = MathHelper.Clamp(Timer / (float)VDDirector.TeleportFireShotFrame, 0f, 1f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, charge);
                ctx.RimCharge = charge;
                if (Timer < VDDirector.TeleportFireShotFrame && Timer % 2 == 0) {
                    ConvergeSparks(ctx, VDVfx.VoidPurple, 50f, 110f, 0.14f);
                }
                if (Timer == VDDirector.TeleportFireShotFrame) {
                    Fire(ctx, depth);
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
            //收招:停在最后一角(深度由 hub 落定拍拉回平面)
            ctx.CoreGlow = 0f;
            if (Timer >= VDDirector.TeleportFireTail) {
                return EndAttack(ctx);
            }
            return null;
        }

        /// <summary>出手:平面角直飞扇;深度角朝垂直于角→玩家方向排开的 5 个落点射纵深弹(远角贯穿、近角越肩),40 帧到平面</summary>
        private void Fire(VDStateContext ctx, float depth) {
            Vector2 core = ctx.Owner.CorePos;
            int half = VDDirector.TeleportFireBolts / 2;
            if (Math.Abs(depth) < 0.01f) {
                Vector2 dir = (ctx.Target.Center - core).SafeNormalize(Vector2.UnitY);
                MuzzleCue(ctx, dir, 4f, "CruiserSpit", 0.85f);
                for (int i = -half; i <= half; i++) {
                    Shoot<VDVoidBolt>(ctx, core, dir.RotatedBy(MathHelper.ToRadians(VDDirector.TeleportFireSpreadDeg * i)) * VDDirector.TeleportFireBoltSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeStraight);
                }
                return;
            }
            Vector2 predicted = PredictTarget(ctx, VDDirector.TeleportFireZLead);
            Vector2 toTarget = (predicted - core).SafeNormalize(Vector2.UnitY);
            Vector2 perp = toTarget.RotatedBy(MathHelper.PiOver2);
            MuzzleCue(ctx, toTarget, 4f, "CruiserSpit", depth > 0f ? 0.7f : 1f, 1f);
            int mode = depth > 0f ? VDVoidBolt.ModeZPierce : VDVoidBolt.ModeZFromNear;
            for (int i = -half; i <= half; i++) {
                Vector2 landing = predicted + perp * (i * VDDirector.TeleportFireLandSpacing);
                (Vector2 vel, float zVel) = AimThroughPlane(core, depth, landing, VDDirector.TeleportFireZFrames);
                ShootDepth<VDVoidBolt>(ctx, core, vel, VDDirector.DmgVoidBolt, depth, zVel, 0f, mode);
            }
        }
    }
}
