using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 虚空驱逐舰的图鉴域主题:「轨道封锁」深空。近黑的深空封面配薰衣草紫封边、暗紫灰纸面;
    /// 书后是星野 + 六边形封锁力场网(向书外渐显,一圈脉冲波缓缓扩散)+ 星云背光,
    /// 书脊上有一道全息扫描线来回巡游,底封边一排封锁刻度亮点顺次点亮
    /// </summary>
    internal sealed class VoidDestroyerLogTheme : CEBossLogTheme
    {
        public static readonly VoidDestroyerLogTheme Instance = new();

        public override Color Cover => new(14, 8, 30);
        public override Color CoverEdge => new(160, 100, 240);
        public override Color Spine => new(9, 5, 20);
        public override Color Paper => new(30, 22, 48);
        public override Color PaperLight => new(150, 120, 210);
        public override Color Rule => new(120, 80, 180);
        public override Color Accent => new(205, 130, 255);
        public override Color Muted => new(165, 145, 205);
        public override Color Dim => new(4, 2, 10);
        public override Color SkyTop => VDVfx.SkyTop;
        public override Color SkyBottom => VDVfx.SkyHorizon;

        public override void DrawAmbienceDetail(SpriteBatch sb, Rectangle screen, Rectangle book, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }

            //星云背光:书右上一团紫云、左下一团更深的
            CEPortraitDraw.Glow(sb, new Vector2(book.Right + 140f, book.Y - 80f), 560f, VDVfx.SkyNebula * (0.55f * blend));
            CEPortraitDraw.Glow(sb, new Vector2(book.X - 170f, book.Bottom + 70f), 460f, VDVfx.VoidDeep * (0.28f * blend));

            //星野:确定性散布,慢闪;每 7 颗里出一颗四芒星
            for (int i = 0; i < 110; i++) {
                float hx = CEPortraitDraw.Hash01(i, 1.3f);
                float hy = CEPortraitDraw.Hash01(i, 7.1f);
                Vector2 p = new(screen.X + hx * screen.Width, screen.Y + hy * screen.Height);
                if (book.Contains((int)p.X, (int)p.Y)) {
                    continue;
                }
                float tw = 0.55f + 0.45f * MathF.Sin(time * (0.8f + hx * 1.6f) + i * 1.7f);
                float a = (0.22f + 0.5f * tw) * blend;
                if (i % 7 == 0) {
                    CEPortraitDraw.Star(sb, p, 3.5f + 2f * tw, VDVfx.RiftWhite with { A = 0 } * (a * 0.8f));
                }
                else {
                    float s = 1f + hy * 1.2f;
                    CEPortraitDraw.Fill(sb, p, new Vector2(s, s), VDVfx.RiftWhite with { A = 0 } * a);
                }
            }

            //六边形封锁力场:向书外渐显,一圈脉冲波从书心扩散
            Vector2 center = book.Center.ToVector2();
            float inner = MathF.Sqrt(book.Width * book.Width + book.Height * book.Height) * 0.5f;
            float wave = (time * 150f) % 1500f;
            CEPortraitDraw.HexGrid(sb, new Vector2(screen.X, screen.Y), new Vector2(screen.Right, screen.Bottom), 58f,
                new Vector2(time * 5f, time * 2.5f), c => {
                    float d = Vector2.Distance(c, center);
                    float baseA = MathHelper.Clamp((d - inner * 0.92f) / 420f, 0f, 1f) * 0.14f;
                    float dw = d - wave;
                    float pulse = MathF.Exp(-dw * dw / (2f * 70f * 70f)) * 0.22f;
                    return (baseA + pulse) * blend;
                }, VDVfx.SkyRing with { A = 0 }, 1f);

            //裂隙丝:几道斜向细亮线缓慢横移
            for (int i = 0; i < 6; i++) {
                float ph = i * 53.7f;
                float len = 90f + i % 3 * 40f;
                float x = (time * (18f + i * 5f) + ph * 11f) % (screen.Width + len * 2f) - len;
                float y = (ph * 4.3f) % screen.Height;
                Vector2 a = new(x, y);
                Vector2 b = a + new Vector2(len, -len * 0.35f);
                float alpha = (0.05f + 0.04f * MathF.Sin(ph + time * 0.9f)) * blend;
                CEPortraitDraw.Line(sb, a, b, 1.2f, VDVfx.RiftWhite with { A = 0 } * alpha);
            }
        }

        public override void DrawOrnament(SpriteBatch sb, Rectangle book, Rectangle spine, Rectangle bottom, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //书脊全息扫描线:一道细亮带自上而下巡游,带一段软尾
            if (spine.Height > 20) {
                float y = spine.Y + (time * 95f) % spine.Height;
                Color line = VDVfx.VoidWhite with { A = 0 };
                CEPortraitDraw.Fill(sb, new Vector2(spine.X, y), new Vector2(spine.Width, 2f), line * (0.55f * blend));
                for (int i = 1; i <= 6; i++) {
                    float ty = y - i * 3f;
                    if (ty < spine.Y) {
                        break;
                    }
                    CEPortraitDraw.Fill(sb, new Vector2(spine.X, ty), new Vector2(spine.Width, 3f), line * (0.18f * (1f - i / 7f) * blend));
                }
                //脊心一点核光缓慢呼吸
                float glowY = spine.Center.Y + MathF.Sin(time * 0.5f) * (spine.Height * 0.3f);
                CEPortraitDraw.Glow(sb, new Vector2(spine.Center.X, glowY), new Vector2(spine.Width * 1.6f, 70f), VDVfx.VoidPurple * (0.35f * blend));
            }
            //底封边封锁刻度:小方块一排,一段亮度沿排顺次点亮
            if (bottom.Height >= 3) {
                int h = Math.Max(1, bottom.Height - 2);
                float run = (time * 160f) % (bottom.Width + 120f) - 60f;
                for (float x = bottom.X + 6; x < bottom.Right - 6; x += 14f) {
                    float d = MathF.Abs(x - (bottom.X + run));
                    float hot = MathHelper.Clamp(1f - d / 90f, 0f, 1f);
                    Color c = Color.Lerp(VDVfx.SkyRing, VDVfx.VoidWhite, hot) with { A = 0 } * ((0.2f + 0.6f * hot) * blend);
                    CEPortraitDraw.Fill(sb, new Vector2(x, bottom.Y + 1), new Vector2(2f, h), c);
                }
            }
        }
    }
}
