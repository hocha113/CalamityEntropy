using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Luminar : ModItem
    {
        public override void SetDefaults() {
            Item.width = 38;
            Item.height = 84;
            Item.damage = 52;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 13;
            Item.useAnimation = 13;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ModContent.RarityType<Lunarblight>();
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.UseSound = SoundID.DD2_SkyDragonsFuryShot;
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;
            Item.autoReuse = true;
            Item.ArmorPenetration = 10;
        }
        public override Vector2? HoldoutOffset() {
            return new Vector2(-2, 0);
        }
        public override bool RangedPrefix() {
            return true;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player) {
            return Main.rand.NextBool(6);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            var pj = p.ToProj();
            pj.Entropy().LuminarArrow = true;
            CEUtils.SyncProj(p);
            return false;
        }
    }
}
