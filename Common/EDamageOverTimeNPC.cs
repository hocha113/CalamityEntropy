using CalamityEntropy.Content.Buffs;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class EDamageOverTimeNPC : GlobalNPC
    {
        public override void UpdateLifeRegen(NPC npc, ref int damage) {
            int damageApply = 0;
            if (npc.HasBuff<LifeOppress>())
                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;
            if (npc.HasBuff<LifeOppress>()) {
                damageApply += 2000;
            }
            var dict = DotBuff.InstanceByType();
            for (int i = 0; i < npc.buffType.Length; i++) {
                if (npc.buffTime[i] > 0) {
                    if (dict.TryGetValue(npc.buffType[i], out var res) && res is DotBuff) {
                        damageApply += res.DamageEnemiesPerSec;
                    }
                }
            }

            // Debuff 伤害倍率不在此自乘：统一由 EGlobalNPC.UpdateLifeRegen 全局放大
            //（GlobalNPC 按 FullName 字母序执行，本类恒先于 EGlobalNPC，放大必然覆盖到本处扣减）
            damage += damageApply;
            npc.lifeRegen -= damageApply * 2;
        }
    }
}
