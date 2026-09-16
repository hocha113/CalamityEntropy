using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class LlScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player) => Main.LocalPlayer.Entropy().llSky > 0;

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals("CalamityEntropy:DimensionLens", isActive);
        }
    }
}
