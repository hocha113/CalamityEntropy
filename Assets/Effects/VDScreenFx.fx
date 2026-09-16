// VDScreenFx — 虚空驱逐舰全屏滤镜(Filters.Scene["CalamityEntropy:VoidDestroyer"])
// 四条通道,全部由 VDScreenShaderData 每帧从 VDScreenFx 静态驱动喂入(UV 已按 GameViewMatrix 折好缩放与反重力):
//   引力透镜  uLensCenter/uLensStrength/uLensRadius:高斯衰减的径向拉扯 + 中心压暗(事件视界)
//   空间裂隙  uRift[3](线段端点 UV)+ uRiftOpen(三段开口量):沿线段法向外推 + RGB 色散 + 缝口亮白
//   暗角      uVignette:四周压暗并向深紫偏色(护盾展开 / 主炮蓄力压场)
//   冲击帧    uImpact:亮度→黑白高对比(整场一次)
// uOpacity 由原版喂(EnablePixelEffect 关则为 0,滤镜整体被跳过),这里用它对原图做整体 lerp。
// 无动态分支:循环 [unroll],所有门用 step/saturate。
sampler uImage0 : register(s0);

float uOpacity;
float uTime;
float2 uLensCenter;
float uLensStrength;
float uLensRadius;
float4 uRift[3];
float4 uRiftOpen;
float uVignette;
float uImpact;
float uAspect;

float4 PixelFunc(float2 uv : TEXCOORD0) : COLOR0
{
    float4 orig = tex2D(uImage0, uv);
    float2 asp = float2(max(uAspect, 0.1), 1.0);

    // 引力透镜:向中心拉,强度高斯衰减
    float2 toC = (uv - uLensCenter) * asp;
    float dist = length(toC);
    float radius = max(uLensRadius, 0.001);
    float lensFall = exp(-dist * dist / (radius * radius));
    float pull = uLensStrength * lensFall;
    float2 dirC = toC / max(dist, 0.0005);
    float2 offset = -dirC * pull * radius / asp;
    float darken = saturate(uLensStrength * 5.0 * exp(-dist * dist / (radius * radius * 0.16)));

    // 空间裂隙:三段线段,法向外推 + 缝口亮
    float2 riftOff = float2(0.0, 0.0);
    float riftGlow = 0.0;
    float opens[3] = { uRiftOpen.x, uRiftOpen.y, uRiftOpen.z };
    [unroll]
    for (int i = 0; i < 3; i++)
    {
        float2 a = uRift[i].xy;
        float2 b = uRift[i].zw;
        float2 ab = (b - a) * asp;
        float2 ap = (uv - a) * asp;
        float t = saturate(dot(ap, ab) / max(dot(ab, ab), 0.00001));
        float2 perp = ap - ab * t;
        float dperp = length(perp);
        float open = opens[i];
        float width = 0.0009 * open + 0.00002;
        float band = exp(-dperp * dperp / width) * step(0.001, open);
        float2 pushDir = perp / max(dperp, 0.0005);
        riftOff += pushDir / asp * band * open * 0.03;
        riftGlow += band * open;
    }

    float2 disp = offset + riftOff;
    float2 finalUv = uv + disp;
    float chroma = saturate(riftGlow + pull * 6.0);
    float4 col;
    col.r = tex2D(uImage0, finalUv + disp * 0.35 * chroma).r;
    col.g = tex2D(uImage0, finalUv).g;
    col.b = tex2D(uImage0, finalUv - disp * 0.35 * chroma).b;
    col.a = 1.0;

    // 视界压暗、缝口亮白(缝口带一点紫边)
    col.rgb *= 1.0 - darken * 0.75;
    col.rgb += float3(0.85, 0.72, 1.0) * saturate(riftGlow) * 0.9;

    // 暗角:四周压暗 + 深紫偏色
    float vig = length((uv - 0.5) * asp);
    float vigMask = smoothstep(0.35, 0.95, vig);
    col.rgb *= 1.0 - uVignette * vigMask * 0.85;
    col.rgb = lerp(col.rgb, col.rgb * float3(0.75, 0.6, 0.95), uVignette * 0.5);

    // 冲击帧:亮度阈值黑白
    float lum = dot(col.rgb, float3(0.3, 0.59, 0.11));
    float bw = smoothstep(0.32, 0.62, lum);
    col.rgb = lerp(col.rgb, float3(bw, bw, bw), saturate(uImpact));

    return lerp(orig, col, saturate(uOpacity));
}

technique Technique1
{
    pass ScreenFxPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
