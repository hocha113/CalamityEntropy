using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 坠日,二阶段签名招:双臂高举在头顶蓄出日球,锁定拍把落点写入 LockedPoint 并亮出落点圈,掷出拍日球砸向落点
    /// 日球自己读落点、爆炸、散火羽环,状态只管姿态和拍点
    /// </summary>
    [VaultState((int)SunStateIndex.Sunfall, typeof(SunStateContext))]
    public class SunfallState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.Sunfall;

        public override Vector2 AnchorFor(SunStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SunDirector.SunfallAnchor.X, SunDirector.SunfallAnchor.Y);

        public override void OnEnter(SunStateContext ctx) {
            base.OnEnter(ctx);
            //日球把零向量当作还没锁定
            if (IsServer) {
                ctx.LockedPoint = Vector2.Zero;
            }
        }

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            NPC npc = ctx.Npc;
            int throwBeat = SunDirector.SunfallThrowBeat;

            if (Timer < throwBeat) {
                Hover(ctx, AnchorFor(ctx), 9f, 0.07f, 140f);
                float charge = SunDirector.Progress(Timer, 0, throwBeat);
                int arm = Timer < 6 ? 2 : 4;
                ctx.ArmLeft = arm;
                ctx.ArmRight = arm;
                ctx.HaloGlow = Math.Max(ctx.HaloGlow, 0.5f + 0.5f * charge);
                ctx.WingPose = -charge;
                ctx.BodyGlow = 0.25f * charge;
            }
            else {
                //掷出后双臂挥下,本体被反冲顶起再慢慢停住
                Direct(ctx);
                npc.velocity *= 0.92f;
                int after = Timer - throwBeat;
                int arm = after < 8 ? 3 : after < 16 ? 2 : 0;
                ctx.ArmLeft = arm;
                ctx.ArmRight = arm;
                ctx.WingPose = 0.5f * (1f - SunDirector.Progress(after, 0, 30));
            }

            if (At(SunDirector.SunfallOrbBeat)) {
                Shoot<SunfallOrb>(ctx, npc.Center + new Vector2(0f, -SunDirector.SunfallOrbHeight), Vector2.Zero, SunDirector.DmgSunfall,
                    npc.whoAmI, ctx.Phase >= 3 ? 2 : 1);
                SunVfx.Sound(SoundID.Item165 with { Pitch = -0.2f }, npc.Center);
            }
            if (At(SunDirector.SunfallLockBeat)) {
                if (IsServer) {
                    ctx.LockedPoint = ctx.Target.Center;
                    MarkNetUpdate(ctx);
                }
                ctx.HaloPulse = 0.7f;
                SunVfx.Sound(SoundID.Item162 with { Pitch = 0.3f }, npc.Center);
            }
            if (At(throwBeat)) {
                npc.velocity = new Vector2(0f, -7f);
                ctx.HaloPulse = 1f;
                SunVfx.Shake(npc.Center, 4f, 2000f);
            }
            return Timer >= SunDirector.SunfallDuration ? EndAttack(ctx) : null;
        }
    }
}