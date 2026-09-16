// VDVoidBeam — 虚空驱逐舰能量射线(轨道光柱 / 湮灭主炮 / 红射线)
// 画在一张沿射线拉伸的白条上:coords.x = 沿射线 0..1,coords.y = 横向 0..1。
// 双向反向滚动的两层噪声让能量在管内流动并让边缘呼吸;白热核心用高次幂衰减;边缘一圈辉光带;两端端帽渐隐。
// 用法:EnterShaderRegion(BlendState.Additive, shader),噪声图绑 GraphicsDevice.Textures[1](LinearWrap),
// 喂 uColor(主色)/uColor2(核心热色)/uEnvelope(宽度包络 0..1)/uLength(长宽比,噪声按它铺,免拉伸)/uOpacity/uSeed 后 Passes[0].Apply。
// 无动态分支。
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float uTime;
float3 uColor;
float3 uColor2;
float uEnvelope;
float uLength;
float uOpacity;
float uSeed;

float4 PixelFunc(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 baseTex = tex2D(uImage0, coords);
    float along = coords.x;
    float across = coords.y * 2.0 - 1.0;
    float tile = max(uLength, 1.0);

    // 两层噪声反向滚动:一层快一层慢,叠出管内湍流
    float n1 = tex2D(uImage1, float2(frac(along * tile * 0.5 - uTime * 1.7 + uSeed), frac(coords.y * 0.9 + uSeed))).r;
    float n2 = tex2D(uImage1, float2(frac(along * tile * 0.23 + uTime * 0.9 + uSeed * 2.0), frac(coords.y * 0.6 + uTime * 0.3))).r;
    float noise = n1 * 0.6 + n2 * 0.4;

    // 半宽随噪声呼吸;d = 0 中心 → 1 边缘
    float halfWidth = max(uEnvelope, 0.001) * (0.72 + 0.28 * noise);
    float d = abs(across) / halfWidth;
    float body = saturate(1.0 - d);
    float core = pow(saturate(1.0 - d * 1.8), 4.0);
    float rim = pow(saturate(1.0 - abs(d - 0.85) * 6.0), 2.0) * 0.6;

    // 端帽:起点 4% 与终点 8% 渐隐
    float cap = smoothstep(0.0, 0.04, along) * (1.0 - smoothstep(0.92, 1.0, along));

    float3 col = uColor * (body * (0.55 + 0.45 * noise)) + uColor2 * core + uColor * rim + core * 0.6;
    float a = saturate(body * 0.85 + core + rim) * cap * uOpacity * baseTex.a;
    return float4(col * a, a) * baseColor;
}

technique Technique1
{
    pass BeamPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
