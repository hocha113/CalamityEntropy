using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FractalStar : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/StarTexture";
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public override void AI() {
            if (Projectile.ai[2]++ == 0) {
                Projectile.rotation = CEUtils.randomRot();
            }
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * Projectile.velocity.Length() * 0.004f;
            Projectile.velocity *= 0.98f;
            if (Projectile.timeLeft <= 10 && Shoot && Main.myPlayer == Projectile.owner) {
                Shoot = false;
                CEUtils.PlaySound("bne_hit", 1, Projectile.Center, 6, 0.56f * CEUtils.WeapSound);
                int type = ModContent.ProjectileType<FractalStarblight>();
                for (int i = 0; i < 360; i += 90) {
                    float rot = ((float)i).ToRadians() + Projectile.rotation;
                    //GlowSparkCal Configure里stretch/glow是Calamity原参,别当PRT/EParticle尾参
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, rot.ToRotationVector2() * 12, Color.DeepSkyBlue, Projectile.scale * 0.04f).Configure(false, 22, Vector2.One, false, true);  //GlowSparkCal Configure里stretch/glow是Calamity原参,别当EParticle尾参
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, rot.ToRotationVector2() * 9, type, Projectile.damage / 3, Projectile.knockBack / 4, Projectile.owner);
                }
            }
        }
        public bool Shoot = true;
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            float w = 0.12f;
            if (Projectile.timeLeft < 20) {
                w = CEUtils.Parabola(Projectile.timeLeft / 20f, 1.4f);
                if (Projectile.timeLeft > 10 && w < 0.2f)
                    w = 0.12f;
            }
            float alpha = Projectile.timeLeft < 10 ? Projectile.timeLeft / 10f : 1f;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(200, 200, 255) * alpha * Projectile.Opacity, Projectile.rotation, tex.Size() / 2f, new Vector2(w, 1) * Projectile.scale * 0.8f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(200, 200, 255) * alpha * Projectile.Opacity, Projectile.rotation, tex.Size() / 2f, new Vector2(1, w) * Projectile.scale * 0.8f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}