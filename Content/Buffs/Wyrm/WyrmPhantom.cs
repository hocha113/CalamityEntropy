using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs.Wyrm
{
    public class WyrmPhantom : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.Entropy().wyrmPhantom = true;
        }
    }
}