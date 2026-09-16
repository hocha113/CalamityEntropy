using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.DustCarverBow
{
    public class CarverBolt : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 280;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return Projectile.position.getRectCentered(20 * Projectile.scale, 20 * Projectile.scale).Intersects(targetHitbox);
        }
        public override void AI() {
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.position, 1400);
            if (target != null && drawcount > 12) {
                Projectile.velocity *= 0.94f;
                Vector2 v = target.Center - Projectile.position;
                v.Normalize();

                Projectile.velocity += v * 4;
            }
            drawcount++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft < 30) {
                Projectile.Opacity -= 1 / 30f;
            }
        }
        float drawcount = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.position, Vector2.Zero, new Color(255, 40, 40), 0f).Configure(new Vector2(2f, 2f), 0, 0.4f, 16);

            CEUtils.PlaySound("GrassSwordHit" + Main.rand.Next(4).ToString(), 1.4f, target.Center, 16, CEUtils.WeapSound * 0.6f);

            float sparkCount = 16;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4, 12);
                int sparkLifetime2 = 12;
                float sparkScale2 = 0.34f;
                sparkScale2 *= 1.4f;
                Color sparkColor2 = Color.Lerp(Color.DarkRed, Color.Crimson, Main.rand.NextFloat());

                PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
            }
        }
        public Color ColorFunction(float completionRatio, Vector2 vertex) {
            return Color.Lerp(Color.Crimson, Color.DarkRed, MathHelper.Clamp(completionRatio * 0.8f, 0f, 1f)) * base.Projectile.Opacity;
        }

        public float WidthFunction(float completionRatio, Vector2 vertex) {
            float num = 20f;
            float num2 = ((!(completionRatio < 0.1f)) ? MathHelper.Lerp(num, 0f, Utils.GetLerpValue(0.1f, 1f, completionRatio, clamped: true)) : ((float)Math.Sin(completionRatio / 0.1f * (MathF.PI / 2f)) * num + 0.1f));
            return num2 * base.Projectile.Opacity * Projectile.scale
            ;
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.StreakGoopAsset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(base.Projectile.oldPos, new CEPrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            Texture2D value = Projectile.GetTexture();
            Main.EntitySpriteDraw(value, Projectile.position - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, value.Size() * 0.5f, base.Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
