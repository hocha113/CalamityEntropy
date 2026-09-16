using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class LifeOppress : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
        }
    }
}