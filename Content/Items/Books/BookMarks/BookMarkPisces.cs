using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkPisces : BookMark
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
        public override Texture2D UITexture => BookMark.GetUITexture("Pisces");
        public override Color tooltipColor => Color.LightBlue;
        public override EBookProjectileEffect getEffect() {
            return new PiscesBMEffect();
        }
    }

    /// <summary>双鱼座书签(2026-08-31 平衡案重做):命中敌怪时在其位置召唤一个小型克苏鲁旋风(固定伤害100)。</summary>
    public class PiscesBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<PiscesWhirlwind>(), FixedDamage(projectile.GetOwner(), 100, projectile.DamageType), projectile.knockBack, projectile.owner);
        }
    }

    /// <summary>小型克苏鲁旋风:原地盘旋的水龙卷,持续拉扯敌怪。</summary>
    public class PiscesWhirlwind : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = 70;
            Projectile.height = 140;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = Terraria.ModLoader.DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 150;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        public override void AI() {
            Projectile.velocity = new Vector2(0, -0.4f);
            // 旋风体:分层旋绕的水雾
            for (int i = 0; i < 3; i++) {
                float h = Main.rand.NextFloat();
                float radius = 12 + h * 26;
                float ang = Main.GameUpdateCount * 0.35f + h * MathHelper.TwoPi;
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2((float)System.Math.Cos(ang) * radius, (h - 0.5f) * Projectile.height), DustID.DungeonWater, new Vector2(-(float)System.Math.Sin(ang) * 4f, -2f));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.3f, 2.1f);
            }
            Lighting.AddLight(Projectile.Center, 0.1f, 0.25f, 0.4f);
            // 轻微向内牵引非Boss敌怪
            foreach (NPC n in Main.ActiveNPCs) {
                if (!n.friendly && !n.boss && !n.dontTakeDamage && n.knockBackResist > 0 && CEUtils.getDistance(n.Center, Projectile.Center) < 180) {
                    n.velocity += (Projectile.Center - n.Center).SafeNormalize(Vector2.Zero) * 0.3f * n.knockBackResist;
                }
            }
        }
    }
}