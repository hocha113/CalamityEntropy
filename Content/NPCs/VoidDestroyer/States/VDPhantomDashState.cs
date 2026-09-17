using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 幻影冲刺:淡出 → 玩家某角 480px 外开门待机 36 帧(门开即预告,接触档前摇)→ 一帧定速 40 冲 30 帧
    /// → 硬刹 10 帧(冲刺 → 急停 → 消失,不是冲完即闪)→ 回到淡出;P1 三冲、P2 起四冲。
    /// 公平阀:伤害窗 = 速度门槛;P2 起路径上留加速虚空弹
    /// </summary>
    [VaultState((int)VDStateIndex.PhantomDash, typeof(VDStateContext))]
    public class VDPhantomDashState : VDStateBase
    {
        public override string StateName => "PhantomDash";
        public override VDStateIndex StateIndex => VDStateIndex.PhantomDash;
        public override bool ContactByDefault => false;

        private enum Beat { FadeOut, Wait, Dash, Brake, End }
        private Beat beat;
        private int dashes;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.FadeOut;
            dashes = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            DeclareDirect(ctx);
            switch (beat) {
                case Beat.FadeOut:
                    npc.velocity *= 0.5f;
                    DeclareAlpha(ctx, MathHelper.Clamp(1f - Timer / (float)VDDirector.PhantomFadeFrames, 0f, 1f), 1f);
                    if (Timer >= VDDirector.PhantomFadeFrames) {
                        if (IsServer) {
                            //服务端选角开门,客户端从包里拿锚点
                            ctx.CornerIndex = Main.rand.Next(4);
                            ctx.AnchorPos = ctx.Target.Center + VDVfx.CornerDirs[ctx.CornerIndex] * VDDirector.PhantomPortalOffset;
                            Vector2 dir = (ctx.Target.Center - ctx.AnchorPos).SafeNormalize(Vector2.UnitY);
                            SpawnVisual<VDPortal>(ctx, ctx.AnchorPos, dir, VDPortal.ModeDash, VDDirector.PhantomPortalLife);
                            npc.netUpdate = true;
                        }
                        SwitchBeat(Beat.Wait);
                    }
                    break;
                case Beat.Wait:
                    npc.Center = ctx.AnchorPos;
                    npc.velocity = Vector2.Zero;
                    DeclareAlpha(ctx, 0f, 1f);
                    if (Timer >= VDDirector.PhantomWaitFrames) {
                        Vector2 dir = (ctx.Target.Center - ctx.AnchorPos).SafeNormalize(Vector2.UnitY);
                        npc.velocity = dir * VDDirector.PhantomDashSpeed;
                        DeclareAlpha(ctx, 1f, 1f);
                        ctx.ContactWindow = true;
                        ctx.WingPulse = 1f;
                        VDVfx.Sound("CruiserDash", 1f, npc.Center, 3);
                        VDVfx.Shake(npc.Center, 4f, 1600f);
                        SwitchBeat(Beat.Dash);
                        MarkNetUpdate(ctx);
                    }
                    break;
                case Beat.Dash:
                    DeclareAlpha(ctx, 1f, 1f);
                    ctx.ContactWindow = npc.velocity.Length() > VDDirector.PhantomContactSpeed;
                    if (ctx.Phase >= 2 && Timer % VDDirector.PhantomTrailInterval == 3) {
                        Shoot<VDVoidBolt>(ctx, npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY) * VDDirector.PhantomTrailSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeDashTrail);
                    }
                    if (Timer >= VDDirector.PhantomDashFrames) {
                        dashes++;
                        SwitchBeat(Beat.Brake);
                        MarkNetUpdate(ctx);
                    }
                    break;
                case Beat.Brake:
                    //硬刹:×0.7/帧,伤害窗随速度关掉,刹车火花
                    DeclareAlpha(ctx, 1f, 1f);
                    npc.velocity *= 0.7f;
                    ctx.ContactWindow = npc.velocity.Length() > VDDirector.PhantomContactSpeed;
                    if (Timer == 1) {
                        VDVfx.SparkBurst(npc.Center, VDVfx.VoidPurple, 12, 3f, 9f, 20, 0.5f, 1f);
                    }
                    if (Timer >= VDDirector.PhantomBrakeFrames) {
                        SwitchBeat(dashes >= VDDirector.PhantomDashes(ctx.Phase) ? Beat.End : Beat.FadeOut);
                    }
                    break;
                default:
                    npc.velocity *= 0.85f;
                    DeclareAlpha(ctx, MathHelper.Clamp(1f - Timer / (float)VDDirector.PhantomEndFade, 0f, 1f), 1f);
                    if (Timer >= VDDirector.PhantomEndFade) {
                        npc.velocity *= 0.5f;
                        return EndAttack(ctx);
                    }
                    break;
            }
            return null;
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
