using CalamityEntropy.Content.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkAerialite : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 5);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Aerialite");
        public override EBookProjectileEffect getEffect() {
            return new AerialiteBMEffect();
        }
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.shotSpeed += 0.4f;
        }

        public override Color tooltipColor => new Color(42, 227, 231);
    }

    public class AerialiteBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            Vector2 shootPosition = projectile.Center + new Vector2(0, -900) + CEUtils.randomVec(128);
            (Projectile.NewProjectile(projectile.GetSource_FromThis(), shootPosition, (target.Center - shootPosition).normalize() * Math.Max(projectile.velocity.Length() * 0.8f, 8), ModContent.ProjectileType<WelkinFeather2>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 25, projectile.DamageType), projectile.knockBack / 3, projectile.owner).ToProj().ModProjectile as EBookBaseProjectile).homing = (projectile.ModProjectile as EBookBaseProjectile).homing;
        }
    }
}