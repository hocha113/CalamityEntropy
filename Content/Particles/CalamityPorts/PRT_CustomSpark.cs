using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //CustomSpark,旧版粒子系统那批,贴图路径构造参数传入和CustomPulse同款
    public class PRT_CustomSpark : BasePRT
    {
        public string TexPath = "CalamityEntropy/Assets/Particles/GlowSpark";
        public Color InitialColor;
        public bool AffectedByGravity;
        public Vector2 Stretch = new Vector2(0.5f, 1.6f);
        public bool GlowCenter;
        public bool FadeIn;
        public float FadeInScale;
        public bool AffectedByLight;
        public float GlowCenterScale = 1f;
        public float GlowOpacity = 1f;
        public float ExtraRotation;
        public float ShrinkSpeed;
        public bool NoShrink;
        public float Spin;
        public float ColorFadeSpeed = 3f;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            TexPath = "CalamityEntropy/Assets/Particles/GlowSpark";
            InitialColor = default;
            AffectedByGravity = false;
            Stretch = new Vector2(0.5f, 1.6f);
            GlowCenter = false;
            FadeIn = false;
            FadeInScale = 0f;
            AffectedByLight = false;
            GlowCenterScale = 1f;
            GlowOpacity = 1f;
            ExtraRotation = 0f;
            ShrinkSpeed = 0f;
            NoShrink = false;
            Spin = 0f;
            ColorFadeSpeed = 3f;   //TexPath和Stretch一堆字段,CanPool忘Reset就带脏状态
        }

        //TexPath现传,Texture只能指白图占位,真图PreDraw里走PRTPathTextures
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_CustomSpark Configure(string texPath, bool affectedByGravity, int lifetime, Vector2 stretch,
            bool useAdditiveBlend = true, bool glowCenter = false, float extraRotation = 0f, bool fadeIn = false,
            bool affectedByLight = false, float shrinkSpeed = 0f, float glowCenterScale = 1f, float glowOpacity = 1f,
            bool noShrink = false, float spin = 0f, float colorFadeSpeed = 3f, PRTRenderLayer? renderLayer = null) {
            TexPath = texPath;
            AffectedByGravity = affectedByGravity;
            Stretch = stretch;
            GlowCenter = glowCenter;
            FadeIn = fadeIn;
            AffectedByLight = affectedByLight;
            ExtraRotation = extraRotation;
            ShrinkSpeed = shrinkSpeed;
            GlowCenterScale = glowCenterScale;
            GlowOpacity = glowOpacity;
            NoShrink = noShrink;
            Spin = spin;
            ColorFadeSpeed = colorFadeSpeed;
            FadeInScale = Scale;
            PRTDrawMode = useAdditiveBlend ? PRTDrawModeEnum.AdditiveBlend : PRTDrawModeEnum.AlphaBlend;
            InitialColor = Color;
            if (FadeIn)
                Scale = 0f;
            if (lifetime > 0)
                Lifetime = lifetime;
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
            if (!FadeIn) {
                if (!NoShrink)
                    Scale *= 0.95f;
                Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, ColorFadeSpeed));
            }
            else if ((float)Time / Lifetime < 0.5f)
                Scale = MathHelper.Lerp(Scale, FadeInScale, 0.2f);   //FadeIn前半段鼓起来
            else
                Scale = MathHelper.Lerp(Scale, FadeInScale, -0.21f);   //后半段缩回去,负lerp是Cal原版写法

            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            ExtraRotation += Spin;
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2 + ExtraRotation;
            Stretch.X *= 1f - 0.2f * ShrinkSpeed;
            Stretch.Y *= 1f + 0.2f * ShrinkSpeed;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D texture = PRTPathTextures.Get(TexPath);   //调用点传的CalamityEntropy/Assets/Particles/xxx,缓存别每帧Request
            Vector2 drawScale = Stretch * Scale;
            Color drawColor = Color;
            if (AffectedByLight)
                drawColor = drawColor.MultiplyRGBA(Lighting.GetColor((Position / 16f).ToPoint()));

            float fadePow = (float)Math.Pow(LifetimeCompletion, 3D);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null,
                Color.Lerp(drawColor, Color.Transparent, fadePow), Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            if (GlowCenter) {
                spriteBatch.Draw(texture, Position - Main.screenPosition, null,
                    Color.Lerp(Color.Lerp(drawColor, Color.White, 0.8f), Color.Transparent, fadePow) * GlowOpacity,
                    Rotation, texture.Size() * 0.5f, drawScale * 0.8f * GlowCenterScale, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
