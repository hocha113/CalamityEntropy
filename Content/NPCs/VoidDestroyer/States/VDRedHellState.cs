using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 深空红魔(全息三模式之一):全息红恶魔投影在 Z 2.5 的远景层(表观在玩家侧上方,一尊压在虚空天幕上的巨影),每轮 170 帧:
    /// 1 红魔换位 + 探照光盘钉到玩家脚下、红魔到光盘拉预警锥,30 红射线从红魔射向镜头(45 帧,光盘慢慢追人),
    /// 60/80(FTW 95)三排全息三叉戟从红魔处收敛到以玩家为心的弧上落点(纵深贯穿,各带落点标记),共三轮后 40 帧收尾。
    /// 本体留在平面可打,只做指挥演出。公平阀:光盘预警 30 帧 + 核心渐亮,出手前 6 帧汇聚粒子静默;三叉戟只在穿过平面那几帧有判定
    /// </summary>
    [VaultState((int)VDStateIndex.RedHell, typeof(VDStateContext))]
    public class VDRedHellState : VDStateBase
    {
        public override string StateName => "RedHell";
        public override VDStateIndex StateIndex => VDStateIndex.RedHell;
        public override Vector2 AnchorFor(VDStateContext ctx) => ctx.Target.Center + VDDirector.RedHellHoverOffset;

        private int round;
        private bool wrapUp;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            round = 0;
            wrapUp = false;
        }

        /// <summary>红魔的平面位置:表观「玩家侧上方」按红魔深度换算</summary>
        private static Vector2 DevilAnchor(VDStateContext ctx, int side)
            => DepthAnchor(ctx, new Vector2(side * VDDirector.RedDevilApparent.X, VDDirector.RedDevilApparent.Y), VDDirector.RedDevilDepth);

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            ctx.CoreColorTarget = VDVfx.HellRed;

            if (wrapUp) {
                ctx.HoloWrapUp = true;
                DeclareHoldRelative(ctx, VDDirector.RedHellHoverOffset, VDDirector.RedHellHoldStiffness, VDDirector.RedHellHoldLerp, VDDirector.RedHellHoldMaxSpeed);
                if (Timer >= VDDirector.RedHellTail) {
                    return EndAttack(ctx);
                }
                return null;
            }

            int t = Timer;
            if (t == 1) {
                if (IsServer) {
                    ctx.SideDir = Main.rand.NextBool() ? -1 : 1;
                    ctx.AnchorPos = DevilAnchor(ctx, ctx.SideDir);
                    if (round == 0) {
                        SpawnVisual<VDHoloRedDevil>(ctx, ctx.AnchorPos, Vector2.Zero, npc.whoAmI);
                    }
                    //探照光盘:预警 30 帧后从红魔射向镜头
                    Shoot<VDRedRay>(ctx, ctx.Target.Center, Vector2.Zero, VDDirector.DmgRedRay, VDDirector.RedHellRayDuration, VDDirector.RedHellRayWarn, npc.whoAmI + 1);
                    npc.netUpdate = true;
                }
                VDVfx.Sound("VoidAnticipation", 0.9f, npc.Center, 3, 0.9f);
            }

            //全息红恶魔的蓄力读数:1~60 蓄满,60~90 回落
            ctx.HoloCharge = t <= 60 ? MathHelper.Clamp(t / 60f, 0f, 1f) : MathHelper.Clamp(1f - (t - 60) / 30f, 0f, 1f);

            int rayEnd = VDDirector.RedHellRayWarn + VDDirector.RedHellRayDuration;
            if (t <= rayEnd) {
                //预警期与发射期本体定住指挥
                DeclareDirect(ctx);
                npc.velocity *= 0.8f;
                float warn = MathHelper.Clamp(t / (float)VDDirector.RedHellRayWarn, 0f, 1f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, warn);
                ctx.RimCharge = warn;
                if (t <= VDDirector.RedHellRayWarn) {
                    //汇聚流在出手前 6 帧断掉:静默即预告
                    if (t < VDDirector.RedHellRayWarn - 6 && t % 2 == 0) {
                        ConvergeSparks(ctx, VDVfx.HellRed, 70f, 150f, 0.1f);
                    }
                    else if (t >= VDDirector.RedHellRayWarn - 6) {
                        ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.45f);
                        ctx.RimSuppress = 1f;
                    }
                }
            }
            else {
                DeclareHoldRelative(ctx, VDDirector.RedHellHoverOffset, VDDirector.RedHellHoldStiffness, VDDirector.RedHellHoldLerp, VDDirector.RedHellHoldMaxSpeed);
            }

            if (t == VDDirector.RedHellRayWarn) {
                //红魔开火那一帧,本体也「下令」:反冲 + 爆闪(射线本身由光盘弹幕画)
                MuzzleCue(ctx, Vector2.UnitY, 5f, null);
            }

            //三叉戟:两排交错,三阶段各 +1 并把张角提到 140°;FTW 追加第三排 5 根
            int n1 = VDDirector.RedHellTridents(ctx.Phase);
            float arc = VDDirector.RedHellArcDeg(ctx.Phase);
            if (t == VDDirector.RedHellTridentFrame1) {
                FireTridentRow(ctx, n1, arc, n1 - 1, 0f);
            }
            if (t == VDDirector.RedHellTridentFrame2) {
                FireTridentRow(ctx, n1 - 1, arc, n1 - 1, 0.5f);
            }
            if (Main.getGoodWorld && t == VDDirector.RedHellTridentFrameFTW) {
                FireTridentRow(ctx, 5, arc, 4, 0f);
            }

            if (t >= VDDirector.RedHellCycle) {
                round++;
                ResetTimer();
                if (round >= VDDirector.RedHellRounds) {
                    wrapUp = true;
                }
                MarkNetUpdate(ctx);
            }
            return null;
        }

        /// <summary>
        /// 从红魔位置(AnchorPos,Z 2.5)向玩家射一排纵深贯穿三叉戟:落点排在以玩家预测位置为心、半径 RedHellLandRadius 的弧上,
        /// 弧心方向 = 红魔投影 → 玩家,角度 = -arc/2 + arc * (i + offset) / divisions;45 帧到平面
        /// </summary>
        private static void FireTridentRow(VDStateContext ctx, int count, float arcDeg, int divisions, float offset) {
            Vector2 from = ctx.AnchorPos;
            float z = VDDirector.RedDevilDepth;
            VDVfx.Sound("VoidAttack", 1.2f * VDDepth.DopplerPitch(z), VDDepth.Project(from, z), 4, 0.9f);
            if (!IsServer) {
                return;
            }
            Vector2 predicted = PredictTarget(ctx, VDDirector.RedHellTridentLead);
            Vector2 dir = (predicted - VDDepth.Project(from, z, ctx.Target.Center)).SafeNormalize(Vector2.UnitX);
            float arc = MathHelper.ToRadians(arcDeg);
            for (int i = 0; i < count; i++) {
                float ang = -arc / 2f + arc * (i + offset) / Math.Max(1, divisions);
                Vector2 landing = predicted + dir.RotatedBy(ang) * VDDirector.RedHellLandRadius;
                (Vector2 vel, float zVel) = AimThroughPlane(from, z, landing, VDDirector.RedHellTridentFrames);
                ShootDepth<VDHoloTrident>(ctx, from, vel, VDDirector.DmgTrident, z, zVel, 0f);
            }
        }
    }
}
