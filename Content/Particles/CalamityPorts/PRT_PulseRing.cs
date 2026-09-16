using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //PulseRing,HollowCircleHardEdge脉冲环,Configure的mode参数对齐Calamity blend分支
    public class PRT_PulseRing : BasePRT
    {
        public float OriginalScale;
        public float FinalScale;
        public Color BaseColor;

        private float opacity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            OriginalScale = 0f;
            FinalScale = 0f;
            BaseColor = default;
            opacity = 0f;   //CanPool,脉冲环opacity每实例独立,Reset得清
        }

        //Assets/Particles/HollowCircleHardEdge → PRTSharedAssets
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_PulseRing Configure(float finalScale, int lifetime,
            PRTDrawModeEnum mode = PRTDrawModeEnum.AdditiveBlend) {
            OriginalScale = Scale;
            FinalScale = finalScale;
            BaseColor = Color;
            PRTDrawMode = mode;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            opacity = 0f;
        }

        public override void AI() {
            float pulseProgress = 1f - MathF.Pow(1f - LifetimeCompletion, 4f);   //脉冲缩放曲线,别换成LifetimeCompletion线性
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);

            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);

            Color = BaseColor * opacity;
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = PRTSharedAssets.HollowCircleHardEdge.Value;   //脉冲环贴图,HollowCircleHardEdge
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * opacity, Rotation, tex.Size() / 2f,
                Scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
