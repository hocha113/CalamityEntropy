using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Weapons.GrassSword;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class BrambleShoot : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 12;
            Projectile.height = 12;
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
            if (Projectile.localAI[2]++ == 0) {
                Projectile.scale *= 2;
            }
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.position, 1400);
            if (target != null && drawcount > 16) {
                Projectile.velocity *= 0.94f;
                Vector2 v = target.Center - Projectile.position;
                v.Normalize();

                Projectile.velocity += v * 4;
            }
            drawcount++;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        float drawcount = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            //PRT_DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.position, Vector2.Zero, new Color(0, 255, 0), 0f).Configure(new Vector2(2f, 2f), 0, 0.2f, 8);

            CEUtils.PlaySound("GrassSwordHit" + Main.rand.Next(4).ToString(), 1.4f, target.Center, 16, CEUtils.WeapSound * 0.6f);

            float sparkCount = 16;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4, 12);
                int sparkLifetime2 = 12;
                float sparkScale2 = 0.34f;
                sparkScale2 *= (1 + Bramblecleave.GetLevel() * 0.05f);
                Color sparkColor2 = Color.Lerp(Color.Green, Color.LightGreen, Main.rand.NextFloat());

                //AltSpark Configure(bool,int)是Ports签名,不是opacity/glow/mode那套
                PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
            }
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public Color ColorFunction(float completionRatio, Vector2 vertex) {
            return Color.Lerp(Color.LawnGreen, Color.Green, MathHelper.Clamp(completionRatio * 0.8f, 0f, 1f)) * base.Projectile.Opacity;
        }

        public float WidthFunction(float completionRatio, Vector2 vertex) {
            float num = 8f;
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
            Texture2D value = CEUtils.getExtraTex("Leaf");
            Main.EntitySpriteDraw(value, Projectile.position - Main.screenPosition, null, Color.White, Projectile.rotation, value.Size() * 0.5f, base.Projectile.scale, SpriteEffects.None);
            return false;
        }
    }


}