using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //stick跟NPC走,CanPool不开,Reset里置null也怕池子里攒活引用
    public class PRT_APRCAlarm : BasePRT
    {
        public bool Glow = true;
        public NPC stick = null;

        public override string Texture => "CalamityEntropy/Assets/Extra/APRCAlarm";

        //blend走Configure尾参mode,跟Glow模板一套
        public PRT_APRCAlarm Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            if (stick != null && !stick.active)
                stick = null;
            if (stick != null)
                Position = stick.Center;
        }

        public override bool PreDraw(SpriteBatch sb) {
            //alpha走0→1(elapsed/0.6f封顶),不是AI里1f-LifetimeCompletion那套
            float elapsed = Time / (float)Lifetime;
            float alpha = elapsed / 0.6f;
            if (alpha > 1)
                alpha = 1;
            Texture2D o = PRTExtraTextures.APRCAlarm2.Value;
            Texture2D tex = PRTExtraTextures.APRCAlarm.Value;
            float orot = 2 - CEUtils.Parabola(alpha * 0.5f, 2);
            float drawScale = 2 - alpha;
            sb.Draw(o, Position - Main.screenPosition, null, Color * alpha * Opacity, orot + Rotation, o.Size() / 2f, Scale * drawScale / 2f, SpriteEffects.None, 0);
            sb.Draw(tex, Position - Main.screenPosition, null, Color * alpha * Opacity, Rotation, o.Size() / 2f, Scale / 2f, SpriteEffects.None, 0);
            return false;
        }
    }


}
