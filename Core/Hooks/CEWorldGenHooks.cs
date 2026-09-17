using CalamityEntropy.Content.Items.Donator.NurglePot;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Core.Hooks
{
    /// <summary>
    /// 挂在原版 <see cref="WorldGen"/> 上的 On_* 钩子:祭坛敲碎事件转发给依赖它的掉落。
    /// </summary>
    internal sealed class CEWorldGenHooks : ICELoader
    {
        void ICELoader.LoadData() {
            CEDetourRegistry.Add(() => On_WorldGen.SmashAltar += SmashAltarHook, () => On_WorldGen.SmashAltar -= SmashAltarHook);
        }

        private static void SmashAltarHook(On_WorldGen.orig_SmashAltar orig, int i, int j) {
            //与原版 SmashAltar 的早退条件一致:只有服务端/单人端、困难模式、非世界生成期的敲击才算一次真正的敲碎
            bool realSmash = Main.netMode != NetmodeID.MultiplayerClient && Main.hardMode && !WorldGen.noTileActions && !WorldGen.gen;
            orig(i, j);
            if (realSmash) {
                KevinsNurglePot.OnAltarSmashed(i, j);
            }
        }
    }
}
