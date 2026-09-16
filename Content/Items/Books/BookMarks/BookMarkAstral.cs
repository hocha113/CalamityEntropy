using CalamityEntropy.Content.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkAstral : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(gold: 80);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Astral");
        public override Color tooltipColor => new Color(122, 122, 190);
        public override EBookProjectileEffect getEffect() {
            return new AstralBMEffect();
        }
    }

    /// <summary>星辉书签(2026-08-31 平衡案重做):命中目标时召唤星蛾的追踪幻星弹(固定基伤30)。</summary>
    public class AstralBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            Player owner = projectile.GetOwner();
            Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-180, 180), -Main.rand.NextFloat(240, 340));
            Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnPos,
                (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 12,
                ModContent.ProjectileType<AstralBullet>(), FixedDamage(owner, 30, projectile.DamageType), projectile.knockBack, projectile.owner);
        }
    }

}