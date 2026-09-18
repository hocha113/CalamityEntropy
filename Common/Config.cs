using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace CalamityEntropy.Common
{
    public class Config : ModConfig
    {
        public static Config Instance => ModContent.GetInstance<Config>();
        public override ConfigScope Mode => ConfigScope.ClientSide;
        [Header("Misc")]
        [SliderColor(224, 165, 56, 128)]
        [Range(0f, 1920f)]
        [DefaultValue(900f)]
        [Increment(1f)]
        public float VoidChargeBarX { get; set; }

        [SliderColor(224, 165, 56, 128)]
        [Range(0f, 1080f)]
        [DefaultValue(100f)]
        [Increment(1f)]
        public float VoidChargeBarY { get; set; }

        [DefaultValue(true)]
        public bool ItemAdditionalInfo { get; set; }

        /// <summary>聊天文字特效开关(自研,替代原灾厄客户端配置 TextEffects),TextEffectHandler 读取。</summary>
        [DefaultValue(true)]
        public bool TextEffects { get; set; }

        /// <summary>稀有度名称特效开关:关掉后本模组稀有度的物品名交回原版画普通描边字。CERarityNameEffects 读取。</summary>
        [DefaultValue(true)]
        public bool RarityNameEffects { get; set; }

        [DefaultValue(true)]
        public bool ScreenWarpEffects { get; set; }

        [DefaultValue(true)]
        public bool ChainsawShakeScreen { get; set; }

        /// <summary>屏幕震动强度(自研,替代原灾厄客户端配置 ScreenshakePower),ScreenShaker 读取。</summary>
        [SliderColor(224, 165, 56, 128)]
        [Range(0f, 2f)]
        [DefaultValue(1f)]
        [Increment(0.05f)]
        public float ScreenShakePower { get; set; }

        [DefaultValue(1f)]
        [Range(0f, 2f)]
        [Increment(0.05f)]
        public float EntropyMeleeWeaponSoundVolume { get; set; }

        //脱离灾厄:MariviumArmorSetOnlyProvideStealthBarWhenHoldingRogueWeapons(潜行条)与
        //CalamityTextEffectCompatibilityFix(灾厄文字特效兼容)配置项已删除
        [Header("Compatibility")]
        [DefaultValue(true)]
        public bool TileEffect { get; set; }
        [DefaultValue(true)]
        public bool EnablePixelEffect { get; set; }

        [DefaultValue(true)]
        public bool EnableLoopingSound { get; set; }

        //EnableRetroLighting 已删除:它唯一的作用是给「把复古光照改回颜色」的强制逻辑开后门,
        //那套强制已连同本项一并移除,复古/迷幻光照现在无需任何开关即可使用
    }
}
