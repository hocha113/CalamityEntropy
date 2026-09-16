using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 裂隙斩:本体定在玩家斜上方,朝空间划缝。每道缝先以发丝白线可见 24 帧(预告 = 承诺),再拉开 12 帧(判定窗 + 全屏沿线撕裂),
    /// 猛合时两侧各喷一排垂直虚空弹。P1 两道依次(第二道与第一道垂直);P2 三道米字同时可见、错拍 10 帧依次开口;
    /// P3 以玩家为心的六边形笼(边线只向外喷弹,笼内安全)开口后,再补一刀穿心。
    /// 缝的几何在服务端出手帧定死并随弹幕生成包过线,状态只管节拍与本体姿态
    /// </summary>
    [VaultState((int)VDStateIndex.RiftCut, typeof(VDStateContext))]
    public class VDRiftCutState : VDStateBase
    {
        public override string StateName => "RiftCut";
        public override VDStateIndex StateIndex => VDStateIndex.RiftCut;
        public override bool NeedsRepositionBlink => true;

        private enum Beat { Position, Seams, Tail }
        private Beat beat;
        private int seamsSpawned;
        private int lastSeamAt;

        /// <summary>单道缝从可见到消散的总帧数</summary>
        private static int SeamLife => VDDirector.RiftAimFrames + VDDirector.RiftOpenFrames + VDDirector.RiftCloseFrames;

        public override void OnEnter(VDStateContext ctx)
        {
            base.OnEnter(ctx);
            beat = Beat.Position;
            seamsSpawned = 0;
            lastSeamAt = 0;
            if (IsServer)
            {
                ctx.RolledAngles[0] = Main.rand.NextFloat(MathHelper.Pi);
                ctx.Npc.netUpdate = true;
            }
        }

        public override IVDState OnUpdate(VDStateContext ctx)
        {
            Timer++;
            NPC npc = ctx.Npc;
            Vector2 hover = new Vector2(ctx.SideDir * VDDirector.RiftHoverOffset.X, VDDirector.RiftHoverOffset.Y);
            int phase = ctx.Phase;
            int total = VDDirector.RiftSeams(phase);

            switch (beat)
            {
                case Beat.Position:
                    DeclareHoverTo(ctx, ctx.Target.Center + hover, 22f, 0.12f, 120f);
                    if (Timer >= 20 || (Timer > 6 && npc.Distance(ctx.Target.Center + hover) < 90f))
                    {
                        SwitchBeat(Beat.Seams);
                    }
                    break;
                case Beat.Seams:
                    DeclareHoldRelative(ctx, hover, 0.1f, 0.3f, 30f);
                    UpdateSeams(ctx, phase, total);
                    break;
                default:
                    DeclareHoldRelative(ctx, hover, 0.1f, 0.3f, 30f);
                    if (Timer >= VDDirector.RiftTail)
                    {
                        return EndAttack(ctx);
                    }
                    break;
            }
            return null;
        }

        private void UpdateSeams(VDStateContext ctx, int phase, int total)
        {
            Vector2 core = ctx.Owner.CorePos;
            //划缝拍:核心亮、翼张,一下一下地「挥」
            if (phase <= 1)
            {
                //P1:两道依次,第二道垂直于第一道
                int period = SeamLife + VDDirector.RiftGap;
                if (seamsSpawned < total && Timer == 1 + seamsSpawned * period)
                {
                    float ang = ctx.RolledAngles[0] + seamsSpawned * MathHelper.PiOver2;
                    SpawnSeam(ctx, PredictTarget(ctx, VDDirector.RiftPredictLead), ang, VDDirector.RiftAimFrames, VDDirector.RiftHalfLength, false);
                    seamsSpawned++;
                    lastSeamAt = Timer;
                }
                if (seamsSpawned >= total && Timer >= lastSeamAt + SeamLife)
                {
                    SwitchBeat(Beat.Tail);
                }
                return;
            }
            if (phase == 2)
            {
                //P2:三道米字同时可见,预告帧依次加长 → 错拍开口
                if (Timer == 1)
                {
                    Vector2 center = PredictTarget(ctx, VDDirector.RiftPredictLead);
                    for (int k = 0; k < total; k++)
                    {
                        float ang = ctx.RolledAngles[0] + MathHelper.Pi / 3f * k;
                        SpawnSeam(ctx, center, ang, VDDirector.RiftAimFrames + VDDirector.RiftGap * k, VDDirector.RiftHalfLength, false);
                    }
                    seamsSpawned = total;
                    lastSeamAt = Timer;
                }
                if (Timer >= lastSeamAt + SeamLife + VDDirector.RiftGap * (total - 1))
                {
                    SwitchBeat(Beat.Tail);
                }
                return;
            }
            //P3:六边形笼(边线向外喷)+ 笼开口时补一刀穿心
            if (Timer == 1)
            {
                Vector2 center = PredictTarget(ctx, VDDirector.RiftPredictLead);
                float r = VDDirector.RiftCageRadius;
                //边心到圆心距离 = R·cos30°,边半长 = R/2
                for (int k = 0; k < 6; k++)
                {
                    float vertexAng = ctx.RolledAngles[0] + MathHelper.Pi / 3f * k;
                    Vector2 mid = center + (vertexAng + MathHelper.Pi / 6f).ToRotationVector2() * (r * 0.8660254f);
                    float edgeAng = vertexAng + MathHelper.Pi / 6f + MathHelper.PiOver2;
                    SpawnSeam(ctx, mid, edgeAng, VDDirector.RiftAimFrames, r * 0.5f, true);
                }
                seamsSpawned = 6;
                lastSeamAt = Timer;
            }
            //笼开口那一帧补穿心一刀(它自己再预告 24 帧,开口时笼已合上)
            if (seamsSpawned == 6 && Timer == 1 + VDDirector.RiftAimFrames)
            {
                float ang = ctx.RolledAngles[0] + MathHelper.PiOver4;
                SpawnSeam(ctx, PredictTarget(ctx, VDDirector.RiftPredictLead * 0.5f), ang, VDDirector.RiftAimFrames, VDDirector.RiftHalfLength, false);
                seamsSpawned = 7;
                lastSeamAt = Timer;
            }
            if (seamsSpawned >= 7 && Timer >= lastSeamAt + SeamLife)
            {
                SwitchBeat(Beat.Tail);
            }
        }

        private void SpawnSeam(VDStateContext ctx, Vector2 center, float angle, int aimFrames, float halfLength, bool outwardOnly)
        {
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

        private void SwitchBeat(Beat next)
        {
            beat = next;
            ResetTimer();
        }
    }
}
