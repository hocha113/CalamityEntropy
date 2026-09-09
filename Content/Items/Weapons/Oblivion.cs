using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Oblivion : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 80;
            Item.damage = 64;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;
            Item.useAnimation = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.rare = ModContent.RarityType<NihilityBlue>();
            Item.shoot = ModContent.ProjectileType<OblivionArrow>();
            Item.UseSound = new Terraria.Audio.SoundStyle("CalamityEntropy/Assets/Sounds/feathershot") { MaxInstances = 60, Volume = 0.3f, PitchRange = (0.8f, 1f) };
            Item.shootSpeed = 16f;
            Item.useAmmo = AmmoID.Arrow;
            Item.autoReuse = true;
            Item.ArmorPenetration = 50;
            Item.noUseGraphic = true;
            Item.crit = 8;
        }
        public bool cs = false;
        public override bool RangedPrefix()
        {
            return true;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return Main.rand.NextBool(12);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            type = ModContent.ProjectileType<OblivionArrow>();
            Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.32f), type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Voidstone))
            {
                CreateRecipe()
                .AddIngredient(ModContent.ItemType<Kinanition>())
                .AddIngredient(CEID.Item_Voidstone, 6)
                .AddIngredient(ModContent.ItemType<ChaoticPiece>(), 6)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Kinanition>()
                .AddIngredient<ChaoticPiece>(10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
