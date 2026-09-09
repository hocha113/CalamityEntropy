using CalamityEntropy.Content.NPCs.LuminarisMoth;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class IllusionaryDew : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
        }
        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 56;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.AbigailSummon;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.rare = ItemRarityID.Yellow;

        }
        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossItem;
        }

        public override bool CanUseItem(Player player)
        {
            //地点/夜晚门槛冻结。只补终焉之战互斥,不改回星辉群系
            return !NPC.AnyNPCs(ModContent.NPCType<Luminaris>()) && !Main.dayTime && !CECal.IsBossRushActive;
        }

        public override bool? UseItem(Player player)
        {
            int type = ModContent.NPCType<Luminaris>();
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.SpawnOnPlayer(player.whoAmI, type);
            else
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);

            return true;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_StarblightSoot))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_StarblightSoot, 6).
                AddIngredient(ItemID.FallenStar, 2).
                AddTile(TileID.WorkBenches).
                Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<StarlitScaleDust>(6)
                .AddIngredient(ItemID.HallowedBar, 4)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
