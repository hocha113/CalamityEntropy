using CalamityEntropy.Common;
using CalamityEntropy.Utilities;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.ID;

namespace CalamityEntropy.Core.Hooks
{
    /// <summary>
    /// 光辉卡 / 神谕卡组对全局光照的增益。五个 <see cref="Lighting"/>.AddLight 重载各挂一个钩子,
    /// 把传入的光强乘上倍率。
    /// </summary>
    internal sealed class CELightingHooks : ICELoader
    {
        public static float BrillianceCardValue = 1.5f;
        public static float OracleDeckBrilValue = 2f;

        //AddLight 是每个发光物块/物品/弹幕/NPC 每帧都会打的路径,倍率不能每次调用都重算:
        //原先一次求值要走两趟 Main.LocalPlayer.Entropy(),查找失败时还会 new 一个 EModPlayer。
        //这里按帧戳缓存,一帧只算一次
        private static uint cachedFrame = uint.MaxValue;
        private static float cachedMultiplier = 1f;

        /// <summary>本地玩家是否开着光辉卡</summary>
        public static bool BrilEnable {
            get {
                return !Main.gameMenu && Main.LocalPlayer.Entropy().brillianceCard > 0;
            }
            set {
                if (Main.gameMenu) {
                    return;
                }
                Main.LocalPlayer.Entropy().brillianceCard = value ? 3 : 0;
            }
        }

        /// <summary>当前帧的全局光照倍率</summary>
        public static float BrillianceLightMultiplier {
            get {
                if (Main.gameMenu) {
                    return 1f;
                }
                uint frame = Main.GameUpdateCount;
                if (cachedFrame != frame) {
                    cachedFrame = frame;
                    cachedMultiplier = ComputeMultiplier();
                }
                return cachedMultiplier;
            }
        }

        private static float ComputeMultiplier() {
            EModPlayer entropy = Main.LocalPlayer.Entropy();
            if (entropy.oracleDeck) {
                return OracleDeckBrilValue;
            }
            if (entropy.brillianceCard > 0) {
                return BrillianceCardValue;
            }
            return 1f;
        }

        void ICELoader.LoadData() {
            //纯客户端视觉:服务端上倍率恒为 1,挂了也只是白白多一层 detour
            if (Main.dedServ) {
                return;
            }
            CEDetourRegistry.Add(() => On_Lighting.AddLight_int_int_int_float += AddLightTileTorchHook, () => On_Lighting.AddLight_int_int_int_float -= AddLightTileTorchHook);
            CEDetourRegistry.Add(() => On_Lighting.AddLight_int_int_float_float_float += AddLightTileRgbHook, () => On_Lighting.AddLight_int_int_float_float_float -= AddLightTileRgbHook);
            CEDetourRegistry.Add(() => On_Lighting.AddLight_Vector2_float_float_float += AddLightWorldRgbHook, () => On_Lighting.AddLight_Vector2_float_float_float -= AddLightWorldRgbHook);
            CEDetourRegistry.Add(() => On_Lighting.AddLight_Vector2_Vector3 += AddLightWorldVectorHook, () => On_Lighting.AddLight_Vector2_Vector3 -= AddLightWorldVectorHook);
            CEDetourRegistry.Add(() => On_Lighting.AddLight_Vector2_int += AddLightWorldTorchHook, () => On_Lighting.AddLight_Vector2_int -= AddLightWorldTorchHook);
        }

        void ICELoader.UnLoadData() {
            cachedFrame = uint.MaxValue;
            cachedMultiplier = 1f;
        }

        //火把重载没有可乘的分量,倍率生效时换成展开后的 RGB 调用
        private static void AddLightWorldTorchHook(On_Lighting.orig_AddLight_Vector2_int orig, Vector2 position, int torchID) {
            float multiplier = BrillianceLightMultiplier;
            if (multiplier > 1) {
                TorchID.TorchColor(torchID, out var R, out var G, out var B);
                Lighting.AddLight((int)position.X / 16, (int)position.Y / 16, R * multiplier, G * multiplier, B * multiplier);
            }
            else {
                orig(position, torchID);
            }
        }

        private static void AddLightWorldVectorHook(On_Lighting.orig_AddLight_Vector2_Vector3 orig, Vector2 position, Vector3 rgb) {
            orig(position, rgb * BrillianceLightMultiplier);
        }

        private static void AddLightWorldRgbHook(On_Lighting.orig_AddLight_Vector2_float_float_float orig, Vector2 position, float r, float g, float b) {
            float multiplier = BrillianceLightMultiplier;
            orig(position, r * multiplier, g * multiplier, b * multiplier);
        }

        private static void AddLightTileRgbHook(On_Lighting.orig_AddLight_int_int_float_float_float orig, int i, int j, float r, float g, float b) {
            float multiplier = BrillianceLightMultiplier;
            orig(i, j, r * multiplier, g * multiplier, b * multiplier);
        }

        private static void AddLightTileTorchHook(On_Lighting.orig_AddLight_int_int_int_float orig, int i, int j, int torchID, float lightAmount) {
            orig(i, j, torchID, lightAmount * BrillianceLightMultiplier);
        }
    }
}
