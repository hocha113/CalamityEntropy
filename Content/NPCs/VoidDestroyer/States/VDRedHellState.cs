using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 红色地狱(全息三模式之一):全息红恶魔停在玩家左/右 30 格,每轮 170 帧:
    /// 1 传送 + 预警竖线,30 红射线(45 帧)向下,60/70(FTW 80)三排全息三叉戟扇射,共三轮后 40 帧收尾。
    /// 公平阀:红射线前 30 帧竖直预警线 + 核心渐亮,出手前 6 帧汇聚粒子静默;本体在预警与发射期定住
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
                    if (round == 0) {
                        SpawnVisual<VDHoloRedDevil>(ctx, npc.Center + new Vector2(0, 140f), Vector2.Zero, npc.whoAmI);
                    }
                    ctx.SideDir = Main.rand.NextBool() ? -1 : 1;
                    ctx.AnchorPos = ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.RedHellDevilOffset, 0f);
                    npc.netUpdate = true;
                }
                VDVfx.Sound("VoidAnticipation", 0.9f, npc.Center, 3, 0.9f);
            }

            //全息红恶魔的蓄力读数:1~60 蓄满,60~90 回落
            ctx.HoloCharge = t <= 60 ? MathHelper.Clamp(t / 60f, 0f, 1f) : MathHelper.Clamp(1f - (t - 60) / 30f, 0f, 1f);

            int rayEnd = VDDirector.RedHellRayWarn + VDDirector.RedHellRayDuration;
            if (t <= rayEnd) {
                //预警期与发射期本体定住
                DeclareDirect(ctx);
                npc.velocity *= 0.8f;
                float warn = MathHelper.Clamp(t / (float)VDDirector.RedHellRayWarn, 0f, 1f);
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, warn);
                if (t <= VDDirector.RedHellRayWarn) {
                    ctx.RedRayWarning = warn;
                    //汇聚流在出手前 6 帧断掉:静默即预告
                    if (t < VDDirector.RedHellRayWarn - 6 && t % 2 == 0) {
                        ConvergeSparks(ctx, VDVfx.HellRed, 70f, 150f, 0.1f);
                    }
                    else if (t >= VDDirector.RedHellRayWarn - 6) {
                        ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.45f);
                    }
                }
            }
            else {
                DeclareHoldRelative(ctx, VDDirector.RedHellHoverOffset, VDDirector.RedHellHoldStiffness, VDDirector.RedHellHoldLerp, VDDirector.RedHellHoldMaxSpeed);
            }

            if (t == VDDirector.RedHellRayWarn) {
                MuzzleCue(ctx, Vector2.UnitY, 5f, null);
                Shoot<VDRedRay>(ctx, ctx.Owner.CorePos, Vector2.UnitY, VDDirector.DmgRedRay, VDDirector.RedHellRayDuration, VDDirector.RedHellRayLength);
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

        /// <summary>从红恶魔位置(AnchorPos)向玩家射一排全息三叉戟:角度 = -arc/2 + arc * (i + offset) / divisions</summary>
        private static void FireTridentRow(VDStateContext ctx, int count, float arcDeg, int divisions, float offset) {
            VDVfx.Sound("VoidAttack", 1.2f, ctx.AnchorPos, 4, 0.9f);
            Vector2 dir = (ctx.Target.Center - ctx.AnchorPos).SafeNormalize(Vector2.UnitX);
            float arc = MathHelper.ToRadians(arcDeg);
            for (int i = 0; i < count; i++) {
                float ang = -arc / 2f + arc * (i + offset) / Math.Max(1, divisions);
                Shoot<VDHoloTrident>(ctx, ctx.AnchorPos, dir.RotatedBy(ang) * VDDirector.RedHellTridentSpeed, VDDirector.DmgTrident);
            }
        }
    }
}
