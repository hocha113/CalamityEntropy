using CalamityEntropy.Content.Buffs;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.FishherdUrn
{
    public class FishherdUrnBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<FishherdUrnPiranha>();
    }
}
