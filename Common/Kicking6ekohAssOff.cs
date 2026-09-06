using CalamityEntropy.Content.ILEditing;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    //反制 DieWithASmile(The End Of A Calamity Menu Theme)的 CalamitasMenuConflict:
    //对方检测到本模组就画全屏遮罩拒绝加载,还会尝试 TryDelete。这里钩住它的三个静态方法,
    //DrawOverlay 改成播爆炸序列+音乐+提示文字,TryDelete 直接吞掉,倒计时或左键后让 EntropyPresent 返回 false 放行。
    //全部通过 EModHooks 注册,随 Unload 一起撤销;对方未加载时整个类不做任何事。
    public class Kicking6ekohAssOff : ModSystem
    {
        public static MethodBase method;
        public static MethodBase method2;
        public static MethodBase method3;
        public static bool blocked = false;
        public override void PostSetupContent()
        {
            ExplosionFrame = 0;
            FrameCounter = 4;
            Counter = 0;
            explosions = new List<TinyExplosion>();
            if (ModLoader.TryGetMod("DieWithASmile", out var mod))
            {
                var sys = mod.Find<ModSystem>("CalamitasMenuConflict");
                if (sys != null)
                {
                    method = sys.GetType().GetMethod("DrawOverlay", BindingFlags.NonPublic | BindingFlags.Static);
                    if (method != null)
                        EModHooks.Add(method, hook);

                    method2 = sys.GetType().GetMethod("TryDelete", BindingFlags.NonPublic | BindingFlags.Static);
                    if (method2 != null)
                        EModHooks.Add(method2, hook2);

                    method3 = sys.GetType().GetMethod("EntropyPresent", BindingFlags.NonPublic | BindingFlags.Static);
                    if (method3 != null)
                        EModHooks.Add(method3, hook3);
                }
            }
        }
        public static Texture2D GetTex(int t) => ModContent.Request<Texture2D>("CalamityEntropy/Content/Particles/realisticexplosion/spr_realisticexplosion_" + t, AssetRequestMode.ImmediateLoad).Value;
        public static int ExplosionFrame = 0;
        public static int FrameCounter = 3;
        public static bool NukedTheFuckingUI = false;
        public static int Counter = 0;
        public static SlotId music;
        public class TinyExplosion
        {
            public Vector2 pos;
            public int frame = 0;
            public int frameCounter = 0;
            public float scale;
            public TinyExplosion(Vector2 pos, float scale)
            {
                this.pos = pos;
                this.scale = scale;
            }
            public void Draw(SpriteBatch spriteBatch)
            {
                if (frame == 0 && frameCounter == 0)
                {
                    CEUtils.PlaySound("badexplosion", 1, volume: 0.8f);
                }
                frameCounter++;
                if (frameCounter > 2)
                {
                    frame++;
                    frameCounter = 0;
                }
                if (frame <= 16)
                {
                    Texture2D tex = GetTex(int.Clamp(frame, 0, 16));
                    spriteBatch.Draw(tex, pos, null, Color.White, 0, tex.Size() * 0.5f, scale, SpriteEffects.None, 0);
                }
            }
        }
        public static List<TinyExplosion> explosions = new List<TinyExplosion>();
        public static void hook2(Action<string> orig, string nm)
        {
        }
        public static bool hook3(Func<bool> orig)
        {
            if (blocked)
                return false;
            return orig();
        }
        public static void hook(Action<SpriteBatch, bool> orig, SpriteBatch spriteBatch, bool b)
        {
            if (!NukedTheFuckingUI)
            {
                if (FrameCounter == 4 && ExplosionFrame == 0)
                {
                    CEUtils.PlaySound("badexplosion");
                }
                orig(spriteBatch, b);
                FrameCounter--;
                if (FrameCounter == 0)
                {
                    FrameCounter = 4;
                    ExplosionFrame++;
                }
                if (ExplosionFrame > 16)
                {
                    music = SoundEngine.PlaySound(CEUtils.GetSound("musrtb") with { IsLooped = true });

                    NukedTheFuckingUI = true;
                    return;
                }
                else
                {
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
                    Texture2D tex = GetTex(int.Clamp(ExplosionFrame, 0, 16));
                    spriteBatch.Draw(tex, Main.ScreenSize.ToVector2() * 0.5f, null, Color.White, 0, tex.Size() * 0.5f, 16, SpriteEffects.None, 0);
                    spriteBatch.End();
                }
                explosions.Add(new TinyExplosion(CEUtils.randomPoint(new Rectangle(0, 0, Main.screenWidth, Main.screenHeight)), Main.rand.NextFloat(4, 6)));
            }
            else
            {
                if (Counter < 80)
                {
                    explosions.Add(new TinyExplosion(CEUtils.randomPoint(new Rectangle(0, 0, Main.screenWidth, Main.screenHeight)), Main.rand.NextFloat(6, 8)));
                }
                float TextRot = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.6f) * 0.2f;
                string text = CalamityEntropy.Instance.GetLocalization("TitleTexts.Kicking6esAssOff").WithFormatArgs(CalamityEntropy.Instance.DisplayName).Value;
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
                Main.spriteBatch.Draw(ModContent.Request<Texture2D>(CEUtils.WhiteTexPath).Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black);
                for (int i = 0; i < 16; i++)
                {
                    spriteBatch.DrawString(FontAssets.MouseText.Value, text, Main.ScreenSize.ToVector2() * 0.5f + (i / 16f * MathHelper.TwoPi).ToRotationVector2() * 4, Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.34f % MathHelper.PiOver2, 0.8f, 0.4f), TextRot, FontAssets.MouseText.Value.MeasureString(text) * 0.5f, 2, SpriteEffects.None, 0);
                }
                spriteBatch.DrawString(FontAssets.MouseText.Value, text, Main.ScreenSize.ToVector2() * 0.5f, Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.32f % MathHelper.PiOver2, 1f, 0.9f), TextRot, FontAssets.MouseText.Value.MeasureString(text) * 0.5f, 2, SpriteEffects.None, 0);
                spriteBatch.End();
                Counter++;
                if (Counter > 60 * 20 || Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    if (SoundEngine.TryGetActiveSound(music, out var snd))
                    {
                        snd.Stop();
                    }
                    Counter = 0;
                    if (ModLoader.TryGetMod("DieWithASmile", out var mod))
                    {
                        var sys = mod.Find<ModSystem>("CalamitasMenuConflict");
                        if (sys != null)
                        {
                            method = sys.GetType().GetMethod("Resolve", BindingFlags.NonPublic | BindingFlags.Static);
                            blocked = true;
                        }
                    }
                }
            }
            if (explosions.Count > 0)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
                for (int i = explosions.Count - 1; i >= 0; i--)
                {
                    explosions[i].Draw(spriteBatch);
                    if (explosions[i].frame > 16)
                        explosions.RemoveAt(i);
                }
                Main.spriteBatch.End();
            }
        }
    }
}
