using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 3 号 双层符文洪流(原注释「2层符文洪流」),100 帧。
    /// <para>
    /// 节拍:倒计时 88 闪到玩家侧方(带起手音),77 一次性铺满两层扇面:
    /// 整数层用较快的 0.8 倍速,半整数层插在缝里用 0.5 倍速,于是玩家看到的是一张交错的网。
    /// </para>
    /// <para>
    /// <b>本招不写任何速度</b>:除瞬移把速度清零外,先知全程保持进招时的惯性。原代码如此,照搬
    /// </para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.RuneTorrentFan, typeof(ProphetStateContext))]
    public class ProphetRuneTorrentFanState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.RuneTorrentFan;

        protected override void RunAttack(ProphetStateContext ctx) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            float difficult = ctx.Difficult;
            int phase = ctx.Phase;

            npc.rotation = (target.Center - npc.Center).ToRotation();

            if (ctx.Countdown == ProphetDirector.TorrentFanBlinkBeat) {
                CrystalCue(npc);
                if (IsServer) {
                    Teleport(ctx, target.Center + target.velocity.SafeNormalize(CEUtils.randomRot().ToRotationVector2())
                        * ProphetDirector.TorrentFanBlinkRadius / difficult);
                }
            }

            if (ctx.Countdown == ProphetDirector.TorrentFanFireBeat) {
                int damage = ProjDamage(ctx);
                Vector2 aim = (target.Center - npc.Center).normalize();
                float spread = phase == 1 ? ProphetDirector.TorrentFanSpreadP1 : ProphetDirector.TorrentFanSpreadP2;
                int layers = phase == 1 ? ProphetDirector.TorrentFanLayersP1 : ProphetDirector.TorrentFanLayersP2;
                float halfLayers = phase == 1 ? ProphetDirector.TorrentFanHalfLayersP1 : ProphetDirector.TorrentFanHalfLayersP2;

                //外层:判定是 <=,所以含正中那一发共 layers + 1 层。正中那一发的 ai0 是 6,两侧是 5
                for (int i = 0; i <= layers; i++) {
                    if (i == 0) {
                        Shoot<RuneTorrent>(ctx, npc.Center, aim * difficult * ProphetDirector.TorrentFanSpeedOuter,
                            damage, 4, ProphetDirector.TorrentFanMaxSpeedCenter * difficult, ProphetDirector.TorrentFanAi1);
                    }
                    else {
                        Shoot<RuneTorrent>(ctx, npc.Center,
                            aim.RotatedBy(i * spread) * difficult * ProphetDirector.TorrentFanSpeedOuter,
                            damage, 4, ProphetDirector.TorrentFanMaxSpeedSide * difficult, ProphetDirector.TorrentFanAi1);
                        Shoot<RuneTorrent>(ctx, npc.Center,
                            aim.RotatedBy(i * -spread) * difficult * ProphetDirector.TorrentFanSpeedOuter,
                            damage, 4, ProphetDirector.TorrentFanMaxSpeedSide * difficult, ProphetDirector.TorrentFanAi1);
                    }
                }

                //内插层:半整数层号,更慢,填在外层的缝里
                for (float i = 0.5f; i <= halfLayers; i++) {
                    Shoot<RuneTorrent>(ctx, npc.Center,
                        aim.RotatedBy(i * spread) * difficult * ProphetDirector.TorrentFanSpeedInner,
                        damage, 4, ProphetDirector.TorrentFanMaxSpeedSide * difficult, ProphetDirector.TorrentFanAi1);
                    Shoot<RuneTorrent>(ctx, npc.Center,
                        aim.RotatedBy(i * -spread) * difficult * ProphetDirector.TorrentFanSpeedInner,
                        damage, 4, ProphetDirector.TorrentFanMaxSpeedSide * difficult, ProphetDirector.TorrentFanAi1);
                }
            }
        }
    }
}
