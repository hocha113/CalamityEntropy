using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.NebulaVortexStaff
{
    /// <summary>
    /// 星云烈焰克隆体的共用部分:沿用原版星云烈焰的 AI(追踪、动画、尘埃、出膛声),
    /// 补回原版按类型写死、克隆体拿不到的半透明白、撞墙小判定与消散表现;不带原版消散时的范围伤害
    /// </summary>
    public abstract class NebulaVortexStaffBlazeBase : ModProjectile
    {
        #region 调参旋钮
        //帧数:原版贴图 4 帧竖排,原版 AI 按 4 帧循环
        public const int FrameCount = 4;
        //撞墙判定边长(像素),原版同值;命中判定仍是 40x40
        public const int TileBox = 8;
        //存在时间(更新次数,每帧 3~4 次更新)
        public const int LifeUpdates = 600;
        //拖尾:记录的更新次数、记录模式(2 = 位置连同朝向)、整体不透明度、尾端最小缩放(不低于本体一半)
        public const int TrailLength = 10;
        public const int TrailMode = 2;
        public const float TrailOpacity = 0.5f;
        public const float TrailMinScale = 0.5f;
        //Glow2 可见半径占边长的比例;光晕不透明度
        public const float GlowVisibleFrac = 0.4f;
        public const float BloomOpacity = 0.6f;
        //出膛与消散闪光的 Glow2 缩放(1 约合 100 像素半径)
        public const float MuzzleFlashScale = 0.2f;
        public const float KillFlashScale = 0.3f;
        //消散:火花数、沿飞行方向的散开角(弧度)、速度范围(像素/帧)、星尘数
        public const int KillSparkCount = 6;
        public const float KillSparkSpread = 0.6f;
        public const float KillSparkMinSpeed = 3f;
        public const float KillSparkMaxSpeed = 9f;
        public const int KillMoteCount = 6;
        //消散音效:原版是爆炸声,每秒 3 团,压低音量并限制同时实例
        public const float KillVolume = 0.35f;
        public const float KillPitchVariance = 0.15f;
        public const int KillMaxInstances = 4;
        //光尘与闪光的不透明度;出膛与消散闪光寿命(帧)
        public const float MoteOpacity = 0.9f;
        public const int MuzzleFlashLife = 8;
        public const int KillFlashLife = 10;
        //消散火花缩放与寿命(帧);星尘散布半径(像素)、速度上限(像素/帧)、缩放范围、寿命(帧)
        public const float KillSparkScale = 0.04f;
        public const int KillSparkLife = 14;
        public const float KillMoteRadius = 10f;
        public const float KillMoteSpeed = 2.5f;
        public const float KillMoteMinScale = 0.25f;
        public const float KillMoteMaxScale = 0.45f;
        public const int KillMoteLife = 18;
        #endregion

        //原版按类型给星云烈焰的半透明白
        public static readonly Color BodyTint = new Color(255, 255, 255, 127);
        //火花贴图的宽、长缩放比
        public static readonly Vector2 SparkSquash = new Vector2(0.4f, 1f);

        protected abstract int VanillaType { get; }
        protected abstract Color BloomColor { get; }
        protected abstract Color SparkColor { get; }
        //光晕半径(像素)
        protected abstract float BloomRadius { get; }
        //消散音效音高偏移
        protected abstract float KillPitch { get; }

        private bool flashed;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = FrameCount;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.CloneDefaults(VanillaType);
            AIType = VanillaType;
            Projectile.timeLeft = LifeUpdates;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
            width = height = TileBox;
            return true;
        }

        public override Color? GetAlpha(Color lightColor) => BodyTint * Projectile.Opacity;

        public override void AI() {
            if (flashed) {
                return;
            }
            flashed = true;
            if (!Main.dedServ) {
                OnFirstUpdate();
            }
        }

        /// <summary>每个端收到这团焰后的第一次更新;只在非专用服务器上调用</summary>
        protected virtual void OnFirstUpdate() {
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, BloomColor, MuzzleFlashScale).Configure(MoteOpacity, true, PRTDrawModeEnum.AdditiveBlend, 0f, MuzzleFlashLife);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = KillVolume, Pitch = KillPitch, PitchVariance = KillPitchVariance, MaxInstances = KillMaxInstances }, Projectile.Center);
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < KillSparkCount; i++) {
                Vector2 vel = dir.RotatedByRandom(KillSparkSpread) * Main.rand.NextFloat(KillSparkMinSpeed, KillSparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, SparkColor, KillSparkScale).Configure(false, KillSparkLife, SparkSquash, true);
            }
            for (int i = 0; i < KillMoteCount; i++) {
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(KillMoteRadius), CEUtils.randomPointInCircle(KillMoteSpeed), BloomColor, Main.rand.NextFloat(KillMoteMinScale, KillMoteMaxScale)).Configure(MoteOpacity, lifetime: KillMoteLife);
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, BloomColor, KillFlashScale).Configure(MoteOpacity, true, PRTDrawModeEnum.AdditiveBlend, 0f, KillFlashLife);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameHeight = tex.Height / FrameCount;
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, tex.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            Color body = Projectile.GetAlpha(lightColor);
            //拖尾用同一帧本体图,越旧越淡越小
            for (int i = TrailLength - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)TrailLength;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, frame, body * (TrailOpacity * t), Projectile.oldRot[i], origin, Projectile.scale * MathHelper.Lerp(TrailMinScale, 1f, t), SpriteEffects.None);
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D glow = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, BloomColor * (BloomOpacity * Projectile.Opacity), 0f, glow.Size() / 2f, BloomRadius / (glow.Width * GlowVisibleFrac), SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, body, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
