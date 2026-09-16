using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 追踪导弹:头顶悬停 → 60 帧核心蓄力(汇聚粒子)→ 激光流追射 + 导弹环齐放(P1 三轮 8 发,P2 起四轮 12 发)
    /// → 核弹(P2 起双发左右夹击)。公平阀:核弹出手前 6 帧粒子全断、核心熄灭(静默即预告),出手帧反冲 + 翼张
    /// </summary>
    [VaultState((int)VDStateIndex.HomingMissiles, typeof(VDStateContext))]
    public class VDHomingMissilesState : VDStateBase
    {
        public override string StateName => "HomingMissiles";
        public override VDStateIndex StateIndex => VDStateIndex.HomingMissiles;
        public override bool NeedsRepositionBlink => true;

        private int volleysDone;

        public override void OnEnter(VDStateContext ctx)
        {
            base.OnEnter(ctx);
            volleysDone = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx)
        {
            Timer++;
            int phase = ctx.Phase;
            int volleys = VDDirector.MissileVolleys(phase);
            int interval = VDDirector.MissileVolleyInterval(phase);
            int lastVolley = VDDirector.MissileChargeFrames + interval * (volleys - 1);
            int nukeTime = lastVolley + VDDirector.MissileNukeDelay;
            bool silence = Timer >= nukeTime - VDDirector.MissileNukeSilence && Timer < nukeTime;

            DeclareHoverTo(ctx, ctx.Target.Center + VDDirector.MissileHoverOffset, VDDirector.MissileHoverSpeed, VDDirector.MissileHoverAccel, VDDirector.MissileHoverSlow);
            Vector2 core = ctx.Owner.CorePos;

            if (Timer <= VDDirector.MissileChargeFrames)
            {
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, Timer / (float)VDDirector.MissileChargeFrames);
                if (Timer == 1)
                {
                    VDVfx.Sound("VoidAnticipation", 1.1f, core, 3, 0.9f);
                }
                if (Timer % 3 == 0)
                {
                    ConvergeSparks(ctx, VDVfx.VoidPurple, 80f, 160f, 0.09f);
                }
            }
            else if (Timer <= VDDirector.MissileLaserEnd)
            {
                //激光流:每 4 帧一发,方向逐发重算(抖动只在服务端掷)
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, 1f);
                if (Timer % VDDirector.MissileLaserInterval == 0)
                {
                    Vector2 dir = (ctx.Target.Center - core).SafeNormalize(Vector2.UnitY);
                    if (IsServer)
                    {
                        dir = dir.RotatedBy(Main.rand.NextFloat(-VDDirector.MissileLaserJitter, VDDirector.MissileLaserJitter));
                    }
                    Shoot<VDCoreLaserBolt>(ctx, core, dir * VDDirector.MissileLaserSpeed, VDDirector.DmgCoreLaser);
                    if (Timer % (VDDirector.MissileLaserInterval * 2) == 0)
                    {
                        VDVfx.Sound("void_laser", 1.5f, core, 6, 0.35f);
                    }
                }
            }

            if (Timer >= VDDirector.MissileChargeFrames && (Timer - VDDirector.MissileChargeFrames) % interval == 0 && volleysDone < volleys)
            {
                FireMissileRing(ctx, VDDirector.MissileRingCount(phase));
                volleysDone++;
            }

            if (silence)
            {
                //静默拍:核心熄灭、身体绷紧(绘制层抖动),是核弹前的吸气
                ctx.CoreGlow = 0f;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.4f);
            }
            if (Timer == nukeTime)
            {
                Vector2 dir = (ctx.Target.Center - core).SafeNormalize(Vector2.UnitY);
                MuzzleCue(ctx, dir, 6f, "VoidAttack", 0.8f);
                VDVfx.Shake(ctx.Npc.Center, 5f, 2000f);
                if (phase >= 2)
                {
                    //两发分别向左右射出,靠追踪弧线从两侧夹击
                    Shoot<VDVoidNuke>(ctx, core, new Vector2(-VDDirector.MissileNukeSideSpeed, 0f), VDDirector.DmgNuke, ctx.Npc.target, VDVoidNuke.DefaultRadius);
                    Shoot<VDVoidNuke>(ctx, core, new Vector2(VDDirector.MissileNukeSideSpeed, 0f), VDDirector.DmgNuke, ctx.Npc.target, VDVoidNuke.DefaultRadius);
                }
                else
                {
                    Shoot<VDVoidNuke>(ctx, core, dir * VDDirector.MissileNukeSpeed, VDDirector.DmgNuke, ctx.Npc.target, VDVoidNuke.DefaultRadius);
                }
            }

            if (Timer >= nukeTime + VDDirector.MissileTail)
            {
                return EndAttack(ctx);
            }
            return null;
        }

        private void FireMissileRing(VDStateContext ctx, int count)
        {
            ctx.WingPulse = 1f;
            VDVfx.Sound("CruiserSpit2", 0.9f, ctx.Npc.Center, 4, 0.9f);
            float offset = Timer * 0.1f;
            for (int i = 0; i < count; i++)
            {
                float ang = MathHelper.TwoPi * i / count + offset;
                Vector2 dir = ang.ToRotationVector2();
                Shoot<VDHomingMissile>(ctx, ctx.Npc.Center + dir * VDDirector.MissileRingRadius, dir * VDDirector.MissileRingSpeed, VDDirector.DmgMissile, ctx.Npc.target);
            }
        }
    }
}
