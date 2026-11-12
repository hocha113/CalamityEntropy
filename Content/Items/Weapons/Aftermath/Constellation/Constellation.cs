using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.Constellation
{
    //悠悠球, 本体命中召唤跟随的五芒星并持续发射追踪魔法星
    public class Constellation : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.Yoyo[Type] = true;
            ItemID.Sets.GamepadExtraRange[Type] = 35;
            ItemID.Sets.GamepadSmartQuickReach[Type] = true;
        }

        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.damage = 150;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.knockBack = 3f;
            Item.channel = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 25);
            Item.shoot = ModContent.ProjectileType<ConstellationYoyo>();
            Item.shootSpeed = 16f;
        }

        public override bool MeleePrefix() => true;

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Chik)
                .AddIngredient<WraithSoulEssence>(10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
