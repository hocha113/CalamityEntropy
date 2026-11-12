namespace CalamityEntropy.Content.Items.Weapons.BossLoot.StarryNight
{
    /// <summary>淡蓝大彗星:伤害 200%,比小彗星快 33%,星核更大更亮、拖尾更长;每波 2 颗</summary>
    public sealed class StarryNightCometLarge : StarryNightComet
    {
        #region 调参旋钮
        //飞行速度(px/更新):策划要比小彗星快 33%
        public const float FlightSpeed = StarryNightCometSmall.FlightSpeed * 4f / 3f;
        //伤害倍率(策划:200%),法书生成时乘上
        public const float DamageMult = 2f;
        //淡蓝:光晕与光照、拖尾、火花;须写在 LargeLook 之前,静态字段按文本顺序初始化
        public static readonly Color GlowColor = new(130, 175, 255);
        public static readonly Color TrailColor = new(120, 170, 255);
        public static readonly Color SparkColor = new(200, 225, 255);
        private static readonly Look LargeLook = new() {
            Speed = FlightSpeed,
            Hitbox = 20,
            //StarryNightCometLarge.png 60x18,白色星核的中心
            Pivot = new Vector2(50f, 9f),
            Glow = GlowColor,
            GlowRadius = 30f,
            GlowIntensity = 0.5f,
            Light = 0.9f,
            //约 77x46px
            TwinkleScale = new Vector2(0.6f, 0.36f),
            TwinkleIntensity = 0.9f,
            Trail = TrailColor,
            //拖尾宽约 20px;26 点约 310px
            TrailScale = 1.6f,
            TrailPoints = 26,
            SparkleInterval = 4,
            SparkleMinScale = 0.22f,
            SparkleMaxScale = 0.34f,
            //冲击环约 86px 直径
            RingScale = 0.55f,
            FlashScale = 1f,
            SparkCount = 7,
            SparkScale = 0.045f,
            Spark = SparkColor,
            //比小彗星低沉、响一些
            ImpactPitchMin = 0.8f,
            ImpactPitchMax = 0.95f,
            ImpactVolume = 0.4f,
        };
        #endregion

        protected override Look Style => LargeLook;
    }
}
