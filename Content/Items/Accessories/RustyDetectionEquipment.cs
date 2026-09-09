using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class RustyDetectionEquipment : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 46;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
        }
        public static string ID = "RustyDetectorEquipment";

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.rocketBoots += 30;
            player.noFallDmg = true;
            player.jumpSpeedBoost += 0.5f;
            player.maxRunSpeed *= 1.10f;
            player.Entropy().addEquip(ID, !hideVisual);
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().addEquipVisual(ID);
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddCalOrOwn(CEID.Item_DubiousPlating, ModContent.ItemType<AzafurePlating>(), 20).
                AddCalOrOwn(CEID.Item_MysteriousCircuitry, ModContent.ItemType<AzafureCircuitry>(), 15).
                //脱离灾厄:灾厄可疑废料按material-map废料族规则换铁锭
                AddCalOrOwn(CEID.Item_SuspiciousScrap, ItemID.IronBar, 1).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
