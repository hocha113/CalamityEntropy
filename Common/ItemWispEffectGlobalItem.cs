using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class ItemWispEffectGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public bool shouldApply() {
            return false;
        }
        public override bool PreDrawInInventory(Item item, SpriteBatch sb, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!shouldApply()) {
                return true;
            }
            checkItemColor(item);
            Effect shader = CEEffectAssets.Wisp;
            shader.Parameters["minColor"].SetValue(new Color(40, 6, 100).ToVector4());
            shader.Parameters["maxColor"].SetValue(new Color(255, 220, 255).ToVector4());
            shader.Parameters["min"].SetValue(item.Entropy().wispColor[0]);
            shader.Parameters["max"].SetValue(item.Entropy().wispColor[1]);
            shader.CurrentTechnique.Passes["EnchantedPass"].Apply();

            sb.End();
            sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.UIScaleMatrix);
            return true;
        }

        public override void PostDrawInInventory(Item item, SpriteBatch sb, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!shouldApply()) {
                return;
            }
            sb.End();
            sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
        }

        public override bool PreDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
            if (!shouldApply()) {
                return true;
            }
            checkItemColor(item);
            Effect shader = CEEffectAssets.Wisp;
            shader.Parameters["minColor"].SetValue(new Color(40, 6, 100).ToVector4());
            shader.Parameters["maxColor"].SetValue(new Color(255, 220, 255).ToVector4());
            shader.Parameters["min"].SetValue(item.Entropy().wispColor[0]);
            shader.Parameters["max"].SetValue(item.Entropy().wispColor[1]);
            shader.CurrentTechnique.Passes["EnchantedPass"].Apply();

            sb.End();
            sb.Begin(0, Main.spriteBatch.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], Main.spriteBatch.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }
        public static void checkItemColor(Item item) {
            if (item.Entropy().wispColor == null) {
                float min = 3;
                float max = 0;
                Texture2D tex = TextureAssets.Item[item.type].Value;
                if (tex.Width <= 1 || tex.Height <= 1) {
                    item.Entropy().wispColor = new float[2];
                    item.Entropy().wispColor[0] = 0;
                    item.Entropy().wispColor[1] = 3;
                    return;
                }
                Color[] colors = new Color[tex.Width * tex.Height];
                tex.GetData(colors);
                foreach (Color cl in colors) {
                    if (cl.A == 0) {
                        continue;
                    }
                    float m = (float)(cl.R + cl.G + cl.B) / (255f);
                    if (m < min) min = m;
                    if (m > max) max = m;
                }
                item.Entropy().wispColor = new float[2];
                item.Entropy().wispColor[0] = min;
                item.Entropy().wispColor[1] = max;
            }
        }

        public override void PostDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI) {
            if (!shouldApply()) {
                return;
            }
            sb.End();
            sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
