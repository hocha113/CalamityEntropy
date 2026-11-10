using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 炎阳天轮,三阶段签名招:本体停住化作日轮,四道长光芒先预告再点亮,匀速转上四秒,期间一圈圈火羽向外扩散
    /// 玩家要顺着转向绕行,同时从火羽圈的空隙里穿过;转向跟侧向走,每次出招换一个方向
    /// </summary>
    [VaultState((int)SunStateIndex.SolarStorm, typeof(SunStateContext))]
    public class SunStormState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.SolarStorm;

        public override Vector2 AnchorFor(SunStateContext ctx) => ctx.Target.Center + SunDirector.StormAnchor;

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            NPC npc = ctx.Npc;
            int windup = SunDirector.StormWindup;
            int activeEnd = windup + SunDirector.StormActiveFrames;
            Vector2 center = npc.Center + SunGoddess.HaloOffset;

            //光芒根部跟着本体,预告亮起后本体不再移动
            if (Timer < SunDirector.StormRayBeat) {
                Hover(ctx, AnchorFor(ctx), 10f, 0.1f, 140f);
            }
            else {
                Direct(ctx);
                npc.velocity *= 0.8f;
            }

            float charge = SunDirector.Progress(Timer, 0, windup);
            bool casting = Timer < activeEnd;
            int arm = casting ? (Timer < 6 ? 2 : 4) : (Timer < activeEnd + 6 ? 2 : 0);
            ctx.ArmLeft = arm;
            ctx.ArmRight = arm;
            ctx.CastLeft = casting ? charge : 0f;
            ctx.CastRight = ctx.CastLeft;
            ctx.HaloGlow = casting ? 0.5f + 0.5f * charge : ctx.HaloGlow;
            ctx.WingPose = casting ? -charge : 0f;
            ctx.BodyGlow = casting ? 0.3f * charge : 0f;
            if (Timer < windup && Timer % 2 == 0) {
                SunVfx.Converge(center, 110f, 260f);
            }

            if (At(SunDirector.StormRayBeat)) {
                if (IsServer) {
                    //四道光芒相隔 90 度,玩家起手在两道正中
                    ctx.LockedAngle = (ctx.Target.Center - center).ToRotation() + MathHelper.Pi / SunDirector.StormRays;
                    MarkNetUpdate(ctx);
                    for (int i = 0; i < SunDirector.StormRays; i++) {
                        float angle = ctx.LockedAngle + MathHelper.TwoPi * i / SunDirector.StormRays;
                        Shoot<SunCoronaRay>(ctx, center, Vector2.Zero, SunDirector.DmgRay, angle, npc.whoAmI, ctx.SideDir * SunCoronaRay.ModeStorm);
                    }
                }
                SunVfx.Sound(SoundID.Item159 with { Pitch = -0.3f }, npc.Center);
            }
            if (At(windup)) {
                ctx.HaloPulse = 1f;
                ctx.ShakeStrength = 0.5f;
                SunVfx.Burst(center, 2.6f);
                SunVfx.Shake(npc.Center, 8f);
                SunVfx.Flash(npc.Center, 0.25f);
                SunVfx.SkyFlash(0.8f);
                SunVfx.Sound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.9f, Pitch = -0.4f }, npc.Center);
            }

            int since = Timer - windup - SunDirector.StormFirstRingDelay;
            if (since >= 0 && Timer < activeEnd - 20 && since % SunDirector.StormRingInterval == 0) {
                FireRing(ctx, center, since / SunDirector.StormRingInterval);
            }
            return Timer >= activeEnd + SunDirector.RayFadeFrames + SunDirector.StormTail ? EndAttack(ctx) : null;
        }

        private static void FireRing(SunStateContext ctx, Vector2 center, int ring) {
            ctx.HaloPulse = Math.Max(ctx.HaloPulse, 0.6f);
            SunVfx.Burst(center, 1.1f);
            SunVfx.Sound(SoundID.Item164 with { Volume = 0.8f, MaxInstances = 3 }, center);
            if (!IsServer) {
                return;
            }
            //相邻两圈错开半格,没有能一直站着的方位
            float baseAngle = ctx.LockedAngle + ring * MathHelper.Pi / SunDirector.StormRingBolts;
            for (int i = 0; i < SunDirector.StormRingBolts; i++) {
                float angle = baseAngle + MathHelper.TwoPi * i / SunDirector.StormRingBolts;
                Shoot<SunFlareBolt>(ctx, center, angle.ToRotationVector2() * SunDirector.StormRingSpeed, SunDirector.DmgFlare, SunDirector.StormRingBoltLife);
            }
        }
    }
}