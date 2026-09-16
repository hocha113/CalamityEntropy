using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Vanity.KM;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class KitsunesFan : ModItem, IDonatorItem, IVanitySkin
    {
        public string DonatorName => "黯月殇梦";

        public override void Load() {
            if (Main.netMode != NetmodeID.Server) {
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/KM/KitsunesMask_Head", EquipType.Head, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/KM/BloodstainedShirt_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/KM/ScarletKilt_Legs", EquipType.Legs, this);
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

            Item.rare = ItemRarityID.Green;
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
                .AddIngredient<BloodstainedShirt>()
                .AddIngredient<KitsunesMask>()
                .AddIngredient<ScarletKilt>()
                .Register();
        }
    }
}
