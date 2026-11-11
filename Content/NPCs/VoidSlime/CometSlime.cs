using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.VoidSlime;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidSlime
{
    /// <summary>彗星史莱姆:击败虚空史莱姆后在太空层刷新,半数概率掉一个超新星凝胶</summary>
    public class CometSlime : ModNPC
    {
        public const float SpawnWeight = 0.18f;

        public override void SetStaticDefaults() {
            Main.npcFrameCount[Type] = 2;
        }

        public override void SetDefaults() {
            NPC.width = 36;
            NPC.height = 26;
            NPC.damage = 180;
            NPC.defense = 40;
            NPC.lifeMax = 9000;
            NPC.knockBackResist = 0.3f;
            NPC.value = Item.buyPrice(gold: 5);
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.aiStyle = NPCAIStyleID.Slime;
            AIType = NPCID.BlueSlime;
            AnimationType = NPCID.BlueSlime;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.CometSlimeBestiary"),
            });
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            return EDownedBosses.downedVoidSlime && spawnInfo.Player.ZoneSkyHeight ? SpawnWeight : 0f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SupernovaGel>(), 2));
        }

        public override Color? GetAlpha(Color drawColor) => Color.Lerp(drawColor, Color.White, 0.6f);

        public override void AI() {
            Lighting.AddLight(NPC.Center, 0.2f, 0.34f, 0.56f);
        }

        public override void HitEffect(NPC.HitInfo hit) {
            int count = NPC.life <= 0 ? 24 : 5;
            for (int i = 0; i < count; i++) {
                Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.IceTorch, hit.HitDirection * 1.5f, -1f, 0, default, 1.3f);
                dust.noGravity = true;
            }
        }
    }

    /// <summary>摘星者:击败虚空史莱姆</summary>
    public class StarPickerAchievement : ModAchievement
    {
        public override string TextureName => "CalamityEntropy/Content/Items/VoidSlime/StarPickerAchievement";

        public override void SetStaticDefaults() {
            Achievement.SetCategory(AchievementCategory.Slayer);
            AddNPCKilledCondition(ModContent.NPCType<VoidSlime>());
        }
    }
}
