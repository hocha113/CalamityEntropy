using Terraria.Achievements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class BookmarkInsertCondition : AchievementCondition
    {
        public static BookmarkInsertCondition Instance = new BookmarkInsertCondition("BookmarkInsert");
        public BookmarkInsertCondition(string name) : base(name) {
        }
    }

    public class BookmarkInsertAchievement : ModAchievement
    {
        public override bool IsLoadingEnabled(Mod mod) {
            return false;
        }
        public override string TextureName => "CalamityEntropy/icon";
        public override string Name => Mod.GetLocalization("AchievementBookmarkInsertName").Value;
        public override LocalizedText Description => Mod.GetLocalization("AchievementBookmarkInsertDesc");
        public override void SetStaticDefaults() {
            AddCondition<BookmarkInsertCondition>(BookmarkInsertCondition.Instance);
        }
    }
}
