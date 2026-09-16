using System;

namespace CalamityEntropy.Core.Construction
{
    /// <summary>
    /// 缓动函数工具类。
    /// t(自变量)会被Clamp在(0,1)内
    /// </summary>
    public static class EasingHandler
    {
        /// <summary>
        /// 二次缓入和缓出
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float EaseInOutQuad(float t)
            => t < 0.5f ? 2f * t * t : 1f - (-2f * t + 2f) * (-2f * t + 2f) / 2f;
        /// <summary>
        /// 指数缓出
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float EaseOutExpo(float t)
            => t == 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);
        /// <summary>
        /// 指树缓入缓出
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float EaseInOutExpo(float t)
            => t < 0.5f ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2;
        /// <summary>
        /// 加速三次方缓动函数。适用静止-加速
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float EaseInCubic(float t)
            => t * t * t;
        /// <summary>
        /// 减速三次方缓动函数，高速逐渐减速至静止动画
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float EaseOutCubic(float t)
            => (float)(1 - Math.Pow(1 - t, 3));

        /// <summary>
        /// 减速回弹缓动。用于弹性收尾，例：物体落地
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float EaseOutBack(float t) {
            if (t == 1)
                return 1;
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;

            return (float)(1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2));
        }
        /// <summary>
        /// 加速回弹缓动，用于蓄力弹出。例：抽屉拉开动画
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float EaseInBack(float t) {
            if (t == 1)
                return 1;
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;

            return c3 * t * t * t - c1 * t * t;
        }
        /// <summary>
        /// 正弦曲线淡入淡出
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float EaseInOutSin(float t) {
            float num = (float)Math.Sin(MathF.PI * t);
            return num;
        }
    }
}