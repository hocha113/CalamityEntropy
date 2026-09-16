using CalamityEntropy.Common;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class TheocracyMark : ModItem, IVanitySkin
    {
        public override void Load() {
            if (Main.netMode != NetmodeID.Server) {
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Legs", EquipType.Legs, this);
            }
        }

        public override void SetStaticDefaults() {
            if (Main.netMode == NetmodeID.Server)
                return;


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

            Item.value = Item.buyPrice(0, 25, 0, 0);

            Item.rare = ModContent.RarityType<Golden>();
        }

        public override void UpdateVanity(Player player) {
            player.GetModPlayer<VanityModPlayer>().TheocracyMark = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            if (!hideVisual) {
                player.GetModPlayer<VanityModPlayer>().TheocracyMark = true;
            }
        }

        public override void AddRecipes() {

        }
    }
}
