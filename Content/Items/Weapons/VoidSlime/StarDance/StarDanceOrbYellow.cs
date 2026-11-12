using System;
using Terraria;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarDance
{
    /// <summary>黄球:朝鼠标直线加速,每个敌人只打一次,打满三次后消失</summary>
    public class StarDanceOrbYellow : StarDanceOrb
    {
        #region 调参旋钮
        //出手速度、每次更新的加速倍率与速度上限(像素/更新),约26次更新加到上限
        public const float StartSpeed = 5f;
        public const float Accel = 1.07f;
        public const float MaxSpeed = 28f;
        //寿命(更新次数),全程约飞出1300像素
        public const int LifeUpdates = 60;
        //能命中3次(策划)
        public const int Hits = 3;
        #endregion

        public static readonly Color BodyColor = new(246, 208, 56);
        public static readonly Color RimColor = new(255, 242, 168);

        protected override Color Body => BodyColor;
        protected override Color Rim => RimColor;
        protected override int Lifetime => LifeUpdates;
        protected override int HitCount => Hits;

        protected override void Move() {
            if (Dying) {
                Projectile.velocity *= DyingDrag;
                return;
            }
            float speed = Math.Min(Projectile.velocity.Length() * Accel, MaxSpeed);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
        }
    }
}
