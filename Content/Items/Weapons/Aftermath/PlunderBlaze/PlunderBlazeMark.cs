using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PlunderBlaze
{
    /// <summary>
    /// 劫掠标记。劫掠怒焰的火焰打出看起来致死的一击时挂上,只有 10 帧;敌人死时还带着它就有概率再掉一份战利品
    /// 用减益而不用 GlobalNPC 字段,是因为命中在拥有者客户端结算、掉落在服务器结算,只有减益会被同步过去
    /// </summary>
    public class PlunderBlazeMark : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            //标签类减益连免疫一切普通减益的敌人也挂得上
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }

    /// <summary>带着劫掠标记死去的普通敌人有概率把物品掉落表再结算一次;钱币与旗帜各走各的流程,不会跟着翻倍</summary>
    public class PlunderBlazeGlobalNPC : GlobalNPC
    {
        private const float DoubleLootChance = 0.33f;   //带标记死去时再掉一份的概率

        //只在服务器或单人端调用,此时雕像与 PreKill 的拦截都已过,本体掉落刚结算完
        public override void OnKill(NPC npc) {
            if (!npc.HasBuff<PlunderBlazeMark>() || !Eligible(npc) || Main.rand.NextFloat() >= DoubleLootChance) {
                return;
            }
            DropAttemptInfo info = new DropAttemptInfo {
                player = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)],
                npc = npc,
                IsExpertMode = Main.expertMode,
                IsMasterMode = Main.masterMode,
                IsInSimulation = false,
                rng = Main.rand
            };
            Main.ItemDropSolver.TryDropping(info);
        }

        /// <summary>算不算普通敌人: 排除首领与首领的部件和随从、蠕虫的非头部体节、雕像刷出的、城镇与友好 NPC、小动物和弹幕型 NPC</summary>
        public static bool Eligible(NPC npc) {
            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type] || NPCID.Sets.BossBestiaryPriority.Contains(npc.type) || npc.GetBossHeadTextureIndex() >= 0) {
                return false;
            }
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) {
                return false;
            }
            return !npc.townNPC && !npc.friendly && !npc.SpawnedFromStatue && !npc.CountsAsACritter && !NPCID.Sets.CountsAsCritter[npc.type] && !NPCID.Sets.ProjectileNPC[npc.type];
        }
    }
}
