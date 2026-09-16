using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //PlasmaExplosion,旧版粒子系统搬来,和DetailedExplosion同属脉冲爆炸类,曲线照搬
    public class PRT_PlasmaExplosionCal : BasePRT
    {
        public float OriginalScale;
        public float FinalScale;
        public Vector2 Squish = Vector2.One;
        public Color BaseColor;

        private float opacity;

        //Assets/Particles/PlasmaExplosion → PRTSharedAssets,Texture指白图占位
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_PlasmaExplosionCal Configure(Vector2 squish, float rotation, float finalScale, int lifetime) {
            Squish = squish;
            Rotation = rotation;
            OriginalScale = Scale;
            FinalScale = finalScale;
            BaseColor = Color;
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
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
            float pulseProgress = 1f - MathF.Pow(1f - LifetimeCompletion, 4f);   //1-Pow(1-Completion,4),Calamity脉冲爆炸通用曲线
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);
            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);
            Color = BaseColor * opacity;
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = PRTSharedAssets.PlasmaExplosion.Value;   //PlasmaExplosion贴图走SharedAssets
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * opacity, Rotation, tex.Size() / 2f,
                Scale * Squish, SpriteEffects.None, 0);
            return false;
        }
    }
}
