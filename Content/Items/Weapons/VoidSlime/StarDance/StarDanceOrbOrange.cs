namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarDance
{
    /// <summary>橙球:黄球方向左右偏10度,匀速直线</summary>
    public class StarDanceOrbOrange : StarDanceOrb
    {
        #region 调参旋钮
        //飞行速度(像素/更新)与寿命(更新次数),全程约飞出1050像素
        public const float Speed = 15f;
        public const int LifeUpdates = 70;
        #endregion

        public static readonly Color BodyColor = new(246, 136, 38);
        public static readonly Color RimColor = new(255, 206, 146);

        protected override Color Body => BodyColor;
        protected override Color Rim => RimColor;
        protected override int Lifetime => LifeUpdates;

        protected override void Move() {
            if (Dying)
                Projectile.velocity *= DyingDrag;
        }
    }
}
