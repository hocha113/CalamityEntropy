using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>阿扎弗橙,阿扎弗工业档。铜橙字身逐字热浪起伏,字底一层薄烟,余烬火星自字底浮升</summary>
    public sealed class AzafureOrange : CERarity
    {
        public static readonly Color Copper = new(204, 71, 35);
        public static readonly Color CopperHot = new(255, 150, 90);
        public static readonly Color Soot = new(46, 18, 10);
        public static readonly Color EmberHot = new(255, 214, 120);
        public static readonly Color EmberCool = new(200, 60, 20);
        public static readonly Color SmokeTint = new(130, 70, 0);

        //同时最多存活的火星槽位;火星存活占周期比例;上浮速度 px/s
        private const int EmberSlots = 3;
        private const float EmberLife = 0.6f;
        private const float EmberRise = 26f;
        //热浪逐字起伏幅度 px,阴影与描边不随动,人眼读作热浪而非抖动
        private const float HeatAmp = 0.7f;

        public override Color BaseColor => Copper;

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            GlyphLayout layout = Layout(text, pos, scale);

            //字底两团缓慢反向旋转的薄烟,压在字后面
            Color smoke = Fade(SmokeTint, fade) * 0.35f;
            for (int k = 0; k < 2; k++) {
                float x = layout.Origin.X + layout.Width * (0.3f + 0.4f * k) + MathF.Sin(time * 0.7f + k * 2.1f) * 6f;
                DrawSmoke(sb, new Vector2(x, layout.BandY(0.9f)), layout.Height * 1.6f, smoke, time * 0.4f * (k == 0 ? 1f : -1f));
            }

            DrawOutline(sb, text, pos, Fade(Soot, fade), scale, 1.4f);

            //铜橙字身,逐字错相的热浪起伏与冷热明暗
            Color body = Fade(Copper, fade);
            Color hot = Fade(CopperHot, fade);
            for (int i = 0; i < layout.Count; i++) {
                float dy = MathF.Sin(time * 5.5f + i * 1.15f) * HeatAmp * scale.Y;
                float heat = 0.5f + 0.5f * MathF.Sin(time * 3.1f + i * 0.8f);
                DrawGlyph(sb, layout, i, new Vector2(0f, dy), Color.Lerp(body, hot, heat * 0.4f), scale);
            }

            //余烬火星:自字底浮升,带横向漂移与轻微摆动,冷却变暗后熄灭
            Color emberHot = Fade(EmberHot, fade);
            Color emberCool = Fade(EmberCool, fade);
            for (int k = 0; k < EmberSlots; k++) {
                float period = 1.1f + Hash01(k, 5) * 0.8f;
                float t = Cycle(time, period, Hash01(k, 9), out int index);
                if (t > EmberLife) {
                    continue;
                }
                float age = t * period;
                float x0 = layout.Origin.X + Hash01(index, k * 3 + 1) * layout.Width;
                float drift = (Hash01(index, k * 3 + 2) - 0.5f) * 18f;
                Vector2 p = new(
                    x0 + drift * age + MathF.Sin(age * 6f + index) * 1.5f,
                    layout.BandY(1f) - EmberRise * age);
                float alive = 1f - t / EmberLife;
                Color c = Color.Lerp(emberHot, emberCool, 1f - alive);
                DrawMote(sb, p, 7f * scale.X, c * (alive * 0.6f));
                DrawFleck(sb, p, 1.6f * scale.X, c * alive);
            }
        }
    }
}
