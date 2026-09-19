using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 虚空驱逐舰的图鉴域主题:「轨道封锁」深空。近黑的深空封面配薰衣草紫封边、暗紫灰纸面;
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
