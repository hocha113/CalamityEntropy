using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.Core
{
    /// <summary>
    /// 手写轮换表 + 家族标签 + 硬性防复读校验。选招不用随机:
    /// 每阶段一张表,压力招(Dash)与区域/弹幕招交替、同家族不相邻、同招间隔 ≥3;
    /// 表只是「建议序」,真正的兜底是 <see cref="IsLegal"/>:候选命中最近三手或与上一手同家族就沿表往后找,
    /// 替补阀、连段队列头、阶段签名首招全部过同一道闸。阶段切换不清历史,变形前后不可能连出同招
    /// </summary>
    public static class VDRotation
    {
        /// <summary>P1(12):弹幕 / 冲刺 / 区域交替,裂隙斩与舰队从 P1 就有</summary>
        private static readonly VDStateIndex[] Phase1 =
        {
            VDStateIndex.ArcFireball, VDStateIndex.PhantomDash, VDStateIndex.RiftCut, VDStateIndex.HomingMissiles,
            VDStateIndex.PhantomFleet, VDStateIndex.VoidFlame, VDStateIndex.PhaseLaser, VDStateIndex.PhantomDash,
            VDStateIndex.ArcFireball, VDStateIndex.RiftCut, VDStateIndex.VoidFlame, VDStateIndex.PhantomFleet,
        };

        /// <summary>P2(14):首手轨道轰炸是阶段签名(变形收尾强制),全息三模式与奇点/支援穿插;末尾蓝色天空回绕到轨道轰炸,家族不相邻</summary>
        private static readonly VDStateIndex[] Phase2 =
        {
            VDStateIndex.OrbitalStrike, VDStateIndex.PhantomDash, VDStateIndex.RedHell, VDStateIndex.HomingMissiles,
            VDStateIndex.Singularity, VDStateIndex.Reinforcement, VDStateIndex.PhantomFleet, VDStateIndex.GreenJungle,
            VDStateIndex.RiftCut, VDStateIndex.TeleportFire, VDStateIndex.OrbitalStrike, VDStateIndex.PhantomDash,
            VDStateIndex.PhaseLaser, VDStateIndex.BlueSky,
        };

        /// <summary>
        /// P3(16):首手湮灭主炮(护盾收尾强制),主炮一轮两手;连段头(裂隙斩/奇点/轨道轰炸)排在其后手不在最近三手里的位置,
        /// 连段后手也记入历史,所以舰队只在表里出现一次(另两次由裂隙斩连段带出)。连段见 <see cref="ChainFollow"/>
        /// </summary>
        private static readonly VDStateIndex[] Phase3 =
        {
            VDStateIndex.AnnihilationCannon, VDStateIndex.RedHell, VDStateIndex.RiftCut, VDStateIndex.Singularity,
            VDStateIndex.Reinforcement, VDStateIndex.OrbitalStrike, VDStateIndex.GreenJungle, VDStateIndex.TeleportFire,
            VDStateIndex.PhantomFleet, VDStateIndex.AnnihilationCannon, VDStateIndex.PhantomDash, VDStateIndex.BlueSky,
            VDStateIndex.Singularity, VDStateIndex.RiftCut, VDStateIndex.TeleportFire, VDStateIndex.PhaseLaser,
        };

        public static VDStateIndex[] TableFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        public static bool IsAttack(VDStateIndex state) => (int)state >= (int)VDStateIndex.ArcFireball;

        public static VDAttackFamily FamilyOf(VDStateIndex state) {
            switch (state) {
                case VDStateIndex.PhantomDash:
                case VDStateIndex.PhantomFleet:
                    return VDAttackFamily.Dash;
                case VDStateIndex.ArcFireball:
                case VDStateIndex.VoidFlame:
                case VDStateIndex.TeleportFire:
                case VDStateIndex.HomingMissiles:
                    return VDAttackFamily.Barrage;
                case VDStateIndex.PhaseLaser:
                case VDStateIndex.RiftCut:
                case VDStateIndex.OrbitalStrike:
                    return VDAttackFamily.Zone;
                case VDStateIndex.Singularity:
                    return VDAttackFamily.Gravity;
                case VDStateIndex.RedHell:
                case VDStateIndex.GreenJungle:
                case VDStateIndex.BlueSky:
                    return VDAttackFamily.Hologram;
                case VDStateIndex.Reinforcement:
                    return VDAttackFamily.Summon;
                case VDStateIndex.AnnihilationCannon:
                    return VDAttackFamily.Finale;
                default:
                    return VDAttackFamily.None;
            }
        }

        /// <summary>硬性防复读判据:不在最近三手里,且与上一手不同家族</summary>
        public static bool IsLegal(VDStateContext ctx, VDStateIndex candidate) {
            if (!IsAttack(candidate)) {
                return false;
            }
            if (ctx.InHistory(candidate)) {
                return false;
            }
            return FamilyOf(candidate) != ctx.LastFamily;
        }

        /// <summary>由注册表创建状态实例(未注册返回 null,框架已打日志)</summary>
        public static IVDState Create(VDStateIndex state) {
            return VaultStateRegistry<VDStateContext>.Create((int)state) as IVDState;
        }

        /// <summary>P3 连段:头招收招直接接的后手(None = 无)</summary>
        public static VDStateIndex ChainFollow(int phase, VDStateIndex head) {
            if (phase < 3) {
                return VDStateIndex.Hub;
            }
            switch (head) {
                //奇点还在牵引时导弹环从四周扑来:引力把导弹的弧线也拉弯
                case VDStateIndex.Singularity:
                    return VDStateIndex.HomingMissiles;
                //缝刚合上舰队就从缝口的位置开门齐冲
                case VDStateIndex.RiftCut:
                    return VDStateIndex.PhantomFleet;
                //从背景俯冲归位直接接一记幻影冲刺
                case VDStateIndex.OrbitalStrike:
                    return VDStateIndex.PhantomDash;
                default:
                    return VDStateIndex.Hub;
            }
        }

        /// <summary>
        /// 替补阀(等价家族):招式失效时退到本槽位的等价替补,而不是打空。
        /// 舰队在玩家拉远时退成传送逼近的幻影冲刺;支援投送在没有地面或前卫满员时退成裂隙斩
        /// (Summon 没有同家族兄弟,这是唯一一处跨家族替补)
        /// </summary>
        public static VDStateIndex Substitute(VDStateContext ctx, VDStateIndex candidate) {
            switch (candidate) {
                case VDStateIndex.PhantomFleet:
                    if (ctx.TargetValid && Vector2.Distance(ctx.Npc.Center, ctx.Target.Center) > VDDirector.FarDashDistance) {
                        return VDStateIndex.PhantomDash;
                    }
                    return candidate;
                case VDStateIndex.Reinforcement: {
                    bool ground = ctx.TargetValid && VDVfx.HasGroundBelow(ctx.Target.Center);
                    bool room = NPC.CountNPCS(ModContent.NPCType<VoidVanguardCultist>()) < VDDirector.MaxVanguards;
                    return ground && room ? candidate : VDStateIndex.RiftCut;
                }
                default:
                    return candidate;
            }
        }

        /// <summary>
        /// 选招(权威端):签名首招优先;否则沿表从 AttackIndex 起找第一个过阀且合法的招,
        /// 序号同步推进;表内无解(理论上不会)退到家族互异的安全对。返回值已 Commit
        /// </summary>
        public static VDStateIndex Pick(VDStateContext ctx) {
            ctx.QueuedChainState = -1;

            if (ctx.ForcedNextState >= 0) {
                VDStateIndex forced = (VDStateIndex)ctx.ForcedNextState;
                ctx.ForcedNextState = -1;
                if (IsAttack(forced)) {
                    Commit(ctx, forced);
                    return forced;
                }
            }

            VDStateIndex[] table = TableFor(ctx.Phase);
            for (int step = 0; step < VDDirector.RotationSearchSteps; step++) {
                int slot = (ctx.AttackIndex + step) % table.Length;
                VDStateIndex candidate = Substitute(ctx, table[slot]);
                if (!IsLegal(ctx, candidate)) {
                    continue;
                }
                ctx.AttackIndex = (ctx.AttackIndex + step + 1) % table.Length;
                QueueChain(ctx, candidate);
                Commit(ctx, candidate);
                return candidate;
            }

            //极端兜底:表里找不到合法招(不可能,但状态机不许死在这)
            ctx.AttackIndex = (ctx.AttackIndex + 1) % table.Length;
            VDStateIndex fallback = IsLegal(ctx, VDStateIndex.PhantomDash) ? VDStateIndex.PhantomDash
                : IsLegal(ctx, VDStateIndex.ArcFireball) ? VDStateIndex.ArcFireball : VDStateIndex.PhantomDash;
            Commit(ctx, fallback);
            return fallback;
        }

        /// <summary>连段入队:后手在当下就要合法(不与头招同家族、不在历史里),否则不排</summary>
        private static void QueueChain(VDStateContext ctx, VDStateIndex head) {
            VDStateIndex follow = ChainFollow(ctx.Phase, head);
            if (!IsAttack(follow) || follow == head) {
                return;
            }
            if (ctx.InHistory(follow) || FamilyOf(follow) == FamilyOf(head)) {
                return;
            }
            ctx.QueuedChainState = (int)follow;
        }

        /// <summary>记账:写历史环与上一手家族</summary>
        public static void Commit(VDStateContext ctx, VDStateIndex picked) {
            ctx.PushHistory(picked);
            ctx.LastFamily = FamilyOf(picked);
        }
    }
}
