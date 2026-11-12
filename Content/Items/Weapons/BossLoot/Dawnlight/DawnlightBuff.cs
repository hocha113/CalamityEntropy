using CalamityEntropy.Content.Buffs;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.Dawnlight
{
    public class DawnlightBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<DawnlightBlade>();
    }
}
