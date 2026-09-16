using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class OverrideModNameDisplay : ModSystem
    {
        //模组列表条目只在整包加载完成后才会绘制,此时字段已就位;专用服务器不挂钩,读不到也无妨
        [VaultLoaden("CalamityEntropy/Assets/Extra/NameMask")]
        private static Asset<Texture2D> NameMaskTex;
        [VaultLoaden("CalamityEntropy/Assets/Effects/NameEffect", AssetMode.EffectValue, "EnchantedPass")]
        private static Effect NameEffectShader;
        public override void Load() {
            if (Main.dedServ) {
                return;
            }
            Main.QueueMainThreadAction(delegate () {
                string text = base.Mod.DisplayName;
                Point size = Utils.ToPoint(FontAssets.MouseText.Value.MeasureString(text) + new Vector2(4, 4));
                _renderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, size.X, size.Y);
                Main.spriteBatch.Begin();
                Main.graphics.GraphicsDevice.SetRenderTarget(_renderTarget);
                Main.graphics.GraphicsDevice.Clear(Color.Transparent);
                for (float i = 0; i < 360; i += 60) {
                    Main.spriteBatch.DrawString(FontAssets.MouseText.Value, text, new Vector2(2, 2) + MathHelper.ToRadians(i).ToRotationVector2() * 1, new Color(0, 0, 255), 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
                }
                Main.spriteBatch.DrawString(FontAssets.MouseText.Value, text, new Vector2(2, 2), new Color(220, 220, 255), 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
                Main.spriteBatch.End();
                Main.graphics.GraphicsDevice.SetRenderTarget(null);
            });
            _uiModItemType = Enumerable.First<Type>(typeof(Main).Assembly.GetTypes(), (Type t) => t.Name == "UIModItem");
            _drawMethod = _uiModItemType.GetMethod("Draw", (BindingFlags)20);
            if (_drawMethod != null) {
                MonoModHooks.Add(_drawMethod, new Action<DrawDelegate, object, SpriteBatch>(this.DrawHook));
            }
        }
        private void DrawHook(DrawDelegate orig, object uiModItem, SpriteBatch sb) {
            orig(uiModItem, sb);
            if (_renderTarget == null || NameMaskTex == null || NameEffectShader == null) {
                return;
            }
            FieldInfo field = _uiModItemType.GetField("_modName", (BindingFlags)36);
            UIText modName = ((field != null) ? field.GetValue(uiModItem) : null) as UIText;
            if (modName == null) {
                return;
            }
            if (!modName.Text.Contains(Mod.DisplayName)) {
                return;
            }
            var texture = NameMaskTex.Value;
            Effect shader = NameEffectShader;
            shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            Vector2 position = modName.GetDimensions().Position() - new Vector2(0f, 2f) - Vector2.One * 2;
            Main.instance.GraphicsDevice.Textures[1] = texture;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.UIScaleMatrix);
            shader.CurrentTechnique.Passes["EnchantedPass"].Apply();
            sb.Draw(_renderTarget, position, Color.White);
            sb.End();
            sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
        }
        private static Type _uiModItemType;

        private static MethodInfo _drawMethod;

        private static RenderTarget2D _renderTarget;

        public delegate void DrawDelegate(object uiModItem, SpriteBatch sb);
    }
}
