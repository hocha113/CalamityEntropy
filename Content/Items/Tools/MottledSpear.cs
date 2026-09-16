using CalamityEntropy.Content.Items.Armor.Azafure;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Tools
{
    public class MottledSpear : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.AmethystHook);
            Item.width = 54;
            Item.height = 48;
            Item.shootSpeed = MottledSpearHook.LaunchSpeed;
            Item.shoot = ModContent.ProjectileType<MottledSpearHook>();
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[1]", MottledSpearHook.GrappleRangInTiles);
            tooltips.Replace("[2]", MottledSpearHook.LaunchSpeed);
            tooltips.Replace("[3]", MottledSpearHook.ReelbackSpeed);
            tooltips.Replace("[4]", MottledSpearHook.PullSpeed);
        }
    }
    public class MottledSpearHook : ModProjectile
    {
        public const float PullSpeed = 12f;

        public const float ReelbackSpeed = 18f;

        public const float LaunchSpeed = 18f;

        public const float GrappleRangInTiles = 24f;

        public override void SetDefaults() {
            base.Projectile.CloneDefaults(230);
        }

        public override bool? CanUseGrapple(Player player) {
            int num = 0;
            for (int i = 0; i < Main.maxProjectiles; i++) {
                if (Main.projectile[i].active && Main.projectile[i].owner == Main.myPlayer && Main.projectile[i].type == base.Projectile.type) {
                    num++;
                }
            }
            if (num > 0) {
                return false;
            }
            return true;
        }

        public override float GrappleRange() {
            return GrappleRangInTiles * 16 * (Projectile.GetOwner().AzafureEnhance() ? 1.5f : 1);
        }

        public override void NumGrappleHooks(Player player, ref int numHooks) {
            numHooks = 1;
        }

        public override void GrappleRetreatSpeed(Player player, ref float speed) {
            hitsnd = false;
            speed = ReelbackSpeed * (Projectile.GetOwner().AzafureEnhance() ? 1.5f : 1);
        }

        public override void GrapplePullSpeed(Player player, ref float speed) {
            if (hitsnd) {
                CEUtils.PlaySound("ExoHit1", 1.6f, Projectile.Center, volume: 0.45f);
                hitsnd = false;
            }
            speed = PullSpeed * (Projectile.GetOwner().AzafureEnhance() ? 1.5f : 1);
            if (Projectile.Distance(player.MountedCenter) < PullSpeed * 2.2f) {
                player.velocity = player.velocity.normalize() * PullSpeed * (Projectile.GetOwner().AzafureEnhance() ? 1.5f : 1);
                Projectile.Kill();
            }
        }

        // 锁链绘制（原生移植，替代灾厄 DrawHook 扩展）：自钩头向玩家逐节铺贴链条
        private void DrawChain(Texture2D chainTexture) {
            Player player = Projectile.GetOwner();
            Vector2 center = Projectile.Center;
            float angleToMountedCenter = Projectile.AngleTo(player.MountedCenter) - MathHelper.PiOver2;
            while (true) {
                float distanceMagnitude = (player.MountedCenter - center).Length();
                if (distanceMagnitude < chainTexture.Height + 1f || float.IsNaN(distanceMagnitude))
                    break;
                center += Projectile.SafeDirectionTo(player.MountedCenter) * chainTexture.Height;
                Color tileAtCenterColor = Lighting.GetColor((int)center.X / 16, (int)(center.Y / 16f));
                Main.spriteBatch.Draw(chainTexture, center - Main.screenPosition,
                    new Rectangle(0, 0, chainTexture.Width, chainTexture.Height),
                    tileAtCenterColor, angleToMountedCenter,
                    chainTexture.Size() / 2, 1f, SpriteEffects.None, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            if (hitsnd)
                Projectile.rotation = (Projectile.Center - Projectile.GetOwner().Center).ToRotation();
            Texture2D hook = Projectile.GetTexture();
            DrawChain(this.getTextureAlt("Chain"));
            Vector2 origin = new Vector2(32, hook.Height / 2);
            Main.EntitySpriteDraw(hook, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, Projectile.Center.X > Projectile.GetOwner().Center.X ? SpriteEffects.None : SpriteEffects.FlipVertically);
            return false;
        }

        public override void AI() {
            if (Projectile.localAI[2]++ == 0) {
                if (Projectile.GetOwner().AzafureEnhance())
                    Projectile.velocity *= 1.5f;
                Projectile.velocity += Projectile.GetOwner().velocity;
                CEUtils.PlaySound("chains_break", 1f, Projectile.Center, volume: 0.18f);
            }
            base.Projectile.spriteDirection = -base.Projectile.direction;
            if (base.Projectile.ai[0] == 2f) {
                base.Projectile.extraUpdates = 1;
            }
            else {
                base.Projectile.extraUpdates = 0;
            }
            Projectile.rotation = (Projectile.Center - Projectile.GetOwner().Center).ToRotation();
        }
        public bool hitsnd = true;
    }
}
