using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 红巨星:蓝恒星膨胀成半径 24 格的红巨星,2.5 秒后每秒向全场放一次爆发冲击,共 8 次
    /// 四个紫色五角星图腾在半径 50 格的轨道上顺时针慢转,站在图腾里不受爆发冲击伤害;第 5 次起每次伴 6 颗橙色小辉星
    /// 公平阀:图腾从第一帧就亮着,首次爆发前有 2.5 秒赶过去;图腾每帧走 4 像素,远慢于玩家;每次爆发前 30 帧恒星发红蓄力
    /// </summary>
    [VaultState((int)VSlimeStateIndex.RedGiant, typeof(VSlimeStateContext))]
    public class VSlimeRedGiantState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.RedGiant;

        private const int LastShock = VSlimeDirector.GiantFirstShock + (VSlimeDirector.GiantShocks - 1) * VSlimeDirector.GiantShockInterval;
        private const int ChargeLead = 30;

        public override int TimeoutFrames => LastShock + VSlimeDirector.GiantTail + 120;
        public override bool RequiresTarget => false;

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            float swell = VSlimeDirector.Smooth(VSlimeDirector.Progress(Timer, 0, VSlimeDirector.GiantSwellFrames));
            float radius = MathHelper.Lerp(VSlimeDirector.MainStarRadius, VSlimeDirector.GiantRadius, swell);
            ctx.Shape = VSlimeShape.Star;
            ctx.StarRadius = radius;
            ctx.StarHeat = swell;
            ctx.HazardRadius = radius * 0.94f;
            ctx.ContactBase = VSlimeDirector.StarContact;
            ctx.HitboxTarget = new Vector2(radius * 1.4f);
            ctx.TotemStrength = VSlimeDirector.Progress(Timer, 0, 40) * (1f - VSlimeDirector.Progress(Timer, LastShock + 40, 40));
            ctx.TotemAngle = MathHelper.PiOver4 + VSlimeDirector.TotemAngularSpeed * Timer;

            for (int k = 0; k < VSlimeDirector.GiantShocks; k++) {
                int beat = VSlimeDirector.GiantFirstShock + k * VSlimeDirector.GiantShockInterval;
                if (Timer >= beat - ChargeLead && Timer < beat) {
                    float p = VSlimeDirector.Progress(Timer, beat - ChargeLead, ChargeLead);
                    ctx.ChargeGlow = p;
                    ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.4f * p);
                }
                if (At(beat)) {
                    Shock(ctx, k);
                }
            }
            if (At(1)) {
                VSlimeVfx.Cue("blackholeCharge", npc.Center, 0.7f, 0.9f, 2);
            }
            return Timer >= LastShock + VSlimeDirector.GiantTail ? new VSlimeSupernovaState() : null;
        }

        private static void Shock(VSlimeStateContext ctx, int index) {
            NPC npc = ctx.Npc;
            ctx.Pulse = 1f;
            VSlimeVfx.Burst(npc.Center, 4f, VSlimeVfx.GiantRed, VSlimeVfx.HoleEmber);
            VSlimeVfx.Shake(npc.Center, 10f);
            VSlimeVfx.SkyFlash(0.6f);
            VSlimeVfx.Cue("shockBlast", npc.Center, 0.8f, 1f, 2);
            if (!IsServer) {
                return;
            }
            Shoot<VSlimeShockwave>(ctx, npc.Center, Vector2.Zero, VSlimeDirector.DmgBurst, VSlimeShockwave.ModeGiant, npc.whoAmI);
            if (index < VSlimeDirector.GiantStarletFrom) {
                return;
            }
            //相邻两次错半格
            int count = VSlimeDirector.GiantStarlets;
            float baseAngle = index * MathHelper.Pi / count;
            for (int i = 0; i < count; i++) {
                Vector2 dir = (baseAngle + MathHelper.TwoPi * i / count).ToRotationVector2();
                Shoot<VSlimeStarlet>(ctx, npc.Center + dir * (VSlimeDirector.GiantRadius - 20f), dir * VSlimeDirector.GiantStarletSpeed, VSlimeDirector.DmgStarlet,
                    VSlimeStarlet.ModeStraight, VSlimeDirector.GiantStarletLife, VSlimeStarlet.ColorOrange);
            }
        }
    }
}
