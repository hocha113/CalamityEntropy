using CalamityEntropy.Content.Buffs;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class EDamageOverTimePlayer : ModPlayer
    {
        public override void UpdateBadLifeRegen() {
            int damageApply = 0;
            if (Player.HasBuff<LifeOppress>()) {
                damageApply += 40;
            }
            var dict = DotBuff.InstanceByType();
            for (int i = 0; i < Player.buffType.Length; i++) {
                if (Player.buffTime[i] > 0) {
                    if (dict.TryGetValue(Player.buffType[i], out var res) && res is DotBuff) {
                        damageApply += res.DamagePlayerPerSec * 2;
                    }
                }
            }
            // 虚空之火:禁用生命再生,先把正回复归零再扣
            if (Player.HasBuff<VoidFire>() && Player.lifeRegen > 0) {
                Player.lifeRegen = 0;
            }
            if (damageApply > 0) {
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= damageApply;
            }
        }
    }
}
