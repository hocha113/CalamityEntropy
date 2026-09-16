using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //BloomParticle,旧版粒子系统搬来,pulse用1-Pow(1-Completion,4)是Calamity原曲线
    public class PRT_BloomCal : BasePRT
    {
        public float OriginalScale;
        public float FinalScale;
        public Color BaseColor;
        public bool Fade = true;

        private float opacity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            OriginalScale = 0f;
            FinalScale = 0f;
            BaseColor = default;
            Fade = true;
            opacity = 0f;   //CanPool,Fade分支的opacity得清
        }

        //Assets/Particles/BloomCircle → PRTSharedAssets,和CustomPulse默认TexPath是同一张
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_BloomCal Configure(float finalScale, int lifetime, bool fade = true) {
            OriginalScale = Scale;
            FinalScale = finalScale;
            BaseColor = Color;
            Fade = fade;
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
            float pulseProgress = 1f - MathF.Pow(1f - LifetimeCompletion, 4f);   //四次方pulse,Calamity Bloom原曲线
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);
            if (Fade)
                opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);

            Color = BaseColor * (Fade ? opacity : 1f);
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);   //Bloom带环境光,数值Cal原版
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = PRTSharedAssets.BloomCircle.Value;   //BloomCircle走VaultLoaden,别改成Texture=@路径
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * (Fade ? opacity : 1f), 0f, tex.Size() / 2f,
                Scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
