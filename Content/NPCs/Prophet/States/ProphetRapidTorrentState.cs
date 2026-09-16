using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 4 号 速射符文洪流(原注释「速射符文洪流」),150 帧。
    /// <para>
    /// 节拍:倒计时 149 闪到玩家侧方并朝玩家推 1;
    /// 119~100 是慢段,每 6 帧一发大散布弹;99~81 是快段,每 2 帧一发小散布弹(音效每 4 帧)。
    /// <b>150~120 这 31 帧什么都不做</b>,是原代码 <c>&gt; 80</c> / <c>&lt; 100</c> / <c>&lt; 120</c>
    /// 三层嵌套留下的空窗,照搬保留。
    /// </para>
    /// <para>散布角是权威端掷骰,已在弹幕生成守卫之内</para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.RapidTorrent, typeof(ProphetStateContext))]
    public class ProphetRapidTorrentState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.RapidTorrent;

        protected override void RunAttack(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            float difficult = ctx.Difficult;
            int phase = ctx.Phase;
            int cd = ctx.Countdown;

            npc.rotation = (target.Center - npc.Center).ToRotation();

            if (cd == ProphetDirector.RapidBlinkBeat)
            {
                if (IsServer)
                {
                    Teleport(ctx, target.Center + target.velocity.SafeNormalize(CEUtils.randomRot().ToRotationVector2())
                        * ProphetDirector.RapidBlinkRadius / difficult);
                }
                //起跑速度是运动,各端都写
                npc.velocity = (target.Center - npc.Center).normalize() * ProphetDirector.RapidLaunchSpeed;
            }

            if (cd <= ProphetDirector.RapidWindowLow)
            {
                return;
            }

            int damage = ProjDamage(ctx);
            if (cd < ProphetDirector.RapidFastBelow)
            {
                if (cd % ProphetDirector.RapidFastSoundPeriod == 0)
                {
                    CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3), Main.rand.NextFloat(0.7f, 1.3f), npc.Center);
                }
                if (IsServer && cd % ProphetDirector.RapidFastPeriod == 0)
                {
                    float scatter = phase == 1 ? ProphetDirector.RapidFastScatterP1 : ProphetDirector.RapidFastScatterP2;
                    Shoot<RuneTorrent>(ctx, npc.Center,
                        (target.Center - npc.Center).normalize().RotatedByRandom(scatter) * difficult * ProphetDirector.RapidFastSpeed,
                        damage, 4, ProphetDirector.RapidMaxSpeed * difficult);
                }
            }
            else if (cd < ProphetDirector.RapidSlowBelow)
            {
                if (cd % ProphetDirector.RapidSlowPeriod == 0)
                {
                    CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3), Main.rand.NextFloat(0.7f, 1.3f), npc.Center);
                    if (IsServer)
                    {
                        float scatter = phase == 1 ? ProphetDirector.RapidSlowScatterP1 : ProphetDirector.RapidSlowScatterP2;
                        Shoot<RuneTorrent>(ctx, npc.Center,
                            (target.Center - npc.Center).normalize().RotatedByRandom(scatter) * difficult * ProphetDirector.RapidSlowSpeed,
                            damage, 4, ProphetDirector.RapidMaxSpeed * difficult);
                    }
                }
            }
        }
    }
}
