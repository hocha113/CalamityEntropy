namespace CalamityEntropy.Content.Items.Weapons.BossLoot.StarryNight
{
    /// <summary>淡橙小彗星:伤害 100%,每波 2 颗</summary>
    public sealed class StarryNightCometSmall : StarryNightComet
    {
        #region 调参旋钮
        //飞行速度(px/更新),extraUpdates 1 下约 18px/帧
        public const float FlightSpeed = 9f;
        //淡橙:光晕与光照、拖尾、火花;须写在 SmallLook 之前,静态字段按文本顺序初始化
        public static readonly Color GlowColor = new(255, 165, 95);
        public static readonly Color TrailColor = new(255, 160, 90);
        public static readonly Color SparkColor = new(255, 215, 170);
        private static readonly Look SmallLook = new() {
            Speed = FlightSpeed,
            Hitbox = 14,
            //StarryNightCometSmall.png 42x14,白色星核的中心
            Pivot = new Vector2(34f, 7f),
            Glow = GlowColor,
            GlowRadius = 22f,
            GlowIntensity = 0.45f,
            Light = 0.6f,
            //约 56x33px
            TwinkleScale = new Vector2(0.44f, 0.26f),
            TwinkleIntensity = 0.8f,
            Trail = TrailColor,
            //拖尾宽约 13px,不小于本体高度(14px)的一半;18 点约 160px
            TrailScale = 1.1f,
            TrailPoints = 18,
            SparkleInterval = 6,
            SparkleMinScale = 0.16f,
            SparkleMaxScale = 0.26f,
            //冲击环约 60px 直径
            RingScale = 0.38f,
            FlashScale = 0.7f,
            SparkCount = 4,
            SparkScale = 0.035f,
            Spark = SparkColor,
            //比大彗星清脆,音量压低(每秒最多 6 颗)
            ImpactPitchMin = 1.15f,
            ImpactPitchMax = 1.35f,
            ImpactVolume = 0.28f,
        };
        #endregion

        protected override Look Style => SmallLook;
    }
}
