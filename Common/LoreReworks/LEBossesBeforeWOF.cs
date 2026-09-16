using CalamityEntropy.Content.Items.Lores;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.LoreReworks
{
    public class LEApychos : LoreEffect
    {
        public override int ItemType => ModContent.ItemType<LoreApsychos>();
        public override void UpdateEffects(Player player) {
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.OnFire3] = true;
        }
    }
}
