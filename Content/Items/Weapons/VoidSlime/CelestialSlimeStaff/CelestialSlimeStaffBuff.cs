using CalamityEntropy.Content.Buffs;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.CelestialSlimeStaff
{
    /// <summary>计数弹幕还在就续上增益;主人端补出唯一的可见史莱姆(原版阿比盖尔的做法)</summary>
    public class CelestialSlimeStaffBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<CelestialSlimeStaffCounter>();

        public override void Update(Player player, ref int buffIndex) {
            base.Update(player, ref buffIndex);
            int slime = ModContent.ProjectileType<CelestialSlimeStaffSlime>();
            if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[ProjType] > 0 && player.ownedProjectileCounts[slime] < 1) {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.Zero, slime, 0, 0f, player.whoAmI);
            }
        }
    }
}
