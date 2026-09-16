using CalamityEntropy.Core.Construction;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core
{
    public class ScreenShakeInfo(Vector2 ShakePosition, float ShakeStrength, int ShakeTime, float ShakeDirection, float ShakeAngleOffset, bool useDiatanceFade, int ShakeEffectDistance)
    {
        /// <summary>
        /// 是否使用震动衰减
        /// </summary>
        public bool UseDiatanceFade = useDiatanceFade;
        /// <summary>
        /// 震动存在时间
        /// </summary>
        public int ShakeTime = 0;
        /// <summary>
        /// 震动强度
        /// </summary>
        public int ShakeLifeTime = ShakeTime;
        /// <summary>
        /// 震动影响的距离
        /// </summary>
        public int ShakeEffectDistance = ShakeEffectDistance;
        /// 震动强度
        /// </summary>
        public float ShakeStrength = ShakeStrength;
        /// <summary>
        /// 震动方向的随机范围
        /// </summary>
        public float ShakeAngleOffset = ShakeAngleOffset;
        /// <summary>
        /// 基础的震动方向
        /// </summary>
        public float ShakeDirection = ShakeDirection;
        /// <summary>
        /// 基础的位置
        /// </summary>
        public Vector2 ShakePosition = ShakePosition;
        public void Update() {
            float Shake = MathHelper.Lerp(ShakeStrength, 0, EasingHandler.EaseOutCubic(ShakeTime / (float)ShakeLifeTime));
            if (UseDiatanceFade) {
                //计算与本地玩家的距离
                Player player = Main.LocalPlayer;

                float toPlayerLength = (ShakePosition - player.Center).Length();
                Shake *= 1 - (toPlayerLength / ShakeEffectDistance);
            }

            Main.screenPosition += Vector2.UnitX.RotatedBy(ShakeDirection).RotatedByRandom(ShakeAngleOffset) * Shake;
            ShakeTime++;
        }
    }
    public class ScreenShakeSystem : ModSystem
    {
        public static readonly List<ScreenShakeInfo> ScreenShakes = [];
        public override void ModifyScreenPosition() {
            if (ScreenShakes.Count == 0)
                return;

            foreach (ScreenShakeInfo shake in ScreenShakes) {
                shake.Update();
            }
            ScreenShakes.RemoveAll(s => s.ShakeTime >= s.ShakeLifeTime);
        }
        public static void AddScreenShakes(Vector2 shakePosition, float shakeStrength, int shakeLifeTime, float shakeDirection, float randomAngleoffset = MathHelper.TwoPi, bool useDistanceFade = true, int ShakeEffectDistance = 1000) {
            ScreenShakeInfo screenShakeInfo = new ScreenShakeInfo(shakePosition, shakeStrength, shakeLifeTime, shakeDirection, randomAngleoffset, useDistanceFade, ShakeEffectDistance);
            ScreenShakes.Add(screenShakeInfo);
        }
    }
}
