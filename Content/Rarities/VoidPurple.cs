using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>虚空紫,终局档。近黑字身逐字明暗呼吸裹紫描边,文本回声按周期向四方扩散淡出(保留旧签名),偶发竖向裂隙微光</summary>
    public sealed class VoidPurple : CERarity
    {
        /// <summary>拾取主色</summary>
        public static readonly Color Void = new(106, 40, 190);
        public static readonly Color Core = new(20, 16, 25);
        public static readonly Color CoreLit = new(48, 30, 70);
        public static readonly Color Rim = new(160, 100, 255);
        public static readonly Color Echo = new(190, 50, 190);
        public static readonly Color EchoFar = new(160, 0, 180);

        //回声层数、扩散最远像素、一层从字身扩散到消失的周期 s
        private const int EchoLayers = 2;
        private const float EchoReach = 12f;
        private const float EchoPeriod = 1f;
        //裂隙微光槽位与存活占周期比例
        private const int RiftSlots = 2;
        private const float RiftLife = 0.18f;

        public override Color BaseColor => Void;

        //前后缀降档只在自有阶梯内走(2026-09-19 撤掉灾厄 CosmicPurple/BurnishedAuric 弱引用)
        public override int GetPrefixedRarity(int offset, float valueMult) => offset switch {
            -2 => ModContent.RarityType<AbyssalBlue>(),
            -1 => ModContent.RarityType<Golden>(),
            _ => Type,
        };

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            GlyphLayout layout = Layout(text, pos, scale);

            DrawGlowBand(sb, layout, Fade(Rim, fade) * 0.14f, 24f, 0.9f);

            //回声:整段文本的副本沿上下左右四向扩散并淡出,两层错半个周期
            for (int layer = 0; layer < EchoLayers; layer++) {
                float s = time / EchoPeriod + layer / (float)EchoLayers;
                s -= MathF.Floor(s);
                float reach = s * EchoReach * scale.X;
                Color c = Fade(Color.Lerp(Echo, EchoFar, s), fade) * ((1f - s) * 0.9f);
                DrawText(sb, text, pos + new Vector2(-reach, 0f), c, scale);
                DrawText(sb, text, pos + new Vector2(reach, 0f), c, scale);
                DrawText(sb, text, pos + new Vector2(0f, -reach), c, scale);
                DrawText(sb, text, pos + new Vector2(0f, reach), c, scale);
            }

            DrawOutline(sb, text, pos, Fade(Rim, fade), scale, 1.3f);

            //近黑字身,逐字错相的明暗呼吸
            Color core = Fade(Core, fade);
            Color lit = Fade(CoreLit, fade);
            for (int i = 0; i < layout.Count; i++) {
                float lerp = 0.5f + 0.5f * MathF.Sin(time * -6f + i * 3f / Math.Max(1, layout.Count));
                DrawGlyph(sb, layout, i, Vector2.Zero, Color.Lerp(core, lit, lerp), scale);
            }

            //裂隙微光:字身上偶发一道竖向的紫色细缝,一闪即逝
            Color rift = Fade(Rim, fade);
            for (int k = 0; k < RiftSlots; k++) {
                float period = 1.3f + Hash01(k, 21) * 1.1f;
                float t = Cycle(time, period, Hash01(k, 23), out int index);
                if (t > RiftLife) {
                    continue;
                }
                float intensity = MathF.Sin(MathHelper.Pi * t / RiftLife);
                float x = layout.Origin.X + Hash01(index, k * 3 + 1) * layout.Width;
                float y0 = layout.BandY(0.1f + 0.3f * Hash01(index, k * 3 + 2));
                float h = layout.Height * (0.25f + 0.25f * Hash01(index, k * 3 + 3));
                DrawVLine(sb, new Vector2(x, y0), h, 1f * scale.X, Additive(rift) * intensity);
                DrawMote(sb, new Vector2(x, y0 + h * 0.5f), 6f * scale.X, rift * (0.5f * intensity));
            }
        }
    }
}
