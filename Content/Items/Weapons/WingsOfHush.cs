using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class WingsOfHush : ModItem
    {
        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 44;
            Item.damage = 800;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(platinum: 2);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<WohLaser>();
            Item.shootSpeed = 26f;
            Item.useAmmo = AmmoID.Arrow;
            Item.noUseGraphic = true;
        }
        public override Vector2? HoldoutOffset() => new Vector2(-28, 0);
        public bool flag = false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            CEUtils.PlaySound("WingOfHushShoot", Main.rand.NextFloat(0.7f, 1.1f), position, 8, 0.7f);
            flag = true;
            int p = Projectile.NewProjectile(source, position + velocity.normalize() * 32, velocity, ModContent.ProjectileType<WohLaser>(), damage, knockback, player.whoAmI);

            Projectile.NewProjectile(source, position + velocity.normalize() * Item.shootSpeed + velocity.normalize().RotatedBy(MathHelper.PiOver2) * 18, velocity.RotatedBy(MathHelper.ToRadians(3)) + player.velocity * 0.2f, ModContent.ProjectileType<WohShot>(), (int)(damage * 0.3f), knockback, player.whoAmI, 0, Main.MouseWorld.X, Main.MouseWorld.Y);
            Projectile.NewProjectile(source, position + velocity.normalize() * Item.shootSpeed + velocity.normalize().RotatedBy(MathHelper.PiOver2) * -18, velocity.RotatedBy(MathHelper.ToRadians(-3)) + player.velocity * 0.2f, ModContent.ProjectileType<WohShot>(), (int)(damage * 0.3f), knockback, player.whoAmI, 0, Main.MouseWorld.X, Main.MouseWorld.Y);
            Projectile.NewProjectile(source, position + velocity.normalize() * Item.shootSpeed + velocity.normalize().RotatedBy(MathHelper.PiOver2) * 18, velocity.RotatedBy(MathHelper.ToRadians(6)) + player.velocity * 0.2f, ModContent.ProjectileType<WohShot>(), (int)(damage * 0.3f), knockback, player.whoAmI, 0, Main.MouseWorld.X, Main.MouseWorld.Y);
            Projectile.NewProjectile(source, position + velocity.normalize() * Item.shootSpeed + velocity.normalize().RotatedBy(MathHelper.PiOver2) * -18, velocity.RotatedBy(MathHelper.ToRadians(-6)) + player.velocity * 0.2f, ModContent.ProjectileType<WohShot>(), (int)(damage * 0.3f), knockback, player.whoAmI, 0, Main.MouseWorld.X, Main.MouseWorld.Y);
            return false;
        }
        public override void HoldItem(Player player) {
            int type = ModContent.ProjectileType<WOHHeld>();
            if (Main.myPlayer == player.whoAmI)
                if (player.ownedProjectileCounts[type] < 1)
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, (Main.MouseWorld - player.MountedCenter).normalize() * 12, type, 0, 0, player.whoAmI);
        }
        public override bool RangedPrefix() {
            return true;
        }
    }
    public class WOHHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/WingsOfHush";
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.friendly = true;
            Projectile.timeLeft = 2;
        }
        public float ofs = 0;
        public override void AI() {
            Player player = Projectile.GetOwner();
            if (player.HeldItem.ModItem != null && player.HeldItem.ModItem is WingsOfHush wh) {
                if (wh.flag) {
                    wh.flag = false;
                    ofs = -16;
                }
                Projectile.timeLeft = 2;
            }
            ofs *= 0.96f;
            Projectile.StickToPlayer();
            player.SetHandRot(Projectile.rotation);

            Projectile.Center += Projectile.rotation.ToRotationVector2() * ofs;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
        public void DrawVoid() {
            Texture2D tex = this.getTextureGlow();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
        }
    }
}
