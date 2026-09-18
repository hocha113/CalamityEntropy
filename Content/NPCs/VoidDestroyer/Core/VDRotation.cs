using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.Core
{
    /// <summary>
    /// 手写轮换表 + 家族标签 + 硬性防复读校验。选招不用随机:
    /// 每阶段一张表,压力招(Dash)与区域/弹幕招交替、同家族不相邻、同招间隔 ≥3;
    /// 表只是「建议序」,真正的兜底是 <see cref="IsLegal"/>:候选命中最近三手或与上一手同家族就沿表往后找,
    /// 替补阀、连段队列头、阶段签名首招全部过同一道闸。阶段切换不清历史,变形前后不可能连出同招。
    /// <para>
    /// 四种玩家持续条件的推演(2026-09-18 读码,未经真机):替补阀只有两处(舰队远距 → 幻影冲刺;支援无地面 / 满员 → 裂隙斩),
    /// 所以塌陷面很小。全程悬空到脚下 60 格无地面:支援投送退成裂隙斩,P2 表第 5 手变裂隙斩后第 8 手的裂隙斩被历史闸拦下顺延到传送火弹,
    /// 裂隙斩一轮两手、不相邻;P3 同理。全程贴脸:没有替补触发,表原样出。全程 1200px 外风筝:两处舰队都退成幻影冲刺,
    /// P1 第 4 手被历史闸拦下(第 1 手刚出过)顺延到虚空火焰,一轮里幻影冲刺 3 手(1 / 7 / 11)占 25%,是各条件下的上限;
    /// P2 / P3 各只有一处舰队,退成冲刺后与既有冲刺槽至少隔三手。地下战斗(远景层不可用)不改选招,只改绘制层。
    /// 纵深招的时长预算:每轮本体带外(不可攻击)的时间 P1 约 22%、P2 约 28%、P3 约 26%,都在 <see cref="VDDirector.FarTimeBudget"/> 之内
    /// </para>
    /// </summary>
    public static class VDRotation
    {
        /// <summary>
        /// P1(12):弹幕 / 冲刺 / 区域交替,纵深环门是 P1 的远景签名(整场的 Z 轴语法从第三手就亮出来);
        /// 深度波形:平面(回旋火)→ 平面(冲刺)→ 远(环门)→ 平面(导弹)→ 立体(舰队)→ 平面(火焰)→ 远(点阵)→ …
        /// </summary>
        private static readonly VDStateIndex[] Phase1 =
        {
            VDStateIndex.ArcFireball, VDStateIndex.PhantomDash, VDStateIndex.DepthGates, VDStateIndex.HomingMissiles,
            VDStateIndex.PhantomFleet, VDStateIndex.VoidFlame, VDStateIndex.PhaseLaser, VDStateIndex.PhantomDash,
            VDStateIndex.RiftCut, VDStateIndex.DepthGates, VDStateIndex.ArcFireball, VDStateIndex.PhantomFleet,
        };

        /// <summary>
        /// P2(14):首手轨道轰炸是阶段签名(变形收尾强制),全息三模式与奇点/支援穿插;深空掠袭在第 12 手接在第二次轨道轰炸之后隔一手,
        /// 远景招不相邻;末尾蓝色天空回绕到轨道轰炸,家族不相邻
        /// </summary>
        private static readonly VDStateIndex[] Phase2 =
        {
            VDStateIndex.OrbitalStrike, VDStateIndex.PhantomDash, VDStateIndex.RedHell, VDStateIndex.HomingMissiles,
            VDStateIndex.Singularity, VDStateIndex.Reinforcement, VDStateIndex.PhantomFleet, VDStateIndex.GreenJungle,
            VDStateIndex.RiftCut, VDStateIndex.TeleportFire, VDStateIndex.OrbitalStrike, VDStateIndex.DeepStrafe,
            VDStateIndex.PhaseLaser, VDStateIndex.BlueSky,
        };

        /// <summary>
        /// P3(16):首手湮灭主炮(护盾收尾强制),主炮一轮两手;连段头(裂隙斩/奇点/轨道轰炸)排在其后手不在最近三手里的位置,
        /// 连段后手也记入历史,所以舰队只在表里出现一次(另一次由裂隙斩连段带出);环门与掠袭各一手,远景招之间至少隔两手。
        /// 连段见 <see cref="ChainFollow"/>
        /// </summary>
        private static readonly VDStateIndex[] Phase3 =
        {
            VDStateIndex.AnnihilationCannon, VDStateIndex.RedHell, VDStateIndex.RiftCut, VDStateIndex.Singularity,
            VDStateIndex.DeepStrafe, VDStateIndex.OrbitalStrike, VDStateIndex.GreenJungle, VDStateIndex.TeleportFire,
            VDStateIndex.PhantomFleet, VDStateIndex.AnnihilationCannon, VDStateIndex.PhantomDash, VDStateIndex.BlueSky,
            VDStateIndex.DepthGates, VDStateIndex.Reinforcement, VDStateIndex.PhaseLaser, VDStateIndex.HomingMissiles,
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
                case VDStateIndex.DeepStrafe:
                    return VDAttackFamily.Barrage;
                case VDStateIndex.PhaseLaser:
                case VDStateIndex.RiftCut:
                case VDStateIndex.OrbitalStrike:
                case VDStateIndex.DepthGates:
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
