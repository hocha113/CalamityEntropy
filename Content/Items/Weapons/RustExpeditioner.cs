using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class RustExpeditioner : ModItem
    {
        public override void SetStaticDefaults()
        {
            AmmoID.Sets.SpecificLauncherAmmoProjectileFallback[Type] = ItemID.RocketLauncher;

        }

        public override void SetDefaults()
        {
            Item.DefaultToRangedWeapon(ProjectileID.RocketI, AmmoID.Rocket, singleShotTime: 50, shotVelocity: 20f, hasAutoReuse: true);
            Item.width = 50;
            Item.height = 20;
            Item.damage = 18;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item61;
            Item.value = Item.buyPrice(gold: 3);
            Item.rare = ItemRarityID.Orange;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-8f, 2f);
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddCalOrOwn(CEID.Item_DubiousPlating, ModContent.ItemType<AzafurePlating>(), 5).AddIngredient(ItemID.IronBar, 15).AddTile(TileID.Anvils).Register();
        }

        public override bool RangedPrefix()
        {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            p.ToProj().Entropy().withGrav = player.Entropy().WeaponBoost == 0;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p);
            }

            return false;
        }
    }
}
