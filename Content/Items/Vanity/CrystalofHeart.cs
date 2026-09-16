using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class CrystalofHeart : ModItem, IVanitySkin
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
            Item.width = 28;
            Item.height = 26;
            Item.accessory = true;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.vanity = true;
            Item.Entropy().tooltipStyle = 8;
            Item.Entropy().strokeColor = new Color(255, 154, 127);
            Item.Entropy().NameColor = new Color(40, 12, 10);
            Item.Entropy().NameLightColor = Color.Red * 0.7f;
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
                .AddIngredient(ItemID.LifeCrystal)
                .AddIngredient(ItemID.LargeRuby)
                .AddIngredient(ItemID.FallenStar, 5)
                .Register();
        }
    }
}
