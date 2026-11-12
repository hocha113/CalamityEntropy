using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    /// <summary>充能弹命中后的弹片爆炸,纯判定不贴图,ai[0] 为充能弹飞行方向</summary>
    public class RequiemShrapnel : ModProjectile
    {
        public const float DamageMult = 1.25f;      //相对充能弹伤害
        public const int Size = 160;                //像素,判定边长
        public const int Lifetime = 6;              //帧
        public const int DamageFrames = 2;          //帧,生成后可造成伤害的时长

        public const float FlashScale = 0.5f;       //Glow2 缩放
        public const int FlashLife = 7;             //帧
        public const float RingStart = 0.1f;        //HollowCircleHardEdge 156 像素的缩放,终值对齐判定边长
        public const float RingEnd = 1f;
        public const int RingLife = 12;             //帧
        public const float RingOpacity = 0.6f;
        public const int ConeSparks = 14;
        public const float ConeSpread = 1.1f;       //弧度,龙息扇面总张角
        public const float ConeSpeedMin = 9f;       //像素/帧
        public const float ConeSpeedMax = 18f;
        public const float ConeScaleMin = 0.035f;   //GlowSpark 缩放
        public const float ConeScaleMax = 0.055f;
        public const int ConeLife = 16;             //帧
        public const int FirePuffs = 6;
        public const float FireSpread = 0.9f;       //弧度
        public const float FireSpeedMin = 2f;       //像素/帧
        public const float FireSpeedMax = 6f;
        public const float FireScaleMin = 0.5f;     //HeavySmoke 单帧 80 像素的缩放
        public const float FireScaleMax = 0.8f;
        public const int FireLife = 22;             //帧
        public const float FireSpin = 0.04f;        //弧度/帧
        public const int SootPuffs = 5;
        public const float SootScaleMin = 0.7f;
        public const float SootScaleMax = 1.1f;
        public const int SootLife = 40;             //帧
        public const float SootOpacity = 0.55f;
        public const int Bits = 5;
        public const float BitSpeedMin = 5f;        //像素/帧
        public const float BitSpeedMax = 10f;
        public const float BitScale = 0.5f;         //StarProj 缩放
        public const int BitLife = 26;              //帧
        public const float SoundPitchMin = 1.55f;   //CEUtils.PlaySound 音高
        public const float SoundPitchMax = 1.75f;
        public const float SoundVolume = 0.32f;
        public const int SoundMaxInstances = 4;
        public const float LightStrength = 1.2f;

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => Projectile.timeLeft > Lifetime - DamageFrames ? null : false;

        public override bool CanHitPvp(Player target) => false;

        public override bool? CanCutTiles() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] != 0f)
                return;
            Projectile.localAI[0] = 1f;
            Burst();
        }

        private void Burst() {
            if (Main.dedServ)
                return;
            Vector2 c = Projectile.Center;
            Vector2 dir = Projectile.ai[0].ToRotationVector2();
            PRTLoader.NewParticle<PRT_ShineParticle>(c, Vector2.Zero, RequiemVfx.Ember, FlashScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FlashLife);
            PRTLoader.NewParticle<PRT_PulseRing>(c, Vector2.Zero, RequiemVfx.Ember * RingOpacity, RingStart).Configure(RingEnd, RingLife);
            for (int i = 0; i < ConeSparks; i++) {
                Vector2 v = dir.RotatedByRandom(ConeSpread) * Main.rand.NextFloat(ConeSpeedMin, ConeSpeedMax);
                Color col = Color.Lerp(RequiemVfx.Ember, RequiemVfx.GoldWhite, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_GlowSparkCal>(c, v, col, Main.rand.NextFloat(ConeScaleMin, ConeScaleMax)).Configure(Main.rand.NextBool(), ConeLife, RequiemVfx.SparkSquash, true);
            }
            for (int i = 0; i < FirePuffs; i++) {
                Vector2 v = dir.RotatedByRandom(FireSpread) * Main.rand.NextFloat(FireSpeedMin, FireSpeedMax);
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(c, v, RequiemVfx.Ember, Main.rand.NextFloat(FireScaleMin, FireScaleMax)).Configure(1f, FireLife, FireSpin, true);
            }
            for (int i = 0; i < SootPuffs; i++) {
                Vector2 v = dir.RotatedByRandom(FireSpread) * Main.rand.NextFloat(FireSpeedMin, FireSpeedMax);
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(c, v, RequiemVfx.Soot * SootOpacity, Main.rand.NextFloat(SootScaleMin, SootScaleMax)).Configure(1f, SootLife, -FireSpin);
            }
            for (int i = 0; i < Bits; i++) {
                Vector2 v = dir.RotatedByRandom(ConeSpread) * Main.rand.NextFloat(BitSpeedMin, BitSpeedMax);
                PRTLoader.NewParticle<PRT_AltSpark>(c, v, RequiemVfx.Silver, BitScale).Configure(true, BitLife);
            }
            CEUtils.PlaySound("cinderExplosion", Main.rand.NextFloat(SoundPitchMin, SoundPitchMax), c, SoundMaxInstances, SoundVolume);
            Lighting.AddLight(c, RequiemVfx.Ember.ToVector3() * LightStrength);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
