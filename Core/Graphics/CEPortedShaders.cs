using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Graphics
{
    // 脱离灾厄: 自有等效着色器注册
    // 灾厄旧键同名换库为 CalamityEntropy:<Name>, 完整映射与参数通道说明见 Doc/decouple/shader-map.md
    // 注册姿势仿照本仓库 EntropySkies: Misc 走 MiscShaderData, 屏效走 Filters.Scene + Load
    [Autoload(Side = ModSide.Client)]
    public sealed class CEPortedShaders : ModSystem
    {
        internal const string ShaderPrefix = "CalamityEntropy:";
        private const string EffectPath = "CalamityEntropy/Assets/Effects/";

        internal static Asset<Effect> ArtAttackShader;
        internal static Asset<Effect> TrailStreakShader;
        internal static Asset<Effect> ExobladePierceShader;
        internal static Asset<Effect> HeavenlyGaleLightningArcShader;
        internal static Asset<Effect> HellBallShader;
        internal static Asset<Effect> RoverDriveShieldShader;
        internal static Asset<Effect> StandardPrimitiveShader;

        public override void PostSetupContent() {
            static Asset<Effect> LoadShader(string name) =>
                ModContent.Request<Effect>($"{EffectPath}{name}", AssetRequestMode.ImmediateLoad);

            ArtAttackShader = LoadShader("ArtAttack");
            RegisterMiscShader(ArtAttackShader, "TrailPass", "ArtAttack");

            TrailStreakShader = LoadShader("TrailStreak");
            RegisterMiscShader(TrailStreakShader, "TrailPass", "TrailStreak");

            ExobladePierceShader = LoadShader("ExobladePierce");
            RegisterMiscShader(ExobladePierceShader, "PiercePass", "ExobladePierce");

            // 电弧: UseImage1 传噪声图
            HeavenlyGaleLightningArcShader = LoadShader("HeavenlyGaleLightningArc");
            RegisterMiscShader(HeavenlyGaleLightningArcShader, "TrailPass", "HeavenlyGaleLightningArc");

            // 护盾屏效双子: 调用点取裸 Effect 逐参数 SetValue, 必须 Load 保证未激活也能 GetShader().Shader
            HellBallShader = LoadShader("HellBall");
            RegisterScreenShader(HellBallShader, "HellBallPass", "HellBall");

            RoverDriveShieldShader = LoadShader("RoverDriveShield");
            RegisterScreenShader(RoverDriveShieldShader, "ShieldPass", "RoverDriveShield");

            // SupremeCalamitas 借色位: 灾厄原注册即原版 FilterMiniTower pass, 无自定义 fx, 同构复刻
            RegisterSceneFilter(new ScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f),
                "SupremeCalamitas", EffectPriority.VeryHigh);

            // 图元渲染兜底: 顶点色直通, CEPrimitiveRenderer 在调用方未指定 shader 时回落到它
            StandardPrimitiveShader = LoadShader("StandardPrimitive");
            RegisterMiscShader(StandardPrimitiveShader, "PrimitivePass", "StandardPrimitiveShader");
        }

        public override void Unload() {
            ArtAttackShader = null;
            TrailStreakShader = null;
            ExobladePierceShader = null;
            HeavenlyGaleLightningArcShader = null;
            HellBallShader = null;
            RoverDriveShieldShader = null;
            StandardPrimitiveShader = null;
        }

        private static void RegisterMiscShader(Asset<Effect> shader, string passName, string name) {
            GameShaders.Misc[$"{ShaderPrefix}{name}"] = new MiscShaderData(shader, passName);
        }

        private static void RegisterSceneFilter(ScreenShaderData data, string name, EffectPriority priority) {
            string key = $"{ShaderPrefix}{name}";
            Filters.Scene[key] = new Filter(data, priority);
            Filters.Scene[key].Load();
        }

        private static void RegisterScreenShader(Asset<Effect> shader, string passName, string name, EffectPriority priority = EffectPriority.High) {
            RegisterSceneFilter(new ScreenShaderData(shader, passName), name, priority);
        }
    }
}
