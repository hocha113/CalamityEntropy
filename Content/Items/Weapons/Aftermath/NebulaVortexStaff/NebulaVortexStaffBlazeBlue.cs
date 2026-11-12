using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.NebulaVortexStaff
{
    /// <summary>
    /// 涡旋自然到期时放出的蓝色星云烈焰:AI 是原版大星云烈焰(500 像素内追踪),贴图是它的蓝色重着色。
    /// ai[2] = FinaleTag 的那一团在各端放终结爆发的声光;原版 AI 不碰 ai[2]
    /// </summary>
    public class NebulaVortexStaffBlazeBlue : NebulaVortexStaffBlazeBase
    {
        #region 调参旋钮
        public const float FinaleTag = 1f;
        //终结音效音高与音量
        public const float FinalePitch = 0.9f;
        public const float FinaleVolume = 0.8f;
        //终结冲击环最终缩放:环贴图边长 156 像素,1.6 约合 16 格直径;内环相对外环的比例
        public const float FinaleRingScale = 1.6f;
        public const float InnerRingRatio = 0.6f;
        //终结火花数与速度范围(像素/帧);中心闪光 Glow2 缩放
        public const int FinaleSparkCount = 16;
        public const float FinaleSparkMinSpeed = 6f;
        public const float FinaleSparkMaxSpeed = 14f;
        public const float FinaleFlashScale = 1f;
        //光晕半径(像素);消散音效音高偏移
        private const float Radius = 34f;
        private const float Pitch = 0.1f;
        //终结音效同时实例上限;冲击环起始缩放与内外环寿命(帧);火花缩放与寿命(帧);闪光寿命(帧)
        public const int FinaleMaxInstances = 2;
        public const float FinaleRingStart = 0.1f;
        public const int OuterRingLife = 24;
        public const int InnerRingLife = 18;
        public const float FinaleSparkScale = 0.06f;
        public const int FinaleSparkLife = 20;
        public const int FinaleFlashLife = 14;
        #endregion

        private static readonly Color Ice = new Color(120, 200, 255);
        private static readonly Color IcePale = new Color(200, 235, 255);
        private static readonly Color Pink = new Color(255, 90, 210);

        protected override int VanillaType => ProjectileID.NebulaBlaze2;
        protected override Color BloomColor => Ice;
        protected override Color SparkColor => IcePale;
        protected override float BloomRadius => Radius;
        protected override float KillPitch => Pitch;

        protected override void OnFirstUpdate() {
            base.OnFirstUpdate();
            if (Projectile.ai[2] != FinaleTag) {
                return;
            }
            CEUtils.PlaySound("energyImpact", FinalePitch, Projectile.Center, FinaleMaxInstances, FinaleVolume);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Ice, FinaleRingStart).Configure(FinaleRingScale, OuterRingLife);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Pink, FinaleRingStart).Configure(FinaleRingScale * InnerRingRatio, InnerRingLife);
            for (int i = 0; i < FinaleSparkCount; i++) {
                Vector2 vel = (MathHelper.TwoPi * i / FinaleSparkCount).ToRotationVector2() * Main.rand.NextFloat(FinaleSparkMinSpeed, FinaleSparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, IcePale, FinaleSparkScale).Configure(false, FinaleSparkLife, SparkSquash, true);
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, FinaleFlashScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FinaleFlashLife);
        }
    }
}
