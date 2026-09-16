using CalamityEntropy.Common;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace CalamityEntropy.Core
{
    internal class SwingSystem : ICELoader
    {
        internal static List<BaseSwing> Swings;
        internal static Dictionary<string, int> SwingFullNameToType;
        internal static Dictionary<int, Asset<Texture2D>> trailTextures;
        internal static Dictionary<int, Asset<Texture2D>> gradientTextures;
        internal static Dictionary<int, Asset<Texture2D>> glowTextures;
        void ICELoader.LoadData() {
            Swings = [];
            SwingFullNameToType = [];
            trailTextures = [];
            gradientTextures = [];
            glowTextures = [];
        }
        void ICELoader.SetupData() {
            Swings = VaultUtils.GetDerivedInstances<BaseSwing>();
            foreach (var swing in Swings) {
                string pathValue = swing.GetType().Name;
                int type = CalamityEntropy.Instance.Find<ModProjectile>(pathValue).Type;
                SwingFullNameToType.Add(pathValue, type);
            }
        }
        void ICELoader.LoadAsset() {
            foreach (var swing in Swings) {
                string path1 = swing.trailTexturePath;
                string path2 = swing.gradientTexturePath;
                string path3 = swing.GlowTexturePath;

                if (path1 == "") {
                    path1 = EffectLoader.AssetPath + "MotionTrail3";
                }
                if (path2 == "") {
                    path2 = EffectLoader.AssetPath + "NullEffectColorBar";
                }

                int type = SwingFullNameToType[swing.GetType().Name];

                trailTextures.TryAdd(type, CEUtils.GetT2DAsset(path1));
                gradientTextures.TryAdd(type, CEUtils.GetT2DAsset(path2));

                if (path3 != "") {
                    glowTextures.TryAdd(type, CEUtils.GetT2DAsset(path3));
                }
            }
        }
        void ICELoader.UnLoadData() {
            Swings = null;
            SwingFullNameToType = null;
            trailTextures = null;
            gradientTextures = null;
            glowTextures = null;
        }
    }
}
