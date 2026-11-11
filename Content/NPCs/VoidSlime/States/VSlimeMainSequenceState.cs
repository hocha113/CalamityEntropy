using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 主序星:本体停在圈心,塑出半径 20 格的蓝恒星,随后十轮放射激光,每轮 24 道,白色预警线亮 1 秒后点亮
    /// 收尾向四周散出 24 和 20 颗两圈慢速蓝色小辉星,然后进红巨星
    /// 公平阀:相邻两道激光夹角 15 度,半径 600 处弧距约 157 像素;预警线就是激光的位置;相邻两轮错半格并慢慢漂移
    /// </summary>
    [VaultState((int)VSlimeStateIndex.MainSequence, typeof(VSlimeStateContext))]
    public class VSlimeMainSequenceState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.MainSequence;

        private const int VolleyStart = VSlimeDirector.MainFormFrames + 10;
        private const int RingBeat = VolleyStart + VSlimeDirector.MainVolleys * VSlimeDirector.MainVolleyInterval;

        public override int TimeoutFrames => RingBeat + VSlimeDirector.MainTail + 120;
        public override bool RequiresTarget => false;

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            float form = VSlimeDirector.Progress(Timer, 0, VSlimeDirector.MainFormFrames);
            float radius = VSlimeDirector.MainStarRadius * VSlimeDirector.EaseOut(form);
            ctx.Shape = VSlimeShape.Star;
            ctx.StarRadius = radius;
            ctx.StarHeat = 0f;
            ctx.HitboxTarget = new Vector2(Math.Max(VSlimeDirector.BodySize.X, radius * 1.4f));
            //恒星长到三成大才有判定,成形期间预警圈标出最终大小
            if (form >= 0.3f) {
                ctx.HazardRadius = radius * 0.94f;
                ctx.ContactBase = VSlimeDirector.StarContact;
            }
            else {
                ctx.NoContact = true;
            }
            if (form < 1f) {
                ctx.WarnRadius = VSlimeDirector.MainStarRadius;
                ctx.WarnStrength = 1f - form;
            }

            if (At(1)) {
                ctx.Pulse = 1f;
                VSlimeVfx.Cue("VortexSpawn", npc.Center, 0.7f, 1f, 2);
            }
            for (int v = 0; v < VSlimeDirector.MainVolleys; v++) {
                if (At(VolleyStart + v * VSlimeDirector.MainVolleyInterval)) {
                    ctx.Pulse = Math.Max(ctx.Pulse, 0.4f);
                    Shoot<VSlimeStarLaser>(ctx, npc.Center, Vector2.Zero, VSlimeDirector.DmgStarLaser,
                        VSlimeDirector.MainVolleyAngle(v), npc.whoAmI, VSlimeDirector.StarLaserCount);
                }
            }
            if (At(RingBeat)) {
                ctx.Pulse = 1f;
                VSlimeVfx.StarBurst(npc.Center, 4f);
                VSlimeVfx.Shake(npc.Center, 8f);
                VSlimeVfx.SkyFlash(0.7f);
                VSlimeVfx.Cue("pulseBlast", npc.Center, 1.2f, 0.9f, 2);
                FireRing(ctx, VSlimeDirector.MainRingA, VSlimeDirector.MainRingSpeedA, 0f);
                FireRing(ctx, VSlimeDirector.MainRingB, VSlimeDirector.MainRingSpeedB, 0.5f);
            }
            return Timer >= RingBeat + VSlimeDirector.MainTail ? new VSlimeRedGiantState() : null;
        }

        /// <summary>从恒星表面等角散出一圈,phase 是错开的格数</summary>
        private static void FireRing(VSlimeStateContext ctx, int count, float speed, float phase) {
            NPC npc = ctx.Npc;
            for (int i = 0; i < count; i++) {
                Vector2 dir = (MathHelper.TwoPi * (i + phase) / count).ToRotationVector2();
                Shoot<VSlimeStarlet>(ctx, npc.Center + dir * (VSlimeDirector.MainStarRadius - 20f), dir * speed, VSlimeDirector.DmgStarlet,
                    VSlimeStarlet.ModeStraight, VSlimeDirector.MainRingLife, VSlimeStarlet.ColorBlue);
            }
        }
    }
}
