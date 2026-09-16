using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkTerra : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(gold: 80);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Terra");
        public override Color tooltipColor => Color.YellowGreen;
        public override EBookProjectileEffect getEffect() {
            return new TerraBMEffect();
        }
    }

    /// <summary>泰拉书签(2026-08-31 平衡案重做):命中后在目标头顶召唤智能弹跳的泰拉巨石
    /// (固定基伤180),持书期间+10防御。</summary>
    public class TerraBMEffect : EBookProjectileEffect
    {
        public override void BookUpdate(Projectile projectile, bool ownerClient) {
            projectile.GetOwner().Entropy().bmTerraDefTime = 2;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-60, 60), -Main.rand.NextFloat(260, 340));
            Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnPos, new Vector2(Main.rand.NextFloat(-2, 2), 6),
                ModContent.ProjectileType<TerraBoulder>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 180, projectile.DamageType), projectile.knockBack, projectile.owner);
        }
    }

    public class TerraBoulder : EBookBaseProjectile
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.tileCollide = true;
            this.gravity = 0.6f;
            Projectile.extraUpdates = 1;
            Projectile.width = Projectile.height = 32;
            Projectile.penetrate = 6;
            Projectile.timeLeft = 420;
        }

        public override void AI() {
            base.AI();
            if (Projectile.velocity.X > 0) {
                Projectile.rotation += 0.1f;
            }
            else {
                Projectile.rotation -= 0.1f;
            }
        }
        // 2026-08-31 平衡案:叶绿箭式智能弹跳——碰撞后弹向最近的敌怪
        public override bool OnTileCollide(Vector2 oldVelocity) {
            NPC target = Projectile.FindTargetWithinRange(600, false);
            if (target != null) {
                Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * MathHelper.Max(9f, oldVelocity.Length());
            }
            else {
                if (oldVelocity.X != 0 && Projectile.velocity.X == 0) {
                    Projectile.velocity.X = oldVelocity.X * -1;
                }
                if (oldVelocity.Y != 0 && Projectile.velocity.Y == 0) {
                    Projectile.velocity.Y = oldVelocity.Y * -1f;
                }
            }
            if (Main.rand.NextBool(5)) {
                Projectile.penetrate -= 1;
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            lightColor = this.color;
            CEUtils.DrawAfterimage(Projectile.GetTexture(), Projectile.Entropy().odp, Projectile.Entropy().odr, Projectile.scale);
            return base.PreDraw(ref lightColor);
        }
    }

}
