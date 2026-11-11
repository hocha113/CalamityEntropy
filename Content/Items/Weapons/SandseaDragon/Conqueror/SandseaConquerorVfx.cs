using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Renderers;
using Terraria.ID;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Conqueror
{
    /// <summary>
    /// 沙海征服者各弹幕共用的本地表现,服务器上直接返回
    /// 都不过网,靠各端的弹幕 AI 与 OnKill 各自调用
    /// </summary>
    internal static class SandseaConquerorVfx
    {
        //族色,橙黄取自 Boss 毒针贴图,浅金取自原版日食太阳的亮环
        public static readonly Color PaleGold = new Color(250, 232, 154);
        public static readonly Color Orange = new Color(255, 166, 87);
        public static readonly Color FangWhite = new Color(245, 245, 255);

        //命中星芒,结构与数值照原版 ParticleOrchestrator.Spawn_TerraBlade,只把绿换成橙
        private const float StarLife = 30f;             //帧
        private const int StarArmCount = 4;             //十字四道芒
        private const float StarArmLength = 3f;         //芒的长轴缩放
        private const float StarArmWidth = 0.5f;        //芒的短轴缩放
        private const float StarOuterScale = 1.1f;      //外两层的整体缩放
        private const float StarCoreScale = 0.7f;       //内层的整体缩放
        private const float StarMajorScale = 1.5f;      //隔一道芒放大的倍率
        private const float StarMajorSpeed = 2f;        //放大的那道芒漂移也加倍
        private const float StarAxis = 4f;              //芒漂移基准长度,像素
        private const float StarSideAxis = 2f;          //侧向两粒的基准长度,像素
        private const float StarDrift = 0.2f;           //每帧漂移占基准长度的比例
        private const float StarSideDrift = 0.5f;
        private const float StarAdditive = 0.35f;       //外两层的加色比例,内层全加色
        private const float StarFadeIn = 5E-06f;
        private const float StarFadeOut = 0.95f;
        private const float StarDustSpread = 0.025f;    //尘粒偏转占整圈的比例
        private const float StarDustSmall = 0.8f;
        private const float StarDustLarge = 1.4f;
        private static readonly Color StarOuter = new Color(1f, 0.52f, 0.14f, 0.5f);    //原版 (0.2, 0.85, 0.4, 0.5)
        private static readonly Color StarMid = new Color(1f, 0.78f, 0.36f, 0.5f);      //原版 (0.4, 1, 0.4, 0.5)
        private static readonly Color StarCore = new Color(1f, 0.62f, 0.1f, 1f);        //原版 (0.2, 1, 0.2, 1)

        //刃命中
        private const int BladeSparkCount = 6;
        private const float BladeSparkSpread = 0.45f;   //弧度,沿刺向散开
        private const float BladeSparkSpeedMin = 5f;    //像素/帧
        private const float BladeSparkSpeedMax = 13f;
        private const float BladeSparkScaleMin = 0.5f;
        private const float BladeSparkScaleMax = 0.9f;
        private const int BladeSparkLifeMin = 10;       //帧
        private const int BladeSparkLifeMax = 16;
        private const float BladeHitPitchMin = 1.2f;    //CEUtils.PlaySound 的音高,1 为原调
        private const float BladeHitPitchMax = 1.5f;
        private const int BladeHitInstances = 4;        //一刺最多两三个目标,留一点余量
        private const float BladeHitVolume = 0.55f;

        //能量球发射
        private const int LaunchDustCount = 6;
        private const float LaunchDustSpread = 0.5f;    //弧度
        private const float LaunchDustSpeedMin = 2f;
        private const float LaunchDustSpeedMax = 6f;
        private const float LaunchDustScaleMin = 1f;
        private const float LaunchDustScaleMax = 1.5f;
        private const float LaunchVolume = 0.45f;       //16 帧一响,压到一半以下
        private const float LaunchPitchVariance = 0.25f;
        private const int LaunchInstances = 3;

        //能量球消散
        private const float PopRingStart = 0.05f;       //环贴图 156 像素的起止缩放
        private const float PopRingEnd = 0.32f;
        private const int PopRingLife = 10;             //帧
        private const int PopDustCount = 8;
        private const float PopDustSpeedMin = 1.5f;
        private const float PopDustSpeedMax = 4.5f;
        private const float PopVolume = 0.4f;
        private const float PopPitchVariance = 0.3f;
        private const int PopInstances = 3;

        //毒牙
        private const int FangSparkCount = 5;
        private const float FangSparkSpread = 0.6f;     //弧度,沿毒牙去向散开
        private const float FangSparkSpeedMin = 3f;
        private const float FangSparkSpeedMax = 9f;
        private const float FangSparkScaleMin = 0.35f;
        private const float FangSparkScaleMax = 0.6f;
        private const int FangSparkLifeMin = 12;        //帧
        private const int FangSparkLifeMax = 18;
        private const int FangVenomDustCount = 3;
        private const float FangVenomDustSpread = 0.8f;
        private const float FangVenomDustSpeedMin = 1f;
        private const float FangVenomDustSpeedMax = 3f;
        private const int FangAppearDustCount = 2;
        private const float FangAppearDustSpeed = 1.2f;
        private const float FangDartVolume = 0.35f;     //每组毒牙只响一次
        private const float FangDartPitch = 0.35f;
        private const float FangDartPitchVariance = 0.2f;
        private const int FangDartInstances = 2;

        /// <summary>橙色的泰拉刃命中星芒,movement 只取方向</summary>
        public static void OrbHitStar(Vector2 position, Vector2 movement) {
            if (Main.dedServ) {
                return;
            }
            float baseRot = movement.ToRotation() + MathHelper.PiOver2;
            Vector2 arm = new Vector2(StarArmLength, StarArmWidth);
            for (int i = 0; i < StarArmCount; i++) {
                Vector2 axis = (MathHelper.PiOver2 * i + baseRot).ToRotationVector2() * StarAxis;
                bool major = i % 2 == 1;
                Vector2 scale = arm * StarOuterScale * (major ? StarMajorScale : 1f);
                Vector2 velocity = -axis * StarDrift * (major ? StarMajorSpeed : 1f);
                AddSparkle(position, axis.ToRotation(), scale, velocity, StarOuter, StarAdditive);
            }
            for (int side = -1; side <= 1; side += 2) {
                Vector2 axis = (MathHelper.PiOver2 * side + baseRot).ToRotationVector2() * StarSideAxis;
                AddSparkle(position, axis.ToRotation(), arm * StarOuterScale, axis.RotatedBy(MathHelper.PiOver2) * StarSideDrift, StarMid, StarAdditive);
            }
            for (int i = 0; i < StarArmCount; i++) {
                Vector2 axis = (MathHelper.PiOver2 * i + baseRot).ToRotationVector2() * StarAxis;
                bool major = i % 2 == 1;
                Vector2 scale = arm * StarCoreScale * (major ? StarMajorScale : 1f);
                Vector2 velocity = axis * StarDrift * (major ? StarMajorSpeed : 1f);
                AddSparkle(position, axis.ToRotation(), scale, velocity, StarCore, 1f);

                Dust small = Dust.NewDustPerfect(position, DustID.Sandnado, axis.RotatedBy(Main.rand.NextFloatDirection() * MathHelper.TwoPi * StarDustSpread) * Main.rand.NextFloat());
                small.noGravity = true;
                small.scale = StarDustSmall;
                Dust large = Dust.NewDustPerfect(position, DustID.Sandnado, -axis.RotatedBy(Main.rand.NextFloatDirection() * MathHelper.TwoPi * StarDustSpread) * Main.rand.NextFloat());
                large.noGravity = true;
                large.scale = StarDustLarge;
            }
        }

        private static void AddSparkle(Vector2 position, float rotation, Vector2 scale, Vector2 velocity, Color tint, float additive) {
            PrettySparkleParticle sparkle = new PrettySparkleParticle();
            sparkle.ColorTint = tint;
            sparkle.LocalPosition = position;
            sparkle.Rotation = rotation;
            sparkle.Scale = scale;
            sparkle.FadeInNormalizedTime = StarFadeIn;
            sparkle.FadeOutNormalizedTime = StarFadeOut;
            sparkle.TimeToLive = StarLife;
            sparkle.FadeOutEnd = StarLife;
            sparkle.FadeInEnd = StarLife / 2f;
            sparkle.FadeOutStart = StarLife / 2f;
            sparkle.AdditiveAmount = additive;
            sparkle.Velocity = velocity;
            sparkle.DrawVerticalAxis = false;
            Main.ParticleSystem_World_OverPlayers.Add(sparkle);
        }

        /// <summary>刃刺中目标,火花顺着刺向飞</summary>
        public static void BladeHit(Vector2 point, Vector2 dir) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < BladeSparkCount; i++) {
                Vector2 velocity = dir.RotatedByRandom(BladeSparkSpread) * Main.rand.NextFloat(BladeSparkSpeedMin, BladeSparkSpeedMax);
                Color color = Main.rand.NextBool() ? PaleGold : Orange;
                PRTLoader.NewParticle<PRT_LineCal>(point, velocity, color, Main.rand.NextFloat(BladeSparkScaleMin, BladeSparkScaleMax)).Configure(false, Main.rand.Next(BladeSparkLifeMin, BladeSparkLifeMax));
            }
            CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(BladeHitPitchMin, BladeHitPitchMax), point, BladeHitInstances, BladeHitVolume * CEUtils.WeapSound);
        }

        /// <summary>刃尖放出两发能量球的一拍</summary>
        public static void OrbLaunch(Vector2 muzzle, Vector2 dir) {
            if (Main.dedServ) {
                return;
            }
            //Item60 在原版既是泰拉刃剑气也是沙元素的远古风暴
            SoundEngine.PlaySound(SoundID.Item60 with { Volume = LaunchVolume, PitchVariance = LaunchPitchVariance, MaxInstances = LaunchInstances }, muzzle);
            for (int i = 0; i < LaunchDustCount; i++) {
                Vector2 velocity = dir.RotatedByRandom(LaunchDustSpread) * Main.rand.NextFloat(LaunchDustSpeedMin, LaunchDustSpeedMax);
                Dust dust = Dust.NewDustPerfect(muzzle, DustID.Sandnado, velocity, 0, default, Main.rand.NextFloat(LaunchDustScaleMin, LaunchDustScaleMax));
                dust.noGravity = true;
            }
        }

        /// <summary>能量球撞物块或打完最后一次命中后的消散</summary>
        public static void OrbPop(Vector2 position) {
            if (Main.dedServ) {
                return;
            }
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(position, Vector2.Zero, Orange, PopRingStart).Configure(Vector2.One, 0f, PopRingEnd, PopRingLife);
            for (int i = 0; i < PopDustCount; i++) {
                Vector2 velocity = (MathHelper.TwoPi * i / PopDustCount).ToRotationVector2() * Main.rand.NextFloat(PopDustSpeedMin, PopDustSpeedMax);
                Dust dust = Dust.NewDustPerfect(position, DustID.Sandnado, velocity);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = PopVolume, PitchVariance = PopPitchVariance, MaxInstances = PopInstances }, position);
        }

        public static void FangAppear(Vector2 position) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < FangAppearDustCount; i++) {
                Dust dust = Dust.NewDustPerfect(position, DustID.Venom, Main.rand.NextVector2Circular(FangAppearDustSpeed, FangAppearDustSpeed));
                dust.noGravity = true;
            }
        }

        /// <summary>一组毒牙里第一发起跳时的声音,Item17 在原版是毒刺射出</summary>
        public static void FangDart(Vector2 position) {
            if (Main.dedServ) {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = FangDartVolume, Pitch = FangDartPitch, PitchVariance = FangDartPitchVariance, MaxInstances = FangDartInstances }, position);
        }

        /// <summary>毒牙命中,白色火花顺着毒牙去向飞</summary>
        public static void FangHit(Vector2 position, Vector2 dir) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < FangSparkCount; i++) {
                Vector2 velocity = dir.RotatedByRandom(FangSparkSpread) * Main.rand.NextFloat(FangSparkSpeedMin, FangSparkSpeedMax);
                PRTLoader.NewParticle<PRT_AltSpark>(position, velocity, FangWhite, Main.rand.NextFloat(FangSparkScaleMin, FangSparkScaleMax)).Configure(false, Main.rand.Next(FangSparkLifeMin, FangSparkLifeMax));
            }
            for (int i = 0; i < FangVenomDustCount; i++) {
                Vector2 velocity = -dir.RotatedByRandom(FangVenomDustSpread) * Main.rand.NextFloat(FangVenomDustSpeedMin, FangVenomDustSpeedMax);
                Dust dust = Dust.NewDustPerfect(position, DustID.Venom, velocity);
                dust.noGravity = true;
            }
        }
    }
}
