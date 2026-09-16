using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_RuneParticle : BasePRT
    {
        public int frame;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            frame = 0;   //池化复用,frame忘了清下一颗符文贴图就错了
            Glow = true;
        }

        //Runes/r0~r13共14帧,Texture只能挂r0应付PRTLoader,PreDraw走PRTFrameTextures按frame取
        public override string Texture => "CalamityEntropy/Content/Particles/Runes/r0";

        public PRT_RuneParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            frame = Main.rand.Next(0, 14);
            if (Lifetime <= 0)
                Lifetime = 42;
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //旧剩余比例1→0,新LifetimeCompletion 0→1,别反了
            Color = Color.Lerp(new Color(110, 120, 255), Color.White, Opacity);
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTFrameTextures.Rune(frame);
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }

        public bool Glow = true;
    }

    public class PRT_RuneParticleHoming : BasePRT
    {
        public int frame = Main.rand.Next(0, 14);
        public Entity homingTarget;   //有entity引用,别开CanPool,Reset里还得置null
        float speed;

        //同PRT_RuneParticle,多帧贴图走PRTFrameTextures
        public override string Texture => "CalamityEntropy/Content/Particles/Runes/r0";

        public PRT_RuneParticleHoming Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            Lifetime = -1;
        }

        public override void AI() {
            if (homingTarget == null) {
                Kill();
                return;
            }
            if (speed < 10)
                speed += 0.05f;
            Color = Color.Lerp(new Color(160, 170, 255), Color.White, (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.5f + 0.5f);
            Velocity *= 1 - speed * 0.08f;
            Velocity += (homingTarget.Center - Position).normalize() * speed * 1.4f;
            if (CEUtils.getDistance(Position, homingTarget.Center) < Velocity.Length() * 1.2f)
                Kill();
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTFrameTextures.Rune(frame);
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }

        public bool Glow = true;
    }



}
