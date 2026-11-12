using System;
using Terraria;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarDance
{
    /// <summary>银球:三颗一组围成一圈,绕玩家逆时针转并顺着螺线向外扩,ai[0] 为起始角</summary>
    public class StarDanceOrbSilver : StarDanceOrb
    {
        #region 调参旋钮
        //起始半径(像素)、每帧向外与沿切线的速度(像素/帧);全程约转240度
        public const float StartRadius = 24f;
        public const float RadialSpeed = 7f;
        public const float TangentSpeed = 9f;
        //扩到40格后消失(任务文件)
        public const float MaxRadius = 640f;
        #endregion

        public static readonly Color BodyColor = new(166, 174, 187);
        public static readonly Color RimColor = new(236, 242, 252);

        protected override Color Body => BodyColor;
        protected override Color Rim => RimColor;
        protected override int Lifetime => (int)((MaxRadius - StartRadius) / RadialSpeed) + ShrinkFrames;
        protected override bool RealFrameLife => true;

        protected override void Move() {
            Player owner = Owner;
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            float r = StartRadius + RadialSpeed * age;
            //切向速度恒定,转过的角度随半径按对数增长;角度减小在屏幕上是逆时针
            float angle = Projectile.ai[0] - TangentSpeed / RadialSpeed * MathF.Log(r / StartRadius);
            Vector2 goal = owner.Center + angle.ToRotationVector2() * r;
            Projectile.velocity = goal - Projectile.Center;
        }
    }
}
