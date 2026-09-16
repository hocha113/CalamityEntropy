using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class SoyMilkBuff : ModBuff
    {
        public int counter;
        public override void SetStaticDefaults() {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = false;
            BuffID.Sets.LongerExpertDebuff[Type] = false;

        }

        public override void Update(Player player, ref int buffIndex) {
            // 灾厄潜行清零随潜行系统退役移除
            player.GetAttackSpeed(DamageClass.Generic) *= 3;
            player.GetDamage(DamageClass.Generic) *= 0.30f;
            player.pickSpeed -= 0.6f;
        }
    }
}
