using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy
{
    public class CERecipeGroups : ModSystem
    {
        public static string gems = "Gems";
        public static string AnyOrichalcumBar = "OrichalcumBars";
        public static string butterflies = "Butterflies";
        public static string evilBar = "EvilBars";
        public static string fairys = "Fairys";
        public static string IronBar = "IronBar";
        public static RecipeGroup gems_;
        public static RecipeGroup AnyOrichalcumBar_;
        public static RecipeGroup butterflies_;
        public static RecipeGroup evilBar_;
        public static RecipeGroup fairys_;
        public static RecipeGroup IronBar_;
        public override void AddRecipeGroups() {
            gems = $"{Mod.Name}:" + gems;
            AnyOrichalcumBar = $"{Mod.Name}:" + AnyOrichalcumBar;
            butterflies = $"{Mod.Name}:" + butterflies;
            evilBar = $"{Mod.Name}:" + evilBar;
            fairys = $"{Mod.Name}:" + fairys;

            AnyOrichalcumBar_ = new RecipeGroup(() => CalamityEntropy.Instance.GetLocalization("AnyOrichalcumBar").Value, ItemID.OrichalcumBar, ItemID.MythrilBar);
            gems_ = new RecipeGroup(() => CalamityEntropy.Instance.GetLocalization("Gems").Value, ItemID.Ruby, ItemID.Sapphire, ItemID.Diamond, ItemID.Emerald, ItemID.Topaz, ItemID.Amethyst);
            butterflies_ = new RecipeGroup(() => CalamityEntropy.Instance.GetLocalization("Butterflies").Value, ItemID.EmpressButterfly, ItemID.GoldButterfly, ItemID.HellButterfly, ItemID.JuliaButterfly, ItemID.MonarchButterfly, ItemID.PurpleEmperorButterfly, ItemID.RedAdmiralButterfly, ItemID.SulphurButterfly, ItemID.TreeNymphButterfly, ItemID.UlyssesButterfly, ItemID.ZebraSwallowtailButterfly);
            evilBar_ = new RecipeGroup(() => CalamityEntropy.Instance.GetLocalization("AnyEvilBar").Value, ItemID.CrimtaneBar, ItemID.DemoniteBar);
            fairys_ = new RecipeGroup(() => CalamityEntropy.Instance.GetLocalization("AnyFairy").Value, 4068, 4069, 4070);
            IronBar_ = new RecipeGroup(() => CalamityEntropy.Instance.GetLocalization("IronBar").Value, ItemID.IronBar, 704);

            RecipeGroup.RegisterGroup(AnyOrichalcumBar, AnyOrichalcumBar_);
            RecipeGroup.RegisterGroup(gems, gems_);
            RecipeGroup.RegisterGroup(butterflies, butterflies_);
            RecipeGroup.RegisterGroup(evilBar, evilBar_);
            RecipeGroup.RegisterGroup(fairys, fairys_);
            RecipeGroup.RegisterGroup(IronBar, IronBar_);
        }


        public static void unload() {
            gems = null;
            butterflies = null;
            AnyOrichalcumBar = null;
            evilBar = null;
        }
    }
}
