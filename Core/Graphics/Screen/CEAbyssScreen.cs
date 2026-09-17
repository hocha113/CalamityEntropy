using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static CalamityEntropy.CalamityEntropy;

namespace CalamityEntropy.Core.Graphics.Screen
{
    /// <summary>
    /// 深渊与血色两套全屏着色:结构完全对称,都是「裂隙类弹幕画成遮罩 → 整屏过 cabyss / cblood」。
    /// 两者都依赖 RT,复古 / 迷幻光照下由 <see cref="CEScreenPipeline"/> 整段跳过。
    /// </summary>
    internal static class CEAbyssScreen
    {
        /// <summary>深渊裂隙与 PRT_Abyssal 粒子的遮罩,经 cabyss 着色</summary>
        public static void DrawAbyssal(GraphicsDevice graphicsDevice) {
            CEScreenPipeline.CaptureScreenTo0(graphicsDevice);

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            foreach (Projectile proj in Main.ActiveProjectiles) {
                if (proj.ModProjectile is AbyssalCrack ac) {
                    ac.draw();
                }
                if (proj.ModProjectile is AbyssalRift ar)
                    ar.draw();
                if (proj.ModProjectile is AbyssBookmarkCrack ac2) {
                    ac2.drawVoid();
                }
                if (proj.ModProjectile is NxCrack nc) {
                    nc.drawCrack();
                }
                if (proj.ModProjectile is YstralynProj yst) {
                    yst.draw_crack();
                }
            }

            DrawAbyssalParticles();

            Main.spriteBatch.End();
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            Main.spriteBatch.Draw(CEScreenPipeline.Screen0, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            EffectLoader.cabyss.CurrentTechnique = EffectLoader.cabyss.Techniques["Technique1"];
            EffectLoader.cabyss.CurrentTechnique.Passes[0].Apply();
            EffectLoader.cabyss.Parameters["clr"].SetValue(new Color(12, 50, 160).ToVector4());
            EffectLoader.cabyss.Parameters["tex1"].SetValue(EffectLoader.AwSky1Tex.Value);
            EffectLoader.cabyss.Parameters["time"].SetValue(Instance.cvcount / 50f);
            EffectLoader.cabyss.Parameters["scrsize"].SetValue(CEScreenPipeline.Screen0.Size());
            EffectLoader.cabyss.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(Instance.cvcount * 1.4f, Instance.cvcount * 1.4f)) / new Vector2(Main.screenWidth, Main.screenHeight));
            Main.spriteBatch.Draw(Main.screenTargetSwap, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, Main.LocalPlayer.gravDir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);

            Main.spriteBatch.End();
        }

        /// <summary>血色裂隙,经 cblood 着色。没有对应弹幕存活时整段不跑</summary>
        public static void DrawBlood(GraphicsDevice graphicsDevice) {
            if (!CEUtils.AnyActiveProj<BloodCrack>())
                return;
            bool f = false;
            foreach (var p in Main.ActiveProjectiles) {
                if (p.ModProjectile != null && p.ModProjectile is BloodCrack) {
                    f = true;
                }
            }
            if (!f) {
                return;
            }

            CEScreenPipeline.CaptureScreenTo0(graphicsDevice);

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            foreach (Projectile proj in Main.ActiveProjectiles) {
                if (proj.ModProjectile is BloodCrack ac) {
                    ac.draw();
                }
            }

            Main.spriteBatch.End();
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            Main.spriteBatch.Draw(CEScreenPipeline.Screen0, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            EffectLoader.cblood.CurrentTechnique = EffectLoader.cblood.Techniques["Technique1"];
            EffectLoader.cblood.CurrentTechnique.Passes[0].Apply();
            EffectLoader.cblood.Parameters["clr"].SetValue(new Color(100, 0, 0).ToVector4());
            EffectLoader.cblood.Parameters["tex1"].SetValue(EffectLoader.BlurryPerlinNoiseTex.Value);
            EffectLoader.cblood.Parameters["time"].SetValue(Instance.cvcount / 50f);
            EffectLoader.cblood.Parameters["scrsize"].SetValue(CEScreenPipeline.Screen0.Size());
            EffectLoader.cblood.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(Instance.cvcount * 1.4f, Instance.cvcount * 1.4f)) / new Vector2(Main.screenWidth, Main.screenHeight));
            Main.spriteBatch.Draw(Main.screenTargetSwap, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, Main.LocalPlayer.gravDir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);

            Main.spriteBatch.End();
        }

        /// <summary>
        /// PRT_Abyssal 的 cvmask 光斑。过滤条件与 <see cref="CEVoidScreen"/> 那套镜像:
        /// 虚空粒子排除 PRT_Abyssal,这里只认 PRT_Abyssal
        /// </summary>
        private static void DrawAbyssalParticles() {
            foreach (var prt in PRTLoader.PRT_InGame_World_Inds) {
                if (!prt.active || prt.Mod != Instance) {
                    continue;
                }
                if (prt is not PRT_Abyssal pt) {
                    continue;
                }
                if (EffectLoader.cvmask == null) {
                    continue;
                }
                Main.spriteBatch.Draw(EffectLoader.cvmask.Value, pt.Position - Main.screenPosition, null, Color.White * 0.06f, pt.Rotation, EffectLoader.cvmask.Value.Size() / 2, (5.4f * pt.Opacity) * 0.05f, SpriteEffects.None, 0);
            }
        }
    }
}
