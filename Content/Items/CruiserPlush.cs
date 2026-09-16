using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class CruiserPlush : ModItem
    {
        public override void Load() {
            if (Main.netMode != NetmodeID.Server) {
                EquipLoader.AddEquipTexture(Mod, "CalamityEntropy/Content/Items/CruiserPlush_Head", EquipType.Head, this);
            }
        }
        public override void SetStaticDefaults() {
            if (Main.netMode != NetmodeID.Server) {
                int equipSlotHead = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
                ArmorIDs.Head.Sets.DrawHead[equipSlotHead] = true;
            }
        }
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 28;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<CruiserPlushTile>();
            Item.rare = ItemRarityID.LightRed;
            Item.accessory = true;
            Item.vanity = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().CrPlush = !hideVisual;
        }

        public override void UpdateVanity(Player player) {
            player.Entropy().CrPlush = true;
        }

    }
}
