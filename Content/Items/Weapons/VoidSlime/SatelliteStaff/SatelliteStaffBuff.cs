using CalamityEntropy.Content.Buffs;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SatelliteStaff
{
    public class SatelliteStaffBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<SatelliteStaffSlime>();
    }
}
