using CalamityEntropy;
using CalamityEntropy.Content.ArmorPrefixes;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.PrefixItem
{
    // Blessing Prefixes
    public class BlessingVoid : BasePrefixItem
    {
        public override string PrefixName => "Void";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_NightmareFuel))
            {
                CreateRecipe().
                AddIngredient<VoidScales>(1).
                AddIngredient(CEID.Item_NightmareFuel, 2)
                .Register();
                return;
            }
            CreateRecipe().
            AddIngredient<VoidScales>(1).
            AddIngredient(ItemID.SpookyWood, 2)
            .Register();
        }
    }
    public class BlessingVoidTouched : BasePrefixItem
    {
        public override string PrefixName => "VoidTouched";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_AscendantSpiritEssence))
            {
                CreateRecipe().
                AddIngredient<VoidScales>(2).
                AddIngredient(CEID.Item_AscendantSpiritEssence, 1)
                .Register();
                return;
            }
            CreateRecipe().
                AddIngredient<VoidScales>(2).
                AddIngredient<WraithSoulEssence>(1)
                .Register();
        }
    }
    public class BlessingLastStand : BasePrefixItem
    {
        public override string PrefixName => "LastStand";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_YharonSoulFragment, CEID.Item_EffulgentFeather, CEID.Item_AshesofAnnihilation, CEID.Item_ExoPrism))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_YharonSoulFragment, 10).
                AddIngredient(CEID.Item_EffulgentFeather, 10).
                AddIngredient(CEID.Item_AshesofAnnihilation, 2)
                .AddIngredient(CEID.Item_ExoPrism, 2)
                .Register();
                return;
            }
            // 脱离灾厄:YharonSoulFragment/AshesofAnnihilation 均映射虚空之鳞,合并数量
            CreateRecipe().
                AddIngredient<VoidScales>(12).
                AddIngredient<NihilityFragments>(10)
                .AddIngredient<VoidBar>(2)
                .Register();
        }
    }
    public class BlessingEnd : BasePrefixItem
    {
        public override string PrefixName => "End";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_YharonSoulFragment, CEID.Item_AshesofAnnihilation, CEID.Item_ExoPrism))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_YharonSoulFragment, 1)
                .AddIngredient(CEID.Item_AshesofAnnihilation, 1)
                .AddIngredient(CEID.Item_ExoPrism, 1)
                .Register();
                return;
            }
            // 脱离灾厄:YharonSoulFragment/AshesofAnnihilation 均映射虚空之鳞,合并数量
            CreateRecipe().
                AddIngredient<VoidScales>(2)
                .AddIngredient<VoidBar>(1)
                .Register();
        }
    }
    public class BlessingHeatDeath : BasePrefixItem
    {
        public override string PrefixName => "HeatDeath";
    }

    // RuneStone Prefixes
    public class RuneStoneShining : BasePrefixItem
    {
        public override string PrefixName => "Shining";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddIngredient(ItemID.Torch, 5)
                .AddIngredient(ItemID.CopperBar, 3)
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.Torch, 5)
                .AddIngredient(ItemID.CopperBar, 3)
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
        }
    }
    public class RuneStoneSilence : BasePrefixItem
    {
        public override string PrefixName => "Silence";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_BloodOrb))
            {
                CreateRecipe().AddIngredient(ItemID.DemoniteBar, 1)
                .AddIngredient(CEID.Item_BloodOrb, 5)
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
                CreateRecipe().AddIngredient(ItemID.CrimtaneBar, 1)
                .AddIngredient(CEID.Item_BloodOrb, 5)
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
                return;
            }
            // 脱离灾厄:BloodOrb 按 material-map 拆双配方,腐化用腐肉、猩红用脊椎骨
            CreateRecipe().AddIngredient(ItemID.DemoniteBar, 1)
                .AddIngredient(ItemID.RottenChunk, 5)
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
            CreateRecipe().AddIngredient(ItemID.CrimtaneBar, 1)
                .AddIngredient(ItemID.Vertebrae, 5)
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
        }
    }
    public class RuneStoneHard : BasePrefixItem
    {
        public override string PrefixName => "Hard";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddRecipeGroup(CERecipeGroups.IronBar, 20)
                .AddIngredient(ItemID.Diamond, 2)
                .Register();
                return;
            }
            CreateRecipe().AddRecipeGroup(CERecipeGroups.IronBar, 20)
                .AddIngredient(ItemID.Diamond, 2)
                .Register();
        }
    }
    public class RuneStoneThorny : BasePrefixItem
    {
        public override string PrefixName => "Thorny";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddIngredient(ItemID.Cactus, 8)
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.Cactus, 8)
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
        }
    }
    public class RuneStoneLight : BasePrefixItem
    {
        public override string PrefixName => "Light";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddIngredient(ItemID.Feather, 4)
                .AddIngredient(ItemID.Cloud, 10)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.Feather, 4)
                .AddIngredient(ItemID.Cloud, 10)
                .Register();
        }
    }
    public class RuneStoneBiochemistry : BasePrefixItem
    {
        public override string PrefixName => "Biochemistry";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_CorrodedFossil))
            {
                CreateRecipe().AddIngredient(ItemID.StoneBlock, 10).
                AddIngredient(CEID.Item_CorrodedFossil, 5)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.StoneBlock, 10).
                AddIngredient(ItemID.FossilOre, 5)
                .Register();
        }
    }
    public class RuneStoneGuarded : BasePrefixItem
    {
        public override string PrefixName => "Guarded";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddIngredient(ItemID.StoneBlock, 10).
                AddRecipeGroup(CERecipeGroups.IronBar, 5)
                .AddIngredient(ItemID.TurtleShell)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.StoneBlock, 10).
                AddRecipeGroup(CERecipeGroups.IronBar, 5)
                .AddIngredient(ItemID.TurtleShell)
                .Register();
        }
    }
    public class RuneStoneRegen : BasePrefixItem
    {
        public override string PrefixName => "Regen";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddIngredient(ItemID.StoneBlock, 10).
                   AddIngredient(ItemID.LifeCrystal, 1)
                   .AddIngredient(ItemID.CopperBar, 5)
                   .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.StoneBlock, 10).
                   AddIngredient(ItemID.LifeCrystal, 1)
                   .AddIngredient(ItemID.CopperBar, 5)
                   .Register();
        }
    }

    // EnchantedScroll Prefixes
    public class EnchantedScrollMassive : BasePrefixItem
    {
        public override string PrefixName => "Massive";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(ItemID.LunarTabletFragment)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(ItemID.LunarTabletFragment)
                .Register();
        }
    }
    public class EnchantedScrollEvoker : BasePrefixItem
    {
        public override string PrefixName => "Evoker";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_LivingShard))
            {
                CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(CEID.Item_LivingShard, 10)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(ItemID.ChlorophyteBar, 10)
                .Register();
        }
    }
    public class EnchantedScrollReckless : BasePrefixItem
    {
        public override string PrefixName => "Reckless";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_EssenceofHavoc))
            {
                CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(CEID.Item_EssenceofHavoc, 2)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(ItemID.SoulofNight, 2)
                .Register();
        }
    }
    public class EnchantedScrollMiracle : BasePrefixItem
    {
        public override string PrefixName => "Miracle";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(ItemID.HallowedBar, 5)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(ItemID.HallowedBar, 5)
                .Register();
        }
    }
    public class EnchantedScrollMagical : BasePrefixItem
    {
        public override string PrefixName => "Magical";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady())
            {
                CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(ItemID.FallenStar, 10)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Ectoplasm)
                .AddIngredient(ItemID.FallenStar, 10)
                .Register();
        }
    }

    // OriginGem Prefixes
    public class OriginGemGreat : BasePrefixItem
    {
        public override string PrefixName => "Great";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_ExodiumCluster, CEID.Item_UnholyEssence))
            {
                CreateRecipe().AddIngredient(CEID.Item_ExodiumCluster, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient(CEID.Item_UnholyEssence, 4)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.LunarOre, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient<NihilityFragments>(4)
                .Register();
        }
    }
    public class OriginGemGodForged : BasePrefixItem
    {
        public override string PrefixName => "GodForged";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_ExodiumCluster, CEID.Item_CosmiliteBar))
            {
                CreateRecipe().AddIngredient(CEID.Item_ExodiumCluster, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient(CEID.Item_CosmiliteBar, 1)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.LunarOre, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient<NihilityFragments>(5)
                .Register();
        }
    }
    public class OriginGemWizard : BasePrefixItem
    {
        public override string PrefixName => "Wizard";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_ExodiumCluster, CEID.Item_RuinousSoul))
            {
                CreateRecipe().AddIngredient(CEID.Item_ExodiumCluster, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient(CEID.Item_RuinousSoul)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.LunarOre, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient<NihilityFragments>()
                .Register();
        }
    }
    public class OriginGemSacrifical : BasePrefixItem
    {
        public override string PrefixName => "Sacrifical";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_ExodiumCluster, CEID.Item_DivineGeode))
            {
                CreateRecipe().AddIngredient(CEID.Item_ExodiumCluster, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient(CEID.Item_DivineGeode, 4)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.LunarOre, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient<NihilityFragments>(4)
                .Register();
        }
    }
    public class OriginGemDestinedGreatness : BasePrefixItem
    {
        public override string PrefixName => "DestinedGreatness";
        public override void AddRecipes()
        {

            if (!ArmorPrefix.Enabled)
                return;
            if (CECal.CalChainReady(CEID.Item_ExodiumCluster, CEID.Item_Necroplasm))
            {
                CreateRecipe().AddIngredient(CEID.Item_ExodiumCluster, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient(CEID.Item_Necroplasm)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.LunarOre, 5)
                .AddIngredient(ItemID.Glass, 5)
                .AddIngredient<NihilityFragments>()
                .Register();
        }
    }
}
