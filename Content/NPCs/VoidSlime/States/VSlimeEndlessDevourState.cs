using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 无尽之噬,四段:
    /// 一 引力场停住并缩到 60 格,本体化作虫洞沿五角星的五条边极速穿行,航线在起跑前整体亮出
    /// 二 闪回圈心张开半径 12 格的红色大黑洞,十五层蓝色小辉星从圈沿以六臂逆时针旋臂的形状被吸入
    /// 三 吸完第十层后每秒向外放一圈 20 颗慢速凝胶球
    /// 四 变回史莱姆蓄力 2 秒,12 点和 6 点两道蓝色射线顺时针扫一整圈
    /// 公平阀:旋臂间隔 60 度,半径 600 处弧距约 628 像素,顺着旋向绕行即可;凝胶圈相邻两颗夹角 18 度;射线转速有 30 帧爬升,靠近圈心跟着转就躲得开
    /// </summary>
    [VaultState((int)VSlimeStateIndex.EndlessDevour, typeof(VSlimeStateContext))]
    public class VSlimeEndlessDevourState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.EndlessDevour;

        private const int EdgesStart = VSlimeDirector.DevourWindup;
        private const int EdgesEnd = EdgesStart + 5 * VSlimeDirector.DevourEdgeFrames;
        private const int SpiralStart = EdgesEnd + VSlimeDirector.DevourHoleOpenFrames;
        /// <summary>第十层被吞掉的拍点,凝胶圈从这里开始</summary>
        private static readonly int GelStart = SpiralStart + VSlimeDirector.DevourAbsorbBeat(9);
        /// <summary>最后一层被吞掉且最后一圈凝胶放出后黑洞收起</summary>
        private static readonly int HoleEnd = Math.Max(
            SpiralStart + VSlimeDirector.DevourAbsorbBeat(VSlimeDirector.DevourLayers - 1) + 10,
            GelStart + (VSlimeDirector.DevourGelRings - 1) * VSlimeDirector.DevourGelInterval + 20);
        private static readonly int ChargeStart = HoleEnd + VSlimeDirector.DevourReformFrames;
        private static readonly int BeamEnd = ChargeStart + VSlimeDirector.DevourBeamCharge + VSlimeDirector.DevourBeamFrames;

        public override int TimeoutFrames => BeamEnd + VSlimeDirector.DevourTail + 180;

        public override Vector2 AnchorFor(VSlimeStateContext ctx) => VSlimeDirector.PentagramVertex(ctx.ArenaCenter, 0);

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.ArenaFollow = false;
            if (Timer < BeamEnd) {
                ctx.ArenaRadiusTarget = VSlimeDirector.ArenaRadiusDevour;
            }

            if (Timer < EdgesStart) {
                Windup(ctx);
            }
            else if (Timer < EdgesEnd) {
                TraceStar(ctx);
            }
            else if (Timer < SpiralStart) {
                OpenHole(ctx);
            }
            else if (Timer < HoleEnd) {
                Devour(ctx);
            }
            else if (Timer < ChargeStart) {
                float p = VSlimeDirector.Progress(Timer, HoleEnd, VSlimeDirector.DevourReformFrames);
                ctx.Shape = p > 0.6f ? VSlimeShape.Slime : VSlimeShape.BlackHole;
                ctx.HoleRadius = VSlimeDirector.DevourHoleRadius * (1f - VSlimeDirector.EaseIn(p));
                ctx.NoContact = true;
                ctx.PentagramDrawn = 5f;
                ctx.PentagramStrength = 1f;
            }
            else if (Timer < BeamEnd) {
                Beams(ctx);
            }
            else {
                float p = VSlimeDirector.Progress(Timer, BeamEnd, VSlimeDirector.DevourTail);
                ctx.PentagramDrawn = 5f;
                ctx.PentagramStrength = 1f - p;
            }

            if (At(HoleEnd)) {
                ctx.Pulse = 0.6f;
                VSlimeVfx.Cue("blackholeEnd", npc.Center, 1f, 0.9f, 2);
            }
            return Timer >= BeamEnd + VSlimeDirector.DevourTail ? EndAttack(ctx) : null;
        }

        private void Windup(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            float p = VSlimeDirector.Progress(Timer, 0, EdgesStart);
            Hover(ctx, VSlimeDirector.PentagramVertex(ctx.ArenaCenter, 0), 18f, 0.14f, 120f);
            ctx.Shape = VSlimeShape.Wormhole;
            ctx.NoContact = true;
            ctx.RimGlow = p;
            //航线整体亮出,还没有一条边被跑过
            ctx.PentagramDrawn = 0f;
            ctx.PentagramStrength = 0.25f + 0.5f * p;
            if (At(1)) {
                VSlimeVfx.Cue("VortexSpawn", npc.Center, 0.9f, 0.8f, 2);
            }
        }

        /// <summary>位置按边号和边内进度解析,速度取下一点减当前点</summary>
        private void TraceStar(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int t = Timer - EdgesStart;
            int edge = t / VSlimeDirector.DevourEdgeFrames;
            float u = (t % VSlimeDirector.DevourEdgeFrames + 1f) / VSlimeDirector.DevourEdgeFrames;
            Vector2 from = VSlimeDirector.PentagramVertex(ctx.ArenaCenter, edge);
            Vector2 to = VSlimeDirector.PentagramVertex(ctx.ArenaCenter, edge + 1);
            Direct(ctx);
            npc.velocity = Vector2.Lerp(from, to, u) - npc.Center;
            ctx.Shape = VSlimeShape.Wormhole;
            ctx.RimGlow = 1f;
            ctx.PentagramDrawn = edge + u;
            ctx.PentagramStrength = 1f;
            ctx.LaneStart = npc.Center;
            ctx.LaneEnd = to;
            ctx.LaneWidth = VSlimeDirector.DevourLaneWidth;
            ctx.LaneStrength = 0.6f;
            if (t % VSlimeDirector.DevourEdgeFrames == 0) {
                ctx.Pulse = 0.5f;
                VSlimeVfx.Cue("Dash2", npc.Center, 1.2f, 0.7f, 3);
                VSlimeVfx.Shake(npc.Center, 4f, 2600f);
            }
        }

        private void OpenHole(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int local = Timer - EdgesEnd;
            npc.velocity = Vector2.Zero;
            Direct(ctx);
            ctx.NoContact = true;
            ctx.PentagramDrawn = 5f;
            ctx.PentagramStrength = 1f;
            if (At(EdgesEnd) && IsServer) {
                ctx.Owner.StartBlink(ctx.ArenaCenter);
            }
            int grow = VSlimeDirector.DevourHoleOpenFrames - VSlimeDirector.BlinkDuration;
            float p = VSlimeDirector.Progress(local, VSlimeDirector.BlinkDuration, grow);
            ctx.Shape = local >= VSlimeDirector.BlinkDuration / 2 ? VSlimeShape.BlackHole : VSlimeShape.Wormhole;
            ctx.HoleRadius = VSlimeDirector.DevourHoleRadius * VSlimeDirector.EaseOut(p);
            ctx.WarnRadius = VSlimeDirector.DevourHoleRadius;
            ctx.WarnStrength = local >= VSlimeDirector.BlinkDuration / 2 ? 0.4f + 0.6f * p : 0f;
            if (At(EdgesEnd + VSlimeDirector.BlinkDuration)) {
                VSlimeVfx.Cue("BlackholeSpawn", npc.Center, 0.8f, 1f, 2);
            }
        }

        private void Devour(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int local = Timer - SpiralStart;
            npc.velocity = Vector2.Zero;
            Direct(ctx);
            ctx.Shape = VSlimeShape.BlackHole;
            ctx.HoleRadius = VSlimeDirector.DevourHoleRadius;
            ctx.HazardRadius = VSlimeDirector.DevourHoleRadius;
            ctx.ContactBase = VSlimeDirector.BlackHoleContact;
            ctx.HitboxTarget = new Vector2(VSlimeDirector.DevourHoleRadius * 1.3f);
            ctx.PentagramDrawn = 5f;
            ctx.PentagramStrength = 1f;
            if (!IsServer) {
                return;
            }

            if (local % VSlimeDirector.DevourLayerInterval == 0 && local / VSlimeDirector.DevourLayerInterval < VSlimeDirector.DevourLayers) {
                int layer = local / VSlimeDirector.DevourLayerInterval;
                float baseAngle = Rng(ctx, 2).NextFloat(MathHelper.TwoPi) + layer * VSlimeDirector.DevourLayerTwist;
                float radius = VSlimeDirector.ArenaRadiusDevour - 12f;
                for (int arm = 0; arm < VSlimeDirector.DevourArms; arm++) {
                    Vector2 dir = (baseAngle + MathHelper.TwoPi * arm / VSlimeDirector.DevourArms).ToRotationVector2();
                    Shoot<VSlimeStarlet>(ctx, npc.Center + dir * radius, Vector2.Zero, VSlimeDirector.DmgStarlet, VSlimeStarlet.ModeSpiral, npc.whoAmI, VSlimeStarlet.ColorBlue);
                }
            }

            int gel = Timer - GelStart;
            if (gel >= 0 && gel % VSlimeDirector.DevourGelInterval == 0 && gel / VSlimeDirector.DevourGelInterval < VSlimeDirector.DevourGelRings) {
                int ring = gel / VSlimeDirector.DevourGelInterval;
                int count = VSlimeDirector.DevourGelCount;
                float baseAngle = ring * MathHelper.Pi / count;
                for (int i = 0; i < count; i++) {
                    Vector2 dir = (baseAngle + MathHelper.TwoPi * i / count).ToRotationVector2();
                    Shoot<VSlimeDarkGel>(ctx, npc.Center + dir * (VSlimeDirector.DevourHoleRadius * 0.9f), dir * VSlimeDirector.DevourGelSpeed, VSlimeDirector.DmgDarkGel,
                        VSlimeDarkGel.ModeStraight, VSlimeDirector.DevourGelLife);
                }
            }
        }

        private void Beams(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            npc.velocity = Vector2.Zero;
            Direct(ctx);
            ctx.PentagramDrawn = 5f;
            ctx.PentagramStrength = 1f;
            if (At(ChargeStart)) {
                //射线弹幕自带 2 秒细线预告和扫向提示
                Shoot<VSlimeGammaRay>(ctx, npc.Center, Vector2.Zero, VSlimeDirector.DmgBlueRay, VSlimeGammaRay.ModePulsar, npc.whoAmI, -MathHelper.PiOver2);
                Shoot<VSlimeGammaRay>(ctx, npc.Center, Vector2.Zero, VSlimeDirector.DmgBlueRay, VSlimeGammaRay.ModePulsar, npc.whoAmI, MathHelper.PiOver2);
                VSlimeVfx.Cue("lasercharge", npc.Center, 0.8f, 0.8f, 2);
            }
            int fire = ChargeStart + VSlimeDirector.DevourBeamCharge;
            if (Timer < fire) {
                float p = VSlimeDirector.Progress(Timer, ChargeStart, VSlimeDirector.DevourBeamCharge);
                ctx.ChargeGlow = p;
                ctx.RimGlow = p;
                ctx.Squash = 0.2f * p;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f * p);
                if (Timer % 2 == 0) {
                    VSlimeVfx.Converge(npc.Center, 120f, 320f, DustID.Shadowflame);
                }
            }
            else {
                ctx.RimGlow = 1f;
                ctx.BodyGlow = 0.3f;
            }
            if (At(fire)) {
                ctx.Pulse = 1f;
                VSlimeVfx.SkyFlash(0.8f);
            }
        }
    }
}
