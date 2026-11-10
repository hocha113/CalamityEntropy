using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SunMoonGods
{
    /// <summary>光阴符文,白天召太阳女神,夜晚召星月贤者,不消耗</summary>
    public class SolunarRune : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 18;
        }

        public override void SetDefaults() {
            Item.width = 50;
            Item.height = 46;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossItem;
        }

        public override bool CanUseItem(Player player) {
            return !NPC.AnyNPCs(ModContent.NPCType<SunGoddess>()) && !NPC.AnyNPCs(ModContent.NPCType<StarmoonSage>())
                && !CECal.IsBossRushActive;
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI != Main.myPlayer) {
                return true;
            }
            int type = Main.dayTime ? ModContent.NPCType<SunGoddess>() : ModContent.NPCType<StarmoonSage>();
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
                .AddIngredient<DimBearing>(5)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}