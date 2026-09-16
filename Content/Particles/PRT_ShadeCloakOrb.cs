using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_ShadeCloakOrb : BasePRT
    {
        public bool Glow = true;
        //odp轨迹List,池化忘Clear会闪,门户系低频也不差这点GC
        //还绑PlayerIndex,复用实例轨迹会挂到上一个玩家身上,服了
        public List<Vector2> odp = new List<Vector2>();
        public int maxLength = 12;
        public int PlayerIndex = 0;

        public override string Texture => "CalamityEntropy/Assets/Extra/a_circle";

        public PRT_ShadeCloakOrb Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 160;
        }

        public override void AI() {
            Vector2 lp = Position;
            //末8tick吸向Vector2.Zero是旧门户收束,不是世界原点bug
            if (Lifetime - Time < 8) {
                Velocity *= 0;
                Position = Vector2.Lerp(Position, Vector2.Zero, (1 - ((Lifetime - Time) / 8f)) * 0.8f);
            }
            else {
                Velocity *= 0.98f;
            }
            //0.1步进插10个点进odp,不是框架位移,别关ShouldUpdatePosition——这类没关,靠Velocity常规走
            for (float i = 0.1f; i <= 1; i += 0.1f)
                AddPoint(Vector2.Lerp(lp, Position, i));
        }

        public void AddPoint(Vector2 pos) {
            odp.Insert(0, pos);
            if (odp.Count > maxLength)
                odp.RemoveAt(odp.Count - 1);
        }

        public override bool PreDraw(SpriteBatch sb) {
            Vector2 worldPos = PlayerIndex.ToPlayer().Center;
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            //就一趟sb.Draw链,无shader无TriangleStrip;i小=轨迹头,i大=尾,sz算法两阶段别改反
            for (int i = 0; i < odp.Count; i++) {
                float sz = (i + 1) / (float)odp.Count;
                if (Lifetime - Time > 60)
                    sz = 1 - (i / (float)odp.Count);
                sb.Draw(tex, odp[i] + worldPos - Main.screenPosition, null, Color * 0.32f, Rotation, tex.Size() / 2f, Scale * 0.12f * sz, SpriteEffects.None, 0);
            }
            return false;
        }
    }


}
