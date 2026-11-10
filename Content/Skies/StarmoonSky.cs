using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.Skies
{
    public class StarmoonSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(ModContent.NPCType<StarmoonSage>()) || GodSkyDrive.Moon.Active;

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals(StarmoonSky.Key, isActive);
        }
    }

    /// <summary>
    /// 星月贤者的天幕:深海军蓝渐变底、缓慢漂移的星云、闪烁的星点,上方一轮月亮随阶段从新月盈成满月
    /// 跨 0 切片,画在调用方背景矩阵空间;可见强度 = opacity × GodSkyDrive.Moon.Intensity
    /// 月相是亮盘上叠一块底色暗盘,暗盘随阶段移开
    /// </summary>
    public class StarmoonSky : CESkyBase
    {
        public const string Key = "CalamityEntropy:StarmoonSky";

        private const int GradientBands = 40;
        private const int StarCount = 90;
        private const float TileTintAmount = 0.25f;

        private struct Star
        {
            public Vector2 Pos;
            public float Size;
            public float Seed;
            public bool Cross;
        }

        private Star[] stars;
        private int counter;

        private static GodSkyDrive Drive => GodSkyDrive.Moon;
        private float Visible => opacity * Drive.Intensity;

        protected override float FadeInStep => SageDirector.SkyFadePerTick;

        protected override bool KeepActive() => NPC.AnyNPCs(ModContent.NPCType<StarmoonSage>()) || Drive.Active;

        public override Color OnTileColor(Color inColor) => Color.Lerp(inColor, SageVfx.SkyTileTint, TileTintAmount * Visible);

        public override float GetCloudAlpha() => 1f - Visible;

        protected override void UpdatePayload(GameTime gameTime) {
            counter++;
        }

        /// <summary>星点布局用固定种子,每次进场都是同一片星空</summary>
        private void EnsureStars() {
            if (stars != null) {
                return;
            }
            UnifiedRandom random = new UnifiedRandom(20261110);
            stars = new Star[StarCount];
            for (int i = 0; i < stars.Length; i++) {
                stars[i] = new Star {
                    Pos = new Vector2(random.NextFloat(), MathF.Pow(random.NextFloat(), 1.4f)),
                    Size = random.NextFloat(0.4f, 1.2f),
                    Seed = random.NextFloat(MathHelper.TwoPi),
                    Cross = random.NextBool(5),
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
            float late = MathHelper.Clamp((Drive.Phase - 1f) * 0.5f, 0f, 1f);
            float flash = Drive.Flash;
            float time = counter / 60f;
            Vector2 camera = CESkyDrawing.RealScreenPosition - Drive.OriginWorldPos;

            CESkyDrawing.BeginCallerSpace(sb, BlendState.AlphaBlend, SamplerState.LinearClamp);
            Color horizon = Color.Lerp(SageVfx.SkyHorizon, SageVfx.SkyHorizonLate, late);
            int bandHeight = (int)MathF.Ceiling(h / GradientBands);
            for (int i = 0; i < GradientBands; i++) {
                float t = i / (float)(GradientBands - 1);
                Color band = Color.Lerp(SageVfx.SkyTop, horizon, t * t);
                sb.Draw(CEUtils.pixelTex, new Rectangle(0, i * bandHeight, (int)w + 1, bandHeight), band * vis);
            }

            CESkyDrawing.BeginCallerSpace(sb, BlendState.Additive, SamplerState.LinearWrap);
            DrawNebula(sb, w, h, vis, late, time, camera);
            DrawStars(sb, w, h, vis, time, camera);

            //月亮位置只给一点视差
            Vector2 drift = camera * -0.012f;
            drift.X = MathHelper.Clamp(drift.X, -w * 0.12f, w * 0.12f);
            drift.Y = MathHelper.Clamp(drift.Y, -h * 0.05f, h * 0.05f);
            Vector2 moon = new Vector2(w * 0.5f, h * 0.18f) + drift;
            float radius = h * 0.075f * (1f + 0.25f * late) * (1f + 0.06f * flash);
            Blit(sb, GodDraw.Glow, moon, GodDraw.Add(SageVfx.StarBlue, (0.22f + 0.2f * late + 0.3f * flash) * vis), 0f, new Vector2(radius * 8f));
            Blit(sb, GodDraw.Disc, moon, GodDraw.Add(SageVfx.MoonSilver, 0.85f * vis), 0f, new Vector2(radius * 2f));
            Blit(sb, GodDraw.SoftLight, moon, GodDraw.Add(SageVfx.StarWhite, 0.35f * vis), 0f, new Vector2(radius * 2.2f));

            //暗盘:阶段 1 盖住大半成新月,阶段 3 完全移开成满月
            float cover = MathHelper.Lerp(0.5f, 2.2f, late);
            if (cover < 2.05f) {
                CESkyDrawing.BeginCallerSpace(sb, BlendState.AlphaBlend, SamplerState.LinearClamp);
                Vector2 shadow = moon + new Vector2(-0.82f, -0.57f) * (radius * cover);
                Color dark = Color.Lerp(SageVfx.SkyTop, horizon, 0.06f);
                Blit(sb, GodDraw.Disc, shadow, dark * vis, 0f, new Vector2(radius * 2.06f));
                CESkyDrawing.BeginCallerSpace(sb, BlendState.Additive, SamplerState.LinearWrap);
            }

            //满月的光环和光芒
            if (late > 0.6f) {
                float full = (late - 0.6f) / 0.4f;
                Blit(sb, GodDraw.BloomRing, moon, GodDraw.Add(SageVfx.MoonSilver, 0.5f * full * vis), 0f, new Vector2(radius * 3f / GodDraw.BloomRingVisible));
                Blit(sb, GodDraw.Flash, moon, GodDraw.Add(SageVfx.StarBlue, 0.3f * full * vis), time * 0.02f, new Vector2(radius * 7f));
            }
            if (flash > 0.01f) {
                Blit(sb, GodDraw.Flash, moon, GodDraw.Add(SageVfx.StarWhite, 0.7f * flash * vis), -time * 0.03f, new Vector2(radius * 8f));
                sb.Draw(CEUtils.pixelTex, CESkyDrawing.CallerFullscreen, GodDraw.Add(SageVfx.StarBlue, 0.1f * flash * vis));
            }
            CESkyDrawing.RestoreCallerBatch(sb);
        }

        /// <summary>星云铺满屏并按相机做慢视差,采样器是 Wrap,源矩形越界自动续接</summary>
        private static void DrawNebula(SpriteBatch sb, float w, float h, float vis, float late, float time, Vector2 camera) {
            Texture2D nebula = CEUtils.getExtraTex("planetarium_blue_base");
            float scale = Math.Max(w / nebula.Width, h / nebula.Height) * 1.15f;
            Vector2 scroll = camera * 0.02f + new Vector2(time * 3f, 0f);
            Rectangle source = new Rectangle((int)(scroll.X / scale), (int)(scroll.Y / scale), (int)(w / scale) + 2, (int)(h / scale) + 2);
            sb.Draw(nebula, Vector2.Zero, source, GodDraw.Add(Color.White, (0.3f + 0.15f * late) * vis), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawStars(SpriteBatch sb, float w, float h, float vis, float time, Vector2 camera) {
            Texture2D dot = GodDraw.SoftLight;
            Texture2D cross = GodDraw.Star;
            //整片星点按相机慢漂,越界绕回另一侧
            Vector2 shift = camera * 0.008f;
            for (int i = 0; i < stars.Length; i++) {
                Star s = stars[i];
                float x = ((s.Pos.X * w - shift.X) % w + w) % w;
                float y = s.Pos.Y * h * 0.9f - MathHelper.Clamp(shift.Y, -h * 0.05f, h * 0.05f);
                float twinkle = 0.6f + 0.4f * MathF.Sin(time * (1.2f + s.Size) + s.Seed);
                Color color = Color.Lerp(SageVfx.StarBlue, SageVfx.StarWhite, s.Size - 0.2f);
                if (s.Cross) {
                    Blit(sb, cross, new Vector2(x, y), GodDraw.Add(color, 0.9f * twinkle * vis), 0f, new Vector2(h * 0.05f * s.Size));
                }
                Blit(sb, dot, new Vector2(x, y), GodDraw.Add(color, 0.8f * twinkle * vis), 0f, new Vector2(h * 0.009f * s.Size));
            }
        }

        /// <summary>按目标像素尺寸居中画一张贴图,尺寸都按屏高折算</summary>
        private static void Blit(SpriteBatch sb, Texture2D tex, Vector2 pos, Color color, float rotation, Vector2 size) {
            sb.Draw(tex, pos, null, color, rotation, tex.Size() / 2f, size / tex.Size(), SpriteEffects.None, 0f);
        }
    }
}