using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //DetailedExplosion,旧版粒子系统搬来,pulse曲线和BloomCal同款,数值别动
    public class PRT_DetailedExplosionCal : BasePRT
    {
        public float OriginalScale;
        public float FinalScale;
        public Vector2 Squish = Vector2.One;
        public Color BaseColor;
        public bool UseAdditive = true;

        private float opacity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            OriginalScale = 0f;
            FinalScale = 0f;
            Squish = Vector2.One;
            BaseColor = default;
            UseAdditive = true;
            opacity = 0f;
        }

        //Assets/Particles/DetailedExplosion → PRTSharedAssets,Texture指白图占位
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_DetailedExplosionCal Configure(Vector2 squish, float rotation, float finalScale, int lifetime,
            bool useAdditiveBlend = true, PRTRenderLayer? renderLayer = null) {
            Squish = squish;
            Rotation = rotation;
            OriginalScale = Scale;
            FinalScale = finalScale;
            BaseColor = Color;
            UseAdditive = useAdditiveBlend;
            PRTDrawMode = useAdditiveBlend ? PRTDrawModeEnum.AdditiveBlend : PRTDrawModeEnum.AlphaBlend;
            if (lifetime > 0)
                Lifetime = lifetime;
            if (renderLayer.HasValue)
                RenderLayer = renderLayer.Value;   //原GeneralDrawLayer搬过来的,PRT层粒度粗一些
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
            opacity = 0f;
        }

        public override void AI() {
            float pulseProgress = 1f - MathF.Pow(1f - LifetimeCompletion, 4f);
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);

            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);

            Color = BaseColor * opacity;
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = PRTSharedAssets.DetailedExplosion.Value;   //跨模组贴图SharedAssets拿,Texture白图占位
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * opacity, Rotation, tex.Size() / 2f,
                Scale * Squish, SpriteEffects.None, 0);
            return false;
        }
    }
}
