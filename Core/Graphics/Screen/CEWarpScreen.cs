using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Donator.Ratziel;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Tiles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Graphics.Screen
{
    /// <summary>
    /// 屏幕扭曲系:刀光(kscreen)与区域碎裂(fscreen)。两者都受客户端配置 ScreenWarpEffects 门控,
    /// 都依赖 RT,复古 / 迷幻光照下由 <see cref="CEScreenPipeline"/> 整段跳过。
    /// </summary>
    internal static class CEWarpScreen
    {
        //历史上这几个 ID 在绘制路径里用 -1 哨兵懒解析,现在统一在 PostSetupContent 期解析一次
        private static int slashType = -1;
        private static int slash2Type = -1;
        private static int voidExplodeType = -1;
        private static int voidRExpType = -1;
        private static int starlessNightProjType = -1;
        private static int cruiserEnergyBallType = -1;
        private static int voidResidueType = -1;
        private static int ratzielSentryType = -1;
        private static int voidOreTileType = -1;

        /// <summary>在 PostSetupContent 期一次解析,由 <see cref="Hooks.CEDrawHooks"/> 调用</summary>
        public static void ResolveTypes() {
            slashType = ModContent.ProjectileType<Slash>();
            slash2Type = ModContent.ProjectileType<Slash2>();
            voidExplodeType = ModContent.ProjectileType<VoidExplode>();
            voidRExpType = ModContent.ProjectileType<VoidRExp>();
            starlessNightProjType = ModContent.ProjectileType<StarlessNightProj>();
            cruiserEnergyBallType = ModContent.ProjectileType<CruiserEnergyBall>();
            voidResidueType = ModContent.ProjectileType<VoidResidue>();
            ratzielSentryType = ModContent.ProjectileType<RatzielSentry>();
            voidOreTileType = ModContent.TileType<VoidOreTile>();
        }

        /// <summary>刀光冲击波:遮罩画进交换缓冲,经 kscreen 把主屏推出位移</summary>
        public static void DrawSlashWarp(GraphicsDevice graphicsDevice) {
            if (CEScreenPipeline.Screen0 == null || CEScreenPipeline.Screen1 == null)
                return;
            if (!Config.Instance.ScreenWarpEffects)
                return;

            CEScreenPipeline.CaptureScreenTo0(graphicsDevice);

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D ksc1 = EffectLoader.ksc1.Value;
            Texture2D shockwave = EffectLoader.shockwave.Value;

            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile == null) {
                    continue;
                }

                if (p.type == slashType) {
                    Main.spriteBatch.Draw(ksc1, p.Center - Main.screenPosition + new Vector2((p.ai[0] + p.ai[1]) / 2 - 165, 0).RotatedBy(p.rotation), null, Color.White, p.rotation + (float)Math.PI / 2, new Vector2(ksc1.Width, ksc1.Height) / 2, new Vector2((p.ai[0] - p.ai[1]) / ksc1.Width * 0.4f, 0.1f), SpriteEffects.None, 0);
                }
                else if (p.type == slash2Type) {
                    Main.spriteBatch.Draw(ksc1, p.Center - Main.screenPosition + new Vector2((p.ai[0] + p.ai[1]) / 2 - 300, 0).RotatedBy(p.rotation), null, Color.White, p.rotation + (float)Math.PI / 2, new Vector2(ksc1.Width, ksc1.Height) / 2, new Vector2((p.ai[0] - p.ai[1]) / ksc1.Width * 1.4f, 1f), SpriteEffects.None, 0);
                }
                else if (p.type == voidExplodeType) {
                    if (p.ModProjectile is VoidExplode ve) {
                        float ks = ve.Projectile.timeLeft * 0.1f;
                        if (ve.Projectile.timeLeft > 10) ks = (20 - (float)ve.Projectile.timeLeft) / 10f;
                        ks *= (1 + ve.Projectile.ai[1]);
                        Main.spriteBatch.Draw(ksc1, ve.Projectile.Center - Main.screenPosition, null, Color.White, 0, new Vector2(ksc1.Width, ksc1.Height) / 2, ks * 2, SpriteEffects.None, 0);
                    }
                }
                else if (p.type == voidRExpType) {
                    if (p.ModProjectile is VoidRExp vre) {
                        float ks = (90f - vre.Projectile.timeLeft) * 0.4f;
                        if (p.ai[0] == 1)
                            ks = p.timeLeft * 0.4f;
                        float a = p.ai[0] == 0 ? (vre.Projectile.timeLeft / 90f) : 1 - (vre.Projectile.timeLeft / 90f);
                        Main.spriteBatch.Draw(shockwave, vre.Projectile.Center - Main.screenPosition, null, Color.White * a, 0, new Vector2(shockwave.Width, shockwave.Height) / 2, ks, SpriteEffects.None, 0);
                    }
                }
                else if (p.type == starlessNightProjType) {
                    if (p.ModProjectile is StarlessNightProj sl) {
                        sl.drawSlash();
                    }
                }
            }

            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            EffectLoader.kscreen.CurrentTechnique = EffectLoader.kscreen.Techniques["Technique1"];
            EffectLoader.kscreen.CurrentTechnique.Passes[0].Apply();
            EffectLoader.kscreen.Parameters["tex0"].SetValue(Main.screenTargetSwap);
            EffectLoader.kscreen.Parameters["i"].SetValue(0.1f);
            Main.spriteBatch.Draw(CEScreenPipeline.Screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        /// <summary>区域碎裂:能量球 / 虚空残渣 / 拉兹尔哨兵的光斑与虚渺矿辉光,经 fscreen 扰动主屏</summary>
        public static void DrawFragWarp(GraphicsDevice graphicsDevice) {
            if (CEScreenPipeline.Screen0 == null || CEScreenPipeline.Screen1 == null)
                return;
            if (!Config.Instance.ScreenWarpEffects)
                return;

            CEScreenPipeline.CaptureScreenTo0(graphicsDevice);

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);

            if (Main.LocalPlayer.Entropy().voidOreNearby > 0 && Config.Instance.TileEffect)
                DrawVoidOres(voidOreTileType);

            bool startBatch = false;
            Texture2D rGlowTex = CEExtraAssets.Circle;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == cruiserEnergyBallType && p.ModProjectile is CruiserEnergyBall ceb) {
                    if (!startBatch) {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        startBatch = true;
                    }
                    CEUtils.DrawGlow(p.Center, Color.White * p.Opacity * 0.72f, 14 * ceb.Scale);
                }
                if (p.type == voidResidueType) {
                    if (!startBatch) {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        startBatch = true;
                    }
                    CEUtils.DrawGlow(p.Center, Color.White * p.Opacity * 0.4f, 3);
                }
                if (p.type == ratzielSentryType) {
                    if (!startBatch) {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        startBatch = true;
                    }
                    CEUtils.DrawGlow(p.Center, Color.White * p.Opacity * 0.42f, 6, true, rGlowTex);
                }
            }
            if (startBatch)
                Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            EffectLoader.fscreen.CurrentTechnique = EffectLoader.fscreen.Techniques["Technique1"];
            EffectLoader.fscreen.CurrentTechnique.Passes[0].Apply();
            EffectLoader.fscreen.Parameters["strengthMult"].SetValue(0.1f);
            EffectLoader.fscreen.Parameters["screen"].SetValue(Main.screenPosition * new Vector2(1, Main.LocalPlayer.gravDir) / Main.ScreenSize.ToVector2());
            EffectLoader.fscreen.Parameters["iTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.034f);
            EffectLoader.fscreen.Parameters["coordMult"].SetValue(new Vector2(1, (float)Main.screenHeight / Main.screenWidth) * 1.2f);
            graphicsDevice.Textures[0] = CEScreenPipeline.Screen0;
            graphicsDevice.Textures[1] = Main.screenTargetSwap;
            graphicsDevice.Textures[2] = CEExtraAssets.VoidBack;
            Main.spriteBatch.Draw(CEScreenPipeline.Screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// 给屏内每块虚渺矿加一圈辉光。扫描范围沿用原版物块遍历的边界钳制写法
        /// (原实现从 <c>TileDrawing</c> 抄来时带了一批只赋值不读取的局部量,这里已剔除,边界计算逐字保留)
        /// </summary>
        public static void DrawVoidOres(int types) {
            if (types <= 0) {
                return;
            }

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            int offScreenRange = Main.offScreenRange;
            Vector2 screenPosition = Main.screenPosition;
            int screenWidth = Main.screenWidth;
            int screenHeight = Main.screenHeight;
            int maxTilesX = Main.maxTilesX;
            int maxTilesY = Main.maxTilesY;
            var _tileArray = Main.tile;

            int num5 = (int)(screenPosition.X / 16f - 1f);
            int num6 = (int)((screenPosition.X + (float)screenWidth) / 16f) + 2;
            int num7 = (int)(screenPosition.Y / 16f - 1f);
            int num8 = (int)((screenPosition.Y + (float)screenHeight) / 16f) + 5;
            int num9 = offScreenRange / 16;
            int num10 = offScreenRange / 16;
            if (num5 - num9 < 4) {
                num5 = num9 + 4;
            }
            if (num6 + num9 > maxTilesX - 4) {
                num6 = maxTilesX - num9 - 4;
            }
            if (num7 - num10 < 4) {
                num7 = num10 + 4;
            }
            if (num8 + num10 > maxTilesY - 4) {
                num8 = maxTilesY - num10 - 4;
            }
            Point screenOverdrawOffset = Main.GetScreenOverdrawOffset();
            for (int i = num7 - num10 + screenOverdrawOffset.Y; i < num8 + num10 - screenOverdrawOffset.Y; i++) {
                for (int j = num5 - num9 + screenOverdrawOffset.X; j < num6 + num9 - screenOverdrawOffset.X; j++) {
                    Tile tile = _tileArray[j, i];
                    if (tile.HasTile && tile.TileType == types) {
                        //传世界坐标,DrawGlow 内部自行换算
                        Vector2 pos = new Vector2(j * 16, i * 16);
                        CEUtils.DrawGlow(pos + new Vector2(8, 8), Color.White * 0.24f, 3.6f, true, null, false);
                    }
                }
            }
            Main.spriteBatch.End();
        }
    }
}
