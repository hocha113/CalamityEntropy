using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.LoreReworks
{
    public class CalLRPlayer : ModPlayer
    {
        public override void OnHurt(Player.HurtInfo info)
        {
            if (CEID.Item_LoreCrabulon > 0 && LoreReworkSystem.Enabled(CEID.Item_LoreCrabulon) && CEID.Buff_Mushy > 0)
            {
                Player.AddBuff(CEID.Buff_Mushy, LECabulon.BuffTime * 60);
            }
        }
    }
}
