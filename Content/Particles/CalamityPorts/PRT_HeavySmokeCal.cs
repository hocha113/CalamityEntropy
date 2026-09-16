using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //灾厄HeavySmokeParticle的移植,Configure对齐原构造,AI数值一行没动
    //MercyShoot形体层+Solar Storm爆炸烟都spawn这粒,glowing=true走Additive桶
    public class PRT_HeavySmokeCal : BasePRT
    {
        private static int FrameAmount = 6;

        public float Spin;
        public bool StrongVisual;
        public bool Glowing;
        public float HueShift;
        public int Variant;
        public bool AffectedByLight;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Spin = 0f;
            StrongVisual = false;
            Glowing = false;
            HueShift = 0f;
            Variant = 0;
            AffectedByLight = false;
        }

        //贴图走PRTSharedAssets.HeavySmoke,Texture指WhiteTexPath占位
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_HeavySmokeCal Configure(float opacity, int lifetime, float rotationSpeed = 0f,
            bool glowing = false, float hueshift = 0f, bool required = false, bool affectedByLight = false) {
            Opacity = opacity;
            Spin = rotationSpeed;
            Glowing = glowing;
            HueShift = hueshift;
            StrongVisual = required;
            AffectedByLight = affectedByLight;
            PRTDrawMode = glowing ? PRTDrawModeEnum.AdditiveBlend : PRTDrawModeEnum.NonPremultiplied;   //glowing开关=旧构造的blend分支,别拆到SetProperty
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 200;
            Variant = Main.rand.Next(7);
        }

        public override void AI() {
            if (Time / (float)Lifetime < 0.2f)   //前20%用Time/Lifetime不用Completion,跟Cal原版写法一致
                Scale += 0.01f;
            else
                Scale *= 0.975f;

            Color = Main.hslToRgb((Main.rgbToHsl(Color).X + HueShift) % 1f, Main.rgbToHsl(Color).Y, Main.rgbToHsl(Color).Z);
            Opacity *= 0.98f;   //Opacity逐帧衰减,跟LifetimeCompletion那套fade叠乘
            Rotation += Spin * (Velocity.X > 0f ? 1f : -1f);
            Velocity *= 0.85f;

            float fade = Utils.GetLerpValue(1f, 0.85f, LifetimeCompletion, clamped: true);
            Color *= fade;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            //6帧竖排80x80,Variant横排7列,跟EHeavySmoke同一套图
            Texture2D tex = PRTSharedAssets.HeavySmoke.Value;
            int animationFrame = (int)Math.Floor(Time / ((float)Lifetime / FrameAmount));
            Rectangle frame = new Rectangle(80 * Variant, 80 * animationFrame, 80, 80);

            Color col = Color * Opacity;
            if (AffectedByLight)
                col = col.MultiplyRGBA(Lighting.GetColor((Position / 16f).ToPoint()));

            spriteBatch.Draw(tex, Position - Main.screenPosition, frame, col, Rotation, frame.Size() / 2f, Scale,
                SpriteEffects.None, 0f);
            return false;
        }
    }
}
