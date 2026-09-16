using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class FlamingBlood : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
        }
    }
}
