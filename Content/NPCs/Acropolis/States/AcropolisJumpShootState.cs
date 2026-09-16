using CalamityEntropy.Content.NPCs.Acropolis.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis.States
{
    /// <summary>
    /// 跳射:原 <c>Jumping = true</c> + <c>JumpAndShoot = 200</c>。
    /// 朝玩家方向横向 12、纵向 -24×scale 起跳,滞空期间把炮口压向本体正下方 220,
    /// 按 23/enrange 的节拍倾泻电球(散布 ±0.03、初速 3、ai0 = 1 对地弹标记)。
    /// <para>
    /// 起跳那一刻把炮口节拍写成 30,所以第一发比常规慢,给玩家一个起跳到开火的读秒窗。
    /// 收招条件在宿主的落地判定里:跳射计数降到 150 以下(起跳后 50 帧)才允许落地,
    /// 之后跳跃冷却降到 20 以下、或踩到实心/平台且下坠超过 8 就收。
    /// 落地后本状态还会多跑一帧并打出最后一发,与原代码一致
    /// (原代码先跑炮口块再把跳射计数清成 -1)。
    /// </para>
    /// </summary>
    [VaultState((int)AcropolisStateIndex.JumpShoot, typeof(AcropolisStateContext))]
    public class AcropolisJumpShootState : AcropolisStateBase
    {
        public override AcropolisStateIndex StateIndex => AcropolisStateIndex.JumpShoot;

        public override void OnEnter(AcropolisStateContext ctx)
        {
            base.OnEnter(ctx);
            NPC npc = ctx.Npc;
            ctx.TeslaCD = AcropolisDirector.TeslaCDAfterSpecial;
            ctx.Airborne = true;
            ctx.Owner.JumpCD = AcropolisDirector.JumpShootJumpCD;
            ctx.JumpAndShoot = AcropolisDirector.JumpShootFrames;
            ctx.TeslaUpCD = AcropolisDirector.JumpShootTeslaUpInit;
            if (ctx.Target != null)
            {
                //起跳冲量各端都写:横向的 /scale*scale 在原式里互相抵消,这里保持原写法不化简
                npc.velocity = new Vector2(
                    AcropolisDirector.JumpLaunchSpeedX * Math.Sign(ctx.Target.Center.X - npc.Center.X) / npc.scale,
                    AcropolisDirector.JumpLaunchSpeedY) * npc.scale;
            }
        }

        public override IVaultState<AcropolisStateContext> OnUpdate(AcropolisStateContext ctx)
        {
            NPC npc = ctx.Npc;
            AcropolisHand cannon = ctx.Cannon;

            //对齐原代码 `JumpAndShoot-- > 0`:自减无条件发生,取自减前的值判分支
            int before = ctx.JumpAndShoot;
            ctx.JumpAndShoot = before - 1;
            if (before > 0)
            {
                ctx.CannonAim = npc.Center + cannon.offset * npc.scale + new Vector2(0f, AcropolisDirector.JumpShootAimDrop);
                ctx.CannonAimTimes = AcropolisDirector.JumpShootAimTimes;
                ctx.TeslaUpCD -= ctx.Enrange;
                if (ctx.TeslaUpCD <= 0f)
                {
                    ctx.TeslaUpCD = AcropolisDirector.JumpShootInterval;
                    CEUtils.PlaySound("ofshoot", 1, cannon.TopPos);
                    if (IsServer)
                    {
                        Shoot<AcropolisTeslaBall>(ctx, cannon.TopPos,
                            cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(AcropolisDirector.JumpShootSpread) * AcropolisDirector.JumpShootSpeed,
                            1f, AcropolisDirector.JumpShootProjAi0, npc.whoAmI);
                    }
                }
            }

            //落地由宿主的落地判定清掉腾空标记,本状态下一帧才收招——与原代码的执行顺序一致
            if (!ctx.Airborne)
            {
                return BackToWalk(ctx);
            }
            return null;
        }
    }
}
