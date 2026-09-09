using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class NetherRift : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 42;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = new SoundStyle("CalamityEntropy/Assets/Sounds/ballandchainhit1");
            Item.damage = 720;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.knockBack = 2.5f;
            Item.channel = true;
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.value = Item.buyPrice(platinum: 6);

            Item.shoot = ModContent.ProjectileType<NetherRiftBlade>();
            Item.shootSpeed = 16f;
        }

        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_CrescentMoon, ItemID.BlueMoon)
                .AddIngredient<VoidBar>(5)
                .AddTile<VoidWellTile>()
                .Register();
        }
    }
}
