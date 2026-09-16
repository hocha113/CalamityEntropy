using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class UpdraftBullet : EBookBaseProjectile
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.light = 0.2f;
            Projectile.timeLeft = 120;
            Projectile.ArmorPenetration = 9;
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            hitbox = Projectile.Center.getRectCentered(46 * Projectile.scale, 46 * Projectile.scale);
        }
        public override void AI() {
            base.AI();
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.GameUpdateCount % 5 == 0) {
                //WindParticle旧EParticle,velocity/scale原值
                var __prt = PRTLoader.NewParticle<PRT_WindParticle>(Projectile.Center + Projectile.velocity * 4, Vector2.Zero, new Color(240, 245, 255), 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation + MathHelper.Pi);
                __prt.v1 = 9;
                __prt.v2 = 3;
                __prt.r = Projectile.rotation + MathHelper.Pi;
                __prt.dir = -1;
                var __prt2 = PRTLoader.NewParticle<PRT_WindParticle>(Projectile.Center + Projectile.velocity * 4, Vector2.Zero, new Color(240, 245, 255), 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation + MathHelper.Pi);
                __prt2.v1 = 9;
                __prt2.v2 = 3;
                __prt2.r = Projectile.rotation + MathHelper.Pi;
                __prt2.dir = 1;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive);

            Texture2D tex = Projectile.GetTexture();

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(200, tex.Height / 2), Projectile.scale * 0.4f, SpriteEffects.None, 0);

            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            return false;
        }
        public override Color baseColor => Color.LightBlue;
        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.velocity = oldVelocity;
            return true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            for (int i = 0; i < 10; i++) {
                var __prt = PRTLoader.NewParticle<PRT_ULineParticle>(target.Center + new Vector2(Main.rand.NextFloat(0, target.width) - (target.width / 2f), Main.rand.NextFloat(0, target.height) - (target.height / 2f)), new Vector2(0, -34), Color.Lerp(this.color, Color.LightBlue, 0.5f), 1).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
                __prt.spd = 0.032f;
                __prt.w2 = 0.85f;
                __prt.w1 = 0.8f;
                __prt.len = 4;
            }
            if ((target.knockBackResist != 0 || target.velocity.Length() > 0.1f) && !target.boss) {
                target.velocity += Projectile.velocity * (0.6f + 0.4f * target.knockBackResist);
            }
            //AdditiveBlend走Configure分桶,旧版粒子系统 Before层那套
            PRTLoader.NewParticle<PRT_HadCircle2>(target.Center, Vector2.Zero, new Color(170, 170, 255), 0).Configure(0, true, PRTDrawModeEnum.AdditiveBlend, 0);
            PRTLoader.NewParticle<PRT_WindParticle>(Projectile.Center, Vector2.Zero, new Color(240, 245, 255), 2).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
        }

        public override void OnKill(int timeLeft) {
            base.OnKill(timeLeft);
            for (int i = 0; i < 3; i++) {
                PRTLoader.NewParticle<PRT_WindParticle>(Projectile.Center, Vector2.Zero, new Color(240, 245, 255), 2).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
            }
            PRTLoader.NewParticle<PRT_UpdraftParticle>(Projectile.Center, Projectile.velocity, Color.White, Projectile.scale * 0.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation);
        }
    }

}