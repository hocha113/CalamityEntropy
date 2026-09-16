using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class MikoMaeTengkan : ModItem, IVanitySkin
    {
        public override void Load() {
            if (Main.netMode != NetmodeID.Server) {
                EquipLoader.AddEquipTexture(Mod, $"CalamityEntropy/Content/Items/Vanity/{Name}_Head", EquipType.Head, this);
                EquipLoader.AddEquipTexture(Mod, $"CalamityEntropy/Content/Items/Vanity/{Name}_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, $"CalamityEntropy/Content/Items/Vanity/{Name}_Legs", EquipType.Legs, this);
            }
        }

        public override void SetStaticDefaults() {
            if (Main.netMode == NetmodeID.Server)
                return;
            int equipSlotHead = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlotHead] = false;
            int equipSlotBody = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlotBody] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlotBody] = true;
            int equipSlotLegs = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlotLegs] = true;
        }

        public override void SetDefaults() {
            Item.width = 34;
            Item.height = 34;
            Item.accessory = true;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.vanity = true;
            Item.Entropy().tooltipStyle = 8;
            Item.Entropy().strokeColor = new Color(255, 180, 212);
            Item.Entropy().NameColor = new Color(120, 80, 110);
            Item.Entropy().NameLightColor = Color.Pink * 0.6f;
        }

        public override void UpdateVanity(Player player) {
            player.GetModPlayer<VanityModPlayer>().vanityEquipped = Name;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            if (!hideVisual) {
                player.GetModPlayer<VanityModPlayer>().vanityEquipped = Name;
            }
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.GoldBar, 8)
                .AddIngredient(ItemID.Topaz, 6)
                .AddIngredient(ItemID.Silk, 2)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe()
                .AddIngredient(ItemID.PlatinumBar, 8)
                .AddIngredient(ItemID.Topaz, 6)
                .AddIngredient(ItemID.Silk, 2)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
