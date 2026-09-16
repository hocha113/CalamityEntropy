using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.PRT;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 8 号 大激光(原注释「大激光」),560 帧,是全场最长也是唯一带承伤加成的一手。
    /// <para>
    /// 节拍:倒计时 556 清场并定位(有禁忌档案馆坐标就落到档案馆,否则离玩家太远就拉回玩家附近);
    /// 556~481 是拖人段,把 2400 以内、离炮口 560 以外的玩家一路拽向炮口正上方 80;
    /// 480 开炮放出眼球 —— 若此刻本体四周 250 见方有实心块,本招作废,倒计时直接压到 30;
    /// 479~61 持续回满范围内玩家的翅膀时间,让人能在炮下走位。
    /// </para>
    /// <para>
    /// <b>本招期间承伤 ×0.5</b>(<c>ModifyIncomingHit</c> 读 <c>ai[3]</c>),同时基础减伤从 0.12 抬到 0.50。
    /// 拖人对玩家的位移写在各端,原代码如此:实际生效的只有玩家自己那一端,其余端的写入会被玩家位置包盖掉
    /// </para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.GrandLaser, typeof(ProphetStateContext))]
    public class ProphetGrandLaserState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.GrandLaser;

        protected override void RunAttack(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            int cd = ctx.Countdown;

            npc.velocity *= ProphetDirector.LaserDrag;
            npc.rotation = (target.Center - npc.Center).ToRotation();

            if (cd == ProphetDirector.LaserSetupBeat)
            {
                //清场:把场上残留的符文晶体全部打掉。各端都跑(照搬原写法)
                int type = ModContent.ProjectileType<RuneCrystalTop>();
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.type == type)
                    {
                        p.Kill();
                    }
                }
                npc.velocity *= 0;

                if (IsServer)
                {
                    // 禁忌档案坐标写入源已随灾厄 IL 删除,新世界恒为 (-1,-1);
                    // 无效坐标跳过档案馆传送(安全短路,防落到世界界外),仅旧档遗留有效值时保留原演出;
                    // 下方距离检查兜底把 Boss 拉回玩家附近
                    if (EDownedBosses.ForbiddenArchiveCenter.X >= 0)
                    {
                        Teleport(ctx, EDownedBosses.GetDungeonArchiveCenterPos() + new Vector2(0, ProphetDirector.LaserArchiveOffsetY));
                    }
                    if (CEUtils.getDistance(npc.Center, target.Center) > ProphetDirector.LaserFallbackDistance)
                    {
                        Teleport(ctx, target.Center - target.velocity.SafeNormalize(-Vector2.UnitY) * ProphetDirector.LaserFallbackRadius);
                    }
                }
            }

            Vector2 focus = npc.Center + new Vector2(0, ProphetDirector.LaserFocusOffsetY);

            if (cd > ProphetDirector.LaserPullUntil)
            {
                npc.velocity *= ProphetDirector.LaserPullDrag;
                foreach (Player plr in Main.ActivePlayers)
                {
                    if (plr.Distance(npc.Center) >= ProphetDirector.LaserAffectRadius)
                    {
                        continue;
                    }
                    if (plr.Distance(focus) <= ProphetDirector.LaserPullRadius)
                    {
                        continue;
                    }
                    plr.Entropy().immune = ProphetDirector.LaserPullImmune;
                    plr.wingTime = plr.wingTimeMax;
                    plr.velocity = (focus - plr.Center).normalize() * ProphetDirector.LaserPullSpeed;
                    plr.Center += (focus - plr.Center).normalize() * ProphetDirector.LaserPullStep;
                    //拖人相位 8 颗 RuneParticle/玩家,Additive 40 帧,纯 VFX 不是攻击判定
                    for (int i = 0; i < ProphetDirector.LaserPullParticles; i++)
                    {
                        PRTLoader.NewParticle<PRT_RuneParticle>(CEUtils.randomPoint(plr.getRect()), Vector2.Zero, Color.LightBlue, 1)
                            .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 40);
                    }
                }
            }

            if (cd > ProphetDirector.LaserWingWindowLow && cd < ProphetDirector.LaserWingWindowHigh)
            {
                foreach (Player plr in Main.ActivePlayers)
                {
                    if (plr.Distance(npc.Center) < ProphetDirector.LaserAffectRadius)
                    {
                        // 原灾厄无限飞行位删除,这里每帧回满翅膀时间即等效(player-api)
                        plr.wingTime = plr.wingTimeMax;
                    }
                }
            }

            if (cd == ProphetDirector.LaserFireBeat)
            {
                //地形检查读的是已同步的图格,各端同算;压缩倒计时是决策,只在权威端广播
                if (CEUtils.CheckSolidTile(npc.Center.getRectCentered(ProphetDirector.LaserBlockedCheckSize, ProphetDirector.LaserBlockedCheckSize)))
                {
                    ctx.Countdown = ProphetDirector.LaserAbortCountdown;
                    MarkNetUpdate(ctx);
                }
                else
                {
                    Shoot<FableEye>(ctx, focus, (target.Center - focus).normalize() * ProphetDirector.LaserEyeSpeed,
                        npc.damage / ProphetDirector.EyeDamageDivisor, 4);
                }
            }
        }
    }
}
