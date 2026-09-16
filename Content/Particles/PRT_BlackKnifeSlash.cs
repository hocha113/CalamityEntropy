using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_BlackKnifeSlash : BasePRT
    {
        public bool Glow = true;
        public float width = 1;

        //纯sb.Draw没自定义批次,默认不开CanPool,斩击特效spawn也低
        //width每帧Parabola重算,池化忘Reset就带上一刀的宽度出生
        public override string Texture => "CalamityEntropy/Content/Particles/AbyssalLine";

        public PRT_BlackKnifeSlash Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 200;
        }

        public override void AI() {
            //1f-LifetimeCompletion是已过比例,Parabola(_,7)管斩击宽度鼓包,跟旧EParticle一致
            width = CEUtils.Parabola(1f - LifetimeCompletion, 7);
        }

        public override bool PreDraw(SpriteBatch sb) {
            //就一趟拉伸Draw,无shader无TriangleStrip无PostDraw第二刀
            //Texture挂AbyssalLine真画ACircle,跟ShadeDash白图占位同理,别"修好"成AbyssalLine
            Texture2D tex = PRTExtraTextures.ACircle.Value;
            //X拉满Y跟width走,拉反了就变成竖条而不是横斩
            sb.Draw(tex, Position - Main.screenPosition, null, Color, Rotation, tex.Size() / 2, new Vector2(Scale * 660 / tex.Width, width / tex.Height), SpriteEffects.None, 0);
            return false;
        }
    }


}
