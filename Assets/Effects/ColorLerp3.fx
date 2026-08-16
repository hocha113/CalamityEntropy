sampler uImage : register(s0);
float4 color;

float4 EnchantedFunction(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 colory = tex2D(uImage, frac(coords));
    return lerp(baseColor, color, (colory.r + colory.g + colory.b) / 3) * float4(colory.r, colory.g, colory.b, 1) * colory.a;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 EnchantedFunction();
    }
}