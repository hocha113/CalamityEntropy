using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //Calamity ImpactParticle,Configure对齐原构造(angularVelocity/lifetime),AI一行没动
    //Cal后缀是和Entropy里ImpactParticle撞名避开的
    public class PRT_ImpactCal : BasePRT
    {
        public float AngularVelocity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            AngularVelocity = 0f;   //池化复用,旋速忘了清下一朵出生就在转
        }

        //StarProj自制贴图走PRTSharedAssets,Texture只能指白图
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_ImpactCal Configure(float angularVelocity, int lifetime) {
            AngularVelocity = angularVelocity;
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
            Rotation += AngularVelocity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            float scaleFactor = CEParticleUtils.Convert01To010(LifetimeCompletion) * 1.3f;   //缩放脉冲,Completion走Convert01To010不是线性
            Vector2 drawScale = new Vector2(0.3f, 1f) * scaleFactor;
            Texture2D texture = PRTSharedAssets.StarProj.Value;   //Impact三Draw叠StarProj,缩放走Convert01To010

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, 0f, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, -Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
