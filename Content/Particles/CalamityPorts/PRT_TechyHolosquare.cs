using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //TechyHolosquareParticle,旧版粒子系统搬来(Calamity类名拼成Holoysquare)
    public class PRT_TechyHolosquare : BasePRT
    {
        public float MistOpacity;
        public float OpacityMult;
        public int Variant;
        public new Rectangle Frame;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            MistOpacity = 0f;
            OpacityMult = 0f;
            Variant = 0;
            Frame = default;
        }

        //Assets/Particles/TechyHolosquare → PRTSharedAssets,色差Draw一行没动
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_TechyHolosquare Configure(int lifetime, float opacity = 1f) {
            MistOpacity = opacity;
            OpacityMult = opacity;
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
            Variant = Main.rand.Next(6);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            switch (Variant) {
                case 0: Frame = new Rectangle(8, 0, 6, 6); break;
                case 1: Frame = new Rectangle(6, 8, 10, 6); break;
                case 2: Frame = new Rectangle(4, 16, 14, 8); break;
                case 3: Frame = new Rectangle(2, 26, 18, 10); break;
                case 4: Frame = new Rectangle(2, 38, 18, 8); break;
                default: Frame = new Rectangle(6, 48, 12, 12); break;
            }
        }

        public override void AI() {
            MistOpacity = (float)Math.Pow(LifetimeCompletion, 0.5f) * OpacityMult;   //透明度跟Completion^0.5走,Calamity原版
            Lighting.AddLight(Position, Color.ToVector3() * MistOpacity);
            Rotation = Velocity.ToRotation();
            Velocity *= 0.875f;
            Scale *= 0.96f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D baseTex = PRTSharedAssets.TechyHolosquare.Value;   //TechyHolosquare,色差Draw原样搬的
            CEParticleUtils.DrawChromaticAberration(Vector2.UnitX.RotatedBy(Rotation), 1.5f, delegate (Vector2 offset, Color colorMod) {
                spriteBatch.Draw(baseTex, Position + offset - Main.screenPosition, Frame,
                    Color.MultiplyRGB(colorMod) * MistOpacity, Rotation, Frame.Size() / 2f, Scale / 2f, SpriteEffects.None, 0);
            });
            return false;
        }
    }
}
