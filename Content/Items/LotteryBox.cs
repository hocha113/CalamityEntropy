using CalamityEntropy.Content.NPCs;
using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items
{
    public class LotteryBox : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 84;
            Item.height = 48;
            Item.maxStack = 1;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Green;
            Item.shoot = ModContent.ProjectileType<LMSpawner>();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                foreach (NPC npc in Main.npc)
                {
                    if (npc.type == ModContent.NPCType<LotteryMachine>())
                    {
                        npc.active = false;
                        npc.netUpdate = true;
                    }
                }
                return true;
            }
            return false;
        }
        public override bool? UseItem(Player player)
        {

            if (Main.netMode == NetmodeID.Server)
            {
                foreach (NPC npc in Main.npc)
                {
                    if (npc.type == ModContent.NPCType<LotteryMachine>())
                    {
                        npc.active = false;
                        npc.netUpdate = true;
                    }
                }
                int np = NPC.NewNPC(player.GetSource_FromAI(), (int)player.Center.X, (int)player.Center.Y, ModContent.NPCType<LotteryMachine>());
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, np);
            }
            return true;
        }


        public override void AddRecipes()
        {
            CreateRecipe().
                AddCalOrOwn(CEID.Item_DubiousPlating, ModContent.ItemType<AzafurePlating>(), 3).
                AddCalOrOwn(CEID.Item_MysteriousCircuitry, ModContent.ItemType<AzafureCircuitry>(), 4).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }
}
