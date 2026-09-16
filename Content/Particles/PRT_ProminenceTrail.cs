using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_ProminenceTrail : BasePRT, IPixelPassPRT
    {
        public bool Glow = true;
        public bool PixelPass { get; set; }
        //odp+像素shader状态,池化容易脏,Prominence spawn频率也不高,不开CanPool
        public List<Vector2> odp = new List<Vector2>();
        public int maxLength = 21;
        public Color color1 = new Color(151, 0, 5);
        public Color color2 = new Color(255, 231, 66);

        public override string Texture => "CalamityEntropy/Assets/Extra/SimpleNoise";

        public PRT_ProminenceTrail Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            //旧drawAll里Prominence走像素pass单独遍历,现挂IPixelPassPRT让EffectLoader回调DrawPixelPass
            PixelPass = true;   //走EffectLoader像素RT通道,常规PRT分桶PreDraw直接return
            if (Lifetime <= 0)
                Lifetime = 11;   //短寿命武器拖尾,11是旧默认
        }

        public override void AI() {
            //最后10tick每帧啃2个odp点,尾端收束,不是Kill条件
            if (Lifetime - Time < 10) {
                if (odp.Count > 0)
                    odp.RemoveAt(0);
                if (odp.Count > 0)
                    odp.RemoveAt(0);
            }
        }

        public void AddPoint(Vector2 pos) {
            //Add+RemoveAt(0)跟Insert(0)那批轨迹方向相反,旧ProminenceTrail就这么存点
            odp.Add(pos);
            if (odp.Count > maxLength)
                odp.RemoveAt(0);
        }

        public override bool PreDraw(SpriteBatch sb) {
            if (PixelPass)
                return false;   //正常路径EffectLoader回调DrawPixelPass,别在这画
            DrawPixelPass(sb);
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);   //非PixelPass兜底路径,End完还得接回PRT批次
            return false;
        }

        public void DrawPixelPass(SpriteBatch sb) {
            if (odp.Count < 3)
                return;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = Color * ((Lifetime - Time) / 12f);
            float width = 0;
            for (int i = 1; i < odp.Count; i++) {
                float c = (float)i / (float)(odp.Count - 1);
                if (c > 0.4) {
                    float x = (c - 0.4f) / 0.6f;
                    width = (float)Math.Sqrt(1 - x * x) * Scale;
                }
                else {
                    width = 1f * Scale;
                }
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 8 * width,
                      new Vector3((((float)i) / odp.Count), 1, 1),
                      b * ((odp.Count - i) / (float)odp.Count)));
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 8 * width,
                      new Vector3((((float)i) / odp.Count), 0, 1),
                      b * ((odp.Count - i) / (float)odp.Count)));
            }
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3) {
                Effect shader = PRTSharedAssets.Prominence.Value;

                //本地EnterShaderRegion等价:End+Begin(Immediate)带shader,原先是灾厄扩展
                CEParticleUtils.EnterShaderRegion(sb, BlendState.NonPremultiplied, shader);
                Main.instance.GraphicsDevice.Textures[1] = PRTExtraTextures.ColormapFire.Value;
                shader.Parameters["color2"].SetValue(color2.ToVector4());
                shader.Parameters["color1"].SetValue(color1.ToVector4());
                shader.Parameters["ofs"].SetValue(Main.GlobalTimeWrappedHourly * 3);
                shader.Parameters["alpha"].SetValue(float.Min(1, (Lifetime - Time) / 10f));
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = PRTExtraTextures.SimpleNoise.Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                //EnterShaderRegion把Sampler搞成AnisotropicClamp了,还回去得跟EffectLoader DrawPixelPassPRT开的那批对上
                //不然同桶里后面的粒子Blend/Sampler全乱
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, PRTLoader.GetBlendStateFor(PRTDrawMode), SamplerState.AnisotropicClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
    }


}
