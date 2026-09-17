using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 裂隙斩:本体定在玩家斜上方,朝空间划缝。每道缝出现前本体先抬手 12 帧(翼张 + 核心亮 + 抬手音),
    /// 缝以发丝白线可见 36 帧(预告 = 承诺),再拉开 12 帧(判定窗 + 全屏沿线撕裂),猛合时两侧各喷一排垂直虚空弹。
    /// P1 两道依次(第二道与第一道垂直);P2 三道米字依次画线,前一道开口那一帧下一道才出现(同时只有一道在开口);
    /// P3 以玩家为心的六边形笼(边线只向外喷弹,笼内安全)开口后,再补一刀穿心。
    /// 缝的几何在服务端出手帧定死并随弹幕生成包过线,状态只管节拍与本体姿态
    /// </summary>
    [VaultState((int)VDStateIndex.RiftCut, typeof(VDStateContext))]
    public class VDRiftCutState : VDStateBase
    {
        public override string StateName => "RiftCut";
        public override VDStateIndex StateIndex => VDStateIndex.RiftCut;
        public override Vector2 AnchorFor(VDStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.RiftHoverOffset.X, VDDirector.RiftHoverOffset.Y);

        private enum Beat { Position, Seams, Tail }
        private Beat beat;
        private int seamsSpawned;
        private int lastSeamAt;

        /// <summary>单道缝从可见到消散的总帧数</summary>
        private static int SeamLife => VDDirector.RiftAimFrames + VDDirector.RiftOpenFrames + VDDirector.RiftCloseFrames;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Position;
            seamsSpawned = 0;
            lastSeamAt = 0;
            if (IsServer) {
                ctx.RolledAngles[0] = Main.rand.NextFloat(MathHelper.Pi);
                ctx.Npc.netUpdate = true;
            }
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            Vector2 hover = new Vector2(ctx.SideDir * VDDirector.RiftHoverOffset.X, VDDirector.RiftHoverOffset.Y);
            int phase = ctx.Phase;
            int total = VDDirector.RiftSeams(phase);

            switch (beat) {
                case Beat.Position:
                    DeclareHoverTo(ctx, ctx.Target.Center + hover, 22f, 0.12f, 120f);
                    if (Timer >= 20 || (Timer > 6 && npc.Distance(ctx.Target.Center + hover) < 90f)) {
                        SwitchBeat(Beat.Seams);
                    }
                    break;
                case Beat.Seams:
                    DeclareHoldRelative(ctx, hover, 0.1f, 0.3f, 30f);
                    UpdateSeams(ctx, phase, total);
                    break;
                default:
                    DeclareHoldRelative(ctx, hover, 0.1f, 0.3f, 30f);
                    if (Timer >= VDDirector.RiftTail) {
                        return EndAttack(ctx);
                    }
                    break;
            }
            return null;
        }

        /// <summary>抬手拍:缝出现前 RiftRaiseFrames 帧翼张、核心亮、抬手音,挥砍的前摇</summary>
        private void Raise(VDStateContext ctx, int framesIntoRaise) {
            float p = MathHelper.Clamp(framesIntoRaise / (float)VDDirector.RiftRaiseFrames, 0f, 1f);
            ctx.WingPulse = Math.Max(ctx.WingPulse, p);
            ctx.CoreGlow = Math.Max(ctx.CoreGlow, 0.4f + 0.6f * p);
            if (framesIntoRaise == 1) {
                VDVfx.Sound("VoidAnticipation", 1.4f, ctx.Owner.CorePos, 4, 0.6f);
            }
        }

        private void UpdateSeams(VDStateContext ctx, int phase, int total) {
            int raise = VDDirector.RiftRaiseFrames;
            if (phase <= 1) {
                //P1:两道依次,每道前有 12 帧抬手,第二道垂直于第一道
                int period = raise + SeamLife + VDDirector.RiftGap;
                if (seamsSpawned < total) {
                    int raiseStart = 1 + seamsSpawned * period;
                    if (Timer >= raiseStart && Timer < raiseStart + raise) {
                        Raise(ctx, Timer - raiseStart + 1);
                    }
                    if (Timer == raiseStart + raise) {
                        float ang = ctx.RolledAngles[0] + seamsSpawned * MathHelper.PiOver2;
                        SpawnSeam(ctx, PredictTarget(ctx, VDDirector.RiftPredictLead), ang, VDDirector.RiftAimFrames, VDDirector.RiftHalfLength, false);
                        seamsSpawned++;
                        lastSeamAt = Timer;
                    }
                }
                if (seamsSpawned >= total && Timer >= lastSeamAt + SeamLife) {
                    SwitchBeat(Beat.Tail);
                }
                return;
            }
            if (phase == 2) {
                //P2:三道米字依次出现,前一道开口那一帧下一道才画线(同时只有一道在开口),第一道前有抬手
                if (seamsSpawned < total) {
                    int spawnAt = 1 + raise + seamsSpawned * VDDirector.RiftAimFrames;
                    if (seamsSpawned == 0 && Timer >= 1 && Timer < 1 + raise) {
                        Raise(ctx, Timer);
                    }
                    if (Timer == spawnAt) {
                        if (seamsSpawned == 0) {
                            ctx.RolledPoints[5] = PredictTarget(ctx, VDDirector.RiftPredictLead);
                        }
                        float ang = ctx.RolledAngles[0] + MathHelper.Pi / 3f * seamsSpawned;
                        SpawnSeam(ctx, ctx.RolledPoints[5], ang, VDDirector.RiftAimFrames, VDDirector.RiftHalfLength, false);
                        seamsSpawned++;
                        lastSeamAt = Timer;
                    }
                }
                if (seamsSpawned >= total && Timer >= lastSeamAt + SeamLife) {
                    SwitchBeat(Beat.Tail);
                }
                return;
            }
            //P3:抬手后六边形笼一起画线(边线向外喷、笼内安全),笼开口那一帧补穿心一刀
            if (Timer >= 1 && Timer < 1 + raise) {
                Raise(ctx, Timer);
            }
            if (Timer == 1 + raise) {
                Vector2 center = PredictTarget(ctx, VDDirector.RiftPredictLead);
                float r = VDDirector.RiftCageRadius;
                //边心到圆心距离 = R·cos30°,边半长 = R/2
                for (int k = 0; k < 6; k++) {
                    float vertexAng = ctx.RolledAngles[0] + MathHelper.Pi / 3f * k;
                    Vector2 mid = center + (vertexAng + MathHelper.Pi / 6f).ToRotationVector2() * (r * 0.8660254f);
                    float edgeAng = vertexAng + MathHelper.Pi / 6f + MathHelper.PiOver2;
                    SpawnSeam(ctx, mid, edgeAng, VDDirector.RiftAimFrames, r * 0.5f, true);
                }
                seamsSpawned = 6;
                lastSeamAt = Timer;
            }
            //笼开口那一帧补穿心一刀(它自己再预告 36 帧,开口时笼已合上)
            if (seamsSpawned == 6 && Timer == 1 + raise + VDDirector.RiftAimFrames) {
                float ang = ctx.RolledAngles[0] + MathHelper.PiOver4;
                SpawnSeam(ctx, PredictTarget(ctx, VDDirector.RiftPredictLead * 0.5f), ang, VDDirector.RiftAimFrames, VDDirector.RiftHalfLength, false);
                seamsSpawned = 7;
                lastSeamAt = Timer;
            }
            if (seamsSpawned >= 7 && Timer >= lastSeamAt + SeamLife) {
                SwitchBeat(Beat.Tail);
            }
        }

        private void SpawnSeam(VDStateContext ctx, Vector2 center, float angle, int aimFrames, float halfLength, bool outwardOnly) {
            //本体的挥砍演出:核心亮、翼张、一记短反冲、划空音
            ctx.CoreGlow = 1f;
            ctx.WingPulse = 1f;
            ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.35f);
            Vector2 dir = angle.ToRotationVector2();
            ctx.Npc.velocity -= dir.RotatedBy(MathHelper.PiOver2) * 2f;
            VDVfx.Sound("CruiserSpit2", 1.3f, ctx.Owner.CorePos, 4, 0.8f);
            int boltDamage = ctx.Owner.ProjDamage(VDDirector.DmgVoidBolt) * (outwardOnly ? -1 : 1);
            Shoot<VDRiftSeam>(ctx, center, dir, VDDirector.DmgRiftSeam, aimFrames, halfLength, boltDamage);
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
