using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 金乌齐飞,对应原版棱彩弹:悬在玩家斜上方,朝玩家一侧的手抬起,逐只放出太阳神鸟
    /// 神鸟起飞角围绕正上方摆开,追踪有窗口和转向上限,过窗只直飞,横移就能甩开
    /// </summary>
    [VaultState((int)SunStateIndex.BirdFlock, typeof(SunStateContext))]
    public class SunBirdFlockState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.BirdFlock;

        public override Vector2 AnchorFor(SunStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SunDirector.BirdAnchor.X, SunDirector.BirdAnchor.Y);

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            int interval = SunDirector.BirdInterval(ctx.Phase);
            int fireEnd = SunDirector.BirdWindup + SunDirector.BirdCount(ctx.Phase) * interval;
            //朝玩家一侧的那只手施法
            int hand = -ctx.SideDir;

            Hover(ctx, AnchorFor(ctx), 14f, 0.08f, 140f);
            float charge = SunDirector.Progress(Timer, 0, SunDirector.BirdWindup);
            float cast = Timer < fireEnd ? charge : 0f;
            int armFrame = ArmCast(Timer, 0, fireEnd + 6);
            if (hand < 0) {
                ctx.ArmLeft = armFrame;
                ctx.CastLeft = cast;
            }
            else {
                ctx.ArmRight = armFrame;
                ctx.CastRight = cast;
            }
            ctx.HaloGlow = Math.Max(ctx.HaloGlow, 0.45f + 0.4f * cast);
            ctx.WingPose = -0.5f * cast;

            if (At(0)) {
                SunVfx.Sound(SoundID.Item164, ctx.Npc.Center);
            }
            if (Timer < SunDirector.BirdWindup && Timer % 2 == 0) {
                SunVfx.Converge(Hand(ctx, hand), 40f, 90f);
            }

            int since = Timer - SunDirector.BirdWindup;
            if (since >= 0 && Timer < fireEnd && since % interval == 0) {
                FireBird(ctx, since / interval, hand);
            }
            return Timer >= fireEnd + SunDirector.BirdTail ? EndAttack(ctx) : null;
        }

        private static void FireBird(SunStateContext ctx, int index, int hand) {
            Vector2 pos = Hand(ctx, hand);
            //起飞角不掷骰,按序号在扇面里来回摆,相邻两只错开
            float angle = -MathHelper.PiOver2 + SunDirector.BirdFanHalfAngle * MathF.Sin(index * 2.4f + 0.6f);
            Shoot<SunGoddessBird>(ctx, pos, angle.ToRotationVector2() * SunDirector.BirdLaunchSpeed, SunDirector.DmgBird,
                ctx.Target.whoAmI, SunDirector.BirdCruiseSpeed(ctx.Phase));
            ctx.HaloPulse = Math.Max(ctx.HaloPulse, 0.45f);
            SunVfx.Embers(pos, 4, 1.5f, 4f);
        }
    }
}