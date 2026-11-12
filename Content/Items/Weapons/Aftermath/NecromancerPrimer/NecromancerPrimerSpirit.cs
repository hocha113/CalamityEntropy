using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.NecromancerPrimer
{
    /// <summary>
    /// 粉色地牢幽魂:直线飞行,命中或撞墙即散;贴图是原版地牢幽魂的粉色重着色,脸朝贴图下方
    /// </summary>
    public class NecromancerPrimerSpirit : ModProjectile
    {
        #region 调参旋钮
        //动画:帧数与每帧时长(帧)
        public const int FrameCount = 3;
        public const int FrameTime = 5;
        //出生后这么多帧内穿墙,贴墙生成时不至于立刻消失
        public const int PhaseFrames = 10;
        //存在时间(帧);判定边长(像素)
        public const int Life = 120;
        public const int HitboxSize = 20;
        //出生淡入帧数
        public const int FadeInFrames = 6;
        //拖尾:记录帧数、记录模式(2 = 位置连同朝向)、整体不透明度、尾端最小缩放(不低于本体一半)
        public const int TrailLength = 10;
        public const int TrailMode = 2;
        public const float TrailOpacity = 0.55f;
        public const float TrailMinScale = 0.5f;
        //发光层不透明度与放大倍率;光照强度
        public const float GlowOpacity = 0.5f;
        public const float GlowScale = 1.15f;
        public const float LightStrength = 0.6f;
        //飞行中每帧飘出光尘的概率
        public const float WispChance = 0.5f;
        //消散:火花数、沿飞行方向的散开角(弧度)、速度范围(像素/帧)、星尘数
        public const int KillSparkCount = 6;
        public const float KillSparkSpread = 0.7f;
        public const float KillSparkMinSpeed = 2f;
        public const float KillSparkMaxSpeed = 7f;
        public const int KillMoteCount = 5;
        //消散音效:每秒最多 12 只,压低音量、音高在 1±0.25 内浮动、同时实例限 3
        public const float KillVolume = 0.25f;
        public const float KillPitchBand = 0.25f;
        public const int KillMaxInstances = 3;
        //飞行光尘:散布半径(像素)、相对速度的后飘比例、缩放范围、不透明度、寿命(帧)
        public const float WispRadius = 6f;
        public const float WispDrag = 0.1f;
        public const float WispMinScale = 0.2f;
        public const float WispMaxScale = 0.32f;
        public const float WispOpacity = 0.8f;
        public const int WispLife = 12;
        //消散火花缩放与寿命(帧);星尘散布半径(像素)、随机扰动(像素/帧)、缩放范围、不透明度、寿命(帧)
        public const float KillSparkScale = 0.035f;
        public const int KillSparkLife = 12;
        public const float KillMoteRadius = 8f;
        public const float KillMoteJitter = 1.5f;
        public const float KillMoteMinScale = 0.25f;
        public const float KillMoteMaxScale = 0.4f;
        public const float KillMoteOpacity = 0.85f;
        public const int KillMoteLife = 16;
        #endregion

        public static readonly Color SpiritPink = new Color(255, 140, 230);
        public static readonly Color SpiritPale = new Color(255, 215, 245);
        //本体略透:alpha 低于颜色通道,叠在背景上偏亮
        public static readonly Color BodyTint = new Color(255, 255, 255, 200);
        //火花贴图的宽、长缩放比
        public static readonly Vector2 SparkSquash = new Vector2(0.4f, 1f);

        private int age;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = FrameCount;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Life;
        }

        public override void AI() {
            age++;
            if (age > PhaseFrames) {
                Projectile.tileCollide = true;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.Opacity = MathHelper.Clamp(age / (float)FadeInFrames, 0f, 1f);
            if (++Projectile.frameCounter >= FrameTime) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }
            Lighting.AddLight(Projectile.Center, SpiritPink.ToVector3() * LightStrength * Projectile.Opacity);
            if (!Main.dedServ && Main.rand.NextFloat() < WispChance) {
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(WispRadius), Projectile.velocity * -WispDrag, SpiritPink, Main.rand.NextFloat(WispMinScale, WispMaxScale)).Configure(WispOpacity, lifetime: WispLife);
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("soul", Main.rand.NextFloat(1f - KillPitchBand, 1f + KillPitchBand), Projectile.Center, KillMaxInstances, KillVolume);
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < KillSparkCount; i++) {
                Vector2 vel = dir.RotatedByRandom(KillSparkSpread) * Main.rand.NextFloat(KillSparkMinSpeed, KillSparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, SpiritPale, KillSparkScale).Configure(false, KillSparkLife, SparkSquash, true);
            }
            for (int i = 0; i < KillMoteCount; i++) {
                Vector2 vel = dir * Main.rand.NextFloat(KillSparkMinSpeed) + CEUtils.randomPointInCircle(KillMoteJitter);
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(KillMoteRadius), vel, SpiritPink, Main.rand.NextFloat(KillMoteMinScale, KillMoteMaxScale)).Configure(KillMoteOpacity, lifetime: KillMoteLife);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameHeight = tex.Height / FrameCount;
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, tex.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            Vector2 center = Projectile.Center - Main.screenPosition;
            //拖尾用同一帧本体图,越旧越淡越小
            for (int i = TrailLength - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)TrailLength;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, frame, SpiritPink * (TrailOpacity * t * Projectile.Opacity), Projectile.oldRot[i], origin, MathHelper.Lerp(TrailMinScale, 1f, t), SpriteEffects.None);
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(tex, center, frame, SpiritPink * (GlowOpacity * Projectile.Opacity), Projectile.rotation, origin, GlowScale, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, center, frame, BodyTint * Projectile.Opacity, Projectile.rotation, origin, 1f, SpriteEffects.None);
            return false;
        }
    }
}
