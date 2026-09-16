using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class HeatDeath : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.velocity *= 0.96f;
        }

        public override void Update(NPC npc, ref int buffIndex) {
            npc.velocity *= npc.boss ? 0.99f : 0.986f;
            if (!npc.dontTakeDamage) {
                int del = ((int)(npc.lifeMax / (6f * 60 * 60) + 1));
                if (npc.life > del) {
                    if (Main.GameUpdateCount % 5 == 0)
                        npc.life -= del;
                    if (Main.GameUpdateCount % 20 == 0) {
                        CombatText.NewText(npc.getRect(), Color.DarkRed, del * 4, false, true);
                    }
                }
            }
        }
    }
}