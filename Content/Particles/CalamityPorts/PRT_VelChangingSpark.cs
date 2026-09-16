using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    public class PRT_VelChangingSpark : BasePRT
    {
        public string TexPath = "CalamityEntropy/Assets/Particles/BloomCircle";
        public Color InitialColor;
        public Vector2 EndVelocity;
        public Vector2 Stretch = Vector2.One;
        public bool GlowCenter;
        public float ExtraRotation;
        public float LerpRate = 0.1f;
        public float ShrinkSpeed;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            TexPath = "CalamityEntropy/Assets/Particles/BloomCircle";
            InitialColor = default;
            EndVelocity = default;
            Stretch = Vector2.One;
            GlowCenter = false;
            ExtraRotation = 0f;
            LerpRate = 0.1f;
            ShrinkSpeed = 0f;
        }

        //默认BloomCircle,具体路径Configure里覆盖,别在PreDraw里ModContent.Request
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_VelChangingSpark Configure(Vector2 endVelocity, string texPath, int lifetime, Vector2 stretch,
            bool useAdditiveBlend = true, bool glowCenter = false, float extraRotation = 0f,
            float shrinkSpeed = 0f, float lerpRate = 0.1f) {
            EndVelocity = endVelocity;
            TexPath = texPath;
            Stretch = stretch;
            GlowCenter = glowCenter;
            ExtraRotation = extraRotation;
            ShrinkSpeed = shrinkSpeed;
            LerpRate = lerpRate;
            InitialColor = Color;
            PRTDrawMode = useAdditiveBlend ? PRTDrawModeEnum.AdditiveBlend : PRTDrawModeEnum.AlphaBlend;
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
            if ((float)Time / Lifetime < 0.5f)
                Scale *= 0.95f;

            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));

            if ((float)Time / Lifetime < 0.8f) {
                Velocity *= 0.95f;
                EndVelocity *= 0.95f;
            }

            Velocity = Vector2.Lerp(Velocity, EndVelocity, LerpRate);   //速度朝EndVelocity收敛,前半段才缩Scale
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2 + ExtraRotation;
            Stretch.X *= 1f - 0.2f * ShrinkSpeed;
            Stretch.Y *= 1f + 0.2f * ShrinkSpeed;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D texture = PRTPathTextures.Get(TexPath);   //动态TexPath,PRTPathTextures缓存,别每帧Request
            Vector2 drawScale = Stretch * Scale;

            float scaleMult = 1f;
            if (Main.zenithWorld) {
                DateTime day = DateTime.Now;
                if (day.DayOfWeek == DayOfWeek.Tuesday) {
                    //是的,天顶周二画猛犸象,Calamity原版彩蛋,CustomPulse那边也有,别删
                    Texture2D joke = PRTSharedAssets.MammothParticle.Value;
                    scaleMult = MathHelper.Lerp(texture.Size().X / joke.Size().X, texture.Size().Y / joke.Size().Y, 0.5f);
                    texture = joke;
                    PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
                }
            }

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f,
                drawScale * scaleMult, SpriteEffects.None, 0f);
            if (GlowCenter) {
                spriteBatch.Draw(texture, Position - Main.screenPosition, null,
                    Color.Lerp(Color.Lerp(Color, Color.White, 0.8f), Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D)),
                    Rotation, texture.Size() * 0.5f, drawScale * 0.8f * scaleMult, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
