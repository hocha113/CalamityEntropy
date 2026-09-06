using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;

namespace CalamityEntropy.Content.Skies
{
    public static class EntropySkies
    {
        public static void setUpSkies()
        {
            //巡游者:天空件在此注册,配套扭曲滤镜的着色器是 VaultLoaden 字段,注册在 setUpShaderFilters
            SkyManager.Instance["CalamityEntropy:Cruiser"] = new CrSky();
            Terraria.Graphics.Effects.Filters.Scene["CalamityEntropy:DimensionLens"] = new Filter(new TransScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["CalamityEntropy:DimensionLens"] = new LlSky();
            Terraria.Graphics.Effects.Filters.Scene["CalamityEntropy:NihTwin"] = new Filter(new TransScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["CalamityEntropy:NihTwin"] = new NihTwinSky();
            Terraria.Graphics.Effects.Filters.Scene["CalamityEntropy:VoidVortex"] = new Filter(new TransScreenShaderData("FilterMiniTower").UseColor(new Color(60, 30, 100)).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["CalamityEntropy:VoidVortex"] = new VoidVortexSky();
            Terraria.Graphics.Effects.Filters.Scene["CalamityEntropy:Snowgrave"] = new Filter(new TransScreenShaderData("FilterMiniTower").UseColor(new Color(200, 200, 255)).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["CalamityEntropy:Snowgrave"] = new SnowgraveSky();
            Terraria.Graphics.Effects.Filters.Scene["CalamityEntropy:SunriseSky"] = new Filter(new TransScreenShaderData("FilterMiniTower").UseColor(Color.White).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["CalamityEntropy:SunriseSky"] = new SunriseSky();
        }

        /// <summary>
        /// 着色器取自 VaultLoaden 静态字段的滤镜。字段要到 PostSetupContent 才赋值,
        /// 所以这批不能跟着 setUpSkies 在 Load 里注册;服务器上字段恒为 null,只在客户端调用。
        /// </summary>
        public static void setUpShaderFilters()
        {
            //巡游者天幕扭曲滤镜:与 CrSky 同键成对(ManageSpecialBiomeVisuals 对缺键无空值保护);
            //强度由 CrScreenShaderData 每帧喂,噪声 VoidBack 绑 s1
            Terraria.Graphics.Effects.Filters.Scene["CalamityEntropy:Cruiser"] = new Filter(
                new CrScreenShaderData(CEEffectAssets.CruiserSkyFilter, "CruiserSkyPass")
                    .UseOpacity(0f)
                    .UseImage(CEExtraAssets.VoidBack, 0, SamplerState.LinearWrap),
                EffectPriority.VeryHigh);
        }
    }
}
