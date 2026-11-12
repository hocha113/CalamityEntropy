using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.VanillaTweaks
{
    /// <summary>给原版物品追加的配方:药水、生命果、幸运马掌、闪亮红气球、云朵瓶、天界星盘;改写天顶剑的配方</summary>
    public class VanillaExtraRecipes : ModSystem
    {
        public const string GoldBarGroup = "CalamityEntropy:AnyGoldBar";

        //水瓶+星辉鳞尘配方白名单,Dust 为鳞尘数
        private static readonly (int Type, int Dust)[] DustPotions = {
            (ItemID.RestorationPotion, 2),
            (ItemID.ObsidianSkinPotion, 1),
            (ItemID.RegenerationPotion, 1),
            (ItemID.GillsPotion, 1),
            (ItemID.SwiftnessPotion, 1),
            (ItemID.IronskinPotion, 1),
            (ItemID.MagicPowerPotion, 2),
            (ItemID.ManaRegenerationPotion, 2),
            (ItemID.FeatherfallPotion, 1),
            (ItemID.SpelunkerPotion, 1),
            (ItemID.ShinePotion, 1),
            (ItemID.NightOwlPotion, 1),
            (ItemID.InvisibilityPotion, 1),
            (ItemID.BattlePotion, 1),
            (ItemID.ThornsPotion, 1),
            (ItemID.ArcheryPotion, 1),
            (ItemID.WaterWalkingPotion, 1),
            (ItemID.GravitationPotion, 1),
            (ItemID.HunterPotion, 1),
            (ItemID.HeartreachPotion, 2),
            (ItemID.CalmingPotion, 1),
            (ItemID.BuilderPotion, 1),
            (ItemID.MiningPotion, 1),
            (ItemID.TitanPotion, 2),
            (ItemID.FlipperPotion, 1),
            (ItemID.SummoningPotion, 2),
            (ItemID.TrapsightPotion, 1),
            (ItemID.AmmoReservationPotion, 2),
            (ItemID.RagePotion, 2),
            (ItemID.WrathPotion, 2),
            (ItemID.LifeforcePotion, 2),
            (ItemID.EndurancePotion, 2),
            (ItemID.InfernoPotion, 2),
            (ItemID.WarmthPotion, 1),
            (ItemID.RecallPotion, 1),
            (ItemID.TeleportationPotion, 2),
            (ItemID.FishingPotion, 1),
            (ItemID.CratePotion, 1),
            (ItemID.SonarPotion, 1),
            (ItemID.WormholePotion, 1),
            (ItemID.PotionOfReturn, 2),
            (ItemID.BiomeSightPotion, 2),
            (ItemID.LuckPotion, 2),
        };

        public override void AddRecipeGroups() {
            RecipeGroup gold = new RecipeGroup(() => Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.GoldBar), ItemID.GoldBar, ItemID.PlatinumBar);
            RecipeGroup.RegisterGroup(GoldBarGroup, gold);
        }

        public override void AddRecipes() {
            AddPotionRecipes();

            Recipe.Create(ItemID.LifeFruit)
                .AddIngredient<StarlitScaleDust>(10)
                .AddIngredient(ItemID.ChlorophyteBar, 5)
                .AddTile(TileID.CookingPots)
                .Register();

            Recipe.Create(ItemID.LuckyHorseshoe)
                .AddRecipeGroup(GoldBarGroup, 20)
                .AddRecipeGroup(RecipeGroupID.IronBar, 20)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.ShinyRedBalloon)
                .AddIngredient(ItemID.Cloud, 30)
                .AddIngredient(ItemID.WhiteString)
                .AddRecipeGroup(CERecipeGroups.evilBar_, 15)
                .AddTile(TileID.Solidifier)
                .Register();

            Recipe.Create(ItemID.CloudinaBottle)
                .AddIngredient(ItemID.Bottle)
                .AddIngredient(ItemID.Cloud, 30)
                .AddIngredient(ItemID.RainCloud, 30)
                .AddTile(TileID.Solidifier)
                .Register();

            Recipe.Create(ItemID.LongRainbowTrailWings)
                .AddIngredient<SupernovaGel>(5)
                .AddTile<GalaxySolidifierTile>()
                .Register();
        }

        public override void PostAddRecipes() {
            int gel = ModContent.ItemType<SupernovaGel>();
            int station = ModContent.TileType<GalaxySolidifierTile>();
            for (int i = 0; i < Recipe.numRecipes; i++) {
                Recipe recipe = Main.recipe[i];
                if (recipe.createItem.type != ItemID.Zenith) {
                    continue;
                }
                recipe.AddIngredient(gel, 8);
                recipe.requiredTile.Clear();
                recipe.AddTile(station);
            }
        }

        private static void AddPotionRecipes() {
            foreach ((int type, int dust) in DustPotions) {
                Recipe.Create(type)
                    .AddIngredient(ItemID.BottledWater)
                    .AddIngredient<StarlitScaleDust>(dust)
                    .AddTile(TileID.AlchemyTable)
                    .Register();
            }
        }
    }
}
