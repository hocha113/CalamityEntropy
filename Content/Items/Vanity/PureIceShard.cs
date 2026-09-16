using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Donator;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class PureIceShard : ModItem, IVanitySkin, IGetFromStarterBag, IDonatorItem
    {
        // 原灾厄 donorItem 旗标改走自有捐赠者提示通道，署名取自专属判定
        public string DonatorName => "Cirno";
        public override void Load() {
            if (Main.netMode != NetmodeID.Server) {
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Head", EquipType.Head, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Legs", EquipType.Legs, this);
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
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.vanity = true;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Yellow;
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
                .AddIngredient(ItemID.IceBlock, 30)
                .AddIngredient(ItemID.FallenStar, 2)
                .AddIngredient(ItemID.PurificationPowder, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public bool OwnAble(Player player, ref int count) {
            return StartBagGItem.NameContains(player, "cirno");
        }
    }
}
