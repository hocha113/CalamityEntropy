using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Integrations
{
    /// <summary>
    /// 向 Wikithis 登记本模组的 wiki 地址与图标。未装 Wikithis 时静默跳过。
    /// </summary>
    internal static class CEWikithisIntegration
    {
        public static void Register() {
            //纯客户端的右键跳转功能,服务端不需要
            if (Main.dedServ || !ModLoader.TryGetMod("Wikithis", out Mod wikithis)) {
                return;
            }
            CalamityEntropy mod = CalamityEntropy.Instance;
            wikithis.Call(0, mod, "http://calentropy.miraheze.org/wiki/{}", GameCulture.CultureName.Chinese);
            wikithis.Call("AddWikiTexture", mod, ModContent.Request<Texture2D>("CalamityEntropy/Assets/UI/icon_s"));
            wikithis.Call(3, mod, ModContent.Request<Texture2D>("CalamityEntropy/Assets/UI/icon_s"));
        }
    }
}
