using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //跟PRT_Abyssal(metaball数据类)不是一回事,这是常规PreDraw的斩击线装饰
    public class PRT_AbyssalLine : BasePRT
    {
        public bool Glow = true;
        public Color spawnColor = new Color(190, 190, 255);
        public Color endColor = Color.Blue;
        public float xscale = 0;
        public float xdec = 0.87f;
        public float xadd = 3.2f;
        public float lx = 3;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            spawnColor = new Color(190, 190, 255);
            endColor = Color.Blue;
            xscale = 0f;
            xdec = 0.87f;
            xadd = 3.2f;
            lx = 3f;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/AbyssalLine";

        public PRT_AbyssalLine Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 50;   //旧默认50,也可能被AI提前Kill
        }

        public override void AI() {
            lx *= 0.88f;
            xscale += xadd;
            xadd *= xdec;
            Velocity *= 0.96f;
            //lx缩到0.01且还没到最后一帧就自杀,不必等Lifetime到点
            if (Time < Lifetime - 1 && lx <= 0.01f)
                Kill();
        }

        public override bool PreDraw(SpriteBatch sb) {
            //画的是ACircle拉成条带不是AbyssalLine贴图,双层叠出刃光
            float remaining = 1f - LifetimeCompletion;   //旧lifePercent 1→0剩余,Completion反的,这里减回来
            Texture2D tex = PRTExtraTextures.ACircle.Value;
            Color lerpClr = Color.Lerp(endColor, spawnColor, remaining);
            sb.Draw(tex, Position - Main.screenPosition, null, lerpClr * remaining * 0.7f, Rotation, tex.Size() / 2, new Vector2(0.6f * (xscale + 0.1f), 0.56f * lx) * Scale, SpriteEffects.None, 0);
            sb.Draw(tex, Position - Main.screenPosition, null, lerpClr * remaining, Rotation, tex.Size() / 2, new Vector2(0.6f * xscale, 0.2f * lx) * Scale, SpriteEffects.None, 0);
            return false;
        }
    }


}
