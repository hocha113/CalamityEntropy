using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
    //灾厄GlowSparkParticle的移植,Update一行没动,贴图走PRTSharedAssets
    public class PRT_GlowSparkCal : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;
        public bool QuickShrink;
        public bool Glowing = true;
        public float ShrinkSpeed = 1f;
        public Vector2 Squash = new Vector2(0.5f, 1.6f);

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            InitialColor = default;
            AffectedByGravity = false;
            QuickShrink = false;
            Glowing = true;
            ShrinkSpeed = 1f;
            Squash = new Vector2(0.5f, 1.6f);
        }

        public override string Texture => CEUtils.WhiteTexPath;   //Texture指白图占位,真图PreDraw里拿SharedAssets

        public override int InGame_World_MaxCount => 8000;   //Solar Storm爆发段一帧几十粒,拍个大上限防截断

        public PRT_GlowSparkCal Configure(bool affectedByGravity, int lifetime, Vector2 squash,
            bool quickShrink = false, bool glow = true, float shrinkSpeed = 1f) {
            AffectedByGravity = affectedByGravity;
            Squash = squash;
            QuickShrink = quickShrink;
            Glowing = glow;
            ShrinkSpeed = shrinkSpeed;
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            InitialColor = Color;
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
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
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            if (QuickShrink) {
                //QuickShrink拉扁Squash,ShrinkSpeed≠1时按倍率改XY,Cal原版分支
                if (ShrinkSpeed == 1f) {
                    Squash.X *= 0.8f;
                    Squash.Y *= 1.2f;
                }
                else {
                    Squash.X *= 1f - 0.2f * ShrinkSpeed;
                    Squash.Y *= 1f + 0.2f * ShrinkSpeed;
                }
            }

            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Vector2 drawScale = Squash * Scale;
            Texture2D texture = PRTSharedAssets.GlowSpark.Value;   //GlowSpark贴图,VaultLoaden在SharedAssets

            float scaleMult = 1f;
            if (Main.zenithWorld) {
                DateTime day = DateTime.Now;
                if (day.DayOfWeek == DayOfWeek.Tuesday) {
                    //天顶周二猛犸象,Calamity原版彩蛋,照搬,不是bug别删
                    Texture2D joke = PRTSharedAssets.MammothParticle.Value;
                    scaleMult = MathHelper.Lerp(texture.Size().X / joke.Size().X, texture.Size().Y / joke.Size().Y, 0.5f);
                    texture = joke;
                }
            }

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f,
                drawScale * scaleMult, SpriteEffects.None, 0f);
            if (Glowing) {
                spriteBatch.Draw(texture, Position - Main.screenPosition, null,
                    Color.Lerp(Color.White, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D)),
                    Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f) * scaleMult, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
