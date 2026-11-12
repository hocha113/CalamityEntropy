using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.DimNova
{
    /// <summary>
    /// 黯淡新星的圆形爆炸,只由主人端生成;开头两帧结算伤害,之后只播动画
    /// ai[0]=判定半径(像素) ai[1]=种类(0 手雷大爆炸、1 凝胶团小爆炸) localAI[0]=已存活帧数
    /// </summary>
    public class DimNovaBlast : ModProjectile
    {
        public const int Big = 0;
        public const int Small = 1;

        //伤害只在开头几帧结算(帧)
        private const int DamageFrames = 2;
        //动画时长(帧)
        private const int Lifetime = 20;
        //Glow2 原图 256 像素,缩放 1 时柔光团的可见半径约 44 像素
        private const float GlowRadiusAtOne = 44f;
        //HollowCircleSoftEdge 原图 2048 像素,圆环外缘半径 1024 像素
        private const float RingRadiusAtOne = 1024f;
        //实体光团: 相对判定半径的大小、起始不透明度;从这个大小的多少倍长起、长多少
        private const float PuffSize = 1.1f;
        private const float PuffOpacity = 0.7f;
        private const float PuffGrowStart = 0.8f;
        private const float PuffGrowRange = 0.4f;
        //冲击环从 0.3 倍判定半径扩到 1.15 倍,起始不透明度
        private const float RingStart = 0.3f;
        private const float RingEnd = 1.15f;
        private const float RingOpacity = 0.8f;
        //加色爆闪: 冰蓝大光晕与白热核心相对判定半径的大小、存活帧数
        private const float FlashSize = 1.6f;
        private const float CoreSize = 0.7f;
        private const int FlashLife = 10;
        //火花(GlowSpark 原图 2048 像素): 大小爆炸各自的数量、速度区间(像素/帧)、大小、存活帧数
        private const int SparkCountBig = 10;
        private const int SparkCountSmall = 5;
        private const float SparkSpeedMin = 5f;
        private const float SparkSpeedMax = 11f;
        private const float SparkScale = 0.04f;
        private const int SparkLife = 16;
        //四溅的冰屑: 大小爆炸各自的数量、最大速度(像素/帧)、透明度、大小
        private const int DustCountBig = 16;
        private const int DustCountSmall = 6;
        private const float DustSpeed = 7f;
        private const int DustAlpha = 100;
        private const float DustScale = 1.4f;
        //大爆炸屏幕震动强度与最远距离(像素)
        private const float BigShake = 4f;
        private const float ShakeRange = 1600f;
        //小爆炸的光照是大爆炸的几成
        private const float SmallLight = 0.5f;
        //爆炸声用虚空史莱姆超新星同款,压低音量、调高音高,小爆炸更高更轻;同时至多 3 个
        private const float BigPitch = 1.25f;
        private const float BigVolume = 0.55f;
        private const float SmallPitch = 1.6f;
        private const float SmallVolume = 0.3f;
        private const int SoundInstances = 3;

        private static readonly Color IceWhite = new(223, 248, 255);
        private static readonly Color IceBlue = new(110, 170, 255);
        private static readonly Color DeepBlue = new(50, 67, 217);
        private static readonly Color HotWhite = new(255, 250, 235);
        private static readonly Vector2 SparkSquash = new(0.5f, 1.4f);
        private static readonly Vector3 BlastLight = new(0.5f, 0.8f, 1.2f);

        private float Radius => Projectile.ai[0];

        private bool IsBig => (int)Projectile.ai[1] == Big;

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() {
            if (Projectile.localAI[0] > DamageFrames)
                return false;
            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => EGlobalProjectile.CircleIntersectsRectangle(Projectile.Center, Radius, targetHitbox);

        public override bool CanHitPvp(Player target) => false;

        public override bool? CanCutTiles() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                int size = (int)(Radius * 2f);
                Projectile.Resize(size, size);
                if (!Main.dedServ)
                    Burst();
            }
            Projectile.localAI[0]++;
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, BlastLight * (Projectile.timeLeft / (float)Lifetime * (IsBig ? 1f : SmallLight)));
        }

        private void Burst() {
            bool big = IsBig;
            Vector2 center = Projectile.Center;
            CEUtils.PlaySound("explosionbig", big ? BigPitch : SmallPitch, center, SoundInstances, big ? BigVolume : SmallVolume);
            if (big)
                CEUtils.SetShake(center, BigShake, ShakeRange);
            PRTLoader.NewParticle<PRT_ShineParticle>(center, Vector2.Zero, IceBlue, Radius * FlashSize / GlowRadiusAtOne).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FlashLife);
            PRTLoader.NewParticle<PRT_ShineParticle>(center, Vector2.Zero, HotWhite, Radius * CoreSize / GlowRadiusAtOne).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FlashLife);
            int sparks = big ? SparkCountBig : SparkCountSmall;
            float start = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < sparks; i++) {
                Vector2 velocity = (start + MathHelper.TwoPi * i / sparks).ToRotationVector2() * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(center, velocity, IceWhite, SparkScale).Configure(false, SparkLife, SparkSquash, true);
            }
            int dusts = big ? DustCountBig : DustCountSmall;
            for (int i = 0; i < dusts; i++) {
                Dust dust = Dust.NewDustPerfect(center, DustID.IceTorch, Main.rand.NextVector2Circular(DustSpeed, DustSpeed), DustAlpha, default, DustScale);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            float t = Projectile.localAI[0] / Lifetime;
            float easeOut = 1f - (1f - t) * (1f - t);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D glow = CEExtraAssets.Glow2;
            float puffFade = (1f - t) * (1f - t);
            Main.EntitySpriteDraw(glow, center, null, IceWhite * (PuffOpacity * puffFade), 0f, glow.Size() / 2f, Radius * PuffSize / GlowRadiusAtOne * (PuffGrowStart + PuffGrowRange * easeOut), SpriteEffects.None);
            Texture2D ring = CEExtraAssets.HollowCircleSoftEdge;
            float ringRadius = Radius * MathHelper.Lerp(RingStart, RingEnd, easeOut);
            Main.EntitySpriteDraw(ring, center, null, Color.Lerp(IceWhite, DeepBlue, t) * (RingOpacity * (1f - t)), 0f, ring.Size() / 2f, ringRadius / RingRadiusAtOne, SpriteEffects.None);
            return false;
        }
    }
}
