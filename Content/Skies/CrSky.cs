using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>
    /// 巡游者虚空天幕(基座重写版):跨 0 切片画三层滚动虚空纹理,盖住原版视差背景与星空日月,
    /// 视觉强度 = 存在包络 opacity(基座管理)× 演出强度 <see cref="CruiserSkyDrive.Intensity"/>。
    /// 闪电由 Update 生灭、Draw 只渲染;全屏扭曲已迁往滤镜(CrScreenShaderData),
    /// 本类不再切 RenderTarget,也不再依赖切片回调次数。
    /// </summary>
    public class CrSky : CESkyBase
    {
        //天空贴图交给 VaultLoaden 在加载期赋值(专用服务器上恒为 null,只在绘制路径读取)
        [VaultLoaden("CalamityEntropy/Assets/Extra/CrSky")]
        private static Asset<Texture2D> crSkyTex;

        //滚动速度(px/tick):按旧实现在典型地表切片数(约 13 次/帧)下的表观速度折算,
        //旧代码的 counter 在每个切片回调里自增,速度本随生物群系漂移,此处取其典型值定格
        private static readonly Vector2 BaseDrift = new(3.9f, -1.3f);
        private static readonly Vector2 AddDriftA = new(11.0f, -6.9f);
        private static readonly Vector2 AddDriftB = new(12.4f, 4.1f);
        //配色沿用旧实现
        private static readonly Color BaseColor = new(30, 20, 60);
        private static readonly Color AddColor = new(66, 60, 94);

        //闪电:同屏上限;自发生成要过强度门槛(弱档消费者如 VoidMonolith 不出闪电),爆发拍点不受限
        private const int BoltCap = 10;
        private const float BoltIntensityGate = 0.55f;

        private int counter;
        private readonly List<LightningBolt> bolts = new();

        protected override bool KeepActive() =>
            Main.LocalPlayer.Entropy().crSky > 0
            || CruiserSkyDrive.Intensity > 0.004f
            || NPC.AnyNPCs(ModContent.NPCType<CruiserHead>());

        protected override void OnReset() => bolts.Clear();

        public override Color OnTileColor(Color inColor)
            => Color.Lerp(inColor, new Color(255, 255, 255, inColor.A), opacity);

        public override float GetCloudAlpha() => (1f - opacity) * 0.97f + 0.03f;

        protected override void UpdatePayload(GameTime gameTime) {
            counter++;
            if (opacity <= 0f) {
                if (bolts.Count > 0)
                    bolts.Clear();
                return;
            }

            //拍点爆发:登场揭幕/二阶段转换时齐发
            int burst = CruiserSkyDrive.ConsumeBurst();
            if (burst > 0) {
                for (int i = 0; i < burst && bolts.Count < BoltCap; i++)
                    bolts.Add(new LightningBolt());
                PlayThunder(0.5f);
            }

            //自发闪电:强度过门槛后,频率随躁动升档(P1 稀疏,P2 密集)
            if (CruiserSkyDrive.Intensity > BoltIntensityGate && bolts.Count < BoltCap) {
                int interval = (int)MathHelper.Lerp(240f, 45f, CruiserSkyDrive.Agitation);
                if (Main.rand.NextBool(interval)) {
                    bolts.Add(new LightningBolt());
                    if (Main.rand.NextBool(6))
                        PlayThunder(Main.rand.NextFloat() * 0.4f);
                }
            }

            for (int i = bolts.Count - 1; i >= 0; i--) {
                if (--bolts[i].timeleft <= 0)
                    bolts.RemoveAt(i);
            }
        }

        private static void PlayThunder(float volume) {
            SoundStyle s = SoundID.Thunder;
            s.Volume = volume;
            s.MaxInstances = 3;
            SoundEngine.PlaySound(s);
        }

        protected override void DrawFront(SpriteBatch spriteBatch) {
            Texture2D tex = crSkyTex.Value;
            float intensity = CruiserSkyDrive.Intensity;

            //基底层:调用方空间 + Wrap 采样,任意分辨率/缩放/重力方向都恰好铺满
            CESkyDrawing.BeginCallerSpace(spriteBatch, BlendState.AlphaBlend, SamplerState.AnisotropicWrap);
            DrawScrollLayer(spriteBatch, tex, BaseDrift, 1f, BaseColor * opacity);

            //叠加层:随演出强度轻微增亮,召唤渐临时天幕逐步"活"起来
            CESkyDrawing.BeginCallerSpace(spriteBatch, BlendState.Additive, SamplerState.AnisotropicWrap);
            Color addCol = AddColor * (opacity * (0.85f + 0.3f * intensity));
            DrawScrollLayer(spriteBatch, tex, AddDriftA, 1.2f, addCol);
            DrawScrollLayer(spriteBatch, tex, AddDriftB, 1.2f, addCol);
            spriteBatch.End();

            //闪电走图元渲染,不经 SpriteBatch;画完再按调用方参数重开批次
            DrawBolts();

            CESkyDrawing.OpenCallerBatch(spriteBatch);
        }

        private void DrawScrollLayer(SpriteBatch sb, Texture2D tex, Vector2 drift, float texScale, Color color) {
            //镜头视差 0.5 + 恒定漂移;取模防大世界坐标丢浮点精度
            Vector2 scroll = CESkyDrawing.RealScreenPosition * -0.5f + drift * counter;
            scroll.X %= tex.Width;
            scroll.Y %= tex.Height;
            Rectangle dest = CESkyDrawing.CallerFullscreen;
            Rectangle src = new((int)-scroll.X, (int)-scroll.Y, (int)(dest.Width / texScale), (int)(dest.Height / texScale));
            sb.Draw(tex, dest, src, color);
        }

        private void DrawBolts() {
            if (bolts.Count == 0)
                return;
            MiscShaderData shader = GameShaders.Misc["CalamityEntropy:ArtAttack"];
            shader.SetShaderTexture(CEExtraAssets.Streak2Asset);
            foreach (LightningBolt bolt in bolts)
                bolt.Draw(shader, opacity);
        }

        /// <summary>天幕闪电:屏幕邻域随机锚点向两侧生长的折线,图元渲染。</summary>
        private class LightningBolt
        {
            private readonly List<Vector2> points = new();
            public int timeleft = 200;
            private const int MaxTime = 200;
            private float drawOpacity;

            public LightningBolt() {
                //散布范围随分辨率等比放大(旧实现固定 ±1200,高分屏会挤在中央)
                float spreadX = Math.Max(1200f, Main.screenWidth * 0.62f);
                float spreadY = Math.Max(1200f, Main.screenHeight * 1.1f);
                Vector2 center = Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f
                    + new Vector2(Main.rand.NextFloat(-spreadX, spreadX), Main.rand.NextFloat(-spreadY, spreadY));
                //两条链从锚点反向生长,拼成一条整折线(旧实现拼好后只画了一半,这里补全)
                float a1 = CEUtils.randomRot();
                float a2 = a1 + MathHelper.Pi;
                List<Vector2> half1 = new();
                List<Vector2> half2 = new();
                Vector2 p1 = center, p2 = center;
                for (int i = 0; i < 20; i++) {
                    half1.Add(p1);
                    half2.Add(p2);
                    a1 += ((float)Main.rand.NextDouble() - 0.5f) * 1f;
                    a2 += ((float)Main.rand.NextDouble() - 0.5f) * 1f;
                    p1 += a1.ToRotationVector2() * Main.rand.Next(50, 66);
                    p2 += a2.ToRotationVector2() * Main.rand.Next(50, 66);
                }
                for (int i = half1.Count - 1; i >= 0; i--)
                    points.Add(half1[i]);
                points.AddRange(half2);
            }

            public void Draw(MiscShaderData shader, float opacity) {
                drawOpacity = opacity;
                //背景窗口内渲染器用被平移过的 screenPosition 做世界→屏幕换算,这里把平移补回去
                CEPrimitiveRenderer.RenderTrail(points,
                    new CEPrimitiveSettings(Width, Colorer,
                        (_, _) => Main.BackgroundViewMatrix.Translation,
                        smoothen: true, pixelate: false, shader), 180);
            }

            private Color Colorer(float completionRatio, Vector2 vertex) {
                float wave = MathF.Sin(completionRatio * MathHelper.Pi);
                return Color.Lerp(Color.MediumPurple, Color.LightBlue, wave) * completionRatio * (drawOpacity * 1.4f);
            }

            private float Width(float completionRatio, Vector2 vertex) {
                float lifeWave = MathF.Sin(timeleft / (float)MaxTime * MathHelper.Pi);
                return 48f * lifeWave * MathF.Sin(completionRatio * MathHelper.Pi);
            }
        }
    }
}
