using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StellarFury
{
    /// <summary>
    /// 星之怒的魔法导弹,白光包着的黑弹,直线飞行;命中敌人时由拥有者在命中点留下黑洞,撞墙、寿终只放消散特效
    /// ai[0] 黑洞伤害,物品 Shoot 按面板 80% 写入,随生成包同步
    /// </summary>
    public class StellarFuryMissile : ModProjectile
    {
        #region 调参旋钮
        public const int ExtraUpdates = 1;              //每帧 2 次更新
        public const float Speed = 9f;                  //像素每更新,每帧 18
        public const int Life = 120;                    //更新,60 帧约 1080 像素
        public const int HitboxSize = 14;               //像素
        public const float PivotX = 20f;                //贴图里弹体前段,旋转原点,像素
        public const float PivotY = 7f;
        public const int FadeInUpdates = 6;             //出膛淡入
        public const float GlowRadius = 24f;            //白光半径,像素
        public const float GlowIntensity = 0.75f;
        public const float HaloRadius = 12f;            //贴身亮晕半径,像素
        public const float HaloIntensity = 0.9f;
        public const float LightStrength = 0.5f;
        public const float TrailScale = 0.36f;          //PRT_TrailParticle 外带半宽 12 像素乘它,不窄于本体一半
        public const int TrailPoints = 18;              //每更新一点
        public const int TrailFadeFrames = 12;          //同暗夜繁星彗星
        public const int SparkleInterval = 5;           //更新
        public const float SparkleRadius = 6f;          //像素
        public const float SparkleDrag = 0.05f;         //顺飞行方向的余速比例
        public const float SparkleMinScale = 0.2f;
        public const float SparkleMaxScale = 0.32f;
        public const int SparkleLife = 16;              //帧
        public const float SparkleSpin = 0.08f;         //弧度每帧
        public const float SparkleBloom = 1.3f;
        public const float BurstScale = 0.8f;           //消散闪光缩放
        public const int BurstLife = 12;                //帧
        public const float BurstBloom = 1.6f;
        public const int SparkCount = 8;
        public const float SparkSpread = 0.8f;          //弧度,顺飞行方向
        public const float TileSparkSpread = 1.2f;      //弧度,撞墙往回溅
        public const float SparkMinSpeed = 3f;          //像素每帧
        public const float SparkMaxSpeed = 8f;
        public const float SparkScale = 0.5f;
        public const int SparkLife = 14;                //帧
        public const float ImpactPitch = 0.85f;
        public const float ImpactVolume = 0.4f;
        public const int SoundInstances = 3;
        public static readonly Vector2 SparkSquash = new(0.3f, 1f);
        public static readonly Color GlowWhite = new(223, 248, 255);
        public static readonly Color TrailColor = new(170, 204, 255);
        public static readonly Color SparkViolet = new(136, 35, 159);
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
            Projectile.Opacity = Math.Min(age / (float)FadeInUpdates, 1f);
            Lighting.AddLight(Projectile.Center, GlowWhite.ToVector3() * LightStrength * Projectile.Opacity);
            if (!Main.dedServ) {
                Visuals();
            }
        }

        private void Visuals() {
            if (trail == null || !trail.active) {
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, TrailColor, TrailScale);
                trail.maxLength = TrailPoints;
                trail.ShouldKillWhenOffScreen = false;
                trail.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend);
            }
            trail.Lifetime = trail.Time + TrailFadeFrames;
            trail.Position = Projectile.Center;
            trail.Color = TrailColor * Projectile.Opacity;
            trail.AddPoint(Projectile.Center + Projectile.velocity);
            if (age % SparkleInterval == 0) {
                PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center + CEUtils.randomPointInCircle(SparkleRadius), Projectile.velocity * SparkleDrag, Color.White,
                    Main.rand.NextFloat(SparkleMinScale, SparkleMaxScale)).Configure(SparkViolet, SparkleLife, SparkleSpin, SparkleBloom);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.owner == Main.myPlayer) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<StellarFuryHole>(), (int)Projectile.ai[0], 0f, Projectile.owner);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            hitTile = true;
            return true;
        }

        public override void OnKill(int timeLeft) {
            //寿终只剩拖尾自己淡掉;别的客户端不知道有没有命中,消散特效一律照放
            if (Main.dedServ || timeLeft <= 0) {
                return;
            }
            CEUtils.PlaySound("energyImpact", ImpactPitch, Projectile.Center, SoundInstances, ImpactVolume);
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Color.White, BurstScale).Configure(SparkViolet, BurstLife, 0f, BurstBloom);
            Vector2 spray = hitTile ? -lastDir : lastDir;
            float spread = hitTile ? TileSparkSpread : SparkSpread;
            for (int i = 0; i < SparkCount; i++) {
                Vector2 vel = spray.RotatedByRandom(spread) * Main.rand.NextFloat(SparkMinSpeed, SparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, i % 2 == 0 ? GlowWhite : SparkViolet, SparkScale).Configure(false, SparkLife, SparkSquash, true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            float o = Projectile.Opacity;
            //白光在下,黑色弹体压在上面
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(GlowWhite, GlowIntensity * o), 0f, GlowRadius * 2f / glow.Width);
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(Color.White, HaloIntensity * o), 0f, HaloRadius * 2f / glow.Width);
            GodDraw.End();
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White * o, Projectile.rotation, new Vector2(PivotX, PivotY), Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
