using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //旧版粒子系统.CustomPulse搬来的,129处调用,Configure对齐原构造(texPath/squish/scale等)
    public class PRT_CustomPulse : BasePRT
    {
        public string TexPath = "CalamityEntropy/Assets/Particles/BloomCircle";
        public Vector2 Squish = Vector2.One;
        public float OriginalScale;
        public float FinalScale;
        public float BaseOpacity = 1f;
        public bool FadeOut = true;
        public float MakeLight = 1f;
        public SpriteEffects Effects = SpriteEffects.None;
        public Color BaseColor;

        private float opacity;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            TexPath = "CalamityEntropy/Assets/Particles/BloomCircle";
            Squish = Vector2.One;
            OriginalScale = 0f;
            FinalScale = 0f;
            BaseOpacity = 1f;
            FadeOut = true;
            MakeLight = 1f;
            Effects = SpriteEffects.None;
            BaseColor = default;
            opacity = 0f;
        }

        //TexPath每次spawn现传,Texture属性只挂占位白图,真图在PreDraw里拿
        public override string Texture => CEUtils.WhiteTexPath;

        public override int InGame_World_MaxCount => 8000;

        public PRT_CustomPulse Configure(string texPath, Vector2 squish, float rotation, float originalScale,
            float finalScale, int lifetime, PRTDrawModeEnum mode = PRTDrawModeEnum.AdditiveBlend,
            float baseOpacity = 1f, bool fadeOut = true, float makeLight = 1f,
            SpriteEffects effects = SpriteEffects.None, PRTRenderLayer? renderLayer = null) {
            TexPath = texPath;
            Squish = squish;
            Rotation = rotation;
            OriginalScale = originalScale;
            FinalScale = finalScale;
            Scale = originalScale;
            BaseColor = Color;
            BaseOpacity = baseOpacity;
            FadeOut = fadeOut;
            MakeLight = makeLight;
            Effects = effects;
            PRTDrawMode = mode;
            Lifetime = lifetime;
            if (renderLayer.HasValue)
                RenderLayer = renderLayer.Value;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
            //Configure里Lifetime直接赋值(多数调用传死值),opacity每帧AI重算
            opacity = 0f;
        }

        public override void AI() {
            float pulseProgress = 1f - MathF.Pow(1f - LifetimeCompletion, 4f);
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);

            //FadeOut=false时opacity恒BaseOpacity,MakeLight=0关Lighting.AddLight
            opacity = (FadeOut ? (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2) : 1f) * BaseOpacity;

            Color = BaseColor * opacity;
            if (MakeLight > 0)
                Lighting.AddLight(Position, (Color.R / 255f) * MakeLight, (Color.G / 255f) * MakeLight, (Color.B / 255f) * MakeLight);
            Velocity *= 0.95f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = PRTPathTextures.Get(TexPath);   //动态路径走运行时缓存,别每帧Request
            float scaleMult = 1f;

            if (Main.zenithWorld) {
                DateTime day = DateTime.Now;
                if (day.DayOfWeek == DayOfWeek.Tuesday) {
                    //是的,天顶世界周二画猛犸象,Calamity原版彩蛋,照搬,不是bug别删
                    Texture2D joke = PRTSharedAssets.MammothParticle.Value;
                    scaleMult = MathHelper.Lerp(tex.Size().X / joke.Size().X, tex.Size().Y / joke.Size().Y, 0.5f);
                    tex = joke;
                    PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
                }
            }

            Color drawColor = Color * opacity;
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, drawColor, Rotation, tex.Size() / 2f,
                Scale * Squish * scaleMult * 0.2f, Effects, 0);
            return false;
        }
    }
}
