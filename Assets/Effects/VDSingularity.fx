// VDSingularity — 虚空奇点吸积盘 + 事件视界
// 画在一张方形白图上(coords 0..1,中心 0.5):极坐标下噪声随半径扭转成旋流,内圈转得快;
// 吸积盘是从视界外沿到 0.55 的环带,一侧多普勒增亮;视界内是不透明黑盘,盘沿一圈细热边。
// 用法:EnterShaderRegion(BlendState.AlphaBlend, shader)(黑盘要能遮背景,不能走 Additive),
// 噪声图绑 GraphicsDevice.Textures[1](LinearWrap),喂 uColor(盘色)/uColor2(热边色)/uCoreRadius(视界半径,0..1 的归一半径)/
// uOpacity/uSpin(旋流角速度)后 Passes[0].Apply。输出预乘 alpha。无动态分支。
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float uTime;
float3 uColor;
float3 uColor2;
float uCoreRadius;
float uOpacity;
float uSpin;

float4 PixelFunc(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = coords - 0.5;
    float r = length(p) * 2.0;
    float ang = atan2(p.y, p.x);

    // 旋流:角度随半径扭,内圈快外圈慢
    float twist = ang + uTime * uSpin * (1.5 - r) + 6.0 / (r + 0.35);
    float2 nuv = float2(twist / 6.2831853 + uTime * 0.05, r * 1.2 - uTime * 0.6);
    float n = tex2D(uImage1, frac(nuv)).r;
    float n2 = tex2D(uImage1, frac(nuv * float2(2.0, 0.5) + 0.37)).r;
    float swirl = n * 0.65 + n2 * 0.35;

    float inner = max(uCoreRadius, 0.02);
    float disk = smoothstep(inner, inner + 0.06, r) * (1.0 - smoothstep(0.55, 1.0, r));
    disk *= 0.35 + 0.65 * swirl;
    float doppler = 0.75 + 0.45 * cos(ang - uTime * 0.7);

    float horizon = 1.0 - smoothstep(inner - 0.02, inner, r);
    float rim = pow(saturate(1.0 - abs(r - inner) * 18.0), 2.0);

    float3 col = uColor * disk * doppler + uColor2 * rim * 1.4 + uColor2 * pow(disk, 3.0) * 0.6;
    float a = saturate(disk * 0.9 + rim) * uOpacity;
    // 盘体半加法(颜色大于 alpha 即在 AlphaBlend 下发光),黑盘写不透明黑
    float4 res = float4(col * a, a * 0.55);
    res = lerp(res, float4(0.0, 0.0, 0.0, uOpacity), horizon);
    return res * baseColor;
}

technique Technique1
{
    pass SingularityPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
