using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>
    /// 巡游者天幕扭曲滤镜数据(键 CalamityEntropy:Cruiser,新链路 CruiserSkyFilter.fxc)。
    /// 一并取代旧实现的两件事:借用的原版 FilterMiniTower(基本惰性)与
    /// 旧 CrSky.Draw 中途切 RenderTarget 的扭曲流程(每视差切片重跑一遍的病灶源头)。
    /// 强度 = <see cref="CruiserSkyDrive.Intensity"/>(UseOpacity,原版再乘 Filter 淡入);
    /// EnablePixelEffect 关闭时强度归零,IsVisible 随之为假,优雅退化。
    /// 激活/停用由 CBScene 的 ManageSpecialBiomeVisuals 统一负责,
    /// 本类不再自灭,旧版与 VoidMonolith 触发路径互相打架的问题随之消失。
    /// </summary>
    public class CrScreenShaderData : ScreenShaderData
    {
        public CrScreenShaderData(Asset<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Apply()
        {
            //强度门:uOpacity = 本值 × Filter 淡入;为 0 时 IsVisible 为假,滤镜整体被跳过
            UseOpacity(Config.Instance.EnablePixelEffect ? CruiserSkyDrive.Intensity : 0f);

            //镜头视差与缩放补偿(滤镜阶段 screenWidth/Height 是真实值,无背景预除)
            Shader?.Parameters["uScreenOffCE"]?.SetValue(
                Main.screenPosition * 0.5f / Main.ScreenSize.ToVector2() * new Vector2(1f, Main.LocalPlayer.gravDir));
            Shader?.Parameters["uCoordMultCE"]?.SetValue(Vector2.One / Main.GameViewMatrix.Zoom);
            //扭曲带中心:常规重力在天空侧(屏高 35%),反重力翻到下侧
            Shader?.Parameters["uBandCenterCE"]?.SetValue(Main.LocalPlayer.gravDir == 1f ? 0.35f : 0.65f);
            base.Apply();
        }
    }
}
