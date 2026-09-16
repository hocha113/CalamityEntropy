using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public abstract class BaseMinionBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            if (player.ownedProjectileCounts[ProjType] > 0) {
                player.buffTime[buffIndex] = 18000;
            }
            else {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }

        public virtual int ProjType => -1;
    }
}