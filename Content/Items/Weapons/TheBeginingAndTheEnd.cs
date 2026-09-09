using CalamityEntropy.Content.Projectiles.BNE;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class TheBeginingAndTheEnd : ModItem, ICEChargeWeapon
    {
        // 充能条 6 秒；原潜伏乘数 伤害0.75/弹速0.8/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(6f, 0.75f, 0.8f, 3f);

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 38;
            Item.damage = 2000;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 9;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.ArmorPenetration = 100;
            Item.knockBack = 4f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.shoot = ModContent.ProjectileType<TheBeginning>();
            Item.shootSpeed = 9f;
            Item.DamageType = DamageClass.Melee;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int p1 = ModContent.ProjectileType<TheBeginning>();
            int p2 = ModContent.ProjectileType<TheEnd>();
            // 双刃大招整组共享一次消耗
            if (CEChargeWeapon.TryConsume(player, Item))
            {
                int r = (Main.rand.NextBool() ? -1 : 1);
                int p = Projectile.NewProjectile(source, position, velocity.RotatedBy(0.2f * r), p1, (int)(damage), knockback, player.whoAmI);
                CEChargeWeapon.Empower(p);
                p = Projectile.NewProjectile(source, position, velocity.RotatedBy(-0.2f * r), p2, (int)(damage), knockback, player.whoAmI);
                CEChargeWeapon.Empower(p);
                return false;
            }
            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(source, position, velocity, p2, damage, knockback, player.whoAmI);
            }
            else
            {
                Projectile.NewProjectile(source, position, velocity, p1, damage, knockback, player.whoAmI);
            }
            return false;
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public static void playShootSound(Vector2 c)
        {
            CEUtils.PlaySound("bne" + Main.rand.Next(0, 3).ToString(), 1, c);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_JawsOfOblivion, ItemID.StarWrath)
                .AddIngredient(ModContent.ItemType<WyrmTooth>(), 12)
                .AddIngredient(ModContent.ItemType<FadingRunestone>())
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
