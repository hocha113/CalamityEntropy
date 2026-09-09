using CalamityEntropy.Content.NPCs.Prophet;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class ProphecyToken : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 15;
        }
        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 56;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.rare = ItemRarityID.Blue;

        }
        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossItem;
        }

        public override bool CanUseItem(Player player)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<TheProphet>()) && player.ZoneDungeon && !CECal.IsBossRushActive;
        }

        public override bool? UseItem(Player player)
        {
            int type = ModContent.NPCType<TheProphet>();
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.SpawnOnPlayer(player.whoAmI, type);
            else
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);

            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Book, 6)
                .AddIngredient(ItemID.SoulofLight, 15)
                .AddIngredient(ItemID.Ectoplasm, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
