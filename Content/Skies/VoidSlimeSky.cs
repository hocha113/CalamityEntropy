using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>
    /// 虚空史莱姆天幕的客户端驱动,租约模式同 GodSkyDrive:宿主每帧 Report,拍点 PushFlash,没人续租就自然衰减
    /// 纯表现,各端看自己的状态机副本,不过线
    /// </summary>
    public static class VSlimeSkyDrive
    {
        private const float TrackPerTick = 0.02f;
        private const float FallPerTick = 0.012f;
        private const float DarknessLerp = 0.04f;
        private const float FlashDecay = 0.93f;

        /// <summary>存在强度 0..1,和天空件的淡入包络相乘成可见强度</summary>
        public static float Intensity { get; private set; }
        /// <summary>压黑程度 0..1,死兆星阶段拉满,极光随它燄灭</summary>
        public static float Darkness { get; private set; }
        /// <summary>拍点闪光 0..1,自衰减</summary>
        public static float Flash { get; private set; }
        public static Vector2 BossWorldPos { get; private set; }
        /// <summary>开打第一帧的本体位置,视差按相机减它算,不用绝对世界坐标</summary>
        public static Vector2 OriginWorldPos { get; private set; }
        public static bool Active => Intensity > 0.004f;

        private static float pendIntensity;
        private static float pendDarkness;
        private static Vector2 pendBossPos;
        private static float pendFlash;
        private static bool pendLease;
        private static bool leaseAlive;

        public static void Report(float intensity, float darkness, Vector2 bossWorldPos) {
            if (Main.dedServ) {
                return;
            }
            pendIntensity = Math.Max(pendIntensity, MathHelper.Clamp(intensity, 0f, 1f));
            pendDarkness = Math.Max(pendDarkness, MathHelper.Clamp(darkness, 0f, 1f));
            pendBossPos = bossWorldPos;
            pendLease = true;
        }

        public static void PushFlash(float strength) {
            if (Main.dedServ || strength <= 0f) {
                return;
            }
            pendFlash = Math.Max(pendFlash, MathHelper.Clamp(strength, 0f, 1f));
        }

        internal static void Update() {
            bool wasAlive = leaseAlive;
            leaseAlive = pendLease;
            if (pendLease) {
                //新一场开打,压黑不从上一场的残值慢慢退回来
                if (!wasAlive && Intensity < 0.05f) {
                    OriginWorldPos = pendBossPos;
                    Darkness = pendDarkness;
                }
                float diff = pendIntensity - Intensity;
                Intensity = Math.Abs(diff) <= TrackPerTick ? pendIntensity : Intensity + Math.Sign(diff) * TrackPerTick;
                Darkness = MathHelper.Lerp(Darkness, pendDarkness, DarknessLerp);
                BossWorldPos = pendBossPos;
            }
            else {
                Intensity = Math.Max(0f, Intensity - FallPerTick);
            }
            Flash = Math.Max(Flash * FlashDecay, pendFlash);
            if (Flash < 0.003f) {
                Flash = 0f;
            }
            pendIntensity = 0f;
            pendDarkness = 0f;
            pendFlash = 0f;
            pendLease = false;
        }

        internal static void Reset() {
            Intensity = 0f;
            Darkness = 0f;
            Flash = 0f;
            BossWorldPos = Vector2.Zero;
            OriginWorldPos = Vector2.Zero;
            pendIntensity = 0f;
            pendDarkness = 0f;
            pendFlash = 0f;
            pendLease = false;
            leaseAlive = false;
        }
    }

    /// <summary>NPC 都上报完再结算,换世界清零</summary>
    [Autoload(Side = ModSide.Client)]
    public class VSlimeSkyDriveSystem : ModSystem
    {
        public override void PostUpdateEverything() => VSlimeSkyDrive.Update();

        public override void ClearWorld() => VSlimeSkyDrive.Reset();
    }

    public class VoidSlimeSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player) => VSlimeSkyDrive.Active;

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals(VoidSlimeSky.Key, isActive);
        }
    }

    /// <summary>
    /// 虚空史莱姆的天幕:太阳消失,深空渐变底、闪烁的星点和偏紫的极光,死兆星阶段整片压黑
    /// 跨 0 切片,画在调用方背景矩阵空间;可见强度 = opacity × VSlimeSkyDrive.Intensity
    /// 苍白形态不开天幕,破壳拍起由状态声明的强度驱动
    /// </summary>
    public class VoidSlimeSky : CESkyBase
    {
        public const string Key = "CalamityEntropy:VoidSlimeSky";

        private const int GradientBands = 40;
        private const int StarCount = 110;
        private const float TileTintAmount = 0.3f;

        private struct Star
        {
            public Vector2 Pos;
            public float Size;
            public float Seed;
            public bool Cross;
        }

        private Star[] stars;
        private int counter;

        private float Visible => opacity * VSlimeSkyDrive.Intensity;

        protected override bool KeepActive() => VSlimeSkyDrive.Active;

        public override Color OnTileColor(Color inColor) => Color.Lerp(inColor, VSlimeVfx.SkyTileTint, TileTintAmount * Visible);

        public override float GetCloudAlpha() => 1f - Visible;

        protected override void UpdatePayload(GameTime gameTime) {
            counter++;
        }

        /// <summary>星点布局用固定种子,每次进场都是同一片星空</summary>
        private void EnsureStars() {
            if (stars != null) {
                return;
            }
            UnifiedRandom random = new UnifiedRandom(20261111);
            stars = new Star[StarCount];
            for (int i = 0; i < stars.Length; i++) {
                stars[i] = new Star {
                    Pos = new Vector2(random.NextFloat(), random.NextFloat()),
                    Size = random.NextFloat(0.4f, 1.2f),
                    Seed = random.NextFloat(MathHelper.TwoPi),
                    Cross = random.NextBool(6),
                };
            }
        }

        protected override void DrawFront(SpriteBatch sb) {
            float vis = Visible;
            if (vis <= 0.004f) {
                return;
            }
            EnsureStars();
            float w = Main.screenWidth;
            float h = Main.screenHeight;
            float dark = VSlimeSkyDrive.Darkness;
            float flash = VSlimeSkyDrive.Flash;
            float time = counter / 60f;
            Vector2 camera = CESkyDrawing.RealScreenPosition - VSlimeSkyDrive.OriginWorldPos;

            CESkyDrawing.BeginCallerSpace(sb, BlendState.AlphaBlend, SamplerState.LinearClamp);
            Color top = Color.Lerp(VSlimeVfx.SkyTop, Color.Black, dark);
            Color horizon = Color.Lerp(VSlimeVfx.SkyHorizon, new Color(4, 3, 10), dark);
            int bandHeight = (int)MathF.Ceiling(h / GradientBands);
            for (int i = 0; i < GradientBands; i++) {
                float t = i / (float)(GradientBands - 1);
                sb.Draw(CEUtils.pixelTex, new Rectangle(0, i * bandHeight, (int)w + 1, bandHeight), Color.Lerp(top, horizon, t * t) * vis);
            }

            CESkyDrawing.BeginCallerSpace(sb, BlendState.Additive, SamplerState.LinearWrap);
            DrawStars(sb, w, h, vis * (1f - 0.5f * dark), time, camera);
            DrawAurora(sb, w, h, vis * (1f - dark), time);
            if (flash > 0.01f) {
                sb.Draw(CEUtils.pixelTex, CESkyDrawing.CallerFullscreen, GodDraw.Add(VSlimeVfx.AuroraViolet, 0.12f * flash * vis));
            }
            CESkyDrawing.RestoreCallerBatch(sb);
        }

        private void DrawStars(SpriteBatch sb, float w, float h, float vis, float time, Vector2 camera) {
            Texture2D dot = GodDraw.SoftLight;
            Texture2D cross = GodDraw.Star;
            //整片星点按相机慢漂,越界绕回另一侧
            Vector2 shift = camera * 0.008f;
            for (int i = 0; i < stars.Length; i++) {
                Star s = stars[i];
                float x = ((s.Pos.X * w - shift.X) % w + w) % w;
                float y = ((s.Pos.Y * h - shift.Y * 0.5f) % h + h) % h;
                float twinkle = 0.6f + 0.4f * MathF.Sin(time * (1.2f + s.Size) + s.Seed);
                Color color = Color.Lerp(VSlimeVfx.Lavender, Color.White, s.Size - 0.2f);
                if (s.Cross) {
                    Blit(sb, cross, new Vector2(x, y), GodDraw.Add(color, 0.9f * twinkle * vis), 0f, new Vector2(h * 0.05f * s.Size));
                }
                Blit(sb, dot, new Vector2(x, y), GodDraw.Add(color, 0.8f * twinkle * vis), 0f, new Vector2(h * 0.009f * s.Size));
            }
        }

        /// <summary>两道极光帘幕:一排竖向光柱沿正弦波排开,底边亮、向上散,紫为主、夹一点青</summary>
        private static void DrawAurora(SpriteBatch sb, float w, float h, float vis, float time) {
            if (vis <= 0.01f) {
                return;
            }
            const int curtains = 2;
            const int rays = 46;
            Texture2D glow = GodDraw.Glow;
            float rayWidth = w * 1.3f / rays * 3.2f;
            for (int c = 0; c < curtains; c++) {
                float drift = time * (0.05f + 0.02f * c);
                for (int i = 0; i < rays; i++) {
                    float u = i / (float)(rays - 1);
                    float x = (u * 1.3f - 0.15f) * w;
                    float wave = MathF.Sin(u * 5.2f + drift * 3f + c * 2.1f) * 0.05f + MathF.Sin(u * 11f - drift * 5f + c) * 0.02f;
                    float baseY = h * (0.3f + 0.13f * c + wave);
                    float height = h * (0.26f + 0.08f * MathF.Sin(u * 17f + time * 0.6f + c * 3f));
                    float shimmer = 0.55f + 0.45f * MathF.Sin(u * 23f - time * 0.9f + c * 1.7f);
                    Color color = Color.Lerp(VSlimeVfx.AuroraViolet, VSlimeVfx.AuroraTeal, 0.25f + 0.25f * MathF.Sin(u * 7f + time * 0.3f + c * 2.4f));
                    Blit(sb, glow, new Vector2(x, baseY - height * 0.42f), GodDraw.Add(color, 0.2f * shimmer * vis), 0f, new Vector2(rayWidth, height));
                    Blit(sb, glow, new Vector2(x, baseY), GodDraw.Add(color, 0.16f * shimmer * vis), 0f, new Vector2(rayWidth, h * 0.05f));
                }
            }
        }

        /// <summary>按目标像素尺寸居中画一张贴图,尺寸都按屏高折算</summary>
        private static void Blit(SpriteBatch sb, Texture2D tex, Vector2 pos, Color color, float rotation, Vector2 size) {
            sb.Draw(tex, pos, null, color, rotation, tex.Size() / 2f, size / tex.Size(), SpriteEffects.None, 0f);
        }
    }
}
