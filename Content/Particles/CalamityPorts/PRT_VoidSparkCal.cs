using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //灾厄VoidSpark的移植,黑芯+透明外圈叠两层,GlowSpark2/GlowSpark走SharedAssets
    public class PRT_VoidSparkCal : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;
        public float Ylength = 1f;
        public float Xlength = 0.6f;

        public float ShrinkSpeed = 1f;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            InitialColor = default;
            AffectedByGravity = false;
            Ylength = 1f;
            Xlength = 0.6f;
            ShrinkSpeed = 1f;
        }

        public override string Texture => CEUtils.WhiteTexPath;   //Texture指白图占位,真图走SharedAssets

        public PRT_VoidSparkCal Configure(bool affectedByGravity, int lifetime, float shrinkSpeed = 1f,
            PRTRenderLayer? renderLayer = null) {
            AffectedByGravity = affectedByGravity;
            ShrinkSpeed = shrinkSpeed;
            InitialColor = Color;
            Scale *= 0.357f;
            PRTDrawMode = PRTDrawModeEnum.AlphaBlend;   //跟SparkCal的Additive不同,黑芯得走Alpha桶
            if (lifetime > 0)
                Lifetime = lifetime;
            //FIXME:GeneralDrawLayer→PRTRenderLayer对不上,Silence黑VoidSpark原意图盖蓝光,层不对可能被盖
            if (renderLayer.HasValue)
                RenderLayer = renderLayer.Value;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
        }

        public override void AI() {
            Scale *= 0.9f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            Ylength *= 1f + 0.25f * ShrinkSpeed;   //ShrinkSpeed拉长Y压扁X,VoidSpark标志性拉伸
            Xlength *= 1f - 0.3f * ShrinkSpeed;

            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Vector2 drawScale = new Vector2(Xlength, Ylength) * Scale;
            Texture2D texture = PRTSharedAssets.GlowSpark2.Value;   //黑芯GlowSpark2+外圈GlowSpark叠两层
            Texture2D texture2 = PRTSharedAssets.GlowSpark.Value;

            spriteBatch.Draw(texture2, Position - Main.screenPosition, null, Color with { A = 0 }, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);   //外圈A=0只要形状
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.Black, Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.75f, 0.9f), SpriteEffects.None, 0f);   //黑芯盖蓝光,Silence靠RenderLayer调遮挡
            return false;
        }
    }
}
