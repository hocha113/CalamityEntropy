using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 湮灭主炮(P3 压轴):闪现到玩家上方 600px → 60 帧锁定(暗角压场、能量翼全张、汇聚流在 72% 处硬切成静默、
    /// 震屏 ∝ charge³、导引线在出手前 40 帧给出扫射起点,起点永远在玩家对侧)→ 出手:射线一帧亮起、本体反冲上抬,
    /// 150 帧以恒定角速度扫 100°,沿线落弹雨 → 过热:射线断、本体下坠 60px 冒火花,60 帧可攻击窗(玩家挣来的喘息)。
    /// 扫射速度在 600px 处约 7px/帧,离本体越近越慢:贴近是活路
    /// </summary>
    [VaultState((int)VDStateIndex.AnnihilationCannon, typeof(VDStateContext))]
    public class VDAnnihilationCannonState : VDStateBase
    {
        public override string StateName => "AnnihilationCannon";
        public override VDStateIndex StateIndex => VDStateIndex.AnnihilationCannon;

        private enum Beat { Charge, Fire, Overheat }
        private Beat beat;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Charge;
            if (IsServer) {
                ctx.Owner.StartBlink(ctx.Target.Center + VDDirector.CannonLockOffset);
            }
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            switch (beat) {
                case Beat.Charge:
                    UpdateCharge(ctx, npc);
                    break;
                case Beat.Fire:
                    DeclareHoldRelative(ctx, VDDirector.CannonLockOffset, 0.03f, 0.15f, 10f);
                    ctx.WingPulse = 1f;
                    ctx.CoreGlow = 1f;
                    ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.3f);
                    VDScreenFx.ReportVignette(VDDirector.CannonVignette);
                    //扫射期间天幕力场保持在被花掉的能量读数上
                    VDSkyDrive.ReportCharge(VDDirector.SkyCannonSweepCharge);
                    if (Timer >= VDDirector.CannonSweepFrames) {
                        SwitchBeat(Beat.Overheat);
                        VDVfx.Sound("VoidBomb", 0.6f, npc.Center, 2, 1f);
                        VDVfx.SparkBurst(ctx.Owner.CorePos, VDVfx.VoidPink, 40, 3f, 12f, 40);
                        MarkNetUpdate(ctx);
                    }
                    break;
                default: {
                    //过热下坠:先坠再刹,火花与烟不断,接触窗关
                    DeclareDirect(ctx);
                    ctx.ContactWindow = false;
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.CannonOverheatFrames, 0f, 1f);
                    float drop = Timer < 20 ? 3f : 0f;
                    npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, drop), 0.15f);
                    ctx.CoreGlow = 0f;
                    ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.25f * (1f - p));
                    VDScreenFx.ReportVignette(VDDirector.CannonVignette * (1f - p));
                    VDSkyDrive.ReportCharge(VDDirector.SkyCannonSweepCharge * (1f - p));
                    if (!Main.dedServ && Timer % 3 == 0) {
                        Vector2 pos = ctx.Owner.CorePos + CEUtils.randomPointInCircle(30f);
                        VDVfx.SparkBurst(pos, VDVfx.VoidPink, 2, 2f, 6f, 24, 0.4f, 0.9f);
                        VDVfx.VoidPuff(pos, new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(1f, 3f)), 1.3f, 0.6f);
                    }
                    if (Timer >= VDDirector.CannonOverheatFrames) {
                        return EndAttack(ctx);
                    }
                    break;
                }
            }
            return null;
        }

        private void UpdateCharge(VDStateContext ctx, NPC npc) {
            DeclareHoldRelative(ctx, VDDirector.CannonLockOffset, 0.08f, 0.25f, 24f);
            float progress = MathHelper.Clamp(Timer / (float)VDDirector.CannonChargeFrames, 0f, 1f);
            ctx.WingPulse = Math.Max(ctx.WingPulse, progress);
            ctx.CoreGlow = Math.Max(ctx.CoreGlow, progress);
            ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, progress * progress * progress);
            VDScreenFx.ReportVignette(VDDirector.CannonVignette * progress);
            //天幕:力场随蓄力爬亮、本体周围六边形逐格填实、星野向本体吸入
            VDSkyDrive.ReportCharge(progress);

            if (Timer == 1) {
                VDVfx.Sound("VoidAnticipation", 0.55f, npc.Center, 2, 1.2f);
            }
            //导引线拍:出手前 40 帧掷扫射起点(玩家对侧),之后死向
            int guideStart = VDDirector.CannonChargeFrames - VDDirector.CannonGuideLead;
            if (Timer == guideStart && IsServer) {
                ctx.SideDir = ctx.Target.Center.X >= npc.Center.X ? 1 : -1;
                ctx.RolledAngles[0] = MathHelper.PiOver2 + ctx.SideDir * MathHelper.ToRadians(VDDirector.CannonStartOffsetDeg);
                npc.netUpdate = true;
            }
            if (Timer >= guideStart) {
                float g = (Timer - guideStart) / (float)VDDirector.CannonGuideLead;
                ctx.AimLineDir = ctx.RolledAngles[0].ToRotationVector2();
                ctx.AimLineStrength = MathHelper.Clamp(g, 0f, 1f);
                ctx.AimLineColor = VDVfx.CannonCore;
            }
            //汇聚流:密度随蓄力升,72% 处硬切成静默
            if (progress < VDDirector.CannonSilenceAt) {
                int every = progress < 0.4f ? 3 : 1;
                if (Timer % every == 0) {
                    ConvergeSparks(ctx, VDVfx.VoidPurple, 160f, 420f, 0.07f);
                    ConvergeSparks(ctx, VDVfx.CannonCore, 90f, 200f, 0.1f);
                }
                if (Timer % 6 == 0) {
                    VDVfx.Shake(npc.Center, 1.5f + 4f * progress * progress * progress, 3000f);
                }
            }
            if (Timer >= VDDirector.CannonChargeFrames) {
                Fire(ctx, npc);
                SwitchBeat(Beat.Fire);
                MarkNetUpdate(ctx);
            }
        }

        private void Fire(VDStateContext ctx, NPC npc) {
            float start = ctx.RolledAngles[0];
            float sweepSign = -ctx.SideDir;
            Vector2 dir = start.ToRotationVector2();
            MuzzleCue(ctx, Vector2.UnitY, VDDirector.CannonRecoil, "VoidAttack", 0.5f, 1.2f);
            VDVfx.Shake(npc.Center, 14f, 4000f);
            VDVfx.Explosion(ctx.Owner.CorePos, 1.3f, 20);
            VDVfx.SparkBurst(ctx.Owner.CorePos, VDVfx.CannonCore, 50, 6f, 22f, 40);
            //天幕:出手一帧力场整面亮起,冲击环从本体扩散
            VDSkyDrive.PushFlash(VDDirector.SkyFlashBeat);
            Shoot<VDAnnihilationBeam>(ctx, ctx.Owner.CorePos, dir, VDDirector.DmgAnnihilationBeam, npc.whoAmI, VDDirector.CannonSweepFrames * sweepSign, start);
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
