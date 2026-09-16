using AlchemistNPCLite.NPCs;
using AlchemistNPCLite.Utilities;
using CalamityEntropy.Common;
using CalamityEntropy.Content.ILEditing;
using System;
using System.Reflection;
using Terraria.ModLoader;

namespace CalamityEntropy
{
    internal class ANPCSupport
    {

        [JITWhenModsEnabled("AlchemistNPCLite")]
        internal static class ANPCShopAdd
        {
            public static void LoadHook() {
                var orgMethod = GetAddShopMethod();
                EModHooks.Add(orgMethod, OperatorAddShopHook);
                CalamityEntropy.Instance.Logger.Info("CalamityEntropy ANPCSupport Hook Loaded");
            }
            public static void LoadShop() {
                if (NPCShopDatabase.TryGetNPCShop(NPCShopDatabase.GetShopName(ModContent.NPCType<Operator>(), "ModBags1"), out var calBagsShop)) {
                    var shop = (NPCShop)calBagsShop;
                    shop.AddModItemToShop(CalamityEntropy.Instance, "ApsychosBag", 800000, () => EDownedBosses.downedApsychos);
                    shop.AddModItemToShop(CalamityEntropy.Instance, "LuminarisBag", 4000000, () => EDownedBosses.downedLuminaris);
                    shop.AddModItemToShop(CalamityEntropy.Instance, "ProphetBag", 6000000, () => EDownedBosses.downedProphet);
                    shop.AddModItemToShop(CalamityEntropy.Instance, "NihilityTwinBag", 22000000, () => EDownedBosses.downedNihilityTwin);
                    shop.AddModItemToShop(CalamityEntropy.Instance, "CruiserBag", 50000000, () => EDownedBosses.downedCruiser);
                }
                else {
                    CalamityEntropy.Instance.Logger.Warn("Cannot find operator's bossbag shop");
                }
                if (NPCShopDatabase.TryGetNPCShop(NPCShopDatabase.GetShopName(ModContent.NPCType<Operator>(), "ModMaterials"), out var meterialShop)) {
                    var shop = (NPCShop)meterialShop;
                    shop.AddModItemToShop(CalamityEntropy.Instance, "HellIndustrialComponents", 5000, () => EDownedBosses.downedAcropolis);
                }
                else {
                    CalamityEntropy.Instance.Logger.Warn("Cannot find operator's meterials shop");
                }

            }
            public static MethodBase GetAddShopMethod() {
                return typeof(Operator).GetMethod("AddShops",
                      System.Reflection.BindingFlags.Public |
                      System.Reflection.BindingFlags.Instance,
                      null,
            Type.EmptyTypes,
            null);
            }
            public static void OperatorAddShopHook(Action<Operator> orig, Operator mnpc) {
                orig.Invoke(mnpc);
                LoadShop();
            }
        }
    }
}
