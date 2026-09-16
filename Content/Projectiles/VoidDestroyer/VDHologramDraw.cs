using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 全息绘制助手:把任意贴图(主要是原版生物/弹幕贴图)经 VDHologram 着色器画成染色扫描线投影。
    /// 着色器缺失时退回加法半透明染色,功能不丢
    /// </summary>
    public static class VDHologramDraw
    {
        public static readonly Color HellRed = new Color(255, 70, 70);
        public static readonly Color JungleGreen = new Color(90, 255, 130);
        public static readonly Color SkyBlue = new Color(90, 170, 255);

        private static Effect Shader => CEEffectAssets.VDHologram?.Value;

        /// <summary>进入全息绘制批次;之后每次 DrawPart 会按贴图尺寸重喂参数</summary>
        public static void Begin()
        {
            Effect shader = Shader;
            if (shader != null)
            {
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive, shader);
            }
            else
            {
                Main.spriteBatch.UseAdditive();
            }
        }

        public static void End()
        {
            Main.spriteBatch.ExitShaderRegion();
        }

        /// <summary>在 Begin/End 之间画一张贴图的全息版本</summary>
        public static void DrawPart(Texture2D tex, Vector2 pos, Rectangle? frame, Color color, float opacity, float rotation, Vector2 origin, float scale, SpriteEffects fx)
        {
            Effect shader = Shader;
            if (shader != null)
            {
                shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                shader.Parameters["uOpacity"]?.SetValue(opacity);
                shader.Parameters["uColor"]?.SetValue(color.ToVector3());
                shader.Parameters["uImageSize"]?.SetValue(tex.Size());
                shader.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(tex, pos, frame, Color.White, rotation, origin, scale, fx, 0f);
            }
            else
            {
                Main.spriteBatch.Draw(tex, pos, frame, color * (opacity * 0.8f), rotation, origin, scale, fx, 0f);
            }
        }

        /// <summary>单张全息绘制(自带 Begin/End)</summary>
        public static void Draw(Texture2D tex, Vector2 pos, Rectangle? frame, Color color, float opacity, float rotation, Vector2 origin, float scale, SpriteEffects fx)
        {
            Begin();
            DrawPart(tex, pos, frame, color, opacity, rotation, origin, scale, fx);
            End();
        }
    }
}
