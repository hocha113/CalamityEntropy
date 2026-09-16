using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.NPCs.NihilityTwin;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Biomes
{
    public class VoidDummyBoime : ModBiome
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.Environment;
        public override string BestiaryIcon => "CalamityEntropy/Assets/VoidDummyBoimeIcon";
        public override string BackgroundPath => "CalamityEntropy/Assets/VoidBack";
        public override bool IsBiomeActive(Player player) {
            return NPC.AnyNPCs(ModContent.NPCType<NihilityActeriophage>()) || NPC.AnyNPCs(ModContent.NPCType<CruiserHead>());
        }
    }
}