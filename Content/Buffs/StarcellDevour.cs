using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    //星细胞之噬:敌怪受击时防御 -25,玩家(PvP)防御 -25
    public class StarcellDevour : ModBuff
    {
        public const int DefenseReduction = 25;

        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex) {
            if (!Main.dedServ && Main.rand.NextBool(4)) {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.IceTorch, 0f, 0f, 120, default, 1.1f);
                d.noGravity = true;
                d.velocity *= 0.3f;
            }
        }

        public override void Update(Player player, ref int buffIndex) {
            player.statDefense -= DefenseReduction;
        }
    }

    public class StarcellDevourGlobalNPC : GlobalNPC
    {
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) {
            if (npc.HasBuff(ModContent.BuffType<StarcellDevour>())) {
                modifiers.Defense.Flat -= StarcellDevour.DefenseReduction;
            }
        }
    }
}
