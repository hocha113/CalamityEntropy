using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //FlameParticle,旧版粒子系统搬来,AI里自己Position+=Velocity所以框架位移必须关
    public class PRT_FlameCal : BasePRT
    {
        public float RelativePower;
        public Color BrightColor;
        public Color DarkColor;
        public int Variant;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            RelativePower = 0f;
            BrightColor = default;
            DarkColor = default;
            Variant = 0;
        }

        //Assets/Particles/Flames → PRTSharedAssets.Flames,Texture指白图占位
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_FlameCal Configure(int lifetime, float relativePower, Color darkColor) {
            BrightColor = Color;
            DarkColor = darkColor;
            RelativePower = relativePower;
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
            Variant = Main.rand.Next(3);
        }

        //框架自动Position+=Velocity,旧FlameParticle AI里也有那行,关位移自己接管,忘删就双倍速度
        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Position += Velocity;
            Scale += RelativePower * 0.01f;
            Position.Y -= RelativePower * 1.25f;
            Scale *= 0.97f;

            Color = Color.Lerp(BrightColor, DarkColor, LifetimeCompletion);
            Color = Color.Lerp(Color, Color.White, Utils.GetLerpValue(0.1f, 0.25f, LifetimeCompletion, true) * Utils.GetLerpValue(0.4f, 0.25f, LifetimeCompletion, true) * 0.7f);
            Color *= Utils.GetLerpValue(0f, 0.15f, LifetimeCompletion, true) * Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true) * 0.6f;
            Color *= 1.5f;
            Color.A = 50;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = PRTSharedAssets.Flames.Value;
            int frameWidth = tex.Width / 3;   //3列横排,Variant在SetProperty里rand
            Rectangle frame = new Rectangle(frameWidth * Variant, 0, frameWidth, tex.Height);
            spriteBatch.Draw(tex, Position - Main.screenPosition, frame, Color, Rotation, new Vector2(frameWidth / 2f, tex.Height / 2f), Scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
