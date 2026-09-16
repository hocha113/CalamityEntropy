using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.PRT;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 1 号 冲刺(原注释「冲刺」),220 帧。
    /// <para>
    /// 节拍:倒计时 220 / 160 / 100 各起手一次。起手当帧锁向玩家 12 帧后的预测点、
    /// 沿反方向弹开(前两次 6,第三次 16),并把推进窗写进 <c>ai[1]</c>(前两次 46 帧,第三次 80 帧)。
    /// 推进窗内每帧沿锁定朝向加速;窗口耗尽转硬刹。倒计时 10 时闪走收尾。
    /// </para>
    /// <para>
    /// 公平阀:起手的反向弹开本身就是预告,锁向之后不再追瞄;第三次重击途中才会沿两侧撒符文弹。
    /// 二阶段起手额外附一把扇形洪流。
    /// </para>
    /// <para>接触伤害只在本招与 11 号招打开(<c>CanHitPlayer</c> 读 <c>ai[3]</c>)</para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.Dash, typeof(ProphetStateContext))]
    public class ProphetDashState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.Dash;

        protected override void RunAttack(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            float difficult = ctx.Difficult;
            int phase = ctx.Phase;
            int cd = ctx.Countdown;

            if (cd == ProphetDirector.DashBeatA || cd == ProphetDirector.DashBeatB || cd == ProphetDirector.DashBeatC)
            {
                bool heavy = cd == ProphetDirector.DashBeatC;
                //锁向 + 反向弹开 + 写推进窗:运动,各端都跑
                npc.rotation = (PredictTarget(ctx, ProphetDirector.DashLeadFrames) - npc.Center).ToRotation();
                npc.velocity += npc.rotation.ToRotationVector2()
                    * -(heavy ? ProphetDirector.DashBackstepHeavy : ProphetDirector.DashBackstepNormal);
                npc.ai[1] = heavy ? ProphetDirector.DashThrustFramesHeavy : ProphetDirector.DashThrustFramesNormal;

                if (phase > 1)
                {
                    CrystalCue(npc);

                    int damage = ProjDamage(ctx);
                    Vector2 aim = (target.Center - npc.Center).normalize();
                    int layers = heavy ? ProphetDirector.DashVolleyLayersHeavy : ProphetDirector.DashVolleyLayersNormal;
                    for (int i = 0; i <= layers; i++)
                    {
                        if (i == 0)
                        {
                            Shoot<RuneTorrent>(ctx, npc.Center, aim * difficult * ProphetDirector.DashVolleySpeedMult,
                                damage, 4, ProphetDirector.VolleyTorrentMaxSpeed * difficult, ProphetDirector.VolleyTorrentAi1);
                        }
                        else
                        {
                            Shoot<RuneTorrent>(ctx, npc.Center,
                                aim.RotatedBy(i * ProphetDirector.DashVolleySpread) * difficult * ProphetDirector.DashVolleySpeedMult,
                                damage, 4, ProphetDirector.VolleyTorrentMaxSpeed * difficult, ProphetDirector.VolleyTorrentAi1);
                            Shoot<RuneTorrent>(ctx, npc.Center,
                                aim.RotatedBy(i * -ProphetDirector.DashVolleySpread) * difficult * ProphetDirector.DashVolleySpeedMult,
                                damage, 4, ProphetDirector.VolleyTorrentMaxSpeed * difficult, ProphetDirector.VolleyTorrentAi1);
                        }
                    }
                }

                //ProminenceTrail 单例挂 trail,Lifetime<1 才重建,冲刺段 13 帧续命。
                //PRTLoader 在服务端只是不入列,仍返回实例,所以这里不额外加 dedServ 守卫(照搬原写法)
                if (ctx.Owner.trail == null || ctx.Owner.trail.Lifetime < 1)
                {
                    ctx.Owner.trail = PRTLoader.NewParticle<PRT_ProminenceTrail>(npc.Center, Vector2.Zero, Color.White,
                        ProphetDirector.TrailSpawnScale);
                    ctx.Owner.trail.color1 = Color.DeepSkyBlue;
                    ctx.Owner.trail.color2 = Color.White;
                    ctx.Owner.trail.maxLength = ProphetDirector.TrailMaxLength;
                    ctx.Owner.trail.Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, -1);
                }

                //锁向那一帧把朝向压进速度,朝向是持久累加量,决策点当场过线
                MarkNetUpdate(ctx);
            }

            if (npc.ai[1] > 0)
            {
                npc.ai[1]--;
                npc.velocity += npc.rotation.ToRotationVector2() * difficult
                    * (cd <= ProphetDirector.DashBeatC ? ProphetDirector.DashThrustHeavyMult : ProphetDirector.DashThrust)
                    * (phase == 1 ? 1 : ProphetDirector.DashThrustPhase2Mult);
                npc.velocity *= ProphetDirector.DashDrag;
                if (ctx.Owner.trail != null)
                {
                    ctx.Owner.trail.Lifetime = ProphetDirector.TrailKeepAlive;
                }
                if (cd < ProphetDirector.DashBeatC)
                {
                    //撒弹周期是整数除法 (int)(10 或 8 / 难度系数),难度越高间隔越短
                    int period = (int)((phase == 1 ? ProphetDirector.DashSideBulletPeriodP1 : ProphetDirector.DashSideBulletPeriodP2) / difficult);
                    if (npc.ai[1] < ProphetDirector.DashSideBulletWindow && cd % period == 0)
                    {
                        CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3), Main.rand.NextFloat(0.7f, 1.3f), npc.Center);
                        int damage = ProjDamage(ctx);
                        Shoot<RuneBulletHostile>(ctx, npc.Center,
                            npc.velocity.RotatedBy(MathHelper.PiOver2) * ProphetDirector.DashSideBulletSpeedFactor * difficult,
                            damage, 2, ProphetDirector.DashSideBulletMaxSpeed * difficult);
                        Shoot<RuneBulletHostile>(ctx, npc.Center,
                            npc.velocity.RotatedBy(-MathHelper.PiOver2) * ProphetDirector.DashSideBulletSpeedFactor * difficult,
                            damage, 2, ProphetDirector.DashSideBulletMaxSpeed * difficult);
                    }
                }
            }
            else
            {
                npc.velocity *= ProphetDirector.DashBrakeDrag;
            }

            if (cd == ProphetDirector.DashExitBeat && IsServer)
            {
                Teleport(ctx, target.Center + CEUtils.randomRot().ToRotationVector2() * ProphetDirector.DashExitRadius);
            }
        }
    }
}
