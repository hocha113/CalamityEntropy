using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //BloodParticle原样翻,ChildSafety.DiscoColor那段是Calamity原版整活,别当bug删
    public class PRT_BloodCal : BasePRT
    {
        public Color InitialColor;

        //Assets/Particles/Blood → PRTSharedAssets.Blood,Texture指白图占位
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_BloodCal Configure(int lifetime) {
            InitialColor = Color;
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
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
            Scale *= 0.98f;
            Velocity.X *= 0.97f;
            Velocity.Y = MathHelper.Clamp(Velocity.Y + 0.9f, -22f, 22f);
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
            if (!ChildSafety.Disabled)
                Color = Main.DiscoColor;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            float verticalStretch = Utils.GetLerpValue(0f, 24f, Math.Abs(Velocity.Y), true) * 0.84f;   //下落越快拉越长,Calamity原版拉伸逻辑
            float brightness = (float)Math.Pow(Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15);
            Vector2 drawScale = new Vector2(1f, verticalStretch + 1f) * Scale * 0.1f;
            Texture2D texture = PRTSharedAssets.Blood.Value;   //真图PreDraw里拿,Texture白图只是堵HasAsset

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color * brightness, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
