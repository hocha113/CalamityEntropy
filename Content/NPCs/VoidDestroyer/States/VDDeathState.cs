using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 死亡演出:0-150 逐级加密的爆炸与震屏;150-210 传送门在头顶打开、本体缩入;210-290 门关闭;330 真正死亡并掉落。
    /// 演出不依赖目标;真死走 StrikeInstantKill,联机下由击杀包把死亡带到各客户端
    /// </summary>
    [VaultState((int)VDStateIndex.Death, typeof(VDStateContext))]
    public class VDDeathState : VDStateBase
    {
        public override string StateName => "Death";
        public override VDStateIndex StateIndex => VDStateIndex.Death;
        public override bool ContactByDefault => false;
        public override bool RunsDuringBlink => true;
        public override int TimeoutFrames => int.MaxValue;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            ctx.Dying = true;
            ctx.BlinkTimer = 0;
            ctx.QueuedChainState = -1;
            VDVfx.ClearOwnProjectiles();
            VDVfx.Sound("VoidAnticipation", 0.8f, ctx.Npc.Center, 2, 1.2f);
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            int t = Timer;
            DeclareDirect(ctx);
            npc.velocity *= 0.9f;
            npc.life = Math.Max(1, npc.life);
            ctx.ArenaActive = false;
            ctx.WingsVisible = false;
            ctx.ShieldVisible = false;

            if (t <= VDDirector.DeathExplosionEnd) {
                DeclareAlpha(ctx, 1f, 1f);
                float progress = t / (float)VDDirector.DeathExplosionEnd;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, progress);
                int interval = Math.Max(4, 16 - t / 10);
                //舰壳失控:描边从虚空紫烧成红,每次内部爆炸都让整圈闪一下,越到后面越密
                ctx.RimCharge = progress;
                ctx.RimColorTarget = Color.Lerp(VDVfx.VoidPurple, VDDirector.RimHeatRed, progress);
                if (t % interval == 0) {
                    ctx.RimFlash = 0.6f;
                }
                if (!Main.dedServ && t % interval == 0) {
                    Vector2 pos = npc.Center + CEUtils.randomPointInCircle(70f);
                    VDVfx.Explosion(pos, Main.rand.NextFloat(0.5f, 0.9f), 30);
                    VDVfx.SparkBurst(pos, VDVfx.VoidPurple, 6, 4f, 12f, 30, 1f, 1f);
                    VDVfx.Sound("VoidBomb", Main.rand.NextFloat(0.9f, 1.1f), pos, 4, 0.6f);
                    VDVfx.Shake(npc.Center, 3f + t / 25f);
                }
                if (t == VDDirector.DeathExplosionEnd) {
                    ctx.AnchorPos = npc.Center + new Vector2(0, -110);
                    VDVfx.Sound("portal_emerge", 1f, ctx.AnchorPos, 2);
                    MarkNetUpdate(ctx);
                }
            }
            else if (t <= VDDirector.DeathPortalIn) {
                float p = (t - VDDirector.DeathExplosionEnd) / (float)(VDDirector.DeathPortalIn - VDDirector.DeathExplosionEnd);
                float eased = VDVfx.EaseOut(p);
                DeclareAlpha(ctx, 1f - eased, MathHelper.Lerp(1f, 0.55f, eased));
                npc.velocity = Vector2.Zero;
                npc.Center = Vector2.Lerp(ctx.AnchorPos + new Vector2(0, 110), ctx.AnchorPos, eased);
                //被门吸入时保持满亮的红热缘光,随本体透明度一起消失
                ctx.RimCharge = 1f;
                ctx.RimColorTarget = VDDirector.RimHeatRed;
            }
            else {
                DeclareAlpha(ctx, 0f, 0.55f);
                npc.velocity = Vector2.Zero;
            }

            //传送门:150 开、250 起关、290 关完
            if (t >= VDDirector.DeathExplosionEnd) {
                if (t <= 180) ctx.PortalOpenness = VDVfx.EaseOut((t - 150) / 30f);
                else if (t <= 250) ctx.PortalOpenness = 1f;
                else if (t <= 290) ctx.PortalOpenness = 1f - VDVfx.EaseOut((t - 250) / 40f);
                else ctx.PortalOpenness = 0f;
            }

            if (t >= VDDirector.DeathDuration - 1 && IsServer && !ctx.DeathPerformanceFinished) {
                //与巡游者一致:走 StrikeInstantKill,联机下由 DamageNPC 包把击杀带到各客户端
                ctx.DeathPerformanceFinished = true;
                npc.StrikeInstantKill();
                npc.netSpam = 9;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}
