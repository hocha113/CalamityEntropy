// VDBeamTapered — 虚空驱逐舰透视射线(梯形光锥:红射线 / 主炮越肩锥 / 瞄准锥 / 点阵射线 / 导引锥)
// 外观与 VDVoidBeam 同一套(双向滚动湍流 + 白热核心 + 边缘辉光 + 端帽),差别在顶点纹理坐标的含义:
//   coords.x = 沿轴像素(0..uLengthPx),coords.y = 到轴线的有符号横向像素(边缘处 = ±四边形半宽)。
// 两者都是屏幕位置的线性函数,跨任意三角剖分都精确插值。用归一化 UV(0..1)画两端宽度不等的梯形时,
// 仿射插值会让中线在两三角形共享对角线的中点处偏离轴线 (hS - hE) / 2 像素,肉眼就是「一条在中间折断的亮线」,
// 噪声图样也沿对角线剪切;像素坐标把这个根因整个绕开。
// 纵深线索:
//   1. 透视校正的沿轴参数 f:w = 1 / Scale(Z),f = t·wS / ((1 - t)·wE + t·wS),噪声按 f 铺,远端图样自然压缩、近端拉开;
//   2. 雾色与远端变暗按 f 在两端预算值(uFogStart/End、uAlphaStart/End,由 C# 按 VDDepth 算)之间插值。
// 端帽 uCap = (起点渐隐比例, 终点渐隐比例),按屏幕 t 计;近端顶进「枢」时由 C# 传 0(C# 钳到 ≥ 0.002 防 smoothstep 退化)。
// 用法:Begin(Immediate, Additive, ..., shader) 后 Passes[0].Apply,再 DrawUserPrimitives 一个四顶点 TriangleStrip;
// 噪声图绑 GraphicsDevice.Textures[1](LinearWrap);s0 不取样。顶点色整体乘在输出上。输出预乘 alpha。无动态分支。
sampler uImage1 : register(s1);

float uTime;
float3 uColor;
float3 uColor2;
// 可见半宽 / 四边形半宽(与 VDVoidBeam 的 uEnvelope 同义)
float uEnvelope;
float uOpacity;
float uSeed;
// 轴长(屏幕像素)
float uLengthPx;
// 起点 / 终点的四边形半宽(屏幕像素)
float uHalfStart;
float uHalfEnd;
// 起点 / 终点的 1 / Scale(Z)(平面为 1,越远越大,镜头前小于 1)
float uWStart;
float uWEnd;
// 噪声沿射线全长的瓦片数
float uTile;
// 两端的雾化量与深度透明度(C# 按 VDDepth.FogAmount / VDDepth.Alpha 预算)
float uFogStart;
float uFogEnd;
float uAlphaStart;
float uAlphaEnd;
float3 uFogColor;
// 端帽渐隐比例(起, 终),按屏幕 t
float2 uCap;

float4 PixelFunc(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float t = saturate(coords.x / max(uLengthPx, 1.0));
    float halfQuad = max(lerp(uHalfStart, uHalfEnd, t), 0.001);
    // d = 0 轴线 → 1 四边形边缘
    float across = coords.y / halfQuad;
    float v = across * 0.5 + 0.5;

    // 透视校正的沿轴参数:1/w 在屏幕上线性,f/w 也线性,解出 f
    float f = t * uWStart / max((1.0 - t) * uWEnd + t * uWStart, 0.0001);

    // 两层噪声反向滚动,按 f 铺:远端压缩、近端拉开就是纵深
    float tile = max(uTile, 1.0);
    float n1 = tex2D(uImage1, float2(frac(f * tile * 0.5 - uTime * 1.7 + uSeed), frac(v * 0.9 + uSeed))).r;
    float n2 = tex2D(uImage1, float2(frac(f * tile * 0.23 + uTime * 0.9 + uSeed * 2.0), frac(v * 0.6 + uTime * 0.3))).r;
    float noise = n1 * 0.6 + n2 * 0.4;

    // 半宽随噪声呼吸
    float halfWidth = max(uEnvelope, 0.001) * (0.72 + 0.28 * noise);
    float d = abs(across) / halfWidth;
    float body = saturate(1.0 - d);
    float core = pow(saturate(1.0 - d * 1.8), 4.0);
    float rim = pow(saturate(1.0 - abs(d - 0.85) * 6.0), 2.0) * 0.6;

    // 端帽:两端各按 uCap 渐隐
    float cap = smoothstep(0.0, uCap.x, t) * (1.0 - smoothstep(1.0 - uCap.y, 1.0, t));

    float3 col = uColor * (body * (0.55 + 0.45 * noise)) + uColor2 * core + uColor * rim + core * 0.6;

    // 雾化:按亮度向雾色靠,再乘深度透明度;远端暗而冷,近端亮而饱和(自发光吃几成雾由 C# 在两端预算值里乘好)
    float fog = lerp(uFogStart, uFogEnd, f);
    float lum = dot(col, float3(0.333, 0.333, 0.333));
    col = lerp(col, uFogColor * (lum + 0.15), fog);
    float aZ = lerp(uAlphaStart, uAlphaEnd, f);

    float a = saturate(body * 0.85 + core + rim) * cap * uOpacity * aZ;
    return float4(col * a, a) * baseColor;
}

technique Technique1
{
    pass TaperedPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
