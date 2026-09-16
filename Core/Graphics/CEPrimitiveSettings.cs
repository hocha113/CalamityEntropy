using Terraria.Graphics.Shaders;

namespace CalamityEntropy.Core.Graphics
{
    /// <summary>
    /// 图元拖尾绘制配置。行为等效替代灾厄同名设置结构在本仓库被用到的子集。
    /// 构造参数名与顺序与灾厄原版前六位完全一致, 原调用点换类型名后命名实参
    /// (smoothen: / pixelate: / shader:)与位置实参均无需改动。
    /// </summary>
    public readonly struct CEPrimitiveSettings
    {
        /// <summary>逐顶点半宽委托。入参为拖尾进度(0-1)与该点屏幕坐标, 返回半宽(总宽的一半, 像素)。</summary>
        public delegate float VertexWidthFunction(float trailLengthInterpolant, Vector2 vertexPosition);

        /// <summary>逐顶点颜色委托。入参为拖尾进度(0-1)与该点屏幕坐标。</summary>
        public delegate Color VertexColorFunction(float trailLengthInterpolant, Vector2 vertexPosition);

        /// <summary>逐点偏移委托。入参为拖尾进度(0-1)与该点世界坐标, 返回附加偏移。</summary>
        public delegate Vector2 VertexOffsetFunction(float trailLengthInterpolant, Vector2 vertexPosition);

        public readonly VertexWidthFunction WidthFunction;
        public readonly VertexColorFunction ColorFunction;
        public readonly VertexOffsetFunction OffsetFunction;

        /// <summary>是否对输入点做 Catmull-Rom 平滑重采样。</summary>
        public readonly bool Smoothen;

        /// <summary>仅为签名兼容保留。本仓库没有像素化图元管线, 实测调用全为 false, true 会被当作 false 处理。</summary>
        public readonly bool Pixelate;

        /// <summary>绘制时应用的着色器; 为空时渲染器回落到顶点色直通着色器。</summary>
        public readonly MiscShaderData Shader;

        public CEPrimitiveSettings(VertexWidthFunction widthFunction, VertexColorFunction colorFunction, VertexOffsetFunction offsetFunction = null, bool smoothen = true, bool pixelate = false, MiscShaderData shader = null) {
            WidthFunction = widthFunction;
            ColorFunction = colorFunction;
            OffsetFunction = offsetFunction;
            Smoothen = smoothen;
            Pixelate = pixelate;
            Shader = shader;
        }
    }
}
