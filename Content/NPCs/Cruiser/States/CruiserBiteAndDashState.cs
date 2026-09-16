using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.StateMachines;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 咬击:清场 → 咬住 → 拖 20 帧 → 甩出并沿航线布下刀光阵 → 绕飞收尾。
    /// <para>
    /// <b>计数停在 0 的那一段是本状态的核心结构</b>:咬中之前 <c>ChangeCounter</c> 完全不推进
    /// (每帧重跑清场与贴近),所以「等咬中」的时长完全由能不能追上玩家决定,原代码<b>没有</b>上限。
    /// 迁移只在状态基类挂了一个 1800 帧的安全网,不改这段判定。
    /// </para>
    /// <para>
    /// 撞墙保护:拖拽期若本体前方 360 处是实心块,计数直接跳到 60,跳过甩出段(刀光阵也就不放),
    /// 直接进绕飞收尾。同样照搬。
    /// </para>
    /// <para>
    /// 对玩家的位置/速度直写(咬住时把玩家钉在嘴前、甩出时给玩家赋速)是原版写法,
    /// 各端都跑:玩家自己的客户端那一次写入才是权威的那一次
    /// </para>
    /// </summary>
    [VaultState((int)CruiserStateIndex.BiteAndDash, typeof(CruiserStateContext))]
    public class CruiserBiteAndDashState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.BiteAndDash;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            IVaultState<CruiserStateContext> next = null;

            if (ctx.ChangeCounter == 0)
            {
                //清场:自家三种散弹一律抹掉,免得拖拽段被自己的弹幕糊满
                List<int> clearTypes = new List<int>
                {
                    ModContent.ProjectileType<VoidStar>(),
                    ModContent.ProjectileType<VoidResidue>(),
                    ModContent.ProjectileType<VoidSpike>()
                };
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (clearTypes.Contains(p.type))
                    {
                        p.Kill();
                    }
                }
                npc.velocity *= CruiserDirector.BiteApproachDrag;
                npc.velocity += (player.Center - npc.Center).normalize() * CruiserDirector.BiteApproachThrust;
                if (CEUtils.getDistance(npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteGrabOffset,
                        player.Center) < CruiserDirector.BiteGrabRange)
                {
                    ctx.ChangeCounter++;
                    player.velocity *= 0;
                    player.Center = npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteGrabOffset;
                    MarkNetUpdate(ctx);
                }
            }
            else
            {
                ctx.ChangeCounter++;
                if (ctx.ChangeCounter < CruiserDirector.BiteDragFrames)
                {
                    ctx.MouthRot += CruiserDirector.BiteMouthRate;
                    npc.velocity = npc.velocity.normalize()
                        * (npc.velocity.Length()
                            + (CruiserDirector.BiteDragSpeedTarget - npc.velocity.Length()) * CruiserDirector.BiteDragSpeedLerp);

                    if (CEUtils.getDistance(npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteGrabOffset,
                            player.Center) < CruiserDirector.BiteGrabRange)
                    {
                        player.velocity *= 0;
                        player.Entropy().immune = CruiserDirector.BiteImmuneFrames;
                        player.Center = npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteGrabOffset;
                    }
                    if (!CEUtils.isAir(npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteWallProbe))
                    {
                        ctx.ChangeCounter = CruiserDirector.BiteWallSkipTo;
                    }
                }
                else
                {
                    Vector2 targetPos = player.Center
                        + (npc.Center - player.Center).normalize().RotatedBy(CruiserDirector.BiteOrbitAngle) * CruiserDirector.BiteOrbitRadius;
                    npc.velocity += (targetPos - npc.Center).normalize() * CruiserDirector.BiteOrbitThrust;
                    npc.velocity *= CruiserDirector.BiteOrbitDrag;
                    if (ctx.ChangeCounter > CruiserDirector.BiteDuration)
                    {
                        next = NextAttack(ctx);
                    }
                }
                //原代码把这一段写在 if/else 之外,所以计数正好等于 20 那一帧,绕飞与甩出会同帧执行
                if (ctx.ChangeCounter == CruiserDirector.BiteDragFrames)
                {
                    if (CEUtils.getDistance(npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteLaunchProbe,
                            player.Center) < CruiserDirector.BiteGrabRange)
                    {
                        player.velocity = npc.velocity * CruiserDirector.BiteLaunchSpeedMult;
                        player.Entropy().CruiserAntiGravTime = CruiserDirector.BiteAntiGravFrames;
                    }

                    if (IsServer)
                    {
                        int slashType = ModContent.ProjectileType<CruiserSlash>();
                        for (int i = 1; i < CruiserDirector.BiteSlashRows; i++)
                        {
                            for (int j = -(CruiserDirector.BiteSlashColumns - 1); j < CruiserDirector.BiteSlashColumns; j++)
                            {
                                if (j == 0)
                                {
                                    Shoot(ctx, slashType,
                                        npc.Center + npc.velocity.normalize() * CruiserDirector.BiteSlashSpacing * i,
                                        npc.velocity);
                                }
                                else
                                {
                                    Shoot(ctx, slashType,
                                        npc.Center + npc.velocity.normalize().RotatedBy(CruiserDirector.BiteSlashAngleStep * j)
                                            * CruiserDirector.BiteSlashSpacing * i,
                                        npc.velocity.RotatedBy(CruiserDirector.BiteSlashAngleStep * j));
                                }
                            }
                        }
                        MarkNetUpdate(ctx);
                    }
                    npc.velocity *= CruiserDirector.BiteSlashSelfDrag;
                }
            }
            return next;
        }
    }
}
