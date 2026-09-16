// VDHologram — 虚空驱逐舰全息投影着色器(红恶魔/丛林陆龟/小白龙等原版贴图的全息化)
// 对原版生物贴图做:按 uColor 整体染色 + 沿贴图 y 方向滚动的扫描线 + 轻微闪烁 + 轮廓增亮。
// 用法:EnterShaderRegion(BlendState.Additive, shader) 后 Passes[0].Apply(),再按常规 spriteBatch.Draw 画原版贴图。
// 参数由调用方每次绘制前喂:uColor(染色)、uOpacity(整体透明度)、uImageSize(贴图像素尺寸)、uTime。
sampler uImage0 : register(s0);

float uTime;
float uOpacity;
float3 uColor;
float2 uImageSize;

float4 PixelFunc(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 c = tex2D(uImage0, coords);
    // 贴图透明处直接丢弃(加法混合下返回 0 即无贡献)
    float mask = step(0.01, c.a);

    // 灰度亮度决定明暗,颜色统一换成全息色
    float lum = dot(c.rgb, float3(0.3, 0.59, 0.11));
    float3 col = uColor * (0.35 + lum * 0.9);

    // 扫描线:每 4 像素一条,随时间向下滚动
    float scan = 0.72 + 0.28 * sin(coords.y * uImageSize.y * 1.5708 + uTime * 6.0);
    // 整体闪烁
    float flicker = 0.92 + 0.08 * sin(uTime * 21.0);

    // 轮廓增亮:与四邻 alpha 的差
    float2 px = 1.0 / uImageSize;
    float aL = tex2D(uImage0, coords - float2(px.x, 0)).a;
    float aR = tex2D(uImage0, coords + float2(px.x, 0)).a;
    float aU = tex2D(uImage0, coords - float2(0, px.y)).a;
    float aD = tex2D(uImage0, coords + float2(0, px.y)).a;
    float edge = saturate(4.0 * c.a - aL - aR - aU - aD);
    col += uColor * edge * 0.9 + edge * 0.35;

    float a = c.a * uOpacity * scan * flicker * mask;
    return float4(col * a, a) * baseColor;
}

technique Technique1
{
    pass HologramPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
