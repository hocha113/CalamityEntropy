using System;
using System.IO;
using System.Text;

namespace CalamityEntropy.Common
{
    public static class ShadowCrystalDeltarune
    {
        public static bool Ch1Crystal = false;
        public static bool Ch2Crystal = false;
        public static bool Ch3Crystal = false;
        public static bool Ch4Crystal = false;
        public static bool Ch5Crystal = false;
        public static bool SaveFileExist = false;
        public static void Load() {
            try {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string drIni = Path.Combine(appData, "DELTARUNE/dr.ini");
                SaveFileExist = Path.Exists(drIni);
                if (SaveFileExist) {
                    string content = File.ReadAllText(drIni, Encoding.UTF8);
                    bool CheckFile(string text, string header) {
                        string[] lines = content.Split("\n");
                        bool flag = false;
                        for (int i = 0; i < lines.Length; i++) {
                            string line = lines[i];
                            if (!flag) {
                                if (line.Contains("[URA]"))
                                    flag = true;
                                continue;
                            }
                            if (line.StartsWith(header)) {
                                if (line[2] == '3')
                                    continue;
                                if (line.Contains("1.000000") || line.Contains("2.000000")) {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                    Ch1Crystal = CheckFile(content, "1_");
                    Ch2Crystal = CheckFile(content, "2_");
                    Ch3Crystal = CheckFile(content, "3_");
                    Ch4Crystal = CheckFile(content, "4_");
                    Ch5Crystal = CheckFile(content, "5_");

                }
            } catch {
            }
        }
        public static void Reset() {
            SaveFileExist = false;
            Ch1Crystal = false;
            Ch2Crystal = false;
            Ch3Crystal = false;
            Ch4Crystal = false;
            Ch5Crystal = false;
        }
    }
}
