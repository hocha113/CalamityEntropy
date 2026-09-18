// VDDepthFog — 虚空驱逐舰纵深雾化着色器(退入深处的本体与弹幕贴图)
// 对任意贴图做:s1 噪声随时间微扰采样坐标(深空热闪)+ 4 抽菱形模糊 + 去饱和 + 向雾色插值,
// 输出预乘 alpha,走 AlphaBlend。远景层里的东西都经它一遍,雾量由 VDDepth.FogAmount 按 Z 给。
// 参数由调用方每次绘制前喂:uFog(雾量 0..1)、uFogColor、uDesat(去饱和 0..1)、uBlur(模糊半径,UV)、
// uShimmer(噪声扰动幅度,UV)、uOpacity、uTime。
// 无动态分支。
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float uTime;
float uOpacity;
float uFog;
float3 uFogColor;
float uDesat;
float2 uBlur;
float uShimmer;

float4 PixelFunc(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // 深空热闪:噪声两向流动,幅度随雾量放大(越远晃得越明显)
    float2 n = tex2D(uImage1, coords * 0.8 + float2(uTime * 0.06, -uTime * 0.04)).rg - 0.5;
    float2 uv = coords + n * uShimmer * (0.4 + 0.6 * uFog);

    // 4 抽菱形模糊 + 双权中心
    float4 c = tex2D(uImage0, uv) * 2.0;
    c += tex2D(uImage0, uv + float2(uBlur.x, 0.0));
    c += tex2D(uImage0, uv - float2(uBlur.x, 0.0));
    c += tex2D(uImage0, uv + float2(0.0, uBlur.y));
    c += tex2D(uImage0, uv - float2(0.0, uBlur.y));
    c /= 6.0;

    // 去饱和再向雾色沉:雾色按雾量盖上去,但保留贴图明暗(远处的机体是暗影不是色块)
    float lum = dot(c.rgb, float3(0.3, 0.59, 0.11));
    float3 rgb = lerp(c.rgb, float3(lum, lum, lum), uDesat);
    rgb = lerp(rgb, uFogColor * (0.5 + lum), uFog);

    float a = c.a * uOpacity;
    return float4(rgb * a, a) * baseColor;
}

technique Technique1
{
    pass DepthFogPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
