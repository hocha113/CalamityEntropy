using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    internal class PRT_Light : BasePRT
    {
        public float SquishStrenght = 1f;
        public float MaxSquish = 3f;
        public float HueShift;
        public float followingRateRatio = 0.9f;
        public Entity entity;
        public override int InGame_World_MaxCount => 14000;   //旧EParticle没上限,这里拍个大数防堆爆

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            SquishStrenght = 1f;
            MaxSquish = 3f;
            HueShift = 0f;
            followingRateRatio = 0.9f;
            entity = null;   //池化复用,entity引用必须清
        }

        //bloom光晕层,旧ICELoader手加载改成VaultLoaden,别在PreDraw里ModContent.Request
        [VaultLoaden("CalamityEntropy/Content/Particles/PRT_Light2")]
        internal static Asset<Texture2D> BloomTex;

        public PRT_Light Configure(float opacity, float squishStrenght = 1f, float maxSquish = 3f,
            float hueShift = 0f, Entity entity = null, float followingRateRatio = 0.9f, int lifetime = -1) {
            Opacity = opacity;
            SquishStrenght = squishStrenght;
            MaxSquish = maxSquish;
            HueShift = hueShift;
            this.entity = entity;
            this.followingRateRatio = followingRateRatio;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 200;
        }

        public override void AI() {
            Velocity *= LifetimeCompletion >= 0.34f ? 0.93f : 1.02f;

            Opacity = LifetimeCompletion > 0.5f ? (float)Math.Sin(LifetimeCompletion * MathHelper.Pi) * 0.2f + 0.8f : (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
            Scale *= 0.95f;

            Color = Main.hslToRgb(Main.rgbToHsl(Color).X + HueShift, Main.rgbToHsl(Color).Y, Main.rgbToHsl(Color).Z);

            if (entity != null && entity.active) {
                //entity跟随手动写,框架只管Velocity位移,跟PRT_Spark/FollowOwner同款
                Position += entity.velocity * followingRateRatio;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            Texture2D bloomTex = BloomTex.Value;

            float squish = MathHelper.Clamp(Velocity.Length() / 10f * SquishStrenght, 1f, MaxSquish);

            float rot = Velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 origin = tex.Size() / 2f;
            Vector2 scale = new(Scale - Scale * squish * 0.3f, Scale * squish);
            float properBloomSize = tex.Height / (float)bloomTex.Height;   //主体和bloom图尺寸不同,按高度比缩

            Vector2 drawPosition = Position - Main.screenPosition;

            //三层叠画:bloom+主体+高光,旧Draw原样搬的,用Main.spriteBatch是历史遗留别改成sb
            Main.spriteBatch.Draw(bloomTex, drawPosition, null, Color * Opacity * 0.8f, rot, bloomTex.Size() / 2f, scale * 2 * properBloomSize, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, drawPosition, null, Color * Opacity * 0.8f, rot, origin, scale * 1.1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, drawPosition, null, Color.White * Opacity * 0.9f, rot, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
