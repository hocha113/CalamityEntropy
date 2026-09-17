using CalamityEntropy.Content.NPCs.Acropolis.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis.States
{
    /// <summary>
    /// 炮击:原 <c>CannonUpAtk = 200</c>。
    /// 节拍:前 60 帧只把炮口抬到玩家头顶 800 的高抛点(预告),之后 140 帧按 12/enrange 的节拍连发电球,
    /// 散布 ±0.6 弧度、初速 5、弹幕 ai0 = -1(高抛弹标记)。每发都给炮臂一点反冲。
    /// <para>
    /// 公平阀:抬炮的 60 帧就是全部预告;本体的走位不停,所以玩家可以边躲边拉扯。
    /// 炮口节拍 <c>TeslaUpCD</c> 各端都跑;瞄点与开火都是<b>声明</b>,宿主在骨架 Step 之后从转过去的枪口出膛,
    /// 弹幕只在权威端生成,散布的随机数也只在权威端摇
    /// </para>
    /// </summary>
    [VaultState((int)AcropolisStateIndex.CannonBarrage, typeof(AcropolisStateContext))]
    public class AcropolisCannonBarrageState : AcropolisStateBase
    {
        public override AcropolisStateIndex StateIndex => AcropolisStateIndex.CannonBarrage;

        public override void OnEnter(AcropolisStateContext ctx) {
            base.OnEnter(ctx);
            //原代码在骰点那一支里写的冷却,放到进态执行,客户端换态时也会写上同一个值
            ctx.TeslaCD = AcropolisDirector.TeslaCDAfterSpecial;
        }

        public override IVaultState<AcropolisStateContext> OnUpdate(AcropolisStateContext ctx) {
            //先判收招:原代码是 CannonUpAtk-- > 0,归零那一帧炮口已经回到常态瞄准
            if (Timer >= AcropolisDirector.BarrageFrames) {
                return BackToWalk(ctx);
            }

            //瞄点声明,宿主 Step 时落地;开火排队,Step 之后从转过去的枪口出膛(原代码同一帧内先转后打)
            ctx.AimCannon(ctx.Player.Center + new Vector2(0f, AcropolisDirector.BarrageAimRise));

            if (Timer >= AcropolisDirector.BarrageFireStartFrame) {
                ctx.TeslaUpCD -= ctx.Enrange;
                if (ctx.TeslaUpCD <= 0f) {
                    ctx.TeslaUpCD = AcropolisDirector.BarrageInterval;
                    //反冲与音效各端都补:节拍量已过线,所以各端的开火帧一致
                    ctx.QueueCannonShot(AcropolisDirector.BarrageSpread, AcropolisDirector.BarrageSpeed,
                        AcropolisDirector.BarrageProjAi0, AcropolisDirector.BarrageRecoil);
                }
            }
            return null;
        }
    }
}
