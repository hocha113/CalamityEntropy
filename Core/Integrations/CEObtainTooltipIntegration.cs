using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.EvilCards;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Core.CalamityRef;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Integrations
{
    /// <summary>
    /// 向 MoreObtainingTooltips 补充本模组几件"没有配方也不掉落自明确来源"的物品的获取途径。
    /// 未装该模组时静默跳过。
    /// </summary>
    internal static class CEObtainTooltipIntegration
    {
        public static void Register() {
            if (!ModLoader.TryGetMod("MoreObtainingTooltips", out Mod mot)) {
                return;
            }
            CalamityEntropy mod = CalamityEntropy.Instance;

            //第一条保留 Info 日志:它同时充当"MOT 接口签名是否还对得上"的加载期探针
            mod.Logger.Info("MOT Support:" + AddSource(mot, "HallowedEnemiesDrop", ModContent.ItemType<HolyMantle>()));

            AddSource(mot, "VoidOreMine", ModContent.ItemType<VoidOre>());
            AddSource(mot, "EvilEnemiesDrop", ModContent.ItemType<BitternessCard>());
            AddSource(mot, "DungeonEnemiesDrop", ModContent.ItemType<BookMarkBlackKnife>());
            AddSource(mot, "AbyssalPiercerObt", ModContent.ItemType<AbyssalPiercer>());
            //贪婪卡 MOT 来源按 CERef.Has 分发,无灾厄不得出现星辉
            AddSource(mot, CERef.Has ? "AstralFishing" : "EvilFishing", ModContent.ItemType<GreedCard>());
        }

        private static object AddSource(Mod mot, string localizationKey, int itemType) {
            return mot.Call("AddCustomizedSource",
                CalamityEntropy.Instance.GetLocalization(localizationKey).Value,
                new int[1] { itemType });
        }
    }
}
