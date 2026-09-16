using CalamityEntropy.Content.NPCs.FriendFinderNPC;
using CalamityEntropy.Core.Cooldowns;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class FriendFinder : ModItem
    {
        public static List<int> summonList;
        public static int CooldownSec = CEUtils.SecondsToFrames(20);
        public override void SetStaticDefaults() {
            summonList = new List<int>() { ModContent.NPCType<AeroSlimeFriendly>(), ModContent.NPCType<DespairStoneFriendly>(), ModContent.NPCType<IceClasperFriendly>(), ModContent.NPCType<ScryllarFriendly>(), ModContent.NPCType<SkyfinFriendly>(), ModContent.NPCType<SoulSlurperFriendly>() };
        }

        public override void SetDefaults() {
            Item.width = 62;
            Item.height = 70;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.noMelee = true;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.scale = 0.6f;
        }
        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2)
                return true;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return !player.HasCooldown("FriendfinderCd");

            return !(player.Entropy().ffinderCd > 0);
        }
        public override bool AltFunctionUse(Player player) {
            return true;
        }

        public override bool? UseItem(Player player) {
            if (player.altFunctionUse == 2) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    foreach (NPC npc in Main.ActiveNPCs) {
                        if (npc.ModNPC is FriendFindNPC && npc.Entropy().friendFinderOwner == player.whoAmI) {
                            npc.active = false;
                            if (Main.dedServ) {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                            }
                        }
                    }
                }
                return true;
            }
            if (!Main.dedServ)
                player.AddCooldown("FriendfinderCd", CooldownSec);
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                return true;
            }
            int n = NPC.NewNPC(player.GetSource_FromAI(), (int)player.position.X, (int)player.position.Y, summonList[Main.rand.Next(0, summonList.Count)]);
            n.ToNPC().localAI[3] = player.whoAmI + 1;
            n.ToNPC().Center = player.Center - new Vector2(0, 60);
            if (Main.dedServ) {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
            }
            player.Entropy().ffinderCd = (int)(CooldownSec * player.Entropy().CooldownTimeMult);

            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.GoldBar, 10)
                .AddIngredient(ItemID.Ruby, 10)
                .AddIngredient(ItemID.Bone, 10)
                .AddIngredient(ItemID.LifeCrystal)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
