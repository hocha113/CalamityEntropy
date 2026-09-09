using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class OverloadFurnace : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.useTime = 3;
            Item.useAnimation = 3;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<OverloadFurnaceHoldout>();
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.shootSpeed = 22f;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.mana = 6;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DubiousPlating, CEID.Item_MysteriousCircuitry))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_DubiousPlating, 5).
                AddIngredient(CEID.Item_MysteriousCircuitry, 2).
                AddIngredient(ItemID.Ruby, 1).
                AddTile(TileID.Anvils).
                Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.GoldBar, 15)
                .AddIngredient(ItemID.Ruby, 5)
                .AddIngredient(ItemID.ManaCrystal, 3)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.PlatinumBar, 15)
                .AddIngredient(ItemID.Ruby, 5)
                .AddIngredient(ItemID.ManaCrystal, 3)
                .AddTile(TileID.Anvils)
                .Register();
        }

        public override bool MagicPrefix()
        {
            return true;
        }
    }
}
