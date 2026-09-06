using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace CalamityEntropy.Assets.Register
{
    // 共享资产静态字段库,由 InnoVault 的 VaultLoaden 在 PostSetupContent 阶段统一赋值,卸载时自动置 null。
    //
    // 使用须知:
    // - 专用服务器上这些字段永远不会被赋值(恒为 null),只能在绘制等客户端路径读取;
    //   非绘制路径要用时必须先判 Main.dedServ。
    // - 字段在 PostSetupContent 之后才可用,Load / SetStaticDefaults / SetDefaults 等加载期禁止读取。
    // - 类级标签的规则是「类路径 + 字段名 = 资源路径」,字段名必须与文件名完全一致(含大小写和下划线)。
    // - 只放普通 static 字段:const / readonly / 实例字段都不会被加载。

    /// <summary>
    /// Assets/Extra 池的高频贴图。原先经 CEUtils.getExtraTex("名字") 逐帧取,
    /// 高频字面名改为直接读这里的静态字段;低频与动态拼接的调用仍走 getExtraTex。
    /// 新增字段前先确认 Assets/Extra 下同名 png 存在。
    /// </summary>
    [VaultLoaden("CalamityEntropy/Assets/Extra/")]
    public static class CEExtraAssets
    {
        //拖尾、斩痕、涂抹
        public static Texture2D Streak1;
        public static Texture2D Streak2;
        public static Texture2D Streak2Trans;
        public static Texture2D StreakGoop;
        public static Texture2D StreakSolid;
        public static Texture2D StreakFaded;
        public static Texture2D SplitTrail;
        public static Texture2D MotionTrail2;
        public static Texture2D MotionTrail5;
        public static Texture2D MegaStreakBacking2;
        public static Texture2D MegaStreakBacking2b;
        public static Texture2D MegaStreakBacking2c;
        public static Texture2D MegaStreakInner;
        public static Texture2D VoltTrailThicc;
        public static Texture2D CircularSmear;
        public static Texture2D CircularSmearSmokey;
        public static Texture2D SemiCircularSmear;
        public static Texture2D SlashSmear;
        public static Texture2D wohslash;
        public static Texture2D SylvestaffStreak;
        public static Texture2D SwordSlashTexture;
        public static Texture2D EternityStreak;
        public static Texture2D BasicTrail;
        public static Texture2D rvslash;

        //圆形、光晕、星形
        public static Texture2D a_circle;
        public static Texture2D Circle;
        public static Texture2D AbyssalCircle2;
        public static Texture2D AbyssalCircle3;
        public static Texture2D AbyssalCircle4;
        public static Texture2D BasicCircle;
        public static Texture2D HollowCircleSoftEdge;
        public static Texture2D Glow;
        public static Texture2D Glow2;
        public static Texture2D GlowCone;
        public static Texture2D SpearArrowGlow2;
        public static Texture2D BloomRing;
        public static Texture2D lightball;
        public static Texture2D StarTexture;
        public static Texture2D StarTexture_White;
        public static Texture2D StarChromatic;
        public static Texture2D SoftRoundExplosion;
        public static Texture2D ShatteredExplosion;
        public static Texture2D StarlessNightGlow;
        public static Texture2D Enchanted;

        //射线、几何形
        public static Texture2D Ray;
        public static Texture2D DeathRay;
        public static Texture2D DeathRay2;
        public static Texture2D Diamond;
        public static Texture2D Triangle;
        public static Texture2D B1;
        public static Texture2D T1;
        public static Texture2D T2;
        public static Texture2D FLEND;
        public static Texture2D vlbw;
        public static Texture2D vlend;
        public static Texture2D LTLine;
        public static Texture2D impact;

        //Cruiser 系激光条带
        public static Texture2D clback;
        public static Texture2D cllight;
        public static Texture2D clinghth;
        public static Texture2D cllight2;

        //噪声、杂项
        public static Texture2D VoronoiShapes;
        public static Texture2D PatchyTallNoise;
        public static Texture2D white;
        public static Texture2D Empty;
        public static Texture2D Extra_201;
        public static Texture2D Noise_10;
        public static Texture2D Perlin;
        public static Texture2D TurbulentNoise;
        public static Texture2D Smoke;
        public static Texture2D VoidBack;

        //Ports 子目录(字段名与文件名一致,路径需单独指定)
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/AtlasMunitionsDropPodGlow")]
        public static Texture2D AtlasMunitionsDropPodGlow;
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/SmallGreyscaleCircle")]
        public static Texture2D SmallGreyscaleCircle;

        //Asset<Texture2D> 形态:SetShaderTexture 等杂项着色器接口只收 Asset 句柄,
        //与同名裸字段共存,底层资产同一份,不重复占用显存。
        [VaultLoaden("CalamityEntropy/Assets/Extra/Streak1")]
        public static Asset<Texture2D> Streak1Asset;
        [VaultLoaden("CalamityEntropy/Assets/Extra/Streak2")]
        public static Asset<Texture2D> Streak2Asset;
        [VaultLoaden("CalamityEntropy/Assets/Extra/StreakGoop")]
        public static Asset<Texture2D> StreakGoopAsset;
        [VaultLoaden("CalamityEntropy/Assets/Extra/StreakSolid")]
        public static Asset<Texture2D> StreakSolidAsset;
        [VaultLoaden("CalamityEntropy/Assets/Extra/StreakFaded")]
        public static Asset<Texture2D> StreakFadedAsset;
        [VaultLoaden("CalamityEntropy/Assets/Extra/SylvestaffStreak")]
        public static Asset<Texture2D> SylvestaffStreakAsset;
        [VaultLoaden("CalamityEntropy/Assets/Extra/Enchanted")]
        public static Asset<Texture2D> EnchantedAsset;
    }

    /// <summary>
    /// Assets/Effects 共享着色器。字段名与着色器文件名一致;
    /// pass 名与「文件名 + Pass」不一致的,用字段级标签单独指明。
    /// 注意:经 VaultLoaden 加载的 Effect 会自动注册 Filters.Scene["CalamityEntropy:文件名"],
    /// 与 EntropySkies 里既有 key 同名时先核对再迁。
    /// </summary>
    [VaultLoaden("CalamityEntropy/Assets/Effects/")]
    public static class CEEffectAssets
    {
        //物品幻彩描边,实际 pass 是 EnchantedPass
        [VaultLoaden("CalamityEntropy/Assets/Effects/Wisp", AssetMode.EffectValue, "EnchantedPass")]
        public static Effect Wisp;

        //白化/透明变换族,pass 都是 EnchantedPass
        [VaultLoaden("CalamityEntropy/Assets/Effects/WhiteTrans", AssetMode.EffectValue, "EnchantedPass")]
        public static Effect WhiteTrans;
        [VaultLoaden("CalamityEntropy/Assets/Effects/Trans", AssetMode.EffectValue, "EnchantedPass")]
        public static Effect Trans;
        [VaultLoaden("CalamityEntropy/Assets/Effects/SlashTrans", AssetMode.EffectValue, "EnchantedPass")]
        public static Effect SlashTrans;
        [VaultLoaden("CalamityEntropy/Assets/Effects/SlashTrans2", AssetMode.EffectValue, "EnchantedPass")]
        public static Effect SlashTrans2;
        [VaultLoaden("CalamityEntropy/Assets/Effects/Fire", AssetMode.EffectValue, "EnchantedPass")]
        public static Effect Fire;
        [VaultLoaden("CalamityEntropy/Assets/Effects/Transform3", AssetMode.EffectValue, "EnchantedPass")]
        public static Effect Transform3;

        //刀光拖尾族,pass 都是 EffectPass
        [VaultLoaden("CalamityEntropy/Assets/Effects/SwordTrail", AssetMode.EffectValue, "EffectPass")]
        public static Effect SwordTrail;
        [VaultLoaden("CalamityEntropy/Assets/Effects/SwordTrail2", AssetMode.EffectValue, "EffectPass")]
        public static Effect SwordTrail2;
        [VaultLoaden("CalamityEntropy/Assets/Effects/SwordTrail3", AssetMode.EffectValue, "EffectPass")]
        public static Effect SwordTrail3;
        [VaultLoaden("CalamityEntropy/Assets/Effects/SwordTrail4", AssetMode.EffectValue, "EffectPass")]
        public static Effect SwordTrail4;
        [VaultLoaden("CalamityEntropy/Assets/Effects/SwordTrail5", AssetMode.EffectValue, "EffectPass")]
        public static Effect SwordTrail5;
        [VaultLoaden("CalamityEntropy/Assets/Effects/RedAdd", AssetMode.EffectValue, "EffectPass")]
        public static Effect RedAdd;

        //漩涡、红移
        [VaultLoaden("CalamityEntropy/Assets/Effects/Vortex", AssetMode.EffectValue, "Pass1")]
        public static Effect Vortex;
        [VaultLoaden("CalamityEntropy/Assets/Effects/RedTrans", AssetMode.EffectValue, "Pass1")]
        public static Effect RedTrans;
        [VaultLoaden("CalamityEntropy/Assets/Effects/ColorLerp2", AssetMode.EffectValue, "Pass1")]
        public static Effect ColorLerp2;

        //深渊亡魂与先知系,pass 名与文件名相同的单独指明
        [VaultLoaden("CalamityEntropy/Assets/Effects/aweffect", AssetMode.EffectValue, "aweffect")]
        public static Effect aweffect;
        [VaultLoaden("CalamityEntropy/Assets/Effects/AWSkyEffect", AssetMode.EffectValue, "AWSkyEffect")]
        public static Effect AWSkyEffect;
        [VaultLoaden("CalamityEntropy/Assets/Effects/awsky2", AssetMode.EffectValue, "EnchantedPass")]
        public static Effect awsky2;
        [VaultLoaden("CalamityEntropy/Assets/Effects/fableeyelaser", AssetMode.EffectValue, "fableeyelaser")]
        public static Effect fableeyelaser;

        //以下为 CompileFX.ps1 产出的 .fxc 着色器,tML 的 FxcReader 认这个后缀,与 .xnb 一样走 Assets.Request
        //巡游者天幕扭曲滤镜:交给 ScreenShaderData 的 Asset 形态构造器,由 EntropySkies 在 PostSetupContent 注册,取代旧 CrSky 的 RenderTarget 扭曲流程
        [VaultLoaden("CalamityEntropy/Assets/Effects/CruiserSkyFilter", AssetMode.Effects, "CruiserSkyPass")]
        public static Asset<Effect> CruiserSkyFilter;
    }
}
