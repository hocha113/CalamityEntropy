using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Particles
{
    /// <summary>
    /// 路径格式 CalamityEntropy/Assets/Particles/xxx 或 ModName/AssetPath；Get 首次 Request 后字典缓存,PreDraw 里只调 Get
    /// </summary>
    internal static class PRTPathTextures
    {
        private static readonly Dictionary<string, Asset<Texture2D>> Cache = new();

        internal static Texture2D Get(string path) {
            //路径进字典就不再Request,PreDraw里直接Get,每帧Request能把帧率吃出坑
            if (!Cache.TryGetValue(path, out Asset<Texture2D> asset)) {
                asset = RequestTexture(path);
                Cache[path] = asset;
            }

            return asset.Value;
        }

        private static Asset<Texture2D> RequestTexture(string path) {
            int slash = path.IndexOf('/');
            if (slash > 0) {
                string modName = path[..slash];
                string assetPath = path[(slash + 1)..];
                Mod mod = ModLoader.GetMod(modName);
                if (mod != null)
                    return mod.Assets.Request<Texture2D>(assetPath);
            }

            return ModContent.Request<Texture2D>(path);
        }
    }
}
