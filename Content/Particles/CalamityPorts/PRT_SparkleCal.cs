using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //SparkleParticle,星形+BloomCircle叠层Draw从Calamity原Draw搬的,贴图Sparkle2/BloomCircle都在PRTSharedAssets
    public class PRT_SparkleCal : BasePRT
    {
        public Color Bloom;
        public float Spin;
        public float BloomScale = 1f;

        private float opacity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Bloom = default;
            Spin = 0f;
            BloomScale = 1f;
            opacity = 0f;   //AI每帧重算,CanPool复用得清
        }

        //跨模组贴图统一白图占位,PreDraw里拿PRTSharedAssets真图
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_SparkleCal Configure(Color bloom, int lifetime, float rotationSpeed = 0f, float bloomScale = 1f,
            bool additiveBlend = true) {
            Bloom = bloom;
            Spin = rotationSpeed;
            BloomScale = bloomScale;
            PRTDrawMode = additiveBlend ? PRTDrawModeEnum.AdditiveBlend : PRTDrawModeEnum.AlphaBlend;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
        }

        public override void AI() {
            opacity = (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);   //正弦脉冲,Solar Storm冲击也spawn SparkleCal
            Velocity *= 0.95f;
            Rotation += Spin * (Velocity.X > 0f ? 1f : -1f);
            Lighting.AddLight(Position, Bloom.R / 255f * opacity, Bloom.G / 255f * opacity, Bloom.B / 255f * opacity);
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D starTexture = PRTSharedAssets.Sparkle2.Value;
            Texture2D bloomTexture = PRTSharedAssets.BloomCircle.Value;   //bloom光晕,自制贴图走VaultLoaden
            float properBloomSize = (float)starTexture.Height / bloomTexture.Height;

            spriteBatch.Draw(bloomTexture, Position - Main.screenPosition, null, Bloom * opacity * 0.5f, 0f,
                bloomTexture.Size() / 2f, Scale * BloomScale * properBloomSize, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, Position - Main.screenPosition, null, Color * opacity * 0.5f,
                Rotation + MathHelper.PiOver4, starTexture.Size() / 2f, Scale * 0.75f, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, Position - Main.screenPosition, null, Color * opacity, Rotation,
                starTexture.Size() / 2f, Scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
