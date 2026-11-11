using CalamityEntropy.Content.NPCs.SandseaDragon;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SandseaDragon
{
    /// <summary>
    /// 沙暴吊坠,不消耗:左键在沙漠或地下沙漠召唤沙海冥龙,右键召来沙尘暴
    /// 沙尘暴是世界状态,只由权威端开;联机客户端发一个空载荷的请求,服务器校验后执行
    /// 本体在场时两个功能都不可用,战斗中的天气归本体管
    /// </summary>
    public class SandstormPendant : ModItem
    {
        public override void SetStaticDefaults() {
            //原版丛林蜥蜴电池 16,天界符 19,这里排在两者之间
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 17;
        }

        public override void SetDefaults() {
            Item.width = 36;
            Item.height = 50;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item44;
            Item.consumable = false;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossItem;
        }

        public override bool AltFunctionUse(Player player) => true;

        private static bool BossAlive() => NPC.AnyNPCs(ModContent.NPCType<SandseaDragonHead>());

        private static bool InDesert(Player player) => player.ZoneDesert || player.ZoneUndergroundDesert;

        public override bool CanUseItem(Player player) {
            if (BossAlive()) {
                return false;
            }
            if (player.altFunctionUse == 2) {
                return !Sandstorm.Happening;
            }
            return InDesert(player) && !CECal.IsBossRushActive;
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI != Main.myPlayer) {
                return true;
            }
            if (player.altFunctionUse == 2) {
                if (Main.netMode == NetmodeID.MultiplayerClient) {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)CEMessageType.RequestSandstorm);
                    packet.Send();
                }
                else {
                    SandseaWeather.Start();
                }
                return true;
            }

            int type = ModContent.NPCType<SandseaDragonHead>();
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                NPC.SpawnOnPlayer(player.whoAmI, type);
            }
            else {
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
            }
            return true;
        }

        /// <summary>服务器收到右键请求:条件与客户端的 CanUseItem 一致,再校验一遍</summary>
        internal static void ReceiveSandstormRequest() {
            if (Main.netMode != NetmodeID.Server || BossAlive() || Sandstorm.Happening) {
                return;
            }
            SandseaWeather.Start();
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.SandBlock, 40)
                .AddIngredient(ItemID.SharkFin, 5)
                .AddIngredient(ItemID.Ectoplasm, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
