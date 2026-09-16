using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using InnoVault.PRT;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 环爆:160 → 121 贴到玩家附近(<c>280 / enrange</c> 处),
    /// 开火窗 110 → 80 之间每 10 帧炸出一轮星弹。
    /// <para>
    /// 每轮从一个随机起始角开始,每 80 度取一个方向(0/80/160/240/320,共 5 个),
    /// 每个方向出两发、重力方向分别是该方向的法线 ±90°,所以一轮 10 发
    /// </para>
    /// </summary>
    [VaultState((int)LuminarisStateIndex.Shoot360, typeof(LuminarisStateContext))]
    public class LuminarisShoot360State : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.Shoot360;

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;
            int c = ctx.Countdown;

            npc.velocity *= 0;
            npc.rotation = 0;

            if (c == LuminarisDirector.Shoot360Frames) {
                ctx.Vec1 = npc.Center;
                ctx.Vec2 = player.Center + (npc.Center - player.Center).normalize() * LuminarisDirector.Shoot360ApproachDistance / enrange;
            }
            if (c > LuminarisDirector.Shoot360ApproachEndFrame) {
                float p = Utils.Remap(c, LuminarisDirector.Shoot360Frames, LuminarisDirector.Shoot360ApproachEndFrame, 0, 1);
                npc.Center = Vector2.Lerp(ctx.Vec1, ctx.Vec2, CEUtils.GetRepeatedCosFromZeroToOne(p, 1));
            }
            if (c <= LuminarisDirector.Shoot360ShootStartFrame && c >= LuminarisDirector.Shoot360ShootEndFrame) {
                if (c % LuminarisDirector.Shoot360ShootInterval == 0) {
                    CEUtils.PlaySound("portal_emerge", 1, npc.Center);
                    if (!Main.dedServ) {
                        PRTLoader.NewParticle<PRT_SparkleCal>(npc.Center, Vector2.Zero, Color.White,
                            LuminarisDirector.Shoot360ImpactScale * LuminarisDirector.Shoot360ImpactOuterMult).Configure(Color.SkyBlue, 12, 0, 4.5f);
                        PRTLoader.NewParticle<PRT_SparkleCal>(npc.Center, Vector2.Zero, Color.White,
                            LuminarisDirector.Shoot360ImpactScale).Configure(Color.SkyBlue, 10, 0, 3f);
                    }
                    if (IsServer) {
                        //起始角吃随机数,而它只喂弹幕(位置与速度都不读它),所以整段收在权威端、不必过线
                        float ag = CEUtils.randomRot();
                        for (int i = 0; i < 360; i += LuminarisDirector.Shoot360AngleStep) {
                            float a = ag + MathHelper.ToRadians(i);
                            Shoot<LuminarisAstralShoot>(ctx, npc.Center, a.ToRotationVector2() * LuminarisDirector.Shoot360ProjSpeed * enrange,
                                1, a + MathHelper.PiOver2, LuminarisDirector.Shoot360ProjGravity * enrange);
                            Shoot<LuminarisAstralShoot>(ctx, npc.Center, a.ToRotationVector2() * LuminarisDirector.Shoot360ProjSpeed * enrange,
                                1, a - MathHelper.PiOver2, LuminarisDirector.Shoot360ProjGravity * enrange);
                        }
                    }
                }
            }

            return Tick(ctx, c);
        }
    }
}
