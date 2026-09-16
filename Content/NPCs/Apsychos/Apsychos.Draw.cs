using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    public partial class Apsychos
    {
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/ApsychosSeg")]
        private static Asset<Texture2D> segTexAsset = null;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/ApsychosTail")]
        private static Asset<Texture2D> tailTexAsset = null;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/Apsychos2")]
        private static Asset<Texture2D> body2TexAsset = null;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/ApsychosSeg2")]
        private static Asset<Texture2D> seg2TexAsset = null;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/ApsychosTail2")]
        private static Asset<Texture2D> tail2TexAsset = null;

        /// <summary>白化着色器入口。武器(GreatSwordofEmbers / CinderConvergencer)和模组预加载都调它,不能挪走</summary>
        public static Effect WhiteTransShader() {
            return CEEffectAssets.WhiteTrans;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            float outline = Context?.Outline ?? 0f;
            float highLight = Context?.HighLight ?? 1f;
            float tailLight = Context?.TailLight ?? 0f;
            int phase = Context?.Phase ?? 1;
            if (outline > 0.01f) {
                DrawOutLine(outline, phase);
            }
            drawColor = Color.White;

            CEEffectAssets.WhiteTrans.Parameters["strength"].SetValue(highLight);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, CEEffectAssets.WhiteTrans, Main.GameViewMatrix.TransformationMatrix);
            CEEffectAssets.WhiteTrans.CurrentTechnique.Passes[0].Apply();
            Texture2D bodyTex = NPC.getTexture();
            Texture2D segTex = segTexAsset.Value;
            Texture2D tailTex = tailTexAsset.Value;
            if (phase == 2) {
                bodyTex = body2TexAsset.Value;
                segTex = seg2TexAsset.Value;
                tailTex = tail2TexAsset.Value;
            }
            Main.EntitySpriteDraw(bodyTex, NPC.Center - Main.screenPosition, null, drawColor, NPC.rotation, bodyTex.Size() * 0.5f, NPC.scale, SpriteEffects.None);

            if (tail != null) {
                Main.EntitySpriteDraw(tailTex, tail.Center - Main.screenPosition, null, drawColor, tail.rotation, new Vector2(20, tailTex.Height * 0.5f), NPC.scale, SpriteEffects.None);
                if (segs != null) {
                    for (int i = 0; i < segs.Count; i++) {
                        TailSeg seg = segs[i];
                        Main.EntitySpriteDraw(segTex, seg.Center - Main.screenPosition, null, drawColor, seg.rotation, segTex.Size() * 0.5f, NPC.scale, SpriteEffects.None);
                    }
                }
                Main.spriteBatch.ExitShaderRegion();
                if (tailLight > 0.01f) {
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
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private void DrawOutLine(float alpha, int phase) {
            if (CEEffectAssets.WhiteTrans == null) {
                return;
            }
            Color drawColor = new Color(255, 80, 40) * alpha;
            if (phase == 2) {
                drawColor = new Color(70, 70, 255) * alpha;
            }
            Texture2D bodyTex = NPC.getTexture();
            Texture2D segTex = segTexAsset.Value;
            Texture2D tailTex = tailTexAsset.Value;
            Main.spriteBatch.End();
            CEEffectAssets.WhiteTrans.Parameters["strength"].SetValue(1);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, CEEffectAssets.WhiteTrans, Main.GameViewMatrix.TransformationMatrix);
            CEEffectAssets.WhiteTrans.CurrentTechnique.Passes[0].Apply();
            for (int ir = 0; ir < 4; ir++) {
                float r = ir * MathHelper.PiOver2 + Main.GlobalTimeWrappedHourly * 10;
                Vector2 ofs = r.ToRotationVector2() * 8 * NPC.scale;
                Main.spriteBatch.Draw(bodyTex, NPC.Center + ofs - Main.screenPosition, null, drawColor, NPC.rotation, bodyTex.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0);
                if (tail != null) {
                    Main.spriteBatch.Draw(tailTex, tail.Center + ofs - Main.screenPosition, null, drawColor, tail.rotation, new Vector2(20, tailTex.Height * 0.5f), NPC.scale, SpriteEffects.None, 0);
                    if (segs != null) {
                        for (int i = 0; i < segs.Count; i++) {
                            TailSeg seg = segs[i];
                            Main.spriteBatch.Draw(segTex, seg.Center + ofs - Main.screenPosition, null, drawColor, seg.rotation, segTex.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0);
                        }
                    }
                }
            }
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
