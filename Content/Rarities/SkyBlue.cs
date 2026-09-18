namespace CalamityEntropy.Content.Rarities
{
    /// <summary>天蓝。主色同旧提示框字色 (84,84,255),拾取飘字与之对齐</summary>
    public sealed class SkyBlue : CEGlowRarity
    {
        public static readonly Color Blue = new(84, 84, 255);

        protected override Color Glow => Blue;
    }
}
