using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //AltLineParticle,AlphaBlend桶(跟LineCal的Additive不同),DrainLine贴图走PRTSharedAssets
    public class PRT_AltLineCal : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;

        //Assets/Particles/DrainLine → PRTSharedAssets,Texture指白图占位
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_AltLineCal Configure(bool affectedByGravity, int lifetime) {
            AffectedByGravity = affectedByGravity;
            InitialColor = Color;
            PRTDrawMode = PRTDrawModeEnum.AlphaBlend;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
            //没开CanPool:跟LineCal同构但AlphaBlend桶+DrainLine,spawn不密先不池化
        }

        public override void AI() {
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));   //淡出Pow(Completion,3)是Calamity原曲线
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;   //慢下来才加重力,跟LineCal/PointCal同一套
            }

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            //贴图DrainLine(非Bloom),Configure落AlphaBlend桶;双Draw叠层跟LineCal一样
            Vector2 drawScale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = PRTSharedAssets.DrainLine.Value;   //真图VaultLoaden在SharedAssets,不是Texture属性那条路径

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
