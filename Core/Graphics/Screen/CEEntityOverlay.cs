using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Projectiles.Pets.Abyss;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Graphics.Screen
{
    /// <summary>
    /// 全屏管线里唯一一批「没有着色器、直接以 AlphaBlend 叠回屏幕」的实体绘制。
    /// <para>
    /// 因为不依赖任何着色器,它是复古 / 迷幻光照下必须照常显示的那一部分:
    /// RT 可用时由 <see cref="CEScreenPipeline.EndCaptureDraw"/> 在历史位置调用(走 RT 合成,画面与改动前一致),
    /// 不可用时由 <see cref="CEScreenPipeline.DrawBeforeInfernoRings"/> 调用直绘版本。
    /// 两条路径共用同一份绘制内容,只是外面套的批次不同。
    /// </para>
    /// </summary>
    internal static class CEEntityOverlay
    {
        private static int voidBottleThrowType = -1;
        private static int cruiserShadowType = -1;
        private static int voidWraithType = -1;
        private static int abyssPetType = -1;
        private static int voidPalProjType = -1;
        private static int shadewindLanceThrowType = -1;
        private static int voidStarType = -1;
        private static int voidStarFType = -1;
        private static int cruiserHeadType = -1;
        private static int starlessNightProjType = -1;

        /// <summary>在 PostSetupContent 期一次解析,由 <see cref="Hooks.CEDrawHooks"/> 调用</summary>
        public static void ResolveTypes() {
            voidBottleThrowType = ModContent.ProjectileType<VoidBottleThrow>();
            cruiserShadowType = ModContent.ProjectileType<CruiserShadow>();
            voidWraithType = ModContent.ProjectileType<VoidWraith>();
            abyssPetType = ModContent.ProjectileType<AbyssPet>();
            voidPalProjType = ModContent.ProjectileType<VoidPalProj>();
            shadewindLanceThrowType = ModContent.ProjectileType<ShadewindLanceThrow>();
            voidStarType = ModContent.ProjectileType<VoidStar>();
            voidStarFType = ModContent.ProjectileType<VoidStarF>();
            cruiserHeadType = ModContent.NPCType<CruiserHead>();
            starlessNightProjType = ModContent.ProjectileType<StarlessNightProj>();
        }

        /// <summary>
        /// RT 路径:内容画进交换缓冲再整块叠回主屏。净效果等同于直接 AlphaBlend 叠加,
        /// 保留这套 RT 往返只为让颜色模式下的画面与改动前逐像素一致
        /// </summary>
        public static void DrawScreenOverlay(GraphicsDevice graphicsDevice) {
            if (CEScreenPipeline.Screen1 == null) {
                return;
            }

            CEScreenPipeline.CaptureScreenTo(graphicsDevice, CEScreenPipeline.Screen1);

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawOverlayContents();
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(CEScreenPipeline.Screen1, Vector2.Zero, Color.White);
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// 直绘路径:同样的内容,直接画在当前绑定的目标上。
        /// 复古 / 迷幻光照下没有可用的 RT,走这条
        /// </summary>
        public static void DrawScreenOverlayDirect() {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawOverlayContents();
            Main.spriteBatch.End();
        }

        /// <summary>
        /// 两条路径共用的绘制内容:暗影冲刺残迹、虚空瓶 / 巡洋者残影、越境追猎之眼、
        /// 虚空魔灵、深渊宠物、影风之枪、虚空星。进入时必须已有活跃批次
        /// </summary>
        private static void DrawOverlayContents() {
            foreach (Player player in Main.ActivePlayers) {
                if (!player.dead && player.Entropy().daPoints.Count > 2) {
                    float scj = 1f / player.Entropy().daPoints.Count;
                    float sc = scj;
                    Color color = player.Entropy().VaMoving > 0 ? Color.Blue : Color.Black;
                    for (int i = 1; i < player.Entropy().daPoints.Count; i++) {
                        CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, CEUtils.Entropy(player).daPoints[i - 1], CEUtils.Entropy(player).daPoints[i], color * 0.6f, 12 * sc, 0);
                        sc += scj;
                    }
                }
            }

            Texture2D voidStar = EffectLoader.voidStar.Value;

            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile == null) {
                    continue;
                }

                if (p.type == voidBottleThrowType || p.type == cruiserShadowType) {
                    Color color = Color.White;
                    p.ModProjectile.PreDraw(ref color);
                }
                if (p.ModProjectile is CrossBorderPursuitProj cbp) {
                    cbp.DrawEye();
                }
                else if (p.type == voidWraithType) {
                    if (p.ModProjectile is VoidWraith vw) {
                        vw.draw();
                    }
                }
                else if (p.type == abyssPetType || p.type == voidPalProjType) {
                    Color color = Color.White;
                    p.ModProjectile.PreDraw(ref color);
                }
                else if (p.type == shadewindLanceThrowType) {
                    if (p.ModProjectile is ShadewindLanceThrow sp) {
                        sp.draw();
                    }
                }
                else if (p.type == voidStarType || p.type == voidStarFType) {
                    Color c = p.type == voidStarFType && p.ai[2] > 0 ? new Color(255, 100, 100) : Color.White;
                    Main.spriteBatch.Draw(voidStar, p.Center - Main.screenPosition, null, c * ((255 - p.alpha) / 255f), p.rotation, voidStar.Size() / 2, new Vector2(1.45f, 0.25f) * p.scale, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(voidStar, p.Center - Main.screenPosition, null, c * ((255 - p.alpha) / 255f), p.rotation, voidStar.Size() / 2, new Vector2(0.25f, 1.45f) * p.scale, SpriteEffects.None, 0);
                    CEUtils.DrawGlow(p.Center, Color.LightBlue * 0.8f, 1f);
                }
            }
        }

        /// <summary>
        /// 晚于全部扭曲通道的实体重绘:巡洋者二阶段本体、巡洋者幻影宠物、无星之夜剑体。
        /// 自带批次、不碰 RT,两条路径都直接调用。
        /// <para>
        /// 幻影宠物与无星之夜剑体的 <c>PreDraw</c> 本来就不画这部分,两条路径无条件代画。
        /// 巡洋者二阶段则是在 <c>CruiserHead.PreDraw</c> 里靠 <see cref="CEScreenPipeline.PixelPassActive"/>
        /// 抑制自绘、等这里代画,所以这里必须用同一个门:门开时它不自绘、由这里画,
        /// 门关时它自绘、这里就不能再画一遍,否则会重影
        /// </para>
        /// </summary>
        public static void DrawLateOverlay() {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (CEScreenPipeline.PixelPassActive) {
                foreach (NPC npc in Main.ActiveNPCs) {
                    if (npc.type == cruiserHeadType && npc.ModNPC is CruiserHead ch && ch.phase == 2) {
                        ch.candraw = true;
                        NPCLoader.PreDraw(npc, Main.spriteBatch, Main.screenPosition, Color.White);
                        ch.PreDraw(Main.spriteBatch, Main.screenPosition, Color.White);
                        NPCLoader.PostDraw(npc, Main.spriteBatch, Main.screenPosition, Color.White);
                        ch.candraw = false;
                    }
                }
            }

            foreach (Projectile proj in Main.ActiveProjectiles) {
                if (proj.ModProjectile != null && proj.ModProjectile is CruiserPhantomPet crp)
                    crp.draw();
                if (proj.type != starlessNightProjType) {
                    continue;
                }

                if (proj.ModProjectile is StarlessNightProj sl) {
                    sl.drawSword();
                }
            }

            Main.spriteBatch.End();
        }
    }
}
