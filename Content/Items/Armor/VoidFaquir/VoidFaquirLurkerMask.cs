using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    [AutoloadEquip(EquipType.Head)]
    public class VoidFaquirLurkerMask : ModItem
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
            Item.defense = 30;
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

            player.Calamity().wearingRogueArmor = true;
            player.GetArmorPenetration(DamageClass.Generic) += 20;
            player.Calamity().rogueStealthMax += 1.35f;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(CEUtils.RogueDC) += 0.25f;
            player.GetCritChance(CEUtils.RogueDC) += 25;
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
