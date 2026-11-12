using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.EmbraceWhip
{
    //鞭子命中时由主人生成的纯视觉弹幕,命中钩子只在主人端跑,借弹幕同步让每个客户端都炸一次日耀
    //ai[0]=火花朝向(弧度),取挥鞭方向 localAI[0]=是否已炸过
    public class EmbraceWhipFlare : ModProjectile
    {
        //存活帧数,光照随之衰减
        private const int Life = 10;
        //火花数、单侧散角(弧度)、速度区间(像素/帧)、寿命区间(帧)
        private const int SparkCount = 7;
        private const float SparkSpread = 0.7f;
        private const float SparkSpeedMin = 5f;
        private const float SparkSpeedMax = 11f;
        private const float SparkScaleMin = 0.6f;
        private const float SparkScaleMax = 1f;
        private const int SparkLifeMin = 16;
        private const int SparkLifeMax = 22;
        //闪光:Glow2原图256像素,0.5倍约128像素
        private const float FlashScale = 0.5f;
        private const int FlashLife = 12;
        private const float RingStart = 0.1f;
        private const float RingEnd = 0.55f;
        private const int RingLife = 14;
        private const float SoundVolume = 0.5f;
        private const float SoundPitch = 0.3f;
        //一鞭可能同时命中多个敌人:音高带浮动,同时最多4个
        private const float SoundPitchVariance = 0.2f;
        private const int SoundMaxInstances = 4;

        private static readonly Color FlashColor = new(255, 200, 80);
        private static readonly Color RingColor = new(255, 160, 50);
        private static readonly Color SparkHot = new(255, 230, 140);
        private static readonly Color SparkDeep = new(255, 120, 30);
        private static readonly Vector3 FlashLight = new(1.2f, 0.8f, 0.3f);

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Life;
        }

        public override bool? CanDamage() => false;

        public override bool? CanCutTiles() => false;

        public override void AI() {
            if (Main.dedServ)
                return;
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Burst();
            }
            Lighting.AddLight(Projectile.Center, FlashLight * (Projectile.timeLeft / (float)Life));
        }

        private void Burst() {
            Vector2 center = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = SoundVolume, Pitch = SoundPitch, PitchVariance = SoundPitchVariance, MaxInstances = SoundMaxInstances }, center);
            PRTLoader.NewParticle<PRT_ShineParticle>(center, Vector2.Zero, FlashColor, FlashScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FlashLife);
            PRTLoader.NewParticle<PRT_PulseRing>(center, Vector2.Zero, RingColor, RingStart).Configure(RingEnd, RingLife);
            for (int i = 0; i < SparkCount; i++) {
                float rot = Projectile.ai[0] + Main.rand.NextFloat(-SparkSpread, SparkSpread);
                Vector2 velocity = rot.ToRotationVector2() * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                Color color = Color.Lerp(SparkHot, SparkDeep, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_AltSpark>(center, velocity, color, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(false, Main.rand.Next(SparkLifeMin, SparkLifeMax));
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
