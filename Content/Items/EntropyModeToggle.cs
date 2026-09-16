using CalamityEntropy.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class EntropyModeToggle : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            ItemID.Sets.ItemNoGravity[Item.type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(4, 10));
        }
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.noMelee = true;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Purple;
            Item.scale = 0.6f;
            Item.UseSound = CEUtils.GetSound("soul");
        }
        public override bool? UseItem(Player player) {
            if (player.altFunctionUse == 2) {
                EDownedBosses.TDR = !EDownedBosses.TDR;
                Main.NewText(EDownedBosses.TDR ? "Enabled TDR" : "Disabled TDR");
                return true;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient) {

                CalamityEntropy.EntropyMode = !CalamityEntropy.EntropyMode;

                if (Main.netMode == NetmodeID.SinglePlayer) {
                    if (CalamityEntropy.EntropyMode) {
                        Main.NewText(Mod.GetLocalization("EntropyModeActive").Value, new Color(170, 18, 225));
                    }
                    else {
                        Main.NewText(Mod.GetLocalization("EntropyModeDeactive").Value, new Color(170, 18, 225));
                    }
                }
                else {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)CEMessageType.SyncEntropyMode);
                    packet.Write(CalamityEntropy.EntropyMode);
                    packet.Send();
                }
            }
            return true;
        }
        public override bool AltFunctionUse(Player player) {
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.FallenStar, 1)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}
