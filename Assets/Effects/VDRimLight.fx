// VDRimLight — 虚空驱逐舰能量逸散描边
// 对本体贴图做:与 2 texel 外八邻 alpha 的最小值作差,得到贴着轮廓内侧、2 texel 宽的实心缘带;
// 再用两层滚动噪声把缘带侵蚀成逸散的丝状能量(uErode 越高丝越碎,爆闪时压回实心整圈);
// 蓄力(uHeat)与出手(uFlash)把缘光整体拉向白热色,爆闪再叠高频闪烁与亮度。
// 外扩光晕不在这里做:本体贴图四边只有 2~4px 留白,着色器往外膨胀会被四边裁掉,
// 由 VoidDestroyer.Draw 用多次屏幕偏移叠画同一遍着色器完成(每次是独立四边形,不吃留白)。
// 取样半径锁死 2 texel:VoidDestroyerTransform 条带帧高 122、行距 124,帧间隙恰好 2px,再大就串到相邻帧。
// 用法:EnterShaderRegion(BlendState.Additive, shader),噪声图绑 GraphicsDevice.Textures[1](LinearWrap),
// 喂 uColor/uHotColor/uHeat/uFlash/uErode/uOpacity/uNoiseScroll/uImageSize/uTime 后 Passes[0].Apply;
// 顶点色(Draw 的 color 参数)整体乘在输出上,叠画各抽的衰减直接走它。输出预乘 alpha。无动态分支。
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float uTime;
float uOpacity;
float3 uColor;
float3 uHotColor;
float uHeat;
float uFlash;
float uErode;
float2 uNoiseScroll;
float2 uImageSize;

float4 PixelFunc(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 c = tex2D(uImage0, coords);
    float2 px = 2.0 / uImageSize;

    // 轴向四邻:轮廓外沿 2px 内至少一邻落在透明处,amin→0、edge→c.a;深处四邻全实,edge→0
    float aL = tex2D(uImage0, coords - float2(px.x, 0.0)).a;
    float aR = tex2D(uImage0, coords + float2(px.x, 0.0)).a;
    float aU = tex2D(uImage0, coords - float2(0.0, px.y)).a;
    float aD = tex2D(uImage0, coords + float2(0.0, px.y)).a;
    float amin = min(min(aL, aR), min(aU, aD));

    // 斜向四邻(半径压到 0.7071,y 向偏移 1.41 texel 仍在帧间隙内),补斜边与尖角,缘带不断线
    float2 d = px * 0.7071;
    float aLU = tex2D(uImage0, coords - d).a;
    float aRD = tex2D(uImage0, coords + d).a;
    float aRU = tex2D(uImage0, coords + float2(d.x, -d.y)).a;
    float aLD = tex2D(uImage0, coords + float2(-d.x, d.y)).a;
    amin = min(amin, min(min(aLU, aRD), min(aRU, aLD)));

    float edge = saturate(c.a - amin);

    // 噪声侵蚀:在贴图像素空间取噪声(与绘制缩放无关),两层反向漂移叠出湍流;uErode 控制侵蚀比例
    float2 nuv = coords * uImageSize / 56.0 + uNoiseScroll;
    float n = tex2D(uImage1, frac(nuv)).r;
    float n2 = tex2D(uImage1, frac(nuv * 0.55 + float2(0.31, 0.77) - uNoiseScroll * 1.4)).r;
    float wisp = smoothstep(0.28, 0.78, n * 0.6 + n2 * 0.4);
    edge *= lerp(1.0, wisp, uErode);

    // 热色:蓄力把缘光拉向白热,出手爆闪叠在其上;缘带内侧再渗一点机体自身亮度,光像从表面漏出来
    float heat = saturate(uHeat + uFlash);
    float3 col = lerp(uColor, uHotColor, heat);
    float lum = dot(c.rgb, float3(0.3, 0.59, 0.11));
    col += uColor * lum * 0.25;

    // 爆闪期间高频闪烁 + 亮度抬升
    float flicker = 1.0 + uFlash * 0.25 * sin(uTime * 40.0);
    float a = edge * uOpacity * (1.0 + uFlash) * flicker;
    return float4(col * a, a) * baseColor;
}

technique Technique1
{
    pass RimPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
