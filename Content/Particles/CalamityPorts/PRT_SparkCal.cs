using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //灾厄SparkParticle的移植,StarProj贴图走PRTSharedAssets
    public class PRT_SparkCal : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            InitialColor = default;
            AffectedByGravity = false;   //池化复用,跟AltSpark同款Reset清单
        }

        public override string Texture => CEUtils.WhiteTexPath;   //跨模组贴图PreDraw里拿,这里指白图堵Warn

        public PRT_SparkCal Configure(bool affectedByGravity, int lifetime) {
            AffectedByGravity = affectedByGravity;
            InitialColor = Color;
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
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));   //立方淡出,spark系通用
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Vector2 drawScale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = PRTSharedAssets.StarProj.Value;   //StarProj,和AltSpark同图源

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);   //0.45内层,跟PRT_Spark/AltSpark同款叠法
            return false;
        }
    }
}
