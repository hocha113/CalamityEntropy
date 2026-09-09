using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Wings)]
    public class IceShardWing : CEBaseWings
    {
        public static float HorSpeed = 7.6f;
        public static float AccMul = 1;
        public static int wTime = 180;
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(wTime, HorSpeed, AccMul, false, 20, 2.8f);
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 28;
            Item.height = 28;
            Item.value = Item.buyPrice(0, 12, 25);
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
            // 脱离灾厄:灾厄 donorItem 捐助者提示行随灾厄退场删除
        }
        public override float BonusAscentWhileFalling => 0.8f;
        public override float BonusAscentWhileRising => 0.1f;
        public override float RisingSpeedThreshold => 0.5f;
        public override float MaxAscentSpeed => 1.8f;
        public override float BaseAscent => 0.1f;
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.SoulofFlight, 20).
                AddCalOrOwn(CEID.Item_CryonicBar, ItemID.HallowedBar, 5).
                AddIngredient(ItemID.PurificationPowder, 5).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}