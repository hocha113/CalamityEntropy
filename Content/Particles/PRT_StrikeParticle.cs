using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //end是拖尾锚点,SetProperty首帧锁在Position,CanPool不开
    public class PRT_StrikeParticle : BasePRT
    {
        public bool Glow = true;
        public Vector2 end;

        //Texture占位,PreDraw借PRTSharedAssets.UpdraftParticle拉伸画斩击
        public override string Texture => "CalamityEntropy/Content/Particles/UpdraftParticle";

        public PRT_StrikeParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            end = Position;
            if (Lifetime <= 0)
                Lifetime = 60;
        }

        public override void AI() {
            Velocity *= 0.8f;
            end = Vector2.Lerp(end, Position, 0.16f);
        }

        public override bool PreDraw(SpriteBatch sb) {
            //blend走Configure尾参mode,拉伸长度跟Position-end距离走
            Texture2D tex = PRTSharedAssets.UpdraftParticle.Value;
            sb.Draw(tex, Position - Main.screenPosition, null, new Color(255, 206, 180),
                (Position - end).ToRotation(), tex.Size() / 2f,
                new Vector2(CEUtils.getDistance(Position, end) / (float)tex.Width, Scale * 0.3f),
                SpriteEffects.None, 0);
            return false;
        }
    }


}
