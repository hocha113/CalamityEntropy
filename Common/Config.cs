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

        [DefaultValue(true)]
        public bool ScreenWarpEffects { get; set; }

        [DefaultValue(true)]
        public bool ChainsawShakeScreen { get; set; }

        [DefaultValue(1f)]
        [Range(0f, 2f)]
        [Increment(0.05f)]
        public float EntropyMeleeWeaponSoundVolume { get; set; }

        [DefaultValue(true)]
        public bool MariviumArmorSetOnlyProvideStealthBarWhenHoldingRogueWeapons { get; set; }

        [Header("Compatibility")]
        [DefaultValue(true)]
        public bool CalamityTextEffectCompatibilityFix { get; set; }
        
        [DefaultValue(true)]
        public bool TileEffect { get; set; }
        [DefaultValue(true)]
        public bool EnablePixelEffect { get; set; }

        [DefaultValue(true)]
        public bool EnableLoopingSound { get; set; }

        [DefaultValue(false)]
        public bool EnableRetroLighting { get; set; }

    }
}
