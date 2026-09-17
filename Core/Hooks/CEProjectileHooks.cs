using CalamityEntropy.Content.Projectiles;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Core.Hooks
{
    /// <summary>
    /// 挂在原版 <see cref="Projectile"/> 上的 On_* 钩子。
    /// 原版鞭子的控制点与手感参数没有 ModProjectile 级的覆写入口,只能从这两个方法转交给 <see cref="BaseWhip"/>。
    /// </summary>
    internal sealed class CEProjectileHooks : ICELoader
    {
        void ICELoader.LoadData() {
            CEDetourRegistry.Add(() => On_Projectile.FillWhipControlPoints += FillWhipControlPointsHook, () => On_Projectile.FillWhipControlPoints -= FillWhipControlPointsHook);
            CEDetourRegistry.Add(() => On_Projectile.GetWhipSettings += GetWhipSettingsHook, () => On_Projectile.GetWhipSettings -= GetWhipSettingsHook);
        }

        private static void GetWhipSettingsHook(On_Projectile.orig_GetWhipSettings orig, Projectile proj, out float timeToFlyOut, out int segments, out float rangeMultiplier) {
            orig(proj, out timeToFlyOut, out segments, out rangeMultiplier);
            if (proj.ModProjectile != null && proj.ModProjectile is BaseWhip bw) {
                bw.ModifyWhipSettings(ref timeToFlyOut, ref segments, ref rangeMultiplier);
            }
        }

        private static void FillWhipControlPointsHook(On_Projectile.orig_FillWhipControlPoints orig, Projectile proj, List<Vector2> controlPoints) {
            orig(proj, controlPoints);
            if (proj.ModProjectile != null && proj.ModProjectile is BaseWhip bw) {
                bw.ModifyControlPoints(controlPoints);
            }
        }
    }
}
