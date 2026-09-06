sampler2D Texture : register(s0);

// ---------- 用户可调参数 ----------
float radius    = 0.5f;
float rotation  = 0.0f;
float tileCount = 1.0f;
float innerWall = 0.0f;    // 0=外壁（凸面），1=内壁（凹面/环绕）

float4 PSFunction(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 centered = coords - 0.5f;
    float x = centered.x;
    float y = centered.y;

    float ratio = x / radius;
    ratio = clamp(ratio, -1.0f, 1.0f);   // 防止 asin 定义域溢出
    float theta = asin(ratio);
    theta += rotation;                  // 叠加旋转

    float direction = 1.0f - 2.0f * innerWall;   // 外壁:+1, 内壁:-1

    const float PI = 3.14159265f;
    float u = frac(theta * direction * tileCount / (2.0f * PI) + 0.5f);

    float v = y + 0.5f;   // 映射回 [0,1]

    return tex2D(Texture, float2(u, v)) * baseColor;
}

technique CylinderWarp
{
    pass P0
    {
        // 仅指定像素着色器，无需顶点着色器
        PixelShader = compile ps_2_0 PSFunction();
        ZEnable     = false;
        ZWriteEnable = false;
        CullMode    = None;
    }
}
