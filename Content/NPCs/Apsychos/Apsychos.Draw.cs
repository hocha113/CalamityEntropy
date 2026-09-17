using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using InnoVault.Rigs2D.Runtime;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    public partial class Apsychos
    {
        /// <summary>白化着色器入口。武器(GreatSwordofEmbers / CinderConvergencer)和模组预加载都调它,不能挪走</summary>
        public static Effect WhiteTransShader() {
            return CEEffectAssets.WhiteTrans;
        }

        /// <summary>
        /// 本体 + 尾巴整副骨架集中绘制(尾尖实体的 <c>PreDraw</c> 返回 false)。
        /// 件全部满亮(原 <c>drawColor = Color.White</c>),层序:本体 → 尾尖 → 12 节尾骨(最后一节压最上),与迁移前的绘制顺序一致;
        /// 白化 shader 由这里开 Immediate 批次包住整副骨架,骨架里没有带状件,批次不会被中途重开
        /// </summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!TailRigReady) {
                return false;
            }
            float outline = Context?.Outline ?? 0f;
            float highLight = Context?.HighLight ?? 1f;
            float tailLight = Context?.TailLight ?? 0f;
            int phase = Context?.Phase ?? 1;
            SyncTailRigPhase(phase);
            if (outline > 0.01f) {
                DrawOutLine(outline, phase);
            }

            CEEffectAssets.WhiteTrans.Parameters["strength"].SetValue(highLight);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, CEEffectAssets.WhiteTrans, Main.GameViewMatrix.TransformationMatrix);
            CEEffectAssets.WhiteTrans.CurrentTechnique.Passes[0].Apply();
            Rig2DDrawContext ctx = Rig2DDrawContext.World().Flat(Color.White);
            Rig2DRenderer.Draw(spriteBatch, rig, in ctx);
            Main.spriteBatch.ExitShaderRegion();

            if (tail != null && tailLight > 0.01f) {
                float p = 110;
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Texture2D ray = CEExtraAssets.Ray;
                Color band = (phase == 1 ? new Color(255, 200, 160) : new Color(160, 160, 255)) * tailLight;
                Main.spriteBatch.Draw(ray, tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale - Main.screenPosition, null, band, Main.GlobalTimeWrappedHourly * 3, ray.Size() * 0.5f, new Vector2(1, 0.3f) * NPC.scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(ray, tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale - Main.screenPosition, null, band, Main.GlobalTimeWrappedHourly * 3, ray.Size() * 0.5f, new Vector2(0.3f, 1) * NPC.scale, SpriteEffects.None, 0);
                Color c1 = phase == 1 ? Color.OrangeRed : Color.Blue;
                CEUtils.DrawGlow(tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale, Color.White, 0.6f * NPC.scale * tailLight, setState: false);
                CEUtils.DrawGlow(tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale, c1, 1f * NPC.scale * tailLight, setState: false);
                CEUtils.DrawGlow(tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale, c1, 1.4f * NPC.scale * tailLight, setState: false);
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        /// <summary>
        /// 冲刺预告描边:白化到底的整副骨架在四个旋转偏移上加色叠画。
        /// 迁移前这里固定用一阶段贴图,现在跟着当前阶段的可见件走(二阶段描边换成二阶段贴图,观感差异极小)
        /// </summary>
        private void DrawOutLine(float alpha, int phase) {
            if (CEEffectAssets.WhiteTrans == null) {
                return;
            }
            Color drawColor = new Color(255, 80, 40);
            if (phase == 2) {
                drawColor = new Color(70, 70, 255);
            }
            Main.spriteBatch.End();
            CEEffectAssets.WhiteTrans.Parameters["strength"].SetValue(1);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, CEEffectAssets.WhiteTrans, Main.GameViewMatrix.TransformationMatrix);
            CEEffectAssets.WhiteTrans.CurrentTechnique.Passes[0].Apply();
            for (int ir = 0; ir < 4; ir++) {
                float r = ir * MathHelper.PiOver2 + Main.GlobalTimeWrappedHourly * 10;
                Vector2 ofs = r.ToRotationVector2() * 8 * NPC.scale;
                //整副骨架平移 ofs:把视口偏移反向挪同样的量
                Rig2DDrawContext ctx = Rig2DDrawContext.World().Flat(drawColor, alpha);
                ctx.ViewOffset = Main.screenPosition - ofs;
                Rig2DRenderer.Draw(Main.spriteBatch, rig, in ctx);
            }
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
