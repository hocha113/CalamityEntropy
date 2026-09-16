using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //PointParticle,additiveBlend决定落桶,PointParticle贴图跨模组走PRTSharedAssets
    public class PRT_PointCal : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;
        public bool UseAdditive = true;
        public bool AffectedByLight;

        //Assets/Particles/PointParticle,PreDraw里拿真图
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_PointCal Configure(bool affectedByGravity, int lifetime, bool additiveBlend = true, bool affectedByLight = false) {
            AffectedByGravity = affectedByGravity;
            UseAdditive = additiveBlend;
            AffectedByLight = affectedByLight;
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
            //InitialColor在Configure里从spawn时Color抄的,后面AI Lerp到透明
        }

        public override void AI() {
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));   //Pow(Completion,3)跟LineCal同曲线
            //受光照:原版每帧Lerp重算后再乘环境光,不会跨帧累积
            if (AffectedByLight)
                Color = Lighting.GetColor((Position / 16).ToPoint()).MultiplyRGBA(Color);
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;   //朝向跟速度走,spark系通用AI
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            //additiveBlend=false时Configure落AlphaBlend桶,贴图还是PointParticle
            Vector2 drawScale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = PRTSharedAssets.PointParticle.Value;   //PointParticle,VaultLoaden在SharedAssets

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
