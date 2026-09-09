using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    [AutoloadEquip(EquipType.Head)]
    public class VoidFaquirCosmosHood : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.defense = 32;
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

        // 2026-08-31 平衡案:职业专属奖励=魔力病持续减半、攻击敌人大幅提升自然生命再生(5hp/s,5秒)
        public override void UpdateArmorSet(Player player)
        {
            player.Entropy().VFSet = true;
            player.Entropy().VFHelmMagic = true;
            player.Entropy().halfManaSick = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().magiVF = true;
            player.GetDamage(DamageClass.Magic) += 0.23f;
            player.GetCritChance(DamageClass.Magic) += 18;
            player.statLifeMax2 += 80;
            player.statManaMax2 += 100;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_TwistingNether))
            {
                CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 14)
                .AddIngredient(CEID.Item_TwistingNether, 4)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 12)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
