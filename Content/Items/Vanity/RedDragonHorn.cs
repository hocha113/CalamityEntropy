using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class RedDragonHorn : ModItem, IVanitySkin, IGetFromStarterBag
    {
        public bool OwnAble(Player player, ref int count)
        {
            return StartBagGItem.NameContains(player, "yevna");
        }

        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                EquipLoader.AddEquipTexture(Mod, $"CalamityEntropy/Content/Items/Vanity/{Name}_Head", EquipType.Head, this);
                EquipLoader.AddEquipTexture(Mod, $"CalamityEntropy/Content/Items/Vanity/{Name}_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, $"CalamityEntropy/Content/Items/Vanity/{Name}_Legs", EquipType.Legs, this);
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
            Item.width = 22;
            Item.height = 24;
            Item.accessory = true;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Red;
            Item.vanity = true;
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
            if (CECal.CalChainReady(CEID.Item_AncientBoneDust, CEID.Item_BloodOrb))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_AncientBoneDust, 6)
                .AddIngredient(CEID.Item_BloodOrb, 2)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            // 血珠按映射拆为脊椎骨/腐肉双平行配方
            CreateRecipe()
                .AddIngredient(ItemID.Bone, 6)
                .AddIngredient(ItemID.Vertebrae, 2)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe()
                .AddIngredient(ItemID.Bone, 6)
                .AddIngredient(ItemID.RottenChunk, 2)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
