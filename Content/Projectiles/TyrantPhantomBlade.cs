using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    // 暴君之力幻影剑：自研移植，行为对齐原借用的灾厄日光幻刃档（加速、10次命中上限、金色）
    public class TyrantPhantomBlade : ModProjectile
    {
        // ai[0] = 旋转方向，ai[1] = 完全可见时长（PoTProj 传 56），ai[2] = 缩放
        public const float FadeInTime = 30f;
        public const float FadeOutTime = 30f;
        public const float MaxVelocity = 32f;
        public const int MaxHits = 10;

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/PowerOfTyrant";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.timeLeft = 220;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI() {
            float fullyVisibleDuration = Projectile.ai[1];
            float timeBeforeFadeOut = fullyVisibleDuration + FadeInTime;
            float projectileDuration = timeBeforeFadeOut + FadeOutTime;

            if (Projectile.localAI[0] == 0f)
                SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);

            Projectile.localAI[0] += 1f;
            Projectile.Opacity = Utils.Remap(Projectile.localAI[0], 0f, fullyVisibleDuration, 0f, 1f) * Utils.Remap(Projectile.localAI[0], timeBeforeFadeOut, projectileDuration, 1f, 0f);
            if (Projectile.localAI[0] >= projectileDuration) {
                Projectile.Kill();
                return;
            }

            Projectile.direction = Projectile.spriteDirection = (int)Projectile.ai[0];
            Projectile.rotation += Projectile.ai[0] * MathHelper.TwoPi * (4f + Projectile.Opacity * 4f) / 90f;
            Projectile.scale = Utils.Remap(Projectile.localAI[0], fullyVisibleDuration + 2f, projectileDuration, 1.12f, 1f) * Projectile.ai[2];

            float dustAngle = Projectile.rotation + Main.rand.NextFloatDirection() * MathHelper.PiOver2 * 0.7f;
            Vector2 dustPosition = Projectile.Center + dustAngle.ToRotationVector2() * 84f * Projectile.scale;
            if (Main.rand.NextBool(3)) {
                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.Venom, null, 100, default, 1.4f);
                dust.noGravity = true;
                dust.velocity *= 0f;
                dust.fadeIn = 1.5f;
            }
            for (int i = 0; i < 3f * Projectile.Opacity; i++) {
                Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                int dustType = Main.rand.NextFloat() < Projectile.Opacity ? DustID.IchorTorch : DustID.YellowTorch;
                Dust dust = Dust.NewDustPerfect(dustPosition, dustType, Projectile.velocity * 0.2f + dustVelocity * 3f, 100, default, 1.4f);
                dust.noGravity = true;
            }

            // 日光档不追踪，逐帧加速到上限
            if (Projectile.velocity.Length() < MaxVelocity) {
                Projectile.velocity *= 1.05f;
                if (Projectile.velocity.Length() > MaxVelocity)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * MaxVelocity;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 distanceFromTarget = targetHitbox.ClosestPointInRect(Projectile.Center) - Projectile.Center;
            float projectileSize = 100f * Projectile.scale;
            if (distanceFromTarget.Length() < projectileSize && Collision.CanHit(Projectile.Center, 0, 0, targetHitbox.Center.ToVector2(), 0, 0))
                return true;
            return null;
        }

        public override void CutTiles() {
            Vector2 startPoint = (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * 60f * Projectile.scale;
            Vector2 endPoint = (Projectile.rotation + MathHelper.PiOver4).ToRotationVector2() * 60f * Projectile.scale;
            Utils.PlotTileLine(Projectile.Center + startPoint, Projectile.Center + endPoint, 60f * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Venom, 120);
            target.AddBuff(BuffID.OnFire3, 120);
            if (Projectile.numHits >= MaxHits)
                Projectile.localAI[0] = Projectile.ai[1] + FadeInTime;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > Projectile.ai[1] + FadeInTime ? false : null;

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            SpriteEffects effects = Projectile.ai[0] >= 0f ? SpriteEffects.None : SpriteEffects.FlipVertically;
            float brightness = Lighting.GetColor(Projectile.Center.ToTileCoordinates()).ToVector3().Length() / (float)Math.Sqrt(3d);
            brightness = Utils.Remap(brightness, 0.2f, 1f, 0f, 1f);
            float glowStrength = MathHelper.Min(0.15f + brightness * 0.85f, Utils.Remap(Projectile.localAI[0], 30f, 96f, 1f, 0f));

            Main.spriteBatch.UseAdditive();
            for (int i = 2; i >= 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                Vector2 drawPos = Projectile.Center - Projectile.velocity * 0.5f * i - Main.screenPosition;
                float ghostRot = Projectile.oldRot[i] + Projectile.ai[0] * MathHelper.TwoPi * 0.1f * -i;
                float fade = 1f - i / 3f;
                float alpha = Projectile.Opacity * fade * fade * 0.85f;
                float amount = Projectile.Opacity * Projectile.Opacity;

                Color colorOne = Color.Lerp(new Color(20, 40, 60, 120), new Color(225, 225, 25, 120), amount);
                Color colorTwo = Color.Lerp(new Color(40, 80, 180), new Color(255, 255, 100), amount);
                // 三重相位残影，复刻原幻影剑的鬼影层
                for (float off = -MathHelper.TwoPi + MathHelper.TwoPi / 3f; off < 0f; off += MathHelper.TwoPi / 3f) {
                    float phaseFade = Utils.Remap(off, -MathHelper.TwoPi, 0f, 0f, 0.5f);
                    Main.spriteBatch.Draw(tex, drawPos, null, colorOne * glowStrength * alpha * phaseFade, ghostRot + off + MathHelper.PiOver4 * Projectile.ai[0], origin, Projectile.scale * 0.975f, effects, 0f);
                    Main.spriteBatch.Draw(tex, drawPos, null, colorTwo * brightness * alpha * phaseFade, ghostRot + off + MathHelper.PiOver4 * Projectile.ai[0], origin, Projectile.scale * 0.78f, effects, 0f);
                }
                Main.spriteBatch.Draw(tex, drawPos, null, colorTwo * brightness * alpha * MathHelper.Lerp(0.05f, 0.4f, glowStrength), ghostRot + MathHelper.PiOver4 * Projectile.ai[0], origin, Projectile.scale * 0.975f, effects, 0f);
                Main.spriteBatch.Draw(tex, drawPos, null, colorOne * glowStrength * alpha, ghostRot + MathHelper.PiOver4 * Projectile.ai[0], origin, Projectile.scale * 0.8f, effects, 0f);
            }

            // 刃尖闪光（原版星芒贴图）
            Texture2D shine = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 shinePos = Projectile.Center - Main.screenPosition + (Projectile.rotation + MathHelper.Pi / (20f / 3f) * Projectile.ai[0]).ToRotationVector2() * (tex.Width * 0.5f - 4f) * Projectile.scale;
            Color shineColor = new Color(255, 255, 50) * glowStrength * Projectile.Opacity * 0.5f;
            shineColor.A = 0;
            Vector2 shineScale = new Vector2(0.5f, 2f) * Projectile.Opacity;
            Main.EntitySpriteDraw(shine, shinePos, null, shineColor, MathHelper.PiOver2 + MathHelper.PiOver4, shine.Size() / 2f, shineScale, SpriteEffects.None);
            Main.EntitySpriteDraw(shine, shinePos, null, shineColor, MathHelper.PiOver4, shine.Size() / 2f, shineScale, SpriteEffects.None);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
