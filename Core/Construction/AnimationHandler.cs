namespace CalamityEntropy.Core.Construction
{
    public struct AnimationHandler(int slots)
    {
        /// <summary>
        /// 当前动画进度
        /// </summary>
        public int[] CurAniProgress = new int[slots];
        /// <summary>
        /// 总动画长度
        /// </summary>
        public int[] TotalAniProgress = new int[slots];
        /// <summary>
        /// 辅助
        /// </summary>
        public float[] AuxFloat = new float[slots];
        /// <summary>
        /// 标记是否完成动画
        /// </summary>
        public bool[] Finished = new bool[slots];
        /// <summary>
        /// 旋转速度增量
        /// </summary>
        public float[] RotSpeed = new float[slots];
    }
    /// <summary>
    /// 动画进程类型
    /// </summary>
    public class AnimationState
    {
        public const int Begin = 0;
        public const int Middle = 1;
        public const int End = 2;
    }
    public static class AnimationFunction
    {
        public static bool IsDone(this AnimationHandler animationHandler, int slot) => animationHandler.CurAniProgress[slot] == animationHandler.TotalAniProgress[slot];
        public static void IsDoneDirect(this ref AnimationHandler animationHandler, int slot) {
            if (animationHandler.IsDone(slot))
                animationHandler.Finished[slot] = true;
            else
                return;
        }
    }
}