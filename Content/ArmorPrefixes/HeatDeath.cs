using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class HeatDeath : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.GetDamage(DamageClass.Generic) *= 1.15f;
            player.Entropy().addEquip("HEATDEATH");
            if (Main.GameUpdateCount % 36 == 0) {
                int deal = (int)(Math.Round(player.statLife * 0.02f) + 1);
                if (player.statLife > deal) {
                    player.statLife -= deal;
                }
            }
            /*foreach (NPC npc in Main.ActiveNPCs)
            {
                if (CEUtils.getDistance(npc.Center, player.Center) < 2000)
                {
                    if (!npc.friendly)
                    {
                        EGlobalNPC.AddVoidTouch(npc, 5, 0.03f * (1 - (CEUtils.getDistance(npc.Center, player.Center) / 2000f)), 400, int.Max(npc.lifeMax / 15000, 6));
                    }
                }
            }*/
        }
        public override float AddDefense() {
            return 0.15f;
        }
        public override int getRollChance() {
            return 0;
        }
        public override Color getColor() {
            return Color.DarkRed;
        }
        public override bool Dramatic() {
            return true;
        }
        public override bool Precious() {
            return true;
        }
    }
}
