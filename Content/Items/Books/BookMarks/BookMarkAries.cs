using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkAries : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.Entropy().stroke = true;
            Item.Entropy().NameColor = Color.LightBlue;
            Item.Entropy().strokeColor = Color.DarkBlue;
            Item.Entropy().tooltipStyle = 4;
            Item.value = Item.buyPrice(gold: 1);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Aries");
        public override Color tooltipColor => Color.LightBlue;
        public override EBookProjectileEffect getEffect() {
            return new AriesBMEffect();
        }
    }

    /// <summary>白羊座书签(2026-08-31 平衡案重做):攻击时召唤暗影之手伤害敌人(固定基伤20)。</summary>
    public class AriesBMEffect : EBookProjectileEffect
    {
        public override void OnShoot(EntropyBookHeldProjectile book) {
            Projectile proj = book.Projectile;
            Player owner = proj.GetOwner();
            NPC target = proj.FindTargetWithinRange(900, false);
            Vector2 pos = target != null ? target.Center : Main.MouseWorld;
            Projectile.NewProjectile(proj.GetSource_FromAI(), pos, Vector2.Zero,
                ModContent.ProjectileType<AriesShadowHand>(), FixedDamage(owner, 20, proj.DamageType), 2f, proj.owner);
        }
    }

    /// <summary>暗影之手:在目标处升起的黑影爪击,短暂滞留造成伤害。</summary>
    public class AriesShadowHand : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = 46;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = Terraria.ModLoader.DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 36;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
        }
        public override void AI() {
            // 从下方升起再收拢的爪形黑雾
            float rise = 1f - Projectile.timeLeft / 36f;
            if (Main.rand.NextBool()) {
                Dust d = Dust.NewDustDirect(Projectile.position + new Vector2(0, Projectile.height * (1f - rise)), Projectile.width, (int)(Projectile.height * rise), DustID.Shadowflame);
                d.noGravity = true;
                d.velocity = new Vector2(Main.rand.NextFloat(-1, 1), -Main.rand.NextFloat(2, 5));
                d.scale = Main.rand.NextFloat(1.2f, 1.9f);
            }
            Lighting.AddLight(Projectile.Center, 0.25f, 0.1f, 0.4f);
        }
    }
}