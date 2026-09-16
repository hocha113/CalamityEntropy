using CalamityEntropy.Core;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs.PortsDoT
{
    /// <summary>寒宇冻结：玩家自身增益（冰系武器命中后自赋），站定不动时负生命回复减半，并发光</summary>
    public class CosmicFreeze : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.GetModPlayer<CEDoTPlayer>().cosmicFreeze = true;
            Lighting.AddLight(player.Center, 0.3f, Main.DiscoG / 400f, 0.5f);
        }
    }
}
