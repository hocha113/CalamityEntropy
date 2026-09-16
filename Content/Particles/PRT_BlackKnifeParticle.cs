using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_BlackKnifeParticle : BasePRT
    {
        //oldPos轨迹List,CanPool没开,Reset忘Clear拖尾会闪
        public List<Vector2> oldPos = new List<Vector2>();
        public bool Glow = true;

        //借弹幕刀贴图,不是Particles目录下的同名资源
        public override string Texture => "CalamityEntropy/Content/Projectiles/BlackKnife";

        public PRT_BlackKnifeParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            for (float i = 0; i < 1; i += 0.025f) {
                oldPos.Add(Position - (1 - i) * Velocity);
                if (oldPos.Count > 400)
                    oldPos.RemoveAt(0);
            }
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            //先淡拖尾再本体,顺序反了拖尾会盖刀
            for (int i = 0; i < oldPos.Count; i++) {
                sb.Draw(tex, oldPos[i] - Main.screenPosition, null, Color * 0.1f * ((float)i / oldPos.Count), Rotation, tex.Size() / 2, Scale, SpriteEffects.None, 0);
            }
            sb.Draw(tex, Position - Main.screenPosition, null, Color, Rotation, tex.Size() / 2, Scale, SpriteEffects.None, 0);
            return false;
        }
    }


}
