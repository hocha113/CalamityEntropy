using CalamityEntropy.Content.NPCs.Cruiser;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class CBScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        //强度中枢淡出尾巴期间保持场景在场,滤镜/天空随 Intensity 自然收干
        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(ModContent.NPCType<CruiserHead>()) || Main.LocalPlayer.Entropy().crSky > 0 || CruiserSkyDrive.Intensity > 0.004f;

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals("CalamityEntropy:Cruiser", isActive);
        }
    }
}
