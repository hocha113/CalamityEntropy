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

        /// <summary>
        /// 甩出拍锁存(本地,不过线)。
        /// <para>
        /// 原判据 <c>ChangeCounter == 20</c> 这一拍里有一句 <c>npc.velocity *= 0.3f</c>,
        /// 那是各端都要跑的硬刹:漏掉它客户端会带着 80 px/f 继续飞、而权威端已经刹到 24,
        /// 一两帧就撞上纠偏器的 160 px 硬对齐门槛,表现为甩出瞬间的瞬移。
        /// 同一拍还给玩家赋速,那一笔必须在玩家自己的客户端上执行才算数。
        /// ChangeCounter 带 ±2 容差收养,等值判定会被跨过,所以改成「闩锁 + <c>&gt;= 20</c>」。
        /// </para>
        /// <para>
        /// <b>撞墙分支必须预先消费这个闩锁</b>:原代码撞墙时把计数直接跳到 60,靠「<c>== 20</c> 再也不成立」
        /// 把甩出与刀光阵一起跳过。换成区间判定后不显式标记已消费,撞墙那一帧会立刻误放整个刀光阵
        /// </para>
        /// </summary>
        private bool launchDone;

        public override void OnEnter(CruiserStateContext ctx) {
            base.OnEnter(ctx);
            launchDone = false;
        }

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            IVaultState<CruiserStateContext> next = null;

            //这个 == 0 不是一次性拍而是<b>窗口判定</b>(还没咬中),所以收养跨过它是正确行为:
            //客户端被收养成权威端的值,意味着权威端已经咬中/还没咬中,客户端跟着走的正是那一支。
            //它也没有「只在这一帧做一次」的副作用——清场与贴近每帧都重跑
            if (ctx.ChangeCounter == 0) {
                //清场:自家三种散弹一律抹掉,免得拖拽段被自己的弹幕糊满
                List<int> clearTypes = new List<int>
                {
                    ModContent.ProjectileType<VoidStar>(),
                    ModContent.ProjectileType<VoidResidue>(),
                    ModContent.ProjectileType<VoidSpike>()
                };
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (clearTypes.Contains(p.type)) {
                        p.Kill();
                    }
                }
                npc.velocity *= CruiserDirector.BiteApproachDrag;
                npc.velocity += (player.Center - npc.Center).normalize() * CruiserDirector.BiteApproachThrust;
                if (CEUtils.getDistance(npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteGrabOffset,
                        player.Center) < CruiserDirector.BiteGrabRange) {
                    ctx.ChangeCounter++;
                    player.velocity *= 0;
                    player.Center = npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteGrabOffset;
                    MarkNetUpdate(ctx);
                }
            }
            else {
                ctx.ChangeCounter++;
                if (ctx.ChangeCounter < CruiserDirector.BiteDragFrames) {
                    ctx.MouthRot += CruiserDirector.BiteMouthRate;
                    npc.velocity = npc.velocity.normalize()
                        * (npc.velocity.Length()
                            + (CruiserDirector.BiteDragSpeedTarget - npc.velocity.Length()) * CruiserDirector.BiteDragSpeedLerp);

                    if (CEUtils.getDistance(npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteGrabOffset,
                            player.Center) < CruiserDirector.BiteGrabRange) {
                        player.velocity *= 0;
                        player.Entropy().immune = CruiserDirector.BiteImmuneFrames;
                        player.Center = npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteGrabOffset;
                    }
                    if (!CEUtils.isAir(npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteWallProbe)) {
                        ctx.ChangeCounter = CruiserDirector.BiteWallSkipTo;
                        //撞墙不甩出:预先把甩出拍标记为已消费,复现原代码「跳到 60 后 == 20 不再成立」
                        launchDone = true;
                    }
                }
                else {
                    Vector2 targetPos = player.Center
                        + (npc.Center - player.Center).normalize().RotatedBy(CruiserDirector.BiteOrbitAngle) * CruiserDirector.BiteOrbitRadius;
                    npc.velocity += (targetPos - npc.Center).normalize() * CruiserDirector.BiteOrbitThrust;
                    npc.velocity *= CruiserDirector.BiteOrbitDrag;
                    if (ctx.ChangeCounter > CruiserDirector.BiteDuration) {
                        next = NextAttack(ctx);
                    }
                }
                //中途加入已经越过甩出拍很久:静默记账,不补演出(基类 CuePassed 的标准用法)
                if (!launchDone && CuePassed(ctx.ChangeCounter, CruiserDirector.BiteDragFrames)) {
                    launchDone = true;
                }
                //原代码把这一段写在 if/else 之外,所以计数正好等于 20 那一帧,绕飞与甩出会同帧执行
                if (!launchDone && ctx.ChangeCounter >= CruiserDirector.BiteDragFrames) {
                    launchDone = true;
                    if (CEUtils.getDistance(npc.Center + npc.rotation.ToRotationVector2() * CruiserDirector.BiteLaunchProbe,
                            player.Center) < CruiserDirector.BiteGrabRange) {
                        player.velocity = npc.velocity * CruiserDirector.BiteLaunchSpeedMult;
                        player.Entropy().CruiserAntiGravTime = CruiserDirector.BiteAntiGravFrames;
                    }

                    if (IsServer) {
                        int slashType = ModContent.ProjectileType<CruiserSlash>();
                        for (int i = 1; i < CruiserDirector.BiteSlashRows; i++) {
                            for (int j = -(CruiserDirector.BiteSlashColumns - 1); j < CruiserDirector.BiteSlashColumns; j++) {
                                if (j == 0) {
                                    Shoot(ctx, slashType,
                                        npc.Center + npc.velocity.normalize() * CruiserDirector.BiteSlashSpacing * i,
                                        npc.velocity);
                                }
                                else {
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
