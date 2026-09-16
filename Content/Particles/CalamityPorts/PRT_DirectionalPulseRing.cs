using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //DirectionalPulseRing,PulseRing加Squish/Rotation定向拉伸版,贴图同一个HollowCircleHardEdge
    public class PRT_DirectionalPulseRing : BasePRT
    {
        public float OriginalScale;
        public float FinalScale;
        public Vector2 Squish = Vector2.One;
        public Color BaseColor;

        private float opacity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            OriginalScale = 0f;
            FinalScale = 0f;
            Squish = Vector2.One;
            BaseColor = default;
            opacity = 0f;
        }

        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_DirectionalPulseRing Configure(Vector2 squish, float rotation, float finalScale, int lifetime) {
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
            //Rotation走Configure传入,不随机(PulseRing才rand)
            opacity = 0f;
        }

        public override void AI() {
            float pulseProgress = 1f - MathF.Pow(1f - LifetimeCompletion, 4f);   //脉冲环同款曲线,和PulseRing/BloomCal一致
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);

            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);

            Color = BaseColor * opacity;
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            //Scale*Squish定向拉伸,贴图跟PulseRing同一个HollowCircleHardEdge
            Texture2D tex = PRTSharedAssets.HollowCircleHardEdge.Value;   //HollowCircleHardEdge,VaultLoaden在SharedAssets
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * opacity, Rotation, tex.Size() / 2f,
                Scale * Squish, SpriteEffects.None, 0);
            return false;
        }
    }
}
