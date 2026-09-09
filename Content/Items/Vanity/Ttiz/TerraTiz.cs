using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Vanity.Ttiz
{
    public class TerraTiz : ModItem, IVanitySkin, IGetFromStarterBag, IDonatorItem
    {
        public string DonatorName => "鈴夕";

        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/Ttiz/{Name}_Head", EquipType.Head, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/Ttiz/{Name}_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/Ttiz/{Name}_Legs", EquipType.Legs, this);
            }
        }

        public override void SetStaticDefaults()
        {
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

        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 46;
            Item.accessory = true;
            Item.vanity = true;
            Item.value = Item.buyPrice(0, 3, 0, 0);
            Item.rare = ModContent.RarityType<ShiningViolet>();
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<VanityModPlayer>().vanityEquipped = Name;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
            {
                player.GetModPlayer<VanityModPlayer>().vanityEquipped = Name;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_NightmareFuel, ItemID.SpookyWood)
                .AddIngredient(ItemID.GoldBar, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public bool OwnAble(Player player, ref int count)
        {
            string s = PGetPlayer.RemoveCharAndToLower(player.name);
            return s == "yumekibou" || s == "ilta";
        }
    }
}
