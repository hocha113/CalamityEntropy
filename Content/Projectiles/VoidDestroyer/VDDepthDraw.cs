using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 纵深绘制助手:按 Z 把一张贴图画成「远处的暗影」或「镜头前的剪影」。
    /// 远端(Z &gt; 0)经 VDDepthFog 着色器(噪声热闪 + 模糊 + 去饱和 + 雾色,AlphaBlend 预乘);
    /// 近端(Z &lt; 近景门槛)经 VDHologram 的加法剪影;平面附近直接原样画。
    /// 传入的 pos 已是投影后的屏幕坐标、scale 已乘 VDDepth.Scale;这里只管颜色与着色器。着色器缺失时退回平色雾化,功能不丢
    /// </summary>
    public static class VDDepthDraw
    {
        private static Effect FogShader => CEEffectAssets.VDDepthFog?.Value;

        /// <summary>按 Z 画一张贴图;alpha 为调用方已含的透明度(不含深度衰减,深度衰减在这里乘)</summary>
        public static void Draw(Texture2D tex, Vector2 pos, Rectangle? frame, Color color, float alpha, float rotation, Vector2 origin, float scale, SpriteEffects fx, float z) {
            float depthAlpha = alpha * VDDepth.Alpha(z);
            if (depthAlpha <= 0.004f) {
                return;
            }
            if (z <= VDDirector.DepthNearLayerZ) {
                //近景剪影:全息扫描线,颜色偏白让它读成「逆光的巨物」而不是一块紫
                VDHologramDraw.Draw(tex, pos, frame, Color.Lerp(VDVfx.VoidPurple, Color.White, 0.35f), depthAlpha, rotation, origin, scale, fx);
                return;
            }
            float fog = VDDepth.FogAmount(z);
            if (fog <= 0.02f) {
                Main.spriteBatch.Draw(tex, pos, frame, color * depthAlpha, rotation, origin, scale, fx, 0f);
                return;
            }
            Effect shader = FogShader;
            if (shader == null) {
                Main.spriteBatch.Draw(tex, pos, frame, VDDepth.Fog(color, z) * depthAlpha, rotation, origin, scale, fx, 0f);
                return;
            }
            Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
            Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
            Main.instance.GraphicsDevice.Textures[1] = noise;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            ApplyFog(shader, tex, fog);
            Main.spriteBatch.Draw(tex, pos, frame, color * depthAlpha, rotation, origin, scale, fx, 0f);
            Main.spriteBatch.ExitShaderRegion();
        }

        /// <summary>喂雾化着色器参数并 Apply(供需要在一个批次里连画多张的调用方使用,调用方自己 EnterShaderRegion)</summary>
        public static void ApplyFog(Effect shader, Texture2D tex, float fog) {
            //模糊半径按贴图像素折成 UV:远处 1.5px 的糊,足以抹掉像素边又不失形
            Vector2 blur = new Vector2(VDDirector.DepthFogBlur / tex.Width, VDDirector.DepthFogBlur / tex.Height) * fog;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uOpacity"]?.SetValue(1f);
            shader.Parameters["uFog"]?.SetValue(fog);
            shader.Parameters["uFogColor"]?.SetValue(VDVfx.FarFog.ToVector3());
            shader.Parameters["uDesat"]?.SetValue(fog * 0.6f);
            shader.Parameters["uBlur"]?.SetValue(blur);
            shader.Parameters["uShimmer"]?.SetValue(VDDirector.DepthFogShimmer);
            shader.CurrentTechnique.Passes[0].Apply();
        }

        /// <summary>进入雾化批次(连画多张时用),之后每张调 <see cref="ApplyFog"/> 再 Draw;结束调 <see cref="EndFog"/></summary>
        public static bool BeginFog() {
            Effect shader = FogShader;
            if (shader == null) {
                return false;
            }
            Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
            Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
            Main.instance.GraphicsDevice.Textures[1] = noise;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            return true;
        }

        public static void EndFog() {
            Main.spriteBatch.ExitShaderRegion();
        }

        public static Effect Fog => FogShader;
    }
}
