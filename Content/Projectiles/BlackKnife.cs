using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class BlackKnife : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
            Projectile.penetrate = -1;
        }
        public int counter = 0;
        public float j = 1;
        public Color clr = Color.White;
        public int white = 0;
        public int w = 0;
        public Vector2 center = Vector2.Zero;
        public bool onNPC = true;
        public int wah = 0;
        public override void AI() {
            if (Projectile.localAI[1]++ == 0)
                CEUtils.PlaySound("darkbladespawn", 1, Projectile.Center);
            NPC target = ((int)(Projectile.ai[0])).ToNPC();
            if (onNPC) {
                center = target.Center;
                wah = target.width + target.height;
            }
            if (!target.active || target.dontTakeDamage) {
                onNPC = false;
            }
            {
                Projectile.rotation = Projectile.ai[1] + MathHelper.Pi;
                white--;
                counter++;
                float d = (wah) * 0.5f + 200;
                if (counter < 60) {
                    d += CEUtils.GetRepeatedCosFromZeroToOne(counter / 60f, 2) * 80;
                }
                else {
                    d += 80;
                }
                w = (int)d;
                if (counter > 60) {
                    clr = Color.White;
                }
                else {
                    clr = Color.Lerp(Color.White, Color.Red, counter / 60f);
                }
                if (counter == 60) {
                    clr = Color.White;
                    white = 5;
                }
                if (counter == 65) {
                    CEUtils.PlaySound("Dizzy", 1, Projectile.Center);
                    //PRT_BlackKnifeParticle AlphaBlend+rotation走Configure,旧DarkBlade trail
                    PRTLoader.NewParticle<PRT_BlackKnifeParticle>(Projectile.Center, Projectile.rotation.ToRotationVector2() * 260, Color.Red, Projectile.scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, Projectile.rotation);  //BlackKnifeParticle AlphaBlend+rotation走Configure,旧DarkBlade trail
                    PRTLoader.NewParticle<PRT_BlackKnifeSlash>(center, Vector2.Zero, Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.ai[1], 6);
                }
                if (counter > 70) {
                    Projectile.Kill();
                }
                Projectile.Center = center + Projectile.ai[1].ToRotationVector2() * d * j;
            }

        }
        public override bool? CanHitNPC(NPC target) {
            return counter < 65 ? false : null;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center + Projectile.ai[1].ToRotationVector2() * w, Projectile.Center - Projectile.ai[1].ToRotationVector2() * w * 10, targetHitbox, 56);
        }
        public override bool PreDraw(ref Color lightColor) {
            if (counter > 64) {
                return false;
            }
            Texture2D tex = white > 0 ? this.getTextureGlow() : Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, clr * Projectile.Opacity, Projectile.rotation, tex.Size() / 2f, Projectile.scale * 0.8f, SpriteEffects.None);
            return false;
        }
    }
}