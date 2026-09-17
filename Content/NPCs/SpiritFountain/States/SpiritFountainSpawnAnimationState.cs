using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.States
{
    /// <summary>
    /// 出场演出。一整段硬时序,三拍:
    /// <list type="number">
    /// <item>首帧落点(档案馆坐标无效就落在目标玩家身上)+ 两发定格闪光</item>
    /// <item>300 帧聚魂:四面八方的归魂粒子往中心收,期间<b>整帧提前收工</b>——
    /// 计时压回 0、脱战判定与眼睛插值全部跳过</item>
    /// <item>显形:眼睛先亮到 0.6,本体才开始浮现;140 帧后喷流减速,200 帧定格收尾并放出一号柱魂环</item>
    /// </list>
    /// <para>公平阀:全程 <c>dontTakeDamage</c>,魂环还没生成,玩家打不到也挨不着。</para>
    /// <para>联机:落点写入各端都跑(读的是各端本地的目标),权威端顺手 netUpdate 让原版位置同步来对账;
    /// 魂环生成只在权威端。<see cref="SpiritFountainStateBase.RequiresTarget"/> 已整族关掉,无目标也照演。</para>
    /// </summary>
    [VaultState((int)SpiritFountainStateIndex.SpawnAnimation, typeof(SpiritFountainStateContext))]
    public class SpiritFountainSpawnAnimationState : SpiritFountainStateBase
    {
        public override SpiritFountainStateIndex StateIndex => SpiritFountainStateIndex.SpawnAnimation;

        /// <summary>出场闪光的本地一次性闸。纯表现,不过线:它的作用就是「本端第一次跑到这里」</summary>
        private bool shineFired;

        public override void OnEnter(SpiritFountainStateContext ctx) {
            base.OnEnter(ctx);
            shineFired = false;
        }

        protected override IVaultState<SpiritFountainStateContext> RunBody(SpiritFountainStateContext ctx) {
            NPC npc = ctx.Npc;
            SpiritFountain owner = ctx.Owner;

            //原代码这一整块在 if (ai == SpawnAnimation) 里,配对的 else 才写 starePoint = 本地玩家,
            //所以演出期间那条 else 不执行
            ctx.StareAtLocalPlayer = false;

            if (!ctx.SetPos) {
                owner.column1.rotation = -MathHelper.PiOver2;
                npc.Opacity = 0;
                ctx.SetPos = true;
                // 禁忌档案坐标写入源已随灾厄 IL 删除,新世界恒为 (-1,-1):下行 pos.X < 10 即安全短路,
                // 落点回退到目标玩家处;仅旧档遗留有效坐标时才用档案馆位置(2026-08-27 核查定稿)
                Vector2 pos = EDownedBosses.GetDungeonArchiveCenterPos();
                npc.Center = pos.X < SpiritFountainDirector.ArchivePosValidX
                    ? (npc.HasValidTarget ? npc.target.ToPlayer().Center : Main.player[0].Center)
                    : pos;
                ctx.StarePoint = npc.Center;
                //落点是决策点:各端各算一次可能差几像素,让权威端立刻发一包由原版位置同步对齐
                MarkNetUpdate(ctx);
            }

            //原代码判的是 GatheringAnimation == 300(那是个纯本地字段,各端第一次跑到这里时必然是 300)。
            //迁移后聚魂倒计时随 ExtraAI 过线并带 ±2 容差收养,拿到快照的客户端会从 300 以下起跑,
            //等值判定被一步跨过 = 客户端永远看不到出场闪光。改成「本状态第一个执行帧」的本地一次性闸:
            //权威端首帧的倒计时就是 300,二者完全等价;客户端则恢复原代码「第一次看见就放」的表现。
            //倒计时已经走完(中途加入到显形段)时不补放,免得凭空多一发 320 帧的大闪光
            if (!shineFired && ctx.GatheringAnimation > 0) {
                shineFired = true;
                if (IsLocal) {
                    //出场首帧双 Shine,lifetime 320 的一次性大粒子
                    PRT_ShineParticle shine1 = PRTLoader.NewParticle<PRT_ShineParticle>(npc.Center, Vector2.Zero, Color.AliceBlue, SpiritFountainDirector.GatheringShineScale1);
                    shine1.flag = true;
                    shine1.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, SpiritFountainDirector.GatheringShineLife);
                    PRT_ShineParticle shine2 = PRTLoader.NewParticle<PRT_ShineParticle>(npc.Center, Vector2.Zero, Color.White, SpiritFountainDirector.GatheringShineScale2);
                    shine2.flag = true;
                    shine2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, SpiritFountainDirector.GatheringShineLife);
                }
            }

            //自减写在判据里:无论是否还在聚魂期都会减,演出后半段它一路走进负数,照搬
            if (ctx.GatheringAnimation-- > 0) {
                if (ctx.GatheringAnimation > SpiritFountainDirector.GatheringSpiritStopAt && IsLocal) {
                    //归魂概率 0.2~1 随倒计时递减,后半段才密起来
                    float chance = SpiritFountainDirector.GatheringSpiritChanceBase
                        + (1 - (ctx.GatheringAnimation - SpiritFountainDirector.GatheringSpiritChanceOffset) / SpiritFountainDirector.GatheringSpiritChanceSpan);
                    if (Main.rand.NextFloat() < chance) {
                        float rr = CEUtils.randomRot();
                        PRT_HomingSpiritParticle spirit = PRTLoader.NewParticle<PRT_HomingSpiritParticle>(
                            npc.Center + rr.ToRotationVector2() * SpiritFountainDirector.GatheringSpiritRadius,
                            rr.ToRotationVector2().RotatedByRandom(SpiritFountainDirector.GatheringSpiritScatter)
                                .RotatedBy(SpiritFountainDirector.GatheringSpiritSwirl * (Main.rand.NextBool() ? 1 : -1))
                                * Main.rand.NextFloat(SpiritFountainDirector.GatheringSpiritSpeedMin, SpiritFountainDirector.GatheringSpiritSpeedMax),
                            Color.AliceBlue, 1);
                        spirit.TargetPos = npc.Center;
                        spirit.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
                    }
                }
                //原代码在这里 return:计时压回 0,并且把尾声(脱战判定 + 眼睛插值 + 摇摆清零)一起跳过
                Timer = 0;
                ctx.HaltFrame = true;
                return null;
            }

            if (Timer > SpiritFountainDirector.SpawnStareStartFrame) {
                ctx.StarePoint = Vector2.Lerp(ctx.StarePoint, Main.LocalPlayer.Center, SpiritFountainDirector.SpawnStareLerp);
            }
            if (npc.Opacity < 1 && ctx.EyeAlpha >= SpiritFountainDirector.SpawnEyeAlphaGate) {
                npc.Opacity += SpiritFountainDirector.SpawnOpacityStep;
            }
            else {
                if (ctx.EyeAlpha < SpiritFountainDirector.SpawnEyeAlphaGate) {
                    ctx.EyeAlpha += SpiritFountainDirector.SpawnEyeAlphaStep;
                }
            }
            owner.column1.alpha = npc.Opacity * SpiritFountainDirector.SpawnColumnAlphaFactor;
            if (Timer > SpiritFountainDirector.SpawnSlowDownFrame) {
                ctx.FountainSpeed = float.Lerp(ctx.FountainSpeed, SpiritFountainDirector.SpawnFountainSpeedTarget, SpiritFountainDirector.SpawnFountainSpeedLerp);
            }

            IVaultState<SpiritFountainStateContext> next = null;
            if (Timer > SpiritFountainDirector.SpawnEndFrame) {
                npc.Opacity = 1;
                owner.column1.alpha = SpiritFountainDirector.SpawnEndColumnAlpha;
                ctx.EyeAlpha = SpiritFountainDirector.SpawnEndEyeAlpha;
                ctx.FountainSpeed = SpiritFountainDirector.SpawnFountainSpeedTarget;
                next = Advance(ctx, StateIndex);
                if (ctx.SpawnSpirits) {
                    ctx.SpawnSpirits = false;
                    ctx.CenterRing = (int)Math.Ceiling(owner.SpiritCount / 2f);
                    owner.SpawnRingSet(0);
                }
            }
            //原代码这一行在换态语句之后、块结束之前,所以换态那一帧也要走
            ctx.EyeAlphaTarget = ctx.EyeAlpha;
            return next;
        }
    }
}
