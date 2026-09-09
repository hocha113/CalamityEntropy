using CalamityEntropy.Common;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkAbyss : BookMark
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = CECal.RarityPureGreen(ModContent.RarityType<GlowGreen>());
            Item.value = Item.buyPrice(platinum: 1, gold: 75);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Abyss");
        public override EBookProjectileEffect getEffect()
        {
            return new AbyssBMEffect();
        }
        public override Color tooltipColor => new Color(93, 134, 196);
    }

    public class AbyssBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            if (Main.rand.NextBool(projectile.HasEBookEffect<APlusBMEffect>() ? 2 : 4) && CECooldowns.CheckCD(ref CECooldowns.BMAbyss, 30))
            {
                Player owner = projectile.GetOwner();
                Vector2 p = target.Center + CEUtils.randomRot().ToRotationVector2() * 300;
                Projectile.NewProjectile(projectile.GetSource_FromThis(), p, (target.Center - p).SafeNormalize(Vector2.One), ModContent.ProjectileType<AbyssBookmarkCrack>(), EBookProjectileEffect.FixedDamage(owner, 350, projectile.DamageType), projectile.knockBack, projectile.owner);
                CEUtils.SetShake(target.Center, 5);
                CEUtils.PlaySound("crack", 1, projectile.Center, 3);
            }
        }
    }
}
