using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Items.VoidSlime
{
    /// <summary>星冕,召唤虚空史莱姆,不消耗</summary>
    public class StarCrown : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 19;
        }

        public override void SetDefaults() {
            Item.width = 42;
            Item.height = 24;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossItem;
        }

        public override bool CanUseItem(Player player) {
            return !NPC.AnyNPCs(ModContent.NPCType<VoidSlimeNPC>()) && !CECal.IsBossRushActive;
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI != Main.myPlayer) {
                return true;
            }
            SoundEngine.PlaySound(SoundID.Roar, player.Center);
            int type = ModContent.NPCType<VoidSlimeNPC>();
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                NPC.SpawnOnPlayer(player.whoAmI, type);
            }
            else {
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
            }
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<NihilityFragments>(5)
                .AddIngredient(ItemID.LunarBar, 5)
                .AddIngredient(ItemID.FallenStar, 20)
                .AddIngredient(ItemID.Gel, 50)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    /// <summary>超新星凝胶,虚空史莱姆和彗星史莱姆掉的材料</summary>
    public class SupernovaGel : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults() {
            Item.width = 20;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.6f);

        public override void PostUpdate() {
            Lighting.AddLight(Item.Center, 0.36f, 0.2f, 0.56f);
        }
    }

    /// <summary>星河固化机,超新星凝胶系物品的制作站,同时等同远古操纵机、秘银砧与固化机</summary>
    public class GalaxySolidifier : ModItem
    {
        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<GalaxySolidifierTile>());
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 24);
            Item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.LunarCraftingStation)
                .AddRecipeGroup(CERecipeGroups.AnyMythrilAnvil)
                .AddIngredient(ItemID.Solidifier)
                .AddIngredient<SupernovaGel>(10)
                .Register();
        }
    }

    public class VoidSlimeTrophy : ModItem
    {
        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<VoidSlimeTrophyTile>());
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 9999;
            Item.value = 50000;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
    }

    public class VoidSlimeRelic : ModItem
    {
        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<VoidSlimeRelicTile>(), 0);
            Item.width = 30;
            Item.height = 40;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.value = Item.buyPrice(0, 5);
        }
    }

    /// <summary>首杀传记,按住 Shift 看全文,没有挂效果</summary>
    public class VoidSlimeLore : CELoreItem
    {
        public override void SetDefaults() {
            Item.width = 38;
            Item.height = 26;
            Item.maxStack = 1;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
    }
}
