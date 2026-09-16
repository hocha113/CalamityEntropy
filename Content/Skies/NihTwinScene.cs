using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class NihTwinScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player) => Main.LocalPlayer.Entropy().NihSky > 0;

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals("CalamityEntropy:NihTwin", isActive);
        }
    }
}
