using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class BlackFlower : ModItem, IVanitySkin, IGetFromStarterBag
    {
        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Head", EquipType.Head, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Legs", EquipType.Legs, this);
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
            Item.width = 30;
            Item.height = 30;

            Item.accessory = true;
            Item.vanity = true;

            Item.value = Item.buyPrice(0, 1, 0, 0);

            Item.rare = ItemRarityID.Red;
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
            if (CECal.CalChainReady(CEID.Item_BloodOrb))
            {
                CreateRecipe()
                .AddIngredient(ItemID.Sunflower)
                .AddIngredient(CEID.Item_BloodOrb, 2)
                .AddIngredient(ItemID.Ruby)
                .AddCondition(Condition.InGraveyard)
                .AddTile(TileID.WorkBenches)
                .Register();
                return;
            }
            // 血珠按映射拆为脊椎骨/腐肉双平行配方
            CreateRecipe()
                .AddIngredient(ItemID.Sunflower)
                .AddIngredient(ItemID.Vertebrae, 2)
                .AddIngredient(ItemID.Ruby)
                .AddCondition(Condition.InGraveyard)
                .AddTile(TileID.WorkBenches)
                .Register();
            CreateRecipe()
                .AddIngredient(ItemID.Sunflower)
                .AddIngredient(ItemID.RottenChunk, 2)
                .AddIngredient(ItemID.Ruby)
                .AddCondition(Condition.InGraveyard)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public bool OwnAble(Player player, ref int count)
        {
            return StartBagGItem.NameContains(player, "tlipoca");
        }
    }
}
