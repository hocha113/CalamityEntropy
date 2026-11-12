using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Buffs
{
    //耀斑灼烧:敌怪每秒 150,玩家(PvP)每秒 30,时长不随专家/大师延长
    public class FlareBurn : DotBuff
    {
        public override int DamagePlayerPerSec => 30;
        public override int DamageEnemiesPerSec => 150;

        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex) {
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                SpawnFlare(npc.position, npc.width, npc.height);
            }
        }

        public override void Update(Player player, ref int buffIndex) {
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                SpawnFlare(player.position, player.width, player.height);
            }
        }

        private static void SpawnFlare(Microsoft.Xna.Framework.Vector2 position, int width, int height) {
            Dust d = Dust.NewDustDirect(position, width, height, DustID.SolarFlare, 0f, -1.6f, 100, default, Main.rand.NextFloat(0.9f, 1.4f));
            d.noGravity = true;
            d.velocity *= 0.6f;
        }
    }
}
