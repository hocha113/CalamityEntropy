using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class VoidTouch : ModBuff
    {
        public int counter;
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;

        }


        public override void Update(Player player, ref int buffIndex)
        {
            if (player.Entropy().voidResistance >= 1)
                return;
            if (Main.GameUpdateCount % (int)(4f / (1 - player.Entropy().voidResistance)) == 0 && player.Entropy().voidResistance < 1)
            {
                int dmg = 3;
                player.statLife -= dmg;
                if (player.statLife <= dmg)
                {
                    player.SetImmuneTimeForAllTypes(0);
                    player.immune = false;
                    player.DelBuff(buffIndex--);
                    player.Hurt(PlayerDeathReason.ByCustomReason(Language.GetText("Mods.CalamityEntropy.KilledByVoidTouch").ToNetworkText(player.name)), dmg, 0);
                }
            }
            var r = Main.rand;
            Dust.NewDust(player.Center, player.width, player.height, DustID.CorruptSpray, (float)r.NextDouble() * 6 - 3, (float)r.NextDouble() * 6 - 3);
            for (int i = 0; i < 1; i++)
            {
                //PRT_Void进EffectLoader虚空shader,Opacity=0.5是旧playerUpdate原值
                var p = PRTLoader.NewParticle<PRT_Void>(player.Center, new Vector2((float)((r.NextDouble() - 0.5) * 6), (float)((r.NextDouble() - 0.5) * 6)), Color.White, 1f);
                p.Opacity = 0.5f;
            }
        }
    }
}
