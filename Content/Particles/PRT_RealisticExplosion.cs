using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //CanPool不开:首帧播音效,池化复用要么重播要么漏播
    //常规PRT桶,CruiserHead/CEUtils.spawn,不耦合CEVoidScreen/CEPixelScreen
    public class PRT_RealisticExplosion : BasePRT
    {
        public int frame = -1;
        public Vector2 size = Vector2.One;

        //真图是spr_realisticexplosion_N序列,Texture随便指白图堵框架路径校验
        public override string Texture => "CalamityEntropy/Assets/Extra/white";

        public PRT_RealisticExplosion Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 40;
        }

        public override void AI() {
            frame++;
            //旧OnSpawn播音,放首帧AI(frame从-1走到0)对齐spawn时机,别挪SetProperty也不开CanPool
            if (frame == 0)
                CEUtils.PlaySound("badexplosion", 0.8f, Position);
            if (frame > 32)
                Kill();
        }

        public override bool PreDraw(SpriteBatch sb) {
            if (frame >= 0 && frame <= 32) {
                //两帧共一张spr_realisticexplosion_N,RequestTex有缓存但别改成每帧无上限spawn
                Texture2D tex = CEUtils.RequestTex("CalamityEntropy/Content/Particles/realisticexplosion/spr_realisticexplosion_" + (frame / 2));
                sb.Draw(tex, Position - Main.screenPosition, null, Color, 0, tex.Size() / 2f, size * Scale, SpriteEffects.None, 0);
            }
            return false;
        }

        public bool Glow = true;
    }


}
