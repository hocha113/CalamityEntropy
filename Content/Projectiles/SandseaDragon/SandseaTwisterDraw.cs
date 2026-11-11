using CalamityEntropy.Content.NPCs.SandseaDragon;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SandseaDragon
{
    /// <summary>
    /// 禁戒旋风的画法,沿用原版沙尘龙卷:同一张沙环贴图自下而上层叠,每层多转约 92 度
    /// 层距随环的大小放大,每个像素上的叠层数与原版相近,整体不透明度不随尺寸变
    /// 只画屏幕纵向范围内的层,100 格高的边界旋风也只画看得见的那一截
    /// </summary>
    internal static class SandseaTwisterDraw
    {
        /// <summary>相邻两层的转角,原版是 5.1 像素层距乘 2π/20</summary>
        private const float SliceTwist = 1.602f;

        private static Texture2D RingTexture {
            get {
                Main.instance.LoadProjectile(ProjectileID.SandnadoHostile);
                return TextureAssets.Projectile[ProjectileID.SandnadoHostile].Value;
            }
        }

        /// <summary>直立的旋风柱,bottom 是底部中点的世界坐标</summary>
        /// <param name="diameter">参数是 0 到 1 的高度比例,返回该处的直径</param>
        /// <param name="fadeBottom">底端淡入段占全高的比例</param>
        /// <param name="density">叠层密度,1 是层距随环的大小放大,越大层越密、柱体越实;有判定的柱子要画得比原版沙尘龙卷更实</param>
        public static void DrawColumn(Vector2 bottom, float height, Func<float, float> diameter, float opacity, float spin = 1f,
            float fadeBottom = 0.08f, float fadeTop = 0.14f, float density = 1f) {
            if (opacity <= 0.01f || height <= 4f) {
                return;
            }
            Texture2D tex = RingTexture;
            Vector2 origin = tex.Size() / 2f;
            float viewTop = Main.screenPosition.Y - 140f;
            float viewBottom = Main.screenPosition.Y + Main.screenHeight + 140f;
            float time = Main.GlobalTimeWrappedHourly * 60f;
            int slice = 0;
            for (float y = 0f; y < height; slice++) {
                float t = y / height;
                float scale = diameter(t) / tex.Width;
                float worldY = bottom.Y - y;
                if (worldY > viewTop && worldY < viewBottom) {
                    float fade = MathHelper.Clamp(t / fadeBottom, 0f, 1f) * MathHelper.Clamp((1f - t) / fadeTop, 0f, 1f);
                    DrawRing(tex, origin, new Vector2(bottom.X, worldY), scale, fade * opacity, -time * (MathHelper.Pi / 50f) * spin + slice * SliceTwist);
                }
                y += Math.Max(5f, 5.1f * scale / density);
            }
        }

        /// <summary>
        /// 漏斗形的巨型龙卷:四股沙柱绕中轴螺旋上升,转到背面的那股压暗,外加一根淡的芯柱填体积
        /// grown 是已经卷起来的高度比例,龙卷从地面往上长
        /// </summary>
        public static void DrawFunnel(Vector2 bottom, float height, Func<float, float> halfWidth, float grown, float opacity) {
            if (opacity <= 0.01f || grown <= 0.01f) {
                return;
            }
            Texture2D tex = RingTexture;
            Vector2 origin = tex.Size() / 2f;
            float viewTop = Main.screenPosition.Y - 220f;
            float viewBottom = Main.screenPosition.Y + Main.screenHeight + 220f;
            float time = Main.GlobalTimeWrappedHourly * 60f;
            const int Strands = 4;
            const float Step = 13f;
            float shown = height * MathHelper.Clamp(grown, 0f, 1f);

            //背面的股先画,正面的后画
            for (int pass = 0; pass < 2; pass++) {
                int slice = 0;
                for (float y = 0f; y < shown; y += Step, slice++) {
                    float worldY = bottom.Y - y;
                    if (worldY < viewTop || worldY > viewBottom) {
                        continue;
                    }
                    float t = y / height;
                    float half = halfWidth(t);
                    float scale = MathHelper.Clamp(half * 0.9f / 30f, 1.2f, 3.4f);
                    float reach = Math.Max(0f, half - 30f * scale * 0.5f);
                    //顶端淡出,生长前沿也淡一段
                    float fade = MathHelper.Clamp(t / 0.05f, 0f, 1f) * MathHelper.Clamp((1f - t) / 0.12f, 0f, 1f) * MathHelper.Clamp((shown - y) / 140f, 0f, 1f);
                    for (int k = 0; k < Strands; k++) {
                        float phase = time * 0.07f + k * MathHelper.TwoPi / Strands + t * 9f;
                        float depth = MathF.Sin(phase);
                        if ((depth >= 0f) != (pass == 1)) {
                            continue;
                        }
                        float x = bottom.X + reach * MathF.Cos(phase);
                        DrawRing(tex, origin, new Vector2(x, worldY), scale, fade * opacity * (0.6f + 0.4f * depth), -time * (MathHelper.Pi / 40f) + slice * SliceTwist + k);
                    }
                }
                if (pass == 0) {
                    DrawColumn(bottom, shown, t => halfWidth(t * shown / height) * 1.15f, opacity * 0.4f, 1.4f, 0.05f, 0.2f);
                }
            }
        }

        private static void DrawRing(Texture2D tex, Vector2 origin, Vector2 worldPos, float scale, float strength, float rotation) {
            Color color = SandseaVfx.Sand * strength;
            color.A = (byte)(color.A * 0.5f);
            Main.EntitySpriteDraw(tex, worldPos - Main.screenPosition, null, color, rotation, origin, scale, SpriteEffects.None, 0);
        }
    }
}
