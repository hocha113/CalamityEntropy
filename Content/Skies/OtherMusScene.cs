using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.NPCs.NihilityTwin;
using CalamityMod.Events;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class OtherMusScene : ModSceneEffect
    {

        public override SceneEffectPriority Priority => (SceneEffectPriority)12;

        public override bool IsSceneEffectActive(Player player)
        {
            if (BossRushEvent.BossRushActive)
                return false;
            if (NPC.FindFirstNPC(ModContent.NPCType<NihilityActeriophage>()) != -1)
            {
                return true;
            }
            return false;

        }
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/vtfight");


    }
    public class LilyMusicScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.Environment;
        public override bool IsSceneEffectActive(Player player)
        {
            return player.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom);
        }
        public override int Music => (Main.dayTime && Main.LocalPlayer.ZoneForest && !Main.LocalPlayer.ZoneJungle && !Main.LocalPlayer.ZoneSnow) ? MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/LMus1") : (!Main.dayTime && Main.LocalPlayer.townNPCs > 2 ? MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/LMus2") : -1);
    }
}
