using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Weapons.GrassSword;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class ItemEnchantEffectGlobalItem : GlobalItem
    {
        //附魔叠加层贴图读共享基座,着色器在加载期就位,只在物品绘制钩子(客户端)里读取
        [VaultLoaden("CalamityEntropy/Assets/Effects/Transform", AssetMode.EffectValue, "EnchantedPass")]
        internal static Effect TransformShader;

        public Color enchantColor = new Color(160, 80, 255, 255);
        public float strength = 0.6f;
        public int brbType = -1;
        public override bool InstancePerEntity => true;
        public bool shouldApply(Item item) {
            if (brbType == -1)
                brbType = ModContent.ItemType<Bramblecleave>();
            if (item.type == brbType) {
                return Main.LocalPlayer.Entropy().BrambleBarCharge >= 0.2f;
            }
            return false;
        }
        public override bool PreDrawInInventory(Item item, SpriteBatch sb, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!shouldApply(item)) {
                return true;
            }
            Asset<Texture2D> texture = CEExtraAssets.EnchantedAsset;
            Effect shader = TransformShader;
            shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.2f);
            shader.Parameters["color"].SetValue(enchantColor.ToVector4());
            shader.Parameters["strength"].SetValue(strength);

            Main.instance.GraphicsDevice.Textures[1] = texture.Value;
            sb.End();
            sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.UIScaleMatrix);
            shader.CurrentTechnique.Passes["EnchantedPass"].Apply();
            return true;
        }

        public override void PostDrawInInventory(Item item, SpriteBatch sb, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!shouldApply(item)) {
                return;
            }
            sb.End();
            sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
        }

        public override bool PreDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
            if (!shouldApply(item)) {
                return true;
            }
            Asset<Texture2D> texture = CEExtraAssets.EnchantedAsset;
            Effect shader = TransformShader;

            shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.2f);
            shader.Parameters["color"].SetValue(enchantColor.ToVector4());
            shader.Parameters["strength"].SetValue(strength);

            Main.instance.GraphicsDevice.Textures[1] = texture.Value;
            sb.End();
            sb.Begin(0, Main.spriteBatch.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], Main.spriteBatch.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes["EnchantedPass"].Apply();
            return true;
        }

        public override void PostDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI) {
            if (!shouldApply(item)) {
                return;
            }
            sb.End();
            sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
