using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //CritSpark,AI里hueShift每帧改Color/Bloom是Calamity原版,不是迁移写错了
    public class PRT_CritSparkCal : BasePRT
    {
        public float Spin;
        public Color Bloom;
        public float BloomScale = 1f;
        public float HueShift;

        private float opacity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Spin = 0f;
            Bloom = default;
            BloomScale = 1f;
            HueShift = 0f;
            opacity = 0f;   //AI每帧重算,CanPool复用得清
        }

        //ThinSparkle+BloomCircle,映射见PRTSharedAssets的Assets/Particles条目
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_CritSparkCal Configure(Color bloom, int lifetime, float rotationSpeed = 1f, float bloomScale = 1f, float hueShift = 0f) {
            Bloom = bloom;
            Spin = rotationSpeed;
            BloomScale = bloomScale;
            HueShift = hueShift;
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
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
            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);
            Velocity *= 0.80f;
            Rotation += Spin * (Velocity.X > 0f ? 1f : -1f) * (LifetimeCompletion > 0.5f ? 1f : 0.5f);

            Color = Main.hslToRgb((Main.rgbToHsl(Color).X + HueShift) % 1f, Main.rgbToHsl(Color).Y, Main.rgbToHsl(Color).Z);
            Bloom = Main.hslToRgb((Main.rgbToHsl(Bloom).X + HueShift) % 1f, Main.rgbToHsl(Bloom).Y, Main.rgbToHsl(Bloom).Z);   //hueShift每帧改Color和Bloom,Cal原版不是笔误

            Lighting.AddLight(Position, Bloom.R / 255f * opacity, Bloom.G / 255f * opacity, Bloom.B / 255f * opacity);
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D sparkTexture = PRTSharedAssets.ThinSparkle.Value;
            Texture2D bloomTexture = PRTSharedAssets.BloomCircle.Value;   //自制BloomCircle,VaultLoaden共享入口
            float properBloomSize = (float)sparkTexture.Height / bloomTexture.Height;

            spriteBatch.Draw(bloomTexture, Position - Main.screenPosition, null, Bloom * opacity * 0.5f, 0f, bloomTexture.Size() / 2f,
                Scale * BloomScale * properBloomSize, SpriteEffects.None, 0);
            spriteBatch.Draw(sparkTexture, Position - Main.screenPosition, null, Color * opacity, Rotation, sparkTexture.Size() / 2f,
                Scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
