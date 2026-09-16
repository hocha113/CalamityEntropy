using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class NyxolithrakenBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<NyxolithrakenDragon>()] > 0) {
                player.buffTime[buffIndex] = 18000;
            }
            else {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}