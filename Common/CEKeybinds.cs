using System;
using System.IO;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class CEKeybinds : ModSystem
    {
        public static ModKeybind VetrasylsEyeBlockHotKey { get; private set; }
        public static ModKeybind RuneDashHotKey { get; private set; }
        public static ModKeybind NihilityAndChaoticArmorConnectKey { get; private set; }
        public static ModKeybind ThrowPoopHotKey { get; set; }
        public static ModKeybind PoopHoldHotKey { get; set; }
        public static ModKeybind CommandMinions { get; set; }
        public static ModKeybind AcropolisMechTransformation { get; set; }
        public override void Load() {
            VetrasylsEyeBlockHotKey = KeybindLoader.RegisterKeybind(Mod, "VetrasylsEyeBlock", "C");
            RuneDashHotKey = KeybindLoader.RegisterKeybind(Mod, "RuneDash", "K");
            CommandMinions = KeybindLoader.RegisterKeybind(Mod, "CommandMinions", "N");
            NihilityAndChaoticArmorConnectKey = KeybindLoader.RegisterKeybind(Mod, "NihilityAndChaoticArmorConnectKey", "U");
            AcropolisMechTransformation = KeybindLoader.RegisterKeybind(Mod, "AcropolisMechTransformation", "U");


            string MyGameFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games");
            string Isaac1 = Path.Combine(MyGameFolder, "Binding of Isaac Repentance").Replace("/", "\\");
            string Isaac2 = Path.Combine(MyGameFolder, "Binding of Isaac Repentance+").Replace("/", "\\");
            bool isaac = Directory.Exists(Isaac1) || Directory.Exists(Isaac2);
            if (isaac) {
                CEKeybinds.ThrowPoopHotKey = KeybindLoader.RegisterKeybind(Mod, "ThrowPoop", "LeftAlt");
                CEKeybinds.PoopHoldHotKey = KeybindLoader.RegisterKeybind(Mod, "KeepPoop", "Q");
            }
        }
        public override void Unload() {
            ThrowPoopHotKey = null;
            PoopHoldHotKey = null;
        }
    }
}
