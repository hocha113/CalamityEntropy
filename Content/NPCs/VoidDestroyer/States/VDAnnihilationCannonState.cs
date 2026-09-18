using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 越肩主炮(P3 压轴):闪现到镜头后方 Z -0.45、玩家头顶 480px(从屏幕上缘压下来的 1.8 倍半透明巨影)→ 75 帧锁定
    /// (暗角压场、能量翼全张、过热描边、汇聚流在 72% 处硬切成静默、震屏 ∝ charge³、导引光锥在出手前 40 帧从巨影收敌到平面枢再指出扫射起点,起点永远在玩家对侧)
    /// → 出手:一条光锥从巨影打进画面、在它平面上的枢(玩家头顶 264px)转成扫射线,150 帧以恒定角速度扫 100°,沿线落弹雨
    /// → 过热:射线断,本体 20 帧俯冲回平面(落地一记),再 40 帧冒火花的可攻击窗(玩家挣来的喘息)。
    /// 扫射几何与旧版一致(绕枢转的整条线),只是炮从镜头后打进来;离枢越近扫得越慢:贴近是活路
    /// </summary>
    [VaultState((int)VDStateIndex.AnnihilationCannon, typeof(VDStateContext))]
    public class VDAnnihilationCannonState : VDStateBase
    {
        public override string StateName => "AnnihilationCannon";
        public override VDStateIndex StateIndex => VDStateIndex.AnnihilationCannon;
        public override float StartDepth(VDStateContext ctx) => VDDirector.CannonNearDepth;
        public override Vector2 AnchorFor(VDStateContext ctx) => DepthAnchor(ctx, VDDirector.CannonNearApparent, VDDirector.CannonNearDepth);

        private enum Beat { Charge, Fire, Overheat }
        private Beat beat;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Charge;
            if (IsServer) {
                ctx.Owner.StartBlink(AnchorFor(ctx));
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
                    ctx.Depth = VDDirector.CannonNearDepth;
                    DeclareHoldRelativeDepth(ctx, VDDirector.CannonNearApparent, VDDirector.CannonNearDepth, 0.03f, 0.15f, 10f);
                    ctx.WingPulse = 1f;
                    ctx.CoreGlow = 1f;
                    //扫射全程描边保持过热白炽(过热风格自带高频闪)
                    ctx.RimCharge = 1f;
                    ctx.RimColorTarget = VDVfx.CannonCore;
                    ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.3f);
                    VDScreenFx.ReportVignette(VDDirector.CannonVignette);
                    //扫射期间天幕力场保持在被花掉的能量读数上
                    VDSkyDrive.ReportCharge(VDDirector.SkyCannonSweepCharge);
                    if (Timer >= VDDirector.CannonSweepFrames) {
                        SwitchBeat(Beat.Overheat);
                        VDVfx.Sound("VoidBomb", 0.6f, ctx.Owner.ProjectedCenter, 2, 1f);
                        VDVfx.SparkBurst(ctx.Owner.ProjectedCorePos, VDVfx.VoidPink, 40, 3f, 12f, 40);
                        MarkNetUpdate(ctx);
                    }
                    break;
                default: {
                    //过热:先从镜头后俯冲回平面(落点就是自己在平面上的位置),再冒火花冷却;全程无接触(俯冲落地那一记除外)
                    DeclareDirect(ctx);
                    ctx.ContactWindow = false;
                    float p = MathHelper.Clamp(Timer / (float)VDDirector.CannonOverheatFrames, 0f, 1f);
                    if (Timer <= VDDirector.CannonOverheatDive + VDDirector.DiveContactFrames) {
                        DeclareDive(ctx, VDDirector.CannonNearDepth, Timer, VDDirector.CannonOverheatDive, npc.Center + new Vector2(0f, VDDirector.CannonOverheatDrop));
                    }
                    else {
                        ctx.Depth = 0f;
                    }
                    float drop = Timer < VDDirector.CannonOverheatDive ? 3f : 0f;
                    npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, drop), 0.15f);
                    ctx.CoreGlow = Timer <= VDDirector.CannonOverheatDive ? ctx.CoreGlow : 0f;
                    //过热的舰壳从烧红慢慢冷回虚空紫,描边随之收干
                    ctx.RimCharge = Math.Max(ctx.RimCharge, 0.6f * (1f - p));
                    ctx.RimColorTarget = Color.Lerp(VDDirector.RimHeatRed, VDVfx.VoidPurple, p);
                    ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.25f * (1f - p));
                    VDScreenFx.ReportVignette(VDDirector.CannonVignette * (1f - p));
                    VDSkyDrive.ReportCharge(VDDirector.SkyCannonSweepCharge * (1f - p));
                    if (!Main.dedServ && Timer % 3 == 0) {
                        Vector2 pos = ctx.Owner.ProjectedCorePos + CEUtils.randomPointInCircle(30f);
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
            ctx.Depth = VDDirector.CannonNearDepth;
            DeclareHoldRelativeDepth(ctx, VDDirector.CannonNearApparent, VDDirector.CannonNearDepth, 0.08f, 0.25f, 24f);
            float progress = MathHelper.Clamp(Timer / (float)VDDirector.CannonChargeFrames, 0f, 1f);
            ctx.WingPulse = Math.Max(ctx.WingPulse, progress);
            ctx.CoreGlow = Math.Max(ctx.CoreGlow, progress);
            //描边随蓄力从虚空粉烧到炮芯色,出手前整圈已是白炽
            ctx.RimCharge = progress;
            ctx.RimColorTarget = Color.Lerp(VDVfx.VoidPink, VDVfx.CannonCore, progress);
            ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, progress * progress * progress);
            VDScreenFx.ReportVignette(VDDirector.CannonVignette * progress);
            //天幕:力场随蓄力爬亮、本体周围六边形逐格填实、星野向本体吸入
            VDSkyDrive.ReportCharge(progress);

            if (Timer == 1) {
                VDVfx.Sound("VoidAnticipation", 0.55f, ctx.Owner.ProjectedCenter, 2, 1.2f);
            }
            //导引线拍:出手前 40 帧掷扫射起点(玩家对侧,相对平面枢),之后死向
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
            //汇聚流:密度随蓄力升,72% 处硬切成静默(汇聚点是巨影的投影核心,半径按近景放大)
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

        /// <summary>出手:光锥从镜头后的巨影打进画面,枢在本体的平面位置;射线弹幕自己每帧钉在枢上、画锥段</summary>
        private void Fire(VDStateContext ctx, NPC npc) {
            float start = ctx.RolledAngles[0];
            float sweepSign = -ctx.SideDir;
            Vector2 dir = start.ToRotationVector2();
            MuzzleCue(ctx, Vector2.UnitY, VDDirector.CannonRecoil, "VoidAttack", 0.5f, 1.2f);
            VDVfx.Shake(npc.Center, 14f, 4000f);
            Vector2 pivot = npc.Center;
            VDVfx.Explosion(pivot, 1.3f, 20);
            VDVfx.SparkBurst(pivot, VDVfx.CannonCore, 50, 6f, 22f, 40);
            //天幕:出手一帧力场整面亮起,冲击环从本体扩散
            VDSkyDrive.PushFlash(VDDirector.SkyFlashBeat);
            Shoot<VDAnnihilationBeam>(ctx, pivot, dir, VDDirector.DmgAnnihilationBeam, npc.whoAmI, VDDirector.CannonSweepFrames * sweepSign, start);
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
