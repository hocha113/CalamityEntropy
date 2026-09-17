using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>
    /// 驱逐舰天幕的场景开关:本体在场或强度仍有尾巴时保活,让淡出走完。
    /// 键与滤镜 CalamityEntropy:VoidDestroyer 不同:那个滤镜由 VDScreenFx.Update 手动开关(拍点要预热),
    /// 若共键走 ManageSpecialBiomeVisuals 会互相打架
    /// </summary>
    public class VDSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(ModContent.NPCType<VoidDestroyer>()) || VDSkyDrive.Active;

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals(VDSky.Key, isActive);
        }
    }

    /// <summary>
    /// 虚空驱逐舰「轨道封锁」天幕:出场传送门打开时整片天空(含原版远景)被虚空吞没,深处悬着一颗正被侵蚀的星球,
    /// 前方一层以本体为中心亮起的六边形封锁力场随拍点呼吸。四层全在 VDSky.fxc 里程序化合成,这里只喂参数。
    /// 主载荷画在跨 0 切片(<see cref="DrawFront"/>:原版星星/日月/全部视差层/大气雾之后、世界之前),
    /// 不透明输出随可见强度淡入,原版远景随之淡出;原版关掉背景时那一帧没有跨 0 切片,退到 <see cref="DrawFar"/> 兜底。
    /// 可见强度 = 存在包络 opacity(基座)× <see cref="VDSkyDrive.Intensity"/>(宿主编排的出场/死亡/撤离斜坡)。
    /// 着色器方块走原始像素空间(无矩阵、Viewport 尺寸),UV 就是视口比例;本体 UV 用 GameViewMatrix 折算,反重力用 uFlip 翻渐变。
    /// 不切 RenderTarget,不依赖 RT 管线与 EnablePixelEffect,复古光照下照样在。状态推进只在 <see cref="UpdatePayload"/>
    /// </summary>
    public class VDSky : CESkyBase
    {
        public const string Key = "CalamityEntropy:VoidDestroyerSky";

        private int counter;

        protected override float FadeInStep => VDDirector.SkyFadeStep;

        protected override bool KeepActive() => NPC.AnyNPCs(ModContent.NPCType<VoidDestroyer>()) || VDSkyDrive.Active;

        /// <summary>可见强度:存在包络 × 驱动强度</summary>
        private float Visible => opacity * VDSkyDrive.Intensity;

        //原版日月被盖住后地表环境光跟着走:向虚空暮色拉,白天压暗、夜里略提亮
        public override Color OnTileColor(Color inColor)
            => Color.Lerp(inColor, VDVfx.SkyTileTint, VDDirector.SkyTileTintAmount * Visible);

        public override float GetCloudAlpha() => 1f - Visible;

        protected override void UpdatePayload(GameTime gameTime) {
            counter++;
        }

        protected override void DrawFar(SpriteBatch spriteBatch) {
            //原版关掉背景时不再调 DrawToDepth,DrawRemainingDepth 退化成 (MinValue, MaxValue) 只触发最远切片,主载荷在这里兜底
            if (!Main.BackgroundEnabled) {
                DrawVoid(spriteBatch);
            }
        }

        protected override void DrawFront(SpriteBatch spriteBatch) {
            DrawVoid(spriteBatch);
        }

        private void DrawVoid(SpriteBatch sb) {
            float vis = Visible;
            if (vis <= 0.004f) {
                return;
            }
            Rectangle vp = CESkyDrawing.ViewportFullscreen;
            Effect fx = CEEffectAssets.VDSky?.Value;
            if (fx == null) {
                DrawFallback(sb, vp, vis);
                return;
            }

            float phase = VDSkyDrive.Phase;
            //P2 → P3 网格换成护盾的淡紫
            Color gridColor = Color.Lerp(VDVfx.VoidPurple, VDVfx.ShieldLavender, MathHelper.Clamp(phase - 2f, 0f, 1f));
            float flip = Main.LocalPlayer.gravDir == 1f ? 0f : 1f;

            fx.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            fx.Parameters["uOpacity"]?.SetValue(vis);
            fx.Parameters["uAspect"]?.SetValue(vp.Width / (float)Math.Max(vp.Height, 1));
            fx.Parameters["uFlip"]?.SetValue(flip);
            //视差按「相机 − 开打原点」算(屏高单位),绝对世界坐标会把星球推出屏、吃掉 hash 精度
            fx.Parameters["uParallax"]?.SetValue((CESkyDrawing.RealScreenPosition - VDSkyDrive.OriginWorldPos) / Math.Max(vp.Height, 1));
            fx.Parameters["uPhase"]?.SetValue(phase);
            fx.Parameters["uErosion"]?.SetValue(VDSkyDrive.Erosion);
            fx.Parameters["uFlash"]?.SetValue(VDSkyDrive.Flash);
            fx.Parameters["uFlashRing"]?.SetValue(VDSkyDrive.FlashRing);
            fx.Parameters["uCharge"]?.SetValue(VDSkyDrive.Charge);
            fx.Parameters["uBossUv"]?.SetValue(WorldToUv(VDSkyDrive.BossWorldPos, vp));
            fx.Parameters["uBossGlow"]?.SetValue(VDSkyDrive.BossGlow);
            fx.Parameters["uGridRadius"]?.SetValue(PhaseLerp(VDDirector.SkyGridBossRadius, phase));
            fx.Parameters["uGridAlpha"]?.SetValue(PhaseLerp(VDDirector.SkyGridAlpha, phase));
            fx.Parameters["uGridCell"]?.SetValue(VDDirector.SkyGridCell);
            fx.Parameters["uPlanetCenter"]?.SetValue(VDDirector.SkyPlanetCenter);
            fx.Parameters["uPlanetRadius"]?.SetValue(VDDirector.SkyPlanetRadius);
            fx.Parameters["uPlanetParallax"]?.SetValue(VDDirector.SkyPlanetParallax);
            fx.Parameters["uStarParallax"]?.SetValue(new Vector2(VDDirector.SkyStarParallaxFar, VDDirector.SkyStarParallaxNear));
            fx.Parameters["uNebulaParallax"]?.SetValue(VDDirector.SkyNebulaParallax);
            fx.Parameters["uColorTop"]?.SetValue(VDVfx.SkyTop.ToVector3());
            fx.Parameters["uColorHorizon"]?.SetValue(VDVfx.SkyHorizon.ToVector3());
            fx.Parameters["uColorNebula"]?.SetValue(VDVfx.SkyNebula.ToVector3());
            fx.Parameters["uColorGrid"]?.SetValue(gridColor.ToVector3());
            fx.Parameters["uColorErosion"]?.SetValue(VDVfx.SkyErosion.ToVector3());
            fx.Parameters["uColorPlanetRim"]?.SetValue(VDVfx.SkyPlanetRim.ToVector3());

            //原始像素空间 + 预乘输出走 AlphaBlend:随 uOpacity 压过原版远景
            CESkyDrawing.BeginRawScreen(sb, BlendState.AlphaBlend, SamplerState.LinearClamp, SpriteSortMode.Immediate, fx);
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.Textures[1] = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            gd.Textures[2] = CEExtraAssets.Perlin ?? CEUtils.getExtraTex("Perlin");
            gd.SamplerStates[2] = SamplerState.LinearWrap;
            fx.CurrentTechnique.Passes[0].Apply();
            sb.Draw(CEUtils.pixelTex, vp, Color.White);
            CESkyDrawing.RestoreCallerBatch(sb);
        }

        /// <summary>着色器缺失时的退化:天际色纯罩 + 星空贴图暗滚动,没有星球与网格</summary>
        private void DrawFallback(SpriteBatch sb, Rectangle vp, float vis) {
            CESkyDrawing.BeginRawScreen(sb, BlendState.AlphaBlend, SamplerState.LinearClamp, SpriteSortMode.Deferred);
            sb.Draw(CEUtils.pixelTex, vp, VDVfx.SkyHorizon * vis);

            Texture2D stars = CEUtils.getExtraTex("StarrySky");
            Vector2 scroll = (CESkyDrawing.RealScreenPosition - VDSkyDrive.OriginWorldPos) * VDDirector.SkyStarParallaxFar + new Vector2(counter * 0.15f, 0f);
            Rectangle src = new Rectangle((int)scroll.X, (int)scroll.Y, vp.Width, vp.Height);
            CESkyDrawing.BeginRawScreen(sb, BlendState.Additive, SamplerState.LinearWrap, SpriteSortMode.Deferred);
            sb.Draw(stars, vp, src, Color.White * (0.35f * vis));
            CESkyDrawing.RestoreCallerBatch(sb);
        }

        /// <summary>世界坐标 → 视口 UV:背景窗口内 Main.screenPosition 被加过背景平移,用还原后的真实相机位置;GameViewMatrix 把缩放与反重力翻转一并折进</summary>
        private static Vector2 WorldToUv(Vector2 world, Rectangle vp) {
            Vector2 px = Vector2.Transform(world - CESkyDrawing.RealScreenPosition, Main.GameViewMatrix.TransformationMatrix);
            return px / new Vector2(Math.Max(vp.Width, 1), Math.Max(vp.Height, 1));
        }

        /// <summary>阶段档位在平滑阶段值之间插值(1.6 = P1 档与 P2 档按 0.6 混)</summary>
        private static float PhaseLerp(Func<int, float> byPhase, float phase) {
            int lo = (int)MathHelper.Clamp(MathF.Floor(phase), 1f, 3f);
            int hi = Math.Min(lo + 1, 3);
            return MathHelper.Lerp(byPhase(lo), byPhase(hi), MathHelper.Clamp(phase - lo, 0f, 1f));
        }
    }
}
