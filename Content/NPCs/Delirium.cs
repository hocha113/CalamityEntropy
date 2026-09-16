using CalamityEntropy.Content.NPCs.FriendFinderNPC;
using CalamityEntropy.Content.NPCs.NihilityTwin;
using CalamityEntropy.Content.NPCs.Prophet;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.NPCs
{
    public class Delirium : ModNPC
    {
        public override bool IsLoadingEnabled(Mod mod) {
            return false;
        }
        // 变身池删除全部灾厄 Boss 条目，保留原版与自有 Boss（本 NPC 目前处于停用状态）
        public static List<int> npcTurns = new List<int>()
        {
            NPCID.KingSlime,
            NPCID.EyeofCthulhu,
            NPCID.BrainofCthulhu,
            NPCID.QueenBee,
            NPCID.SkeletronHead,
            NPCID.Deerclops,
            NPCID.QueenSlimeBoss,
            NPCID.Retinazer,
            NPCID.Spazmatism,
            NPCID.Plantera,
            NPCID.Golem,
            NPCID.DukeFishron,
            NPCID.HallowBoss,
            NPCID.CultistBoss,
            NPCID.MoonLordCore,
            ModContent.NPCType<NihilityActeriophage>(),
            ModContent.NPCType<TheProphet>()
        };

        public override void SetStaticDefaults() {
            this.HideFromBestiary();
        }
        public override void SetDefaults() {
            NPC.friendly = false;
            NPC.damage = 300;
            NPC.lifeMax = 3400000;

        }
        public override void OnSpawn(IEntitySource source) {
            NPC.netUpdate = true;
            NPC.netSpam = 0;
            int npc = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, npcTurns[Main.rand.Next(npcTurns.Count)]);
            NPC spawn = npc.ToNPC();
            spawn.Center = NPC.Center;
            spawn.lifeMax = NPC.lifeMax;
            spawn.life = NPC.life;
            spawn.damage = NPC.damage;
            spawn.GetGlobalNPC<DeliriumGlobalNPC>().delirium = true;
            spawn.GetGlobalNPC<DeliriumGlobalNPC>().damage = NPC.damage;
            spawn.GetGlobalNPC<DeliriumGlobalNPC>().counter = 180;

            spawn.netUpdate = true;
            spawn.netSpam = 0;
            NPC.active = false;
        }
    }

    public class DeliriumGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool delirium = false;
        public int counter = 0;
        public int damage = 0;
        public override GlobalNPC Clone(NPC from, NPC to) {
            var n = to.GetGlobalNPC<DeliriumGlobalNPC>();
            n.delirium = delirium;
            n.counter = counter;
            return n;
        }
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) {
            /* binaryWriter.Write(delirium);
             binaryWriter.Write(counter);
             binaryWriter.Write(npc.lifeMax);
             binaryWriter.Write(npc.life);
             binaryWriter.Write(npc.damage);*/
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) {
            /*delirium = binaryReader.ReadBoolean();
            counter = binaryReader.ReadInt32();
            npc.lifeMax = binaryReader.ReadInt32();
            npc.life = binaryReader.ReadInt32();
            npc.damage = binaryReader.ReadInt32();*/
        }

        public override bool CheckActive(NPC npc) {
            return !delirium;
        }
    }
}
