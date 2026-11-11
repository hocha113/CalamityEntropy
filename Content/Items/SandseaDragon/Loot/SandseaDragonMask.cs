using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SandseaDragon.Loot
{
    [AutoloadEquip(EquipType.Head)]
    public class SandseaDragonMask : ModItem
    {
        public override void SetStaticDefaults() {
            //整头面具,胡须饰品不画(原版 Boss 面具同)
            ArmorIDs.Head.Sets.PreventBeardDraw[Item.headSlot] = true;
        }

        public override void SetDefaults() {
            Item.width = 34;
            Item.height = 30;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 75); //同原版 Boss 面具
            Item.vanity = true;
        }
    }
}
