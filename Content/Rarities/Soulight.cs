using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>魂光白,特殊档。白字身裹冷蓝描边,横向魂光带呼吸,一两缕灵火自字身缓慢上飘</summary>
    public sealed class Soulight : CERarity
    {
        //横向光带贴图(256x32),加载期由 VaultLoaden 赋值,服务端为 null
        [VaultLoaden("CalamityEntropy/Assets/Extra/Soulight")]
        private static Asset<Texture2D> soulightTex;

        /// <summary>拾取主色</summary>
        public static readonly Color Soul = new(220, 240, 255);
        public static readonly Color Body = new(210, 240, 255);
        public static readonly Color ColdEdge = new(40, 140, 255);
        public static readonly Color Wisp = new(180, 220, 255);

        //灵火槽位数;存活占周期比例;上飘速度 px/s
        private const int WispSlots = 2;
        private const float WispLife = 0.8f;
        private const float WispRise = 18f;

        public override Color BaseColor => Soul;

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            GlyphLayout layout = Layout(text, pos, scale);

            //横向魂光带:沿行宽拉伸,缓慢呼吸
            Texture2D band = soulightTex?.Value;
            if (band != null) {
                float breath = Breath(time, 2.4f, 0.65f, 0.9f);
                Vector2 center = new(layout.MidX + 1f, pos.Y + layout.Height / 3f);
                Vector2 bandScale = new((layout.Width + 14f) / band.Width, MathF.Max(4f, layout.Height - 8f) / band.Height);
                sb.Draw(band, center, null, Additive(Color.White) * (fade * breath), 0f, band.Size() * 0.5f, bandScale, SpriteEffects.None, 0f);
            }

            DrawOutline(sb, text, pos, Fade(ColdEdge, fade), scale, 1.2f);
            DrawText(sb, text, pos, Fade(Body, fade), scale);

            //灵火:自字身缓慢上飘并左右轻摆,淡入淡出
            Color wisp = Fade(Wisp, fade);
            for (int k = 0; k < WispSlots; k++) {
                float period = 2.2f + Hash01(k, 31) * 1.2f;
                float t = Cycle(time, period, Hash01(k, 37), out int index);
                if (t > WispLife) {
                    continue;
                }
                float age = t * period;
                float alive = MathF.Sin(MathHelper.Pi * t / WispLife);
                Vector2 p = new(
                    layout.Origin.X + Hash01(index, k * 3 + 1) * layout.Width + MathF.Sin(age * 3f + index) * 2.5f,
                    layout.BandY(0.8f) - WispRise * age);
                DrawMote(sb, p, (9f + 4f * alive) * scale.X, wisp * (alive * 0.8f));
                DrawFleck(sb, p, 1.4f * scale.X, Color.White * (fade * alive * 0.9f));
            }
        }
    }
}
