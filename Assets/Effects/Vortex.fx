sampler TextureSampler : register(s0);

float2 Center;
float Strength;
float AspectRatio;
float FadeOutDistance; // 渐隐开始的半径距离(0-1)
float FadeOutWidth;    // 渐隐宽度
float2 TexOffset;
float enhanceLightAlpha;

float4 PixelShaderFunction(float4 baseColor : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // 调整坐标以考虑宽高比
    float2 adjustedTexCoord = texCoord - Center;
    adjustedTexCoord.y /= AspectRatio;
    
    // 计算当前点到中心的距离
    float distance = length(adjustedTexCoord);
    
    // 计算透明度 (0=完全透明, 1=不透明)
    float alpha = 1.0;
    if (distance > FadeOutDistance)
    {
        alpha = 1.0 - smoothstep(FadeOutDistance, FadeOutDistance + FadeOutWidth, distance);
    }
    
    // 计算漩涡扭曲
    float angle = atan2(adjustedTexCoord.y, adjustedTexCoord.x);
    angle += Strength * distance;
    
    adjustedTexCoord.x = cos(angle) * distance;
    adjustedTexCoord.y = sin(angle) * distance;
    
    // 恢复宽高比调整
    adjustedTexCoord.y *= AspectRatio;
    adjustedTexCoord += Center;
    
    // 采样纹理并应用透明度
    float4 color = tex2D(TextureSampler, adjustedTexCoord + TexOffset);
    if(color.r > enhanceLightAlpha)
    {
        float z = enhanceLightAlpha + (color.r - enhanceLightAlpha) * 3.5;
        color = float4(z, z, z, color.a);
    }
    color *= float4(alpha, alpha, alpha, alpha);
    
    return color * baseColor;
}

technique VortexTechnique
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}