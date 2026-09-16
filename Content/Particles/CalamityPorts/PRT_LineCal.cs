using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //Calamity LineParticle,DrainLineBloom双Draw从原PreDraw搬的,Cal后缀避本模同名
    public class PRT_LineCal : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            InitialColor = default;
            AffectedByGravity = false;   //CanPool,InitialColor和重力开关都得Reset清
        }

        //真图DrainLineBloom走PRTSharedAssets,Texture只能指白图
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_LineCal Configure(bool affectedByGravity, int lifetime) {
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
                Lifetime = 30;   //Calamity LineParticle原默认
        }

        public override void AI() {
            Scale *= 0.95f;
            //淡出走LifetimeCompletion三次方,跟Calamity原公式一字没动
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            //双Draw第二层0.45x略窄,叠DrainLineBloom;不用End/Begin,跟ERing图元路数不同
            Vector2 drawScale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = PRTSharedAssets.DrainLineBloom.Value;   //DrainLineBloom,和AltLineCal的DrainLine是两张图

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
