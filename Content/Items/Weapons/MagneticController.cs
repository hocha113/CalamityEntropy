using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class MagneticController : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.ItemNoGravity[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 7;
            Item.crit = 0;
            Item.DamageType = DamageClass.Summon;
            Item.width = 16;
            Item.height = 16;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.shoot = ModContent.ProjectileType<Teletor>();
            Item.shootSpeed = 2f;
            Item.value = 460;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item15;
            Item.noMelee = true;
            Item.mana = 10;
            Item.buffType = ModContent.BuffType<TeletorBuff>();
            Item.rare = ItemRarityID.Orange;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddCalOrOwn(CEID.Item_DubiousPlating, ModContent.ItemType<AzafurePlating>(), 6).
                AddCalOrOwn(CEID.Item_MysteriousCircuitry, ModContent.ItemType<AzafureCircuitry>(), 4).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}