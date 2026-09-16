using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class YharimPower : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = false;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.GetDamage(DamageClass.Generic) += 0.05f;
            player.GetCritChance(DamageClass.Generic) += 4;
            player.pickSpeed -= 0.3f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.1f;
            player.GetKnockback(DamageClass.Summon) += 4;
            player.moveSpeed *= 1.12f;
            player.luck += 0.3f;
            player.statDefense += 8;
            player.lifeRegen += 2;
        }
    }
}
