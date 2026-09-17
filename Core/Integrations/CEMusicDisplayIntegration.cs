using CalamityEntropy.Content.NPCs.Acropolis;
using CalamityEntropy.Content.NPCs.Apsychos;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.NPCs.LuminarisMoth;
using CalamityEntropy.Content.NPCs.Prophet;
using CalamityEntropy.Utilities;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Integrations
{
    /// <summary>
    /// 向 MusicDisplay 登记本模组的 Boss 曲目(曲名、作者、用途)。未装 MusicDisplay 时静默跳过。
    /// </summary>
    internal static class CEMusicDisplayIntegration
    {
        public static void Register() {
            if (!ModLoader.TryGetMod("MusicDisplay", out Mod display)) {
                return;
            }
            CalamityEntropy mod = CalamityEntropy.Instance;

            AddMusic(display, "Assets/Sounds/Music/Apsychos", "Musics.Apsychos", "Musics.Francium", CEUtils.GetNPCName<Apsychos>());
            AddMusic(display, "Assets/Sounds/Music/CruiserBoss", "Musics.Cruiser", "Musics.Francium", CEUtils.GetNPCName<CruiserHead>());
            AddMusic(display, "Assets/Sounds/Music/SpectralForesight", "Musics.Prophet1", "Musics.Francium", CEUtils.GetNPCName<TheProphet>());
            AddMusic(display, "Assets/Sounds/Music/Prophet2", "Musics.Prophet2", "Musics.BadbfHX", CEUtils.GetNPCName<TheProphet>());
            //虚无双子没有单独的 ModNPC 展示名可取,直接借 BossChecklist 的条目名
            AddMusic(display, "Assets/Sounds/Music/vtfight", "Musics.Cruiser", "Musics.BadbfHX",
                mod.GetLocalization("NPCs.NihilityActeriophage.BossChecklistIntegration.EntryName"));
            AddMusic(display, "Assets/Sounds/Music/LuminarisBoss", "Musics.Luminaris", "Musics.BadbfHX", CEUtils.GetNPCName<Luminaris>());
            AddMusic(display, "Assets/Sounds/Music/HellBlazenRobotics", "Musics.Acropolis", "Musics.SobaNoodles", CEUtils.GetNPCName<AcropolisMachine>());
        }

        private static void AddMusic(Mod display, string musicPath, string nameKey, string authorKey, object themeOfArg) {
            CalamityEntropy mod = CalamityEntropy.Instance;
            display.Call("AddMusic",
                (short)MusicLoader.GetMusicSlot(mod, musicPath),
                mod.GetLocalization(nameKey),
                mod.GetLocalization(authorKey),
                mod.GetLocalization("Musics.ThemeOf").WithFormatArgs(themeOfArg));
        }
    }
}
