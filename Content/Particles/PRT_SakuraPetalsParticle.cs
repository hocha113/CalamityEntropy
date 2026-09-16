using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_SakuraPetalsParticle : BasePRT
    {
        public int tex = Main.rand.Next(4);
        public bool FallThrough = Main.rand.NextBool(5);
        public bool check = true;

        //SakuraPetalsParticle0~3四张横排变体,Texture挂0号帧,PreDraw按tex走PRTFrameTextures
        public override string Texture => "CalamityEntropy/Content/Particles/SakuraPetalsParticle0";

        public override bool ShouldUpdatePosition() => false;   //位移在AI里TileCollision算,框架自动+=Velocity会双倍

        public PRT_SakuraPetalsParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            if (Lifetime <= 0)
                Lifetime = Main.rand.Next(8 * 60, 14 * 60);
        }

        public override void AI() {
            int remaining = Lifetime - Time;
            if (remaining % 22 == 0 && Velocity.X != 0)
                FallThrough = Main.rand.NextBool(5);
            if (remaining % 40 == 0 && Velocity.X != 0) {
                tex++;
                if (tex > 3)
                    tex = 0;
            }
            if (remaining < 60)
                Opacity -= 1 / 60f;
            if (check) {
                check = false;
                if (CEUtils.CheckSolidTile(Position.getRectCentered(10, 10)))
                    Kill();
            }
            int w = 6;
            if (FallThrough ? CEUtils.CheckSolidTileOrPlatform(Position.getRectCentered(w + 2, w + 2)) : CEUtils.CheckSolidTile(Position.getRectCentered(w + 2, w + 2)))
                Velocity.X = 0;
            if (Velocity.X == 0 && remaining > 60)
                Time += 2;
            Position += Collision.TileCollision(Position - new Vector2(w * 0.5f, w * 0.5f), Velocity, w, w, !FallThrough, !FallThrough);
            Rotation += Velocity.X * 0.02f;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D texImg = PRTFrameTextures.Sakura(tex);
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            sb.Draw(texImg, Position - Main.screenPosition, null, clr, Rotation, texImg.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }

        public override void PostDraw(SpriteBatch sb) {
            CEUtils.DrawGlow(Position, Color.Pink * 0.46f * Opacity, 0.54f * Scale);
            //DrawGlow会动SpriteBatch,End完BeginDrawingWithMode按当前PRT桶接回去
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
        }

        public bool Glow = true;
    }


}
