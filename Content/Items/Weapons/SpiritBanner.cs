using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class SpiritBanner : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 3;
        }

        public override void SetDefaults() {
            Item.damage = 132;
            Item.crit = 0;
            Item.knockBack = 2;
            Item.DamageType = DamageClass.Summon;
            Item.width = 64;
            Item.height = 64;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.shoot = ModContent.ProjectileType<SpiritBannerMinion>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(gold: 60);
            Item.UseSound = SoundID.Item44;
            Item.noMelee = true;
            Item.mana = 16;
            Item.buffType = ModContent.BuffType<SpiritGathering>();
            Item.rare = ItemRarityID.Yellow;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;

            return false;
        }

        public override bool CanShoot(Player player) {
            return player.ownedProjectileCounts[Item.shoot] == 0 && player.maxMinions - player.slotsMinions >= 3;
        }
    }
}
