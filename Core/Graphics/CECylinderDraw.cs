using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Core.Graphics
{
    /// <summary>
    /// 用 Cylinder 着色器把一张平面贴图卷成柱面的通用绘制助手。
    /// 与屏幕特效管线无关,任何绘制时机都能直接调用
    /// </summary>
    internal static class CECylinderDraw
    {
        /// <summary>
        /// 画一段柱面
        /// </summary>
        /// <param name="tex">柱面表面贴图,会沿周向平铺</param>
        /// <param name="pos">屏幕坐标的柱心</param>
        /// <param name="blend">混合状态</param>
        /// <param name="Height">取用贴图高度的比例</param>
        /// <param name="rad">柱面半径参数,喂给着色器</param>
        /// <param name="rot">周向滚动相位</param>
        /// <param name="tiles">周向平铺次数</param>
        /// <param name="FullRot">整体旋转</param>
        /// <param name="inner">true 时画内壁</param>
        /// <param name="startBatch">false 表示调用方已有活跃批次,本函数负责先 End 再用 begin_ 还原</param>
        public static void DrawCylinder(Texture2D tex, Vector2 pos, Color color, BlendState blend, float Height = 1, float scale = 1, float rad = 0.5f, float rot = 0, int tiles = 2, float FullRot = 0, bool inner = false, bool startBatch = true) {
            Effect shader = EffectLoader.Cylinder?.Value;
            if (shader == null)
                return;
            if (!startBatch)
                Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.ZoomMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            shader.Parameters["radius"].SetValue(rad);
            shader.Parameters["rotation"].SetValue(rot);
            shader.Parameters["tileCount"].SetValue(tiles);
            shader.Parameters["innerWall"].SetValue(inner ? 1 : 0);
            Main.spriteBatch.Draw(tex, pos, new Rectangle(0, 0, tex.Width, (int)(tex.Height * Height)), color, FullRot, new Vector2(tex.Width * 0.5f, tex.Height * Height * 0.5f), scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            if (!startBatch)
                Main.spriteBatch.begin_();
        }
    }
}
