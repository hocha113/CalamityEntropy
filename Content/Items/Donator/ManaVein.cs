using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class ManaVein : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = false;
        }
    }
}
