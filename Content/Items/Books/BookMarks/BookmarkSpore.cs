using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkSpore : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Blue;
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Spore");
        public override EBookProjectileEffect getEffect() {
            return new BookmarkSporeEffect();
        }

        public override Color tooltipColor => new Color(80, 80, 255);
        private static int projType = -1;
        public static int ProjType { get { if (projType == -1) { projType = ModContent.ProjectileType<ExplosiveSpore>(); } return projType; } }
    }

    public class BookmarkSporeEffect : EBookProjectileEffect
    {
        public override void BookUpdate(Projectile projectile, bool ownerClient) {
            if (ownerClient) {
                if (projectile.GetOwner().ownedProjectileCounts[BookmarkSpore.ProjType] < 1) {
                    if (projectile.ModProjectile is EntropyBookHeldProjectile eb)
                        eb.ShootSingleProjectile(BookmarkSpore.ProjType, projectile.Center, projectile.rotation.ToRotationVector2() * 3 + new Vector2(0, -8), 1, 1, 1, fixedBaseDamage: 10);
                }
            }
        }
    }
    public class ExplosiveSpore : EBookBaseProjectile
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.penetrate = -1;
            Projectile.light = 0.6f;
            Projectile.localNPCHitCooldown = -1;
            Projectile.width = Projectile.height = 16;
            Projectile.timeLeft = 12 * 60;
            Projectile.ArmorPenetration = 16;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (active) {
                Texture2D tex = Projectile.GetTexture();
                Rectangle frame = new Rectangle(0, (18 * ((int)Projectile.Entropy().counter / 4 % 4)), tex.Width, 18);
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, new Vector2(tex.Width / 2f, 9), Projectile.scale, SpriteEffects.None);
            }
            return false;
        }
        public int Delay = 60;
        public bool active = true;
        public override void AI() {
            if (active) {
                if (Projectile.timeLeft <= 100)
                    Projectile.Opacity -= 0.01f;
                if (Projectile.localAI[1]++ == 0) {
                    CEUtils.PlaySound("SporeSpawn", 1, Projectile.Center);
                }
                var player = Projectile.GetOwner();
                NPC target = Projectile.FindMinionTarget();
                if (Delay-- < 0 && target != null) {
                    Projectile.velocity += (target.Center - Projectile.Center).normalize() * 1f;
                    Projectile.velocity *= 0.97f;
                }
                else {
                    Vector2 pos = player.Center + new Vector2(0, -40);
                    if (CEUtils.getDistance(Projectile.Center, pos) > 36) {
                        Projectile.velocity += (pos - Projectile.Center).normalize() * 1f;
                        Projectile.velocity *= 0.96f;
                        Projectile.velocity.Y *= 0.95f;
                    }
                }
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.MushroomTorch);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.1f;
                Projectile.rotation = Projectile.velocity.X * 0.08f;
            }
            else {
                Projectile.velocity *= 0;
            }
        }
        public override bool? CanHitNPC(NPC target) {
            return active ? null : false;
        }
        public override void OnKill(int timeLeft) {
            base.OnKill(timeLeft);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            if (active) {
                CEUtils.PlaySound("SporeGas", 1, Projectile.Center);
                ((EntropyBookHeldProjectile)ShooterModProjectile).ShootSingleProjectile(ModContent.ProjectileType<SporeGas>(), Projectile.Center, Vector2.Zero, 0.15f);
                Projectile.timeLeft = 240;
                active = false;
            }

        }
    }
    public class SporeGas : EBookBaseProjectile
    {
        public override Color baseColor => Color.Blue;
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.penetrate = -1;
            Projectile.localNPCHitCooldown = 6;
            Projectile.width = Projectile.height = 172;
            Projectile.timeLeft = 360;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
        public override void AI() {
            base.AI();
            if (Projectile.localAI[2]++ == 0) {
                for (int i = 0; i < 36; i++) {
                    //MediumMistCal带Cal后缀,CalamityPorts签名,color/lifetime/scale跟旧mist构造一致
                    PRTLoader.NewParticle<PRT_MediumMistCal>(Projectile.Center, CEUtils.randomPointInCircle(14), Color.Lerp(this.color, Color.White, 0.5f), Projectile.scale).Configure(this.color, 230, 0.005f);
                }
            }
            if (Main.GameUpdateCount % 2 == 0)
                for (int i = 0; i < 4; i++) {
                    PRTLoader.NewParticle<PRT_MediumMistCal>(Projectile.Center + CEUtils.randomPointInCircle(100), CEUtils.randomPointInCircle(2), Color.Lerp(this.color, Color.White, 0.5f), Projectile.scale).Configure(this.color, 230, 0.005f);
                }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (target.velocity.Length() > 2 && !target.boss)
                target.velocity *= 0.64f;
        }
    }
}