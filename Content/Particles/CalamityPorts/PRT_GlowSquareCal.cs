using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //GlowSquareParticle,Glowing=true时PreDraw叠一层半透白是原版双Draw
    public class PRT_GlowSquareCal : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;
        public bool Glowing = true;
        public float Spin;

        //GlowSquareParticle贴图跨模组,映射在PRTSharedAssets.GlowSquareParticle
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_GlowSquareCal Configure(bool affectedByGravity, int lifetime, float size, bool glow, float rotationSpeed) {
            AffectedByGravity = affectedByGravity;
            Glowing = glow;
            Spin = rotationSpeed;
            InitialColor = Color;
            Scale = size / 10f;
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            Rotation = rotationSpeed;
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
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            Rotation += Spin;   //Spin是Configure传的角速度,跟速度朝向那套spark类不一样
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Vector2 drawScale = new Vector2(0.8f, 1.2f) * Scale;
            Texture2D texture = PRTSharedAssets.GlowSquareParticle.Value;   //真图SharedAssets.GlowSquareParticle

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            if (Glowing) {
                spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.White * 0.5f, Rotation,
                    texture.Size() * 0.5f, drawScale * 0.8f, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
