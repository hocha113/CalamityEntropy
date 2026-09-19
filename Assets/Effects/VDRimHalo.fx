// VDRimHalo — 虚空驱逐舰描边光晕(实心剪影)
// 把本体贴图整个当 alpha 遮罩涂成描边色(mask = c.a)。VoidDestroyer.Draw 在屏幕空间偏移叠画多抽、垫在本体之下:
// 本体压在上面,只剩剪影之外那一圈,合起来是一条宽度 = 偏移半径的外扩描边带。
// 这样做是因为本体贴图四边只有 2~4px 留白,任何在贴图内做膨胀的着色器都会被四边裁掉(VDRimLight 的缘带只能贴在内侧 2 texel),
// 独立四边形往外挪不吃留白,也不需要 RenderTarget,复古 / 迷幻光照下照常。
// 与 VDRimLight 参数完全同名(两层噪声、uErode 侵蚀、uHeat/uFlash 热色、uNoiseScroll/uRadialScroll 流向),宿主同一份 ApplyRimShader 喂两支;
// uErode 高 = 外环碎成向外逸散的丝,低 = 内环实心贴身。
// 用法:EnterShaderRegion(BlendState.Additive, shader),噪声图绑 GraphicsDevice.Textures[1](LinearWrap),
// 喂全部参数后 Passes[0].Apply;顶点色(Draw 的 color 参数)整体乘在输出上,叠画各抽的亮度分摊走它。
// uOpacity 允许大于 1(加法混合下就是更亮)。输出预乘 alpha。无动态分支。
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float uTime;
float uOpacity;
float3 uColor;
float3 uHotColor;
float uHeat;
float uFlash;
float uErode;
// 直角层噪声图样的平移量(噪声 UV,宿主 wrap 在 [0,1))
float2 uNoiseScroll;
// 极坐标层噪声图样沿径向的位移(>0 向外流,<0 向内吸)
float uRadialScroll;
// 极坐标层的混合权重 0..1
float uRadialMix;
// 当前绘制帧在整张贴图里的 UV 中心
float2 uFrameCenter;
float2 uImageSize;

float4 PixelFunc(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 c = tex2D(uImage0, coords);
    float mask = c.a;

    // 像素坐标(整张贴图空间),噪声取样与绘制缩放无关;与 VDRimLight 同一套图样,两支着色器的丝对得上
    float2 pix = coords * uImageSize;
    float nA = tex2D(uImage1, frac(pix / 72.0 - uNoiseScroll)).r;
    float nA2 = tex2D(uImage1, frac(pix / 130.0 + float2(0.31, 0.77) + uNoiseScroll)).r;
    float cart = nA * 0.6 + nA2 * 0.4;

    float2 rel = pix - uFrameCenter * uImageSize;
    float ang = atan2(rel.y, rel.x) / 6.2831853 + 0.5;
    float rad = length(rel) / 72.0;
    float nB = tex2D(uImage1, frac(float2(ang * 8.0, rad - uRadialScroll))).r;
    float nB2 = tex2D(uImage1, frac(float2(ang * 5.0 + 0.37, rad * 0.55 + uRadialScroll))).r;
    float polar = nB * 0.6 + nB2 * 0.4;

    float wisp = smoothstep(0.26, 0.76, lerp(cart, polar, uRadialMix));
    mask *= lerp(1.0, wisp, uErode);

    // 热色:蓄力与出手把光晕拉向白热
    float heat = saturate(uHeat + uFlash);
    float3 col = lerp(uColor, uHotColor, heat);

    // 爆闪期间高频闪烁 + 亮度抬升
    float flicker = 1.0 + uFlash * 0.25 * sin(uTime * 40.0);
    float a = mask * uOpacity * (1.0 + uFlash) * flicker;
    return float4(col * a, a) * baseColor;
}

technique Technique1
{
    pass HaloPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
