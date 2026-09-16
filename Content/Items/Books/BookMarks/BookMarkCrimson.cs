using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkCrimson : BookMark
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<BookMarkCorrupt>();
        }
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 5);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Crimson");
        public override EBookProjectileEffect getEffect() {
            return new CrimsonBMEffect();
        }
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.lifeSteal += 0.17f;
            modifer.Crit += 4;
        }
        public override Color tooltipColor => Color.Crimson;
    }

    public class CrimsonBMEffect : EBookProjectileEffect
    {
        public override void OnProjectileSpawn(Projectile projectile, bool ownerClient) {
            (projectile.ModProjectile as EBookBaseProjectile).color = Color.Crimson;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            if (Main.rand.NextBool(projectile.HasEBookEffect<APlusBMEffect>() ? 3 : 5)) {
                for (int i = 0; i < 16; i++) {
                    // 原灾厄 BloodBlast 改用自有 BloodSpray; 血珠自地下升起, 关掉碰撞并缩短寿命保持原节奏
                    var blood = Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center + new Vector2(Main.rand.NextFloat(-80, 80), 400), new Vector2(0, -18), ModContent.ProjectileType<BloodSpray>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 8, projectile.DamageType), projectile.knockBack / 3, projectile.owner).ToProj();
                    blood.DamageType = projectile.DamageType;
                    blood.tileCollide = false;
                    blood.timeLeft = 90;
                }
            }

        }
    }
}
