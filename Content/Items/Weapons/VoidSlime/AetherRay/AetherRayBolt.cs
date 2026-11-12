using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.AetherRay
{
    /// <summary>
    /// 浮游炮的蓝色激光弹,直线、不穿透;伤害与暴击取开火那一刻的武器面板
    /// 出膛声、枪口星芒与浮游炮镜头闪光在各端首次更新时放
    /// </summary>
    public class AetherRayBolt : ModProjectile
    {
        #region 调参旋钮
        public const int ExtraUpdates = 2;              //每帧 3 次更新
        public const int UpdatesPerFrame = ExtraUpdates + 1;
        public const float Speed = 14f;                 //像素每更新,每帧 42
        public const int Life = 150;                    //更新,50 帧约 2100 像素,够到索敌边缘
        public const int HitboxSize = 10;               //像素
        public const float PivotX = 22f;                //贴图亮头,旋转原点,像素
        public const float PivotY = 5f;
        public const float GlowRadius = 16f;            //像素
        public const float GlowIntensity = 0.7f;
        public const float LightStrength = 0.45f;
        public const float TrailScale = 0.32f;          //PRT_TrailParticle 外带半宽 12 像素乘它,不窄于本体一半
        public const int TrailPoints = 12;              //每更新一点
        public const int TrailFadeFrames = 12;          //同暗夜繁星彗星
        public const float SparkleScale = 0.35f;
        public const int SparkleLife = 10;              //帧
        public const int SparkCount = 6;
        public const float SparkSpread = 0.6f;          //弧度,顺飞行方向
        public const float TileSparkSpread = 1.2f;      //弧度,撞墙往回溅
        public const float SparkMinSpeed = 2f;          //像素每帧
        public const float SparkMaxSpeed = 7f;
        public const float SparkScale = 0.45f;
        public const int SparkLife = 12;                //帧
        public const float FirePitchMin = 1.15f;
        public const float FirePitchMax = 1.35f;
        public const float FireVolume = 0.32f;
        public const float HitPitch = 1.2f;
        public const float HitVolume = 0.25f;
        public const int SoundInstances = 3;
        public static readonly Vector2 SparkSquash = new(0.3f, 1f);
        public static readonly Color BoltColor = new(85, 138, 255);
        public static readonly Color BoltCore = new(223, 248, 255);
        #endregion

        private int age;
        private bool hitTile;
        private Vector2 lastDir = Vector2.UnitX;
        private PRT_TrailParticle trail;

        public override void SetDefaults() {
            Projectile.width = Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = Life;
        }

        public override void AI() {
            age++;
            lastDir = Projectile.velocity.SafeNormalize(lastDir);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, BoltColor.ToVector3() * LightStrength);
            if (!Main.dedServ) {
                Visuals();
            }
        }

        private void Visuals() {
            if (age == 1) {
                CEUtils.PlaySound("lasershoot", Main.rand.NextFloat(FirePitchMin, FirePitchMax), Projectile.Center, SoundInstances, FireVolume);
                PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, BoltCore, SparkleScale).Configure(BoltColor, SparkleLife);
                AetherRayDrone.OnBoltFired(Projectile.owner);
            }
            if (trail == null || !trail.active) {
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, BoltColor, TrailScale);
                trail.maxLength = TrailPoints;
                trail.ShouldKillWhenOffScreen = false;
                trail.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend);
            }
            trail.Lifetime = trail.Time + TrailFadeFrames;
            trail.Position = Projectile.Center;
            trail.AddPoint(Projectile.Center + Projectile.velocity);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            hitTile = true;
            return true;
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ || timeLeft <= 0) {
                return;
            }
            CEUtils.PlaySound("energyImpact", HitPitch, Projectile.Center, SoundInstances, HitVolume);
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, BoltCore, SparkleScale).Configure(BoltColor, SparkleLife);
            Vector2 spray = hitTile ? -lastDir : lastDir;
            float spread = hitTile ? TileSparkSpread : SparkSpread;
            for (int i = 0; i < SparkCount; i++) {
                Vector2 vel = spray.RotatedByRandom(spread) * Main.rand.NextFloat(SparkMinSpeed, SparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, BoltColor, SparkScale).Configure(false, SparkLife, SparkSquash, true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            //光晕在下,像素本体压在上面保持清晰
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(BoltColor, GlowIntensity), 0f, GlowRadius * 2f / glow.Width);
            GodDraw.End();
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(PivotX, PivotY), Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
