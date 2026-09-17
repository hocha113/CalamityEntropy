// VDSky — 虚空驱逐舰「轨道封锁」天幕(SkyManager["CalamityEntropy:VoidDestroyerSky"],由 Content/Skies/VDSky.cs 驱动)
// VDSky 在跨 0 切片(原版全部远景之后、世界之前)用原始像素空间画一张全屏白方块,本着色器按 UV 程序化合成四层(自远至近):
//   深空底幕    顶端近黑紫 → 天际略亮的渐变 + 两层滚动噪声(s1)相乘的低幅星云
//   程序化星野  hash 网格点星两层,视差 uParallax × 远/近系数,微弱闪烁;主炮蓄力时向本体轻微吸入
//   被吞噬的星球  圆盘 + 边缘暗化 + 经纬映射的表面(s2)真自转(中央快、边缘压缩)+ 冷紫背光呼吸;
//                uErosion 按钉在球面上的噪声阈值挖空成虚空黑,挖口热边随流动噪声爬行、脉动、拍点上闪;
//                一圈倾斜的碎屑环绕行(近侧压在星球前、远侧被星球挡住),环上有绕行的亮碎块
//   封锁力场    六边形距离场细边线,以 uBossUv 为中心高斯亮化(乘核心亮度),拍点整面亮 + 从本体扩散的冲击环(扫过星球时也把它照亮),主炮蓄力时本体周围逐格填实
// 输出预乘 alpha(col*uOpacity, uOpacity):AlphaBlend 下随 uOpacity 淡入,原版远景随之淡出。反重力时 uFlip = 1 翻转渐变与星球位置。
// 所有距离都在「屏高归一」平面里算(x 乘宽高比),任意分辨率下圆是圆、六边形是正六边形。
// 无动态分支:全部门用 step / saturate / lerp。参数由 VDSky.DrawVoid 每帧喂。
sampler uImage0 : register(s0);   // 全屏白方块,只取 UV
sampler uImage1 : register(s1);   // TurbulentNoise:星云、侵蚀场(LinearWrap)
sampler uImage2 : register(s2);   // Perlin:星球表面(LinearWrap)

float uTime;
float uOpacity;         // 存在包络 × 强度
float uAspect;          // 宽 / 高
float uFlip;            // 反重力 1
float2 uParallax;       // 相机位置 ÷ 屏高(世界像素换成屏高单位)
float uPhase;           // 1..3 平滑(预留:配色已在 C# 侧按它插好)
float uErosion;         // 星球侵蚀 0..1
float uFlash;           // 拍点闪光 0..1
float uFlashRing;       // 冲击环半径(屏高单位),< 0 无环
float uCharge;          // 主炮蓄力 0..1
float2 uBossUv;         // 本体屏幕 UV
float uBossGlow;        // 核心亮度 0..1
float uGridRadius;      // 网格亮化半径(屏高单位)
float uGridAlpha;       // 网格基础亮度
float uGridCell;        // 六边形格边长(屏高单位)
float2 uPlanetCenter;   // 星球圆心 UV
float uPlanetRadius;    // 星球半径(屏高单位)
float uPlanetParallax;  // 星球视差
float uPlanetSpin;      // 星球自转(圈 / 秒)
float uRingSpin;        // 碎屑环绕行(圈 / 秒)
float2 uStarParallax;   // 星野视差:x 远层 y 近层
float uNebulaParallax;  // 星云视差
float3 uColorTop;
float3 uColorHorizon;
float3 uColorNebula;
float3 uColorGrid;
float3 uColorErosion;
float3 uColorPlanetRim;
float3 uColorRing;

// 输入须先压进小范围(格 id 走 fmod 512):乘数一大 frac 就只剩几档,星星会排成格子
float hash21(float2 p)
{
    p = frac(p * float2(127.1, 311.7));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// 一层点星:cells 为每屏高的格数,density 为有星格比例;每格至多一颗,偏离格心 ±0.3 格,半径 0.04~0.09 格
float StarLayer(float2 p, float cells, float density, float seed)
{
    float2 g = p * cells;
    // 格 id 卷进 [0,512):视差已按开打原点归零,±20 屏内不会撞到 fmod 的负数分支
    float2 id = fmod(floor(g) + 4096.0, 512.0);
    float2 f = frac(g) - 0.5;
    float h = hash21(id + seed);
    float2 off = (float2(hash21(id + seed + 7.1), hash21(id + seed + 3.3)) - 0.5) * 0.6;
    float d = length(f - off);
    float present = step(1.0 - density, h);
    float size = 0.04 + 0.05 * hash21(id + seed + 11.7);
    float star = present * saturate(1.0 - d / size);
    star *= star;
    float tw = 0.7 + 0.3 * sin(uTime * (1.0 + 2.0 * h) + h * 6.2831 + seed);
    return star * tw;
}

// 六边形网格距离场:x = 到最近格边的距离(0 在边上 → 0.5 在格心),yz = 格 id。p 须为正
float3 HexCoords(float2 p)
{
    float2 r = float2(1.0, 1.7320508);
    float2 h = r * 0.5;
    float2 a = fmod(p, r) - h;
    float2 b = fmod(p - h, r) - h;
    float2 gv = lerp(b, a, step(dot(a, a), dot(b, b)));
    float2 q = abs(gv);
    float hd = max(dot(q, normalize(float2(1.0, 1.7320508))), q.x);
    float2 id = p - gv;
    return float3(0.5 - hd, id);
}

float4 PixelFunc(float2 uv : TEXCOORD0) : COLOR0
{
    float asp = max(uAspect, 0.1);
    // 玩家视角的竖向:0 天顶 → 1 天际(反重力翻转)
    float sy = lerp(uv.y, 1.0 - uv.y, uFlip);
    // 屏高归一平面:x 乘宽高比,距离各向同性
    float2 sp = float2(uv.x * asp, uv.y);
    float2 bp = float2(uBossUv.x * asp, uBossUv.y);

    // ---- 深空底幕 + 星云 ----
    float3 col = lerp(uColorTop, uColorHorizon, smoothstep(0.1, 1.0, sy));
    float2 nuv1 = sp * 0.9 + uParallax * uNebulaParallax + float2(uTime * 0.004, uTime * 0.002);
    float2 nuv2 = sp * 1.7 - uParallax * uNebulaParallax * 0.6 + float2(-uTime * 0.003, uTime * 0.005) + 0.37;
    float n1 = tex2D(uImage1, frac(nuv1)).r;
    float n2 = tex2D(uImage1, frac(nuv2)).r;
    float neb = n1 * n2;
    col += uColorNebula * neb * 0.55;

    // ---- 星野:两层视差;主炮蓄力时采样点向本体挪,星野像被吸进去 ----
    float2 starP = sp + (bp - sp) * uCharge * 0.05;
    float sFar = StarLayer(starP + uParallax * uStarParallax.x, 34.0, 0.35, 0.0);
    float sNear = StarLayer(starP + uParallax * uStarParallax.y + 13.7, 20.0, 0.25, 2.0);
    col += float3(0.75, 0.7, 1.0) * (sFar * 0.45 + sNear * 0.65);

    // ---- 以本体为中心的量:力场亮化与拍点冲击环(星球与网格都要用) ----
    float bd = length(sp - bp);
    float r2 = max(uGridRadius * uGridRadius, 0.0001);
    float bossMask = exp(-bd * bd / r2) * (0.35 + 0.65 * uBossGlow);
    // 冲击环:半径由驱动推进,亮度随半径自衰(与闪光的衰减脱钩,环能走完整屏)
    float ringOn = step(0.0, uFlashRing);
    float ringD = (bd - uFlashRing) / 0.06;
    float ringFade = saturate(1.0 - uFlashRing / 2.0);
    float shock = exp(-ringD * ringD) * ringOn * ringFade;

    // ---- 被吞噬的星球 ----
    // 圆心:固定屏幕位 + 极慢漂移(免得读成贴图)+ 视差
    float2 pc = float2(uPlanetCenter.x * asp, lerp(uPlanetCenter.y, 1.0 - uPlanetCenter.y, uFlip))
        - uParallax * uPlanetParallax
        + float2(sin(uTime * 0.13), cos(uTime * 0.09)) * 0.006;
    float2 pp = (sp - pc) / max(uPlanetRadius, 0.01);
    float d = length(pp);
    float disk = 1.0 - smoothstep(0.985, 1.0, d);
    float z = sqrt(saturate(1.0 - d * d));
    float3 nrm = float3(pp.x, pp.y, z);
    // 经纬映射:表面沿经线真旋转,中央走得快、边缘压缩,读成球体在转
    float lon = atan2(nrm.x, nrm.z) / 6.2831853 + uTime * uPlanetSpin;
    float lat = asin(clamp(nrm.y, -1.0, 1.0)) / 3.1415927 + 0.5;
    float2 suv = float2(lon, lat);
    float terrain = tex2D(uImage2, frac(suv * float2(2.0, 1.0))).r * 0.6 + tex2D(uImage2, frac(suv * float2(5.0, 2.5) + 0.31)).r * 0.4;
    float continents = smoothstep(0.42, 0.6, terrain);
    float3 light = normalize(float3(-0.55, -0.45, 0.7));
    float ndl = saturate(dot(nrm, light));
    float limb = pow(z, 0.6);
    float rimBreath = 0.85 + 0.15 * sin(uTime * 0.8);
    float3 albedo = lerp(float3(0.045, 0.03, 0.075), float3(0.20, 0.15, 0.30), terrain) + float3(0.06, 0.03, 0.09) * continents;
    float3 planet = albedo * (0.3 + 0.7 * ndl) * limb + uColorPlanetRim * pow(1.0 - z, 3.0) * 0.4 * rimBreath;
    // 侵蚀:阈值场钉在球面上随星球转;下半球(朝战场)加权先被啃;挖口热边随流动噪声爬行、脉动,拍点上闪
    float2 euv = suv * float2(3.0, 1.5) + 0.13;
    float en = tex2D(uImage1, frac(euv)).r * 0.8 + tex2D(uImage1, frac(euv * 2.1 + 0.41)).r * 0.2;
    float eatField = en * 0.85 + 0.15 * (0.5 + 0.5 * pp.y);
    float thr = 1.0 - uErosion * 0.95;
    float eaten = smoothstep(thr - 0.04, thr + 0.04, eatField);
    float edge = saturate(1.0 - abs(eatField - thr) / 0.07);
    edge *= edge * step(0.02, uErosion);
    float flow = tex2D(uImage1, frac(euv * 2.5 + float2(uTime * 0.05, uTime * 0.11))).r;
    float crawl = 0.55 + 0.9 * flow;
    float pulse = 0.85 + 0.15 * sin(uTime * 2.7 + en * 12.0);
    float3 eatenCol = float3(0.01, 0.0, 0.03) + uColorErosion * (0.04 + 0.16 * flow * en);
    float3 planetCol = lerp(planet, eatenCol, eaten) + uColorErosion * edge * crawl * pulse * (0.9 + uFlash * 1.2);
    // 本体的冲击环扫过星球时把它照亮一下
    planetCol += uColorGrid * shock * 0.25;
    // 碎屑环:被撕下的物质在倾斜轨道上绕行;近侧(下半)压在星球前,远侧被星球挡住;环上有绕行的亮碎块
    float2 q = float2(pp.x, pp.y * 3.2);
    float rr = length(q);
    float band = smoothstep(1.3, 1.45, rr) * (1.0 - smoothstep(1.85, 2.1, rr));
    float rang = atan2(q.y, q.x) / 6.2831853 + uTime * uRingSpin;
    float2 ruv = float2(rang, rr * 2.0);
    float rn = tex2D(uImage1, frac(ruv * float2(4.0, 1.0))).r;
    float rn2 = tex2D(uImage1, frac(ruv * float2(9.0, 2.0) + 0.5)).r;
    float ringDensity = band * (rn * 0.7 + rn2 * 0.5) * (0.5 + 0.5 * smoothstep(0.3, 0.7, rn));
    float2 rcell = floor(float2(frac(rang) * 64.0, (rr - 1.3) * 12.0));
    float glint = step(0.93, hash21(rcell)) * band * (0.6 + 0.4 * sin(uTime * 5.0 + rcell.x));
    float ringVis = lerp(1.0 - disk, 1.0, step(0.0, pp.y));
    float3 ringCol = (uColorRing * ringDensity * 0.7 + uColorErosion * glint * 0.6) * ringVis;
    col = lerp(col, planetCol, disk);
    col += ringCol;
    // 星球外圈微晕,随侵蚀加深、随背光呼吸
    float halo = exp(-max(d - 1.0, 0.0) * 5.0) * (1.0 - disk);
    col += uColorErosion * halo * (0.05 + 0.10 * uErosion) * rimBreath;

    // ---- 封锁力场六边形网格 ----
    float3 hc = HexCoords(sp / max(uGridCell, 0.005) + 100.0);
    float edgeDist = hc.x;
    float edgeLine = 1.0 - smoothstep(0.0, 0.05, edgeDist);
    float cellHash = hash21(hc.yz);
    float breathe = 0.6 + 0.4 * sin(uTime * 1.3 + cellHash * 6.2831);
    float gridI = uGridAlpha * breathe + bossMask * 0.35 + uFlash * 0.45 + shock * 0.9 + uCharge * 0.2;
    // 主炮蓄力:本体周围的格逐格填实(格 hash 决定先后)
    float interior = smoothstep(0.02, 0.45, edgeDist);
    float cellFill = uCharge * exp(-bd * bd / 0.06) * (0.25 + 0.5 * cellHash) * 0.35;
    col += uColorGrid * (edgeLine * gridI + interior * cellFill);
    // 拍点整片天微亮
    col += uColorGrid * uFlash * 0.04;

    return float4(col * uOpacity, uOpacity);
}

technique Technique1
{
    pass SkyPass
    {
        PixelShader = compile ps_3_0 PixelFunc();
    }
}
