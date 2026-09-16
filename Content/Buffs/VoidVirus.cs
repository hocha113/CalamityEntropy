using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class VoidVirus : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;

        }

        public override void Update(NPC npc, ref int buffIndex) {
            if (Main.GameUpdateCount % 20 == 0) {
                int dot = (int)(80 * npc.Entropy().DebuffDamageMult());
                if (npc.life > dot) {
                    npc.life -= dot;
                    CombatText.NewText(npc.getRect(), Color.SkyBlue, dot, false, true);
                }
                else {
                    npc.SimpleStrikeNPC(dot, 0, false, 0, DamageClass.Default);
                }
            }
        }

        public override void Update(Player player, ref int buffIndex) {

            if (Main.GameUpdateCount % 12 == 0) {
                if (player.statLife > 2) {
                    player.statLife -= 2;
                }
                else {
                    player.Hurt(PlayerDeathReason.ByCustomReason(Language.GetText("Mods.CalamityEntropy.KilledByVoidVirus").ToNetworkText(player.name)), 4, 0, dodgeable: false, armorPenetration: 114514, quiet: true);
                }
            }
        }
    }

    public class VoidVirusDebuffNPC : GlobalNPC
    {
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) {
            if (npc.HasBuff<VoidVirus>()) {
                modifiers.ArmorPenetration += 10;
            }
        }
    }

}
