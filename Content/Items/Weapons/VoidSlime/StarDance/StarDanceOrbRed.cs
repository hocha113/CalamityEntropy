using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarDance
{
    /// <summary>
    /// 红球:从玩家处转着飞出,并入以玩家为中心的圆轨顺时针转,ai[0] 为发射时的瞄准角;
    /// 同轨的球共用一个时钟,各自只记相位,每帧朝前后邻居的中点挪一点,久了就均匀排开
    /// </summary>
    public class StarDanceOrbRed : StarDanceOrb
    {
        #region 调参旋钮
        //存在5秒、轨道半径12格、同轨最多8颗(策划)
        public const int LifeFrames = 300;
        public const float OrbitRadius = 192f;
        public const int MaxOnOrbit = 8;
        //飞入轨道的帧数,以及这段时间里落后于目标位置的角度(弧度),看起来是转着飞出去
        public const int SpinInFrames = 24;
        public const float SpinInSweep = MathHelper.Pi;
        //绕一圈的帧数
        public const int OrbitPeriod = 180;
        //每帧把前后间距差的这一比例拿来调相位,越大排匀越快
        public const float SpacingRate = 0.1f;
        #endregion

        public static readonly Color BodyColor = new(232, 58, 66);
        public static readonly Color RimColor = new(255, 168, 160);

        protected override Color Body => BodyColor;
        protected override Color Rim => RimColor;
        protected override int Lifetime => LifeFrames;
        protected override bool RealFrameLife => true;

        //相对共用时钟的相位(弧度),各端自己推算
        public float Phase;
        private bool placed;

        //角度增大在屏幕上是顺时针
        private static float OrbitAngle() => Main.GameUpdateCount % OrbitPeriod / (float)OrbitPeriod * MathHelper.TwoPi;

        protected override void Move() {
            Player owner = Owner;
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            if (!placed) {
                placed = true;
                TrimOrbit();
                Phase = PickPhase();
            }
            if (Projectile.FinalExtraUpdate() && !Dying)
                Spread();
            float t = Utils.GetLerpValue(0f, SpinInFrames, age, true);
            float ease = 1f - MathF.Pow(1f - t, 3f);
            float angle = OrbitAngle() + Phase - SpinInSweep * (1f - ease);
            Vector2 goal = owner.Center + angle.ToRotationVector2() * (OrbitRadius * ease);
            Projectile.velocity = goal - Projectile.Center;
        }

        private bool SameOrbit(Projectile p) {
            return p.active && p.whoAmI != Projectile.whoAmI && p.type == Type && p.owner == Projectile.owner
                && p.ModProjectile is StarDanceOrbRed red && red.placed && !red.Dying
                && p.Entropy().IndexOfTwistedTwinShootedThisProj == Projectile.Entropy().IndexOfTwistedTwinShootedThisProj;
        }

        //轨道满了就让最早的消失;各端算出来的都是同一颗
        private void TrimOrbit() {
            while (true) {
                StarDanceOrbRed oldest = null;
                int count = 1;
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (!SameOrbit(p))
                        continue;
                    count++;
                    if (oldest == null || p.timeLeft < oldest.Projectile.timeLeft)
                        oldest = (StarDanceOrbRed)p.ModProjectile;
                }
                if (count <= MaxOnOrbit || oldest == null)
                    return;
                oldest.StartDying(false);
            }
        }

        //入轨时取最大空档的中点;轨道上没有别的球就朝瞄准方向飞出
        private float PickPhase() {
            List<float> phases = new();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (SameOrbit(p))
                    phases.Add(Wrap(((StarDanceOrbRed)p.ModProjectile).Phase));
            }
            if (phases.Count == 0)
                return Projectile.ai[0] - OrbitAngle();
            phases.Sort();
            float best = phases[^1] + phases[0] + MathHelper.TwoPi;
            float bestGap = phases[0] + MathHelper.TwoPi - phases[^1];
            for (int i = 1; i < phases.Count; i++) {
                float gap = phases[i] - phases[i - 1];
                if (gap > bestGap) {
                    bestGap = gap;
                    best = phases[i] + phases[i - 1];
                }
            }
            return best / 2f;
        }

        //朝前后邻居的中点挪;相位完全重合时按序号分先后
        private void Spread() {
            float ahead = float.MaxValue;
            float behind = float.MaxValue;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (!SameOrbit(p))
                    continue;
                float d = Wrap(((StarDanceOrbRed)p.ModProjectile).Phase - Phase);
                if (d < 1e-4f || MathHelper.TwoPi - d < 1e-4f)
                    d = p.whoAmI > Projectile.whoAmI ? 1e-4f : MathHelper.TwoPi - 1e-4f;
                ahead = Math.Min(ahead, d);
                behind = Math.Min(behind, MathHelper.TwoPi - d);
            }
            if (ahead == float.MaxValue)
                return;
            Phase = Wrap(Phase + SpacingRate * (ahead - behind) / 2f);
        }

        private static float Wrap(float a) {
            a %= MathHelper.TwoPi;
            return a < 0f ? a + MathHelper.TwoPi : a;
        }
    }
}
