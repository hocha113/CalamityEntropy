using CalamityEntropy.Common;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Integrations
{
    /// <summary>
    /// 给几个常见联装模组的 Boss 补血条颜色。全部按内部名 TryFind,
    /// 对方改名或没装都只是这一条不生效,不影响其余。
    /// </summary>
    internal static class CEForeignBossbarColors
    {
        public static void Register() {
            //血条颜色是纯客户端表现,服务端不需要
            if (Main.dedServ) {
                return;
            }
            try {
                if (ModLoader.TryGetMod("SOTS", out Mod sots)) {
                    Add(sots, "SubspaceSerpentHead", new Color(115, 114, 160));
                    Add(sots, "PutridPinky1", Color.Pink);
                    Add(sots, "PutridPinkyPhase2", Color.Pink);
                    Add(sots, "Polaris", new Color(200, 250, 250));
                    Add(sots, "NewPolaris", new Color(200, 250, 250));
                    Add(sots, "Lux", new Color(255, 200, 230));
                    Add(sots, "Glowmoth", new Color(255, 240, 200));
                    Add(sots, "PharaohsCurse", Color.Gold);
                    Add(sots, "UnusedAdvisorHead", new Color(238, 208, 255));
                }
                if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fs)) {
                    Add(fs, "AbomBoss", new Color(249, 226, 77));
                    Add(fs, "BanishedBaron", new Color(230, 240, 242));
                    Add(fs, "CosmosChampion", Color.DarkOrange);
                    Add(fs, "EarthChampion", Color.Orange);
                    Add(fs, "LifeChampion", Color.Gold);
                    Add(fs, "NatureChampion", Color.Green);
                    Add(fs, "ShadowChampion", new Color(143, 100, 234));
                    Add(fs, "SpiritChampion", Color.DarkGoldenrod);
                    Add(fs, "TerraChampion", Color.DarkGreen);
                    Add(fs, "TimberChampion", new Color(230, 240, 242));
                    Add(fs, "WillChampion", new Color(234, 213, 143));
                    Add(fs, "CursedCoffin", Color.Yellow);
                    Add(fs, "LifeChallenger", Color.Gold);
                    Add(fs, "Magmaw", Color.Gray);
                    Add(fs, "MutantBoss", CalamityEntropy.AprilFool ? new Color(217, 142, 67) : new Color(100, 200, 255));
                    Add(fs, "TrojanSquirrel", new Color(147, 108, 85));
                }
            } catch {
                CalamityEntropy.Instance.Logger.Warn("CalamityEntropy: Other mods' bossbar color failed to setup");
            }
        }

        /// <summary>按内部名取对方的 ModNPC 类型再上色;找不到就跳过</summary>
        public static void Add(Mod mod, string name, Color color) {
            if (mod == null) {
                return;
            }
            if (mod.TryFind<ModNPC>(name, out var mnpc)) {
                EntropyBossbar.bossbarColor[mnpc.Type] = color;
            }
        }
    }
}
