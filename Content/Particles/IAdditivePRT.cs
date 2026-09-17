using Microsoft.Xna.Framework.Graphics;

namespace CalamityEntropy.Content.Particles
{
    /// <summary>
    /// 不走 PRT 常规分桶的 Additive 绘制契约；CEPixelScreen 攒批后单独开 Additive 批次调 Draw
    /// 给 PreDraw 里自己管 SpriteBatch、跟 PRT 桶状态对不上的粒子留的钩子,目前无实现类
    /// </summary>
    internal interface IAdditivePRT
    {
        public void Draw(SpriteBatch spriteBatch);
    }

    /// <summary>
    /// 像素化 RT 通道粒子，受 CEScreenPipeline.PixelPassActive 门控（绚丽特效关闭、或复古 / 迷幻光照下
    /// 原版不捕获画面时）都不显示——旧行为，别加回退绘制
    /// 绘制发生在 CEPixelScreen → Screen2 RT，之后 ApplyPixelShader 过 Pixel shader
    /// </summary>
    internal interface IPixelPassPRT
    {
        bool PixelPass { get; set; }
        void DrawPixelPass(SpriteBatch sb);
    }
}

