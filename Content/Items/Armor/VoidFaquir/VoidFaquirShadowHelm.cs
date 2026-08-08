using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    [AutoloadEquip(EquipType.Head)]
    public class VoidFaquirShadowHelm : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.defense = 34;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<VoidFaquirBodyArmor>() && legs.type == ModContent.ItemType<VoidFaquirCuises>();
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawOutlines = true;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetArmorPenetration(DamageClass.Generic) += 20;
            player.Entropy().shootSpeed += 1.0f;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.32f;
            player.GetCritChance(DamageClass.Ranged) += 30;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 14)
                .AddIngredient(ModContent.ItemType<TwistingNether>(), 4)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
