using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class SunGoddessSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(ModContent.NPCType<SunGoddess>()) || GodSkyDrive.Sun.Active;

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals(SunGoddessSky.Key, isActive);
        }
    }

    /// <summary>
    /// 太阳女神的天幕:压暗的琥珀色渐变底,上方一轮随阶段变大变红的太阳,地平线的余晖和上升的火星
    /// 跨 0 切片,画在调用方背景矩阵空间;可见强度 = opacity × GodSkyDrive.Sun.Intensity
    /// 底色压暗是为了把金色留给本体和弹幕
    /// </summary>
    public class SunGoddessSky : CESkyBase
    {
        public const string Key = "CalamityEntropy:SunGoddessSky";

        private const int GradientBands = 40;
        private const int EmberCount = 40;
        private const float TileTintAmount = 0.3f;

        private struct Ember
        {
            public Vector2 Pos;
            public float Speed;
            public float Size;
            public float Seed;
        }

        private readonly Ember[] embers = new Ember[EmberCount];
        private bool embersReady;
        private int counter;

        private static GodSkyDrive Drive => GodSkyDrive.Sun;
        private float Visible => opacity * Drive.Intensity;

        protected override float FadeInStep => SunDirector.SkyFadePerTick;

        protected override bool KeepActive() => NPC.AnyNPCs(ModContent.NPCType<SunGoddess>()) || Drive.Active;

        public override Color OnTileColor(Color inColor) => Color.Lerp(inColor, SunVfx.SkyTileTint, TileTintAmount * Visible);

        public override float GetCloudAlpha() => 1f - Visible;

        protected override void OnReset() {
            embersReady = false;
        }

        protected override void UpdatePayload(GameTime gameTime) {
            counter++;
            if (!embersReady) {
                embersReady = true;
                for (int i = 0; i < embers.Length; i++) {
                    embers[i] = NewEmber(Main.rand.NextFloat());
                }
            }
            float haste = 1f + 0.5f * (Drive.Phase - 1f);
            for (int i = 0; i < embers.Length; i++) {
                ref Ember e = ref embers[i];
                e.Pos.Y -= e.Speed * haste;
                e.Pos.X += MathF.Sin(counter * 0.02f + e.Seed) * 0.0006f;
                if (e.Pos.Y < -0.05f) {
                    e = NewEmber(1.05f);
                }
            }
        }

        private static Ember NewEmber(float y) => new Ember {
            Pos = new Vector2(Main.rand.NextFloat(), y),
            Speed = Main.rand.NextFloat(0.0008f, 0.0026f),
            Size = Main.rand.NextFloat(0.5f, 1.2f),
            Seed = Main.rand.NextFloat(MathHelper.TwoPi),
        };

        protected override void DrawFront(SpriteBatch sb) {
            float vis = Visible;
            if (vis <= 0.004f) {
                return;
            }
            float w = Main.screenWidth;
            float h = Main.screenHeight;
            float late = MathHelper.Clamp((Drive.Phase - 1f) * 0.5f, 0f, 1f);
            float flash = Drive.Flash;
            float time = counter / 60f;

            CESkyDrawing.BeginCallerSpace(sb, BlendState.AlphaBlend, SamplerState.LinearClamp);
            Color horizon = Color.Lerp(SunVfx.SkyHorizon, SunVfx.SkyHorizonLate, late);
            int bandHeight = (int)MathF.Ceiling(h / GradientBands);
            for (int i = 0; i < GradientBands; i++) {
                float t = i / (float)(GradientBands - 1);
                Color band = Color.Lerp(SunVfx.SkyTop, horizon, t * t);
                sb.Draw(CEUtils.pixelTex, new Rectangle(0, i * bandHeight, (int)w + 1, bandHeight), band * vis);
            }

            CESkyDrawing.BeginCallerSpace(sb, BlendState.Additive, SamplerState.LinearClamp);
            //地平线余晖
            Blit(sb,GodDraw.Glow, new Vector2(w * 0.5f, h * 1.02f), GodDraw.Add(SunVfx.Ember, 0.3f * vis), 0f, new Vector2(w * 1.5f, h * 0.75f));
            DrawSun(sb, w, h, vis, late, flash, time);
            DrawEmbers(sb, w, h, vis, time);
            if (flash > 0.01f) {
                sb.Draw(CEUtils.pixelTex, CESkyDrawing.CallerFullscreen, GodDraw.Add(SunVfx.Ember, 0.12f * flash * vis));
            }
            CESkyDrawing.RestoreCallerBatch(sb);
        }

        private void DrawSun(SpriteBatch sb, float w, float h, float vis, float late, float flash, float time) {
            //远景只给一点视差
            Vector2 drift = (CESkyDrawing.RealScreenPosition - Drive.OriginWorldPos) * -0.012f;
            drift.X = MathHelper.Clamp(drift.X, -w * 0.12f, w * 0.12f);
            drift.Y = MathHelper.Clamp(drift.Y, -h * 0.05f, h * 0.05f);
            Vector2 sun = new Vector2(w * 0.5f, h * 0.18f) + drift;
            float radius = h * 0.08f * (1f + 0.36f * late) * (1f + 0.08f * flash);
            Color hot = Color.Lerp(SunVfx.Gold, SunVfx.Ember, 0.5f * late);

            Blit(sb,GodDraw.Glow, sun, GodDraw.Add(SunVfx.Ember, 0.3f * vis), 0f, new Vector2(radius * 9f));
            if (late > 0.5f) {
                Blit(sb,GodDraw.Corona, sun, GodDraw.Add(SunVfx.Crimson, (late - 0.5f) * 0.9f * vis), time * 0.02f, new Vector2(radius * 7.4f));
            }
            Blit(sb,GodDraw.Corona, sun, GodDraw.Add(SunVfx.Ember, 0.55f * vis), time * 0.04f, new Vector2(radius * 5.2f));
            Blit(sb,GodDraw.Corona, sun, GodDraw.Add(hot, 0.45f * vis), -time * 0.03f, new Vector2(radius * 4.2f));
            Blit(sb,GodDraw.Flash, sun, GodDraw.Add(SunVfx.Gold, (0.22f + 0.6f * flash) * vis), time * 0.02f, new Vector2(radius * 7f));
            Blit(sb,GodDraw.Disc, sun, GodDraw.Add(hot, 0.75f * vis), 0f, new Vector2(radius * 2f));
            Blit(sb,GodDraw.SoftLight, sun, GodDraw.Add(SunVfx.Core, 0.8f * vis), 0f, new Vector2(radius * 2.6f));
            Blit(sb,GodDraw.BloomRing, sun, GodDraw.Add(SunVfx.Gold, 0.45f * vis), 0f, new Vector2(radius * 2.7f / GodDraw.BloomRingVisible));
        }

        private void DrawEmbers(SpriteBatch sb, float w, float h, float vis, float time) {
            Texture2D tex = GodDraw.SoftLight;
            for (int i = 0; i < embers.Length; i++) {
                Ember e = embers[i];
                float twinkle = 0.55f + 0.45f * MathF.Sin(time * 3f + e.Seed * 5f);
                //越往上越淡
                float fade = MathHelper.Clamp(e.Pos.Y * 1.6f, 0f, 1f);
                Color color = Color.Lerp(SunVfx.Ember, SunVfx.Gold, e.Size - 0.5f);
                Blit(sb,tex, new Vector2(e.Pos.X * w, e.Pos.Y * h), GodDraw.Add(color, 0.7f * twinkle * fade * vis), 0f, new Vector2(h * 0.012f * e.Size));
            }
        }

        /// <summary>按目标像素尺寸居中画一张贴图,尺寸都按屏高折算,分辨率和缩放变了构图不变</summary>
        private static void Blit(SpriteBatch sb, Texture2D tex, Vector2 pos, Color color, float rotation, Vector2 size) {
            sb.Draw(tex, pos, null, color, rotation, tex.Size() / 2f, size / tex.Size(), SpriteEffects.None, 0f);
        }
    }
}