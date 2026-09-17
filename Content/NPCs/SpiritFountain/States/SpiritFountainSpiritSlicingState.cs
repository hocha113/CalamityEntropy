using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.States
{
    /// <summary>
    /// 十字斩。双柱一横一竖同时扫过全场,每 132 帧(90 + 42)在第 90 帧翻一次向,
    /// 端点距离 ±1400,正负由权威端开局骰一次。本体这一段可以被直接打。
    /// <para>
    /// <b>终局循环态,不再退出。</b>跑满 800 帧后,在周期开头 40 帧的窗口里把计时<b>原地归零</b>重开——
    /// 原代码写的是「切到自己」,但状态没变、只是 <c>aiTimer = 0</c>,
    /// 所以这里不走换态:计时本身随快照过线,原地归零是两端都能由计时纯推导出来的量,
    /// 换态反而会写出一个和旧值相同的 ai[3],客户端根本察觉不到。
    /// </para>
    /// <para>联机:两柱的行程端点是骰出来的持久量,权威端骰、随 ExtraAI 过线;翻向由计时推导,各端同算。</para>
    /// </summary>
    [VaultState((int)SpiritFountainStateIndex.SpiritSlicing, typeof(SpiritFountainStateContext))]
    public class SpiritFountainSpiritSlicingState : SpiritFountainStateBase
    {
        /// <summary>终局循环态,不设超时:它本来就该一直跑下去</summary>
        public override int TimeoutFrames => int.MaxValue;

        public override SpiritFountainStateIndex StateIndex => SpiritFountainStateIndex.SpiritSlicing;

        protected override IVaultState<SpiritFountainStateContext> RunBody(SpiritFountainStateContext ctx) {
            NPC npc = ctx.Npc;
            SpiritFountain owner = ctx.Owner;

            npc.dontTakeDamage = false;
            ctx.EyeAlphaTarget = 1;
            int t = SpiritFountainDirector.SlicingPeriod;

            //原代码把两柱的朝向写在 aiTimer == 1 那一拍里。计时随快照过线且带 ±2 容差收养,
            //客户端可能一步跨过第 1 帧,而 SyncNPC 本身不带 NPC.rotation、这两个量在本状态里
            //又没有第二处写入口,跨过去就会拿着上一招的朝向画满整个 801 帧周期。
            //这两行写的是常量,从第 1 帧起每帧重写一遍与只写一次完全等价(本状态里没有别的写入口),
            //所以改成区间判定,彻底取消「被跨过」这个可能
            if (Timer >= SpiritFountainDirector.SlicingInitFrame) {
                owner.column1.rotation = -MathHelper.PiOver2;
                owner.column2.rotation = 0;
            }
            //骰点仍然钉死在恰好第 1 帧:权威端的 Timer 从不被收养
            //(ReceiveExtraAI 只在客户端跑,服务端会丢弃 MessageID.SyncNPC),
            //严格 +1 单调递增,所以这一拍在权威端不可能被跳过、也不可能重放
            if (Timer == SpiritFountainDirector.SlicingInitFrame && IsServer) {
                //两柱各自的起始方向:只在权威端骰,结果随 ExtraAI 的 Num 过线
                owner.column1.Num = SpiritFountainDirector.SlicingReach * (Main.rand.NextBool() ? 1 : -1);
                owner.column2.Num = SpiritFountainDirector.SlicingReach * (Main.rand.NextBool() ? 1 : -1);
                MarkNetUpdate(ctx);
            }
            owner.column1.offset.X = float.Lerp(owner.column1.offset.X, owner.column1.Num, SpiritFountainDirector.SlicingOffsetLerp);
            owner.column2.offset.Y = float.Lerp(owner.column2.offset.Y, owner.column2.Num, SpiritFountainDirector.SlicingOffsetLerp);
            if (Timer % t == SpiritFountainDirector.SlicingFlipPhase) {
                owner.column1.Num *= -1;
                owner.column2.Num *= -1;
                //翻向是决策点。权威端这一拍是精确的;客户端万一因收养跨过或重放了它,
                //靠这一包把 Num 直接对回来(Num 在 ExtraAI 里是无容差直取)
                MarkNetUpdate(ctx);
            }

            if (Timer > SpiritFountainDirector.SlicingLoopAfter && Timer % t < SpiritFountainDirector.SlicingLoopWindow) {
                //原代码在这里写了 ai = SpiritSlicing(状态没变)+ aiTimer = 0,等价于原地重开。
                //窗口宽 40 帧,±2 的收养容差跨不过去;发包让客户端的计时跟着一起重开,
                //免得在途的旧快照把刚归零的计时又拽回 800 上下
                Timer = 0;
                MarkNetUpdate(ctx);
            }
            return null;
        }
    }
}
