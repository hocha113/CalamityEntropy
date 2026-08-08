using CalamityEntropy.Content.Items.Armor.VoidFaquir;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public partial class EModPlayer : ModPlayer
    {
        public Vector2 VFProjectilePos = Vector2.Zero;
        public bool VoidFaquirBonusAny = false;
        public bool VoidFaquirBonusMelee = false;
        public bool VoidFaquirBonusMage = false;
        public bool VoidFaquirBonusRanger = false;
        public bool VoidFaquirBonusSummoner = false;
        public bool VoidFaquirBonusRogue = false;
        public float VoidChargeBarValue = 0;
        public int VoidChargeBarDraw = 0;
        public void ResetVoidFaquir()
        {
            VoidFaquirBonusAny = false;
            VoidFaquirBonusMelee = false;
            VoidFaquirBonusMage = false;
            VoidFaquirBonusRanger = false;
            VoidFaquirBonusSummoner = false;
            VoidFaquirBonusRogue = false;
        }
        public static int MeleeProjType = -1;
        public float VFMeleeCharge = 0;
        public int MeleeChargeCountDownDelay = 0;
        public void UpdateVF()
        {
            if (VoidChargeBarDraw > 0)
                VoidChargeBarDraw--;
            if (MeleeChargeCountDownDelay > 0)
                MeleeChargeCountDownDelay--;
            if(MeleeChargeCountDownDelay <= 0)
            {
                if (VFMeleeCharge > 1)
                    VFMeleeCharge = 1;
                if (VFMeleeCharge < 1 && VFMeleeCharge > 0)
                {
                    VFMeleeCharge -= 0.0025f;
                    if(VFMeleeCharge < 0)
                        VFMeleeCharge = 0;
                }
            }
            if (VoidFaquirBonusMelee)
            {
                if (MeleeProjType == -1)
                    MeleeProjType = ModContent.ProjectileType<VoidFaquirEnergyBallMelee>();
                if (Main.myPlayer == Player.whoAmI && Player.ownedProjectileCounts[MeleeProjType] == 0)
                {
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, MeleeProjType, 0, 0, Player.whoAmI);
                }
            }
            else
            {
                VFMeleeCharge = 0;
            }
        }
    }
}