using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    // 虚空发信器:夜晚召唤虚空驱逐舰。生成/联机分发沿用 VoidBottle 的 SpawnOnPlayer 路径
    public class VoidTransmitter : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 17;
        }

        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 33;
            Item.useAnimation = 32;
            Item.useTime = 32;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = SoundID.Item92;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossItem;
        }

        public override bool CanUseItem(Player player) {
            return !Main.dayTime && !NPC.AnyNPCs(ModContent.NPCType<VoidDestroyer>()) && !CECal.IsBossRushActive;
        }

        public override bool? UseItem(Player player) {
            int type = ModContent.NPCType<VoidDestroyer>();
            //传送门音效由 Boss 出场首帧播放,这里不重复
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.SpawnOnPlayer(player.whoAmI, type);
            else
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe().
                AddIngredient(ItemID.LunarBar, 5).
                AddIngredient(ItemID.Wire, 10).
                AddIngredient(ModContent.ItemType<AzafureCircuitry>(), 5).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
