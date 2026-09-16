using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //GlowOrbParticle,additiveBlend决定落Additive还是AlphaBlend桶,和Calamity GeneralDrawLayer分支对应
    public class PRT_GlowOrbCal : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;
        public bool UseAdditive = true;
        public float FadeOut = 1f;
        public bool Important;
        public bool GlowCenter = true;

        //Assets/Particles/GlowOrbParticle → PRTSharedAssets,Texture指白图占位
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_GlowOrbCal Configure(bool affectedByGravity, int lifetime, bool additiveBlend = true,
            bool needed = false, bool glowCenter = true) {
            AffectedByGravity = affectedByGravity;
            UseAdditive = additiveBlend;
            Important = needed;
            GlowCenter = glowCenter;
            InitialColor = Color;
            PRTDrawMode = additiveBlend ? PRTDrawModeEnum.AdditiveBlend : PRTDrawModeEnum.AlphaBlend;
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
            FadeOut -= 0.1f;   //GlowCenter那层白芯用FadeOut衰减,不是LifetimeCompletion
            Scale *= 0.93f;
            Color = Color.Lerp(InitialColor, InitialColor * 0.2f, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Vector2 drawScale = Vector2.One * Scale;
            Texture2D texture = PRTSharedAssets.GlowOrbParticle.Value;   //GlowOrbParticle跨模组,VaultLoaden映射

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            if (GlowCenter) {
                spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.White * FadeOut, Rotation,
                    texture.Size() * 0.5f, drawScale * new Vector2(0.5f, 0.5f), SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
