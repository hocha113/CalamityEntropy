sampler uImage0 : register(s0);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // 预计算常量
    static const float THRESHOLD = 0.3f;
    static const float2 OFFSET_SCALE = float2(2.0f, 2.0f) / uImageSize0;
    static const float4 GLOW_COLOR = float4(0.9f, 0.86f, 0.0f, 0.0f);
    static const float4 GLOW_COLOR2 = float4(0.6f, 0.6f, 0.9f, 0.0f);

    float4 colory = tex2D(uImage0, coords);
    
    // 提前计算采样坐标，减少重复计算
    float2 offset1 = OFFSET_SCALE;
    float2 offset2 = OFFSET_SCALE * 2.0f;
    
    // 第一次采样：4个方向
    float a1 = tex2D(uImage0, coords + float2(0.0f, offset1.y)).a;
    float a2 = tex2D(uImage0, coords + float2(-offset1.x, 0.0f)).a;
    float a3 = tex2D(uImage0, coords + float2(offset1.x, 0.0f)).a;
    float a4 = tex2D(uImage0, coords + float2(0.0f, -offset1.y)).a;
    
    float ga = 0.0f;
    
    // 使用向量比较和any函数减少分支
    float4 alphaTest1 = float4(a1, a2, a3, a4);
    bool anyBelowThreshold1 = any(alphaTest1 < THRESHOLD);
    
    if (anyBelowThreshold1)
    {
        ga = 0.9f;
    }
    else
    {
        // 第二次采样：更远的4个方向
        float b1 = tex2D(uImage0, coords + float2(0.0f, offset2.y)).a;
        float b2 = tex2D(uImage0, coords + float2(-offset2.x, 0.0f)).a;
        float b3 = tex2D(uImage0, coords + float2(offset2.x, 0.0f)).a;
        float b4 = tex2D(uImage0, coords + float2(0.0f, -offset2.y)).a;
        
        float4 alphaTest2 = float4(b1, b2, b3, b4);
        bool anyBelowThreshold2 = any(alphaTest2 < THRESHOLD);
        
        ga = anyBelowThreshold2 ? 0.7f : 0.5f;
    }
    float gao = ga;
    // 合并计算
    float timeFactor = 0.64f + 0.36f * sin(uTime * 5.0f + (coords.y * uImageSize0.y - uSourceRect[1]) * 0.2f);
    ga *= timeFactor;
    
    return (colory * sampleColor) + (((gao < 0.6) ? GLOW_COLOR2 : GLOW_COLOR) * ga) * float4(colory.a, colory.a, colory.a, colory.a) * sampleColor;
}

technique Technique1
{
    pass DyePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}