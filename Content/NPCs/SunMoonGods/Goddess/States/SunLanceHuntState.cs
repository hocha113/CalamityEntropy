using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 日矛·追猎,对应原版以太长枪:长枪逐支出现在玩家周围,出生即锁向并画出航线,预告走完沿线射出
    /// 偶数枪瞄玩家当前位置,奇数枪瞄预判点,站着不动和匀速直跑都会被点到
    /// 方位按固定步进铺开,起始方位在起手拍掷一次
    /// </summary>
    [VaultState((int)SunStateIndex.LanceHunt, typeof(SunStateContext))]
    public class SunLanceHuntState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.LanceHunt;

        public override Vector2 AnchorFor(SunStateContext ctx) => ctx.Target.Center + SunDirector.LanceAnchor;

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            int interval = SunDirector.LanceInterval(ctx.Phase);
            int count = SunDirector.LanceCount(ctx.Phase);
            int fireEnd = SunDirector.LanceWindup + count * interval;

            Hover(ctx, AnchorFor(ctx), 12f, 0.08f, 140f);
            float cast = Timer < fireEnd ? SunDirector.Progress(Timer, 0, SunDirector.LanceWindup) : 0f;
            int arm = ArmCast(Timer, 0, fireEnd + 6);
            ctx.ArmLeft = arm;
            ctx.ArmRight = arm;
            ctx.CastLeft = cast;
            ctx.CastRight = cast;
            ctx.HaloGlow = Math.Max(ctx.HaloGlow, 0.45f + 0.4f * cast);
            ctx.WingPose = -0.4f * cast;

            if (At(0)) {
                if (IsServer) {
                    ctx.LockedAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                }
                SunVfx.Sound(SoundID.Item162, ctx.Npc.Center);
            }

            int since = Timer - SunDirector.LanceWindup;
            if (since >= 0 && Timer < fireEnd && since % interval == 0) {
                SpawnLance(ctx, since / interval);
            }
            return Timer >= fireEnd + SunDirector.LanceTail ? EndAttack(ctx) : null;
        }

        private static void SpawnLance(SunStateContext ctx, int index) {
            Player target = ctx.Target;
            Vector2 outward = (ctx.LockedAngle + index * SunDirector.LanceAngleStep).ToRotationVector2();
            Vector2 spawn = target.Center + outward * SunDirector.LanceSpawnRadius;
            Vector2 aim = target.Center;
            if (index % 2 == 1) {
                Vector2 lead = target.velocity * SunDirector.LanceTelegraphFrames;
                float length = lead.Length();
                if (length > SunDirector.LanceLeadMax) {
                    lead *= SunDirector.LanceLeadMax / length;
                }
                aim += lead;
            }
            float rotation = (aim - spawn).SafeNormalize(-outward).ToRotation();
            Shoot<SunLance>(ctx, spawn, Vector2.Zero, SunDirector.DmgLance, rotation, SunDirector.LanceTelegraphFrames);
            ctx.HaloPulse = Math.Max(ctx.HaloPulse, 0.3f);
            SunVfx.Sound(SoundID.Item162 with { Volume = 0.5f, PitchVariance = 0.2f, MaxInstances = 4 }, spawn);
        }
    }
}