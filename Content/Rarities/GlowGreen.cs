namespace CalamityEntropy.Content.Rarities
{
    /// <summary>辉绿。主色同旧提示框字色 (80,255,80),拾取飘字与之对齐</summary>
    public sealed class GlowGreen : CEGlowRarity
    {
        public static readonly Color Green = new(80, 255, 80);

        protected override Color Glow => Green;
    }
}
