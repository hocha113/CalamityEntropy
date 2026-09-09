using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.Marivinium
{
    [AutoloadEquip(EquipType.Body)]
    public class MariviniumBodyArmor : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 18;
            Item.value = Item.buyPrice(platinum: 2, gold: 80);
            Item.defense = 50;
            Item.rare = ModContent.RarityType<AbyssalBlue>();
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().mariviniumBody = true;
            player.GetDamage(DamageClass.Generic) += 0.15f;
            player.GetCritChance(DamageClass.Generic) += 5;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_OmegaBlueChestplate, ItemID.HallowedPlateMail)
                .AddIngredient<WyrmTooth>(6)
                .AddIngredient<FadingRunestone>()
                .AddTile<AbyssalAltarTile>()
                .Register();
        }
    }
}
