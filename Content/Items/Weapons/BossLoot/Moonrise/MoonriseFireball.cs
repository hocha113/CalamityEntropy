using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.Moonrise
{
    /// <summary>
    /// 月上梢头的追踪火球,新月与弦月发射。出膛片刻后转向附近看得见的敌人,命中即碎,无视 35 防御
    /// ai[0] 追踪目标的 NPC 序号,NoTarget 为无,只由拥有者选定并同步;localAI[0] 已飞行帧数
    /// </summary>
    public class MoonriseFireball : ModProjectile
    {
        public const float NoTarget = -1f;              //ai[0] 取值: 没有目标
        public const float LaunchSpeed = 14f;           //出膛速度,像素每帧
        private const int Frames = 4;                   //贴图竖排帧数
        private const int FrameTicks = 4;               //每帧停留,帧
        private const int Size = 16;                    //判定边长,像素
        private const int LifeFrames = 150;             //寿命,帧,2.5 秒
        private const int FadeFrames = 15;              //寿命最后这些帧里淡出
        private const int IgnoredDefense = 35;          //无视的防御
        private const int TrailRecordMode = 2;          //ProjectileID.Sets.TrailingMode 取值,逐帧记下旧位置与朝向
        private const int HomingDelay = 8;              //出膛后这么多帧才开始追踪
        private const float HomingRange = 640f;         //索敌半径,像素,40 格
        private const int RetargetInterval = 10;        //有目标时重新索敌的间隔,帧;没有目标时每帧都找
        private const float HomingSpeed = 16f;          //追踪时趋近的速度,像素每帧
        private const float HomingInertia = 9f;         //转向惯性,每帧朝目标方向修正 1/9
        private const float HeadX = 14f;                //火球头部圆心在单帧(24×20,朝 +X)上的像素坐标,绘制原点
        private const float HeadY = 11f;
        private const int TrailLength = 10;             //拖尾段数,也是记下的旧位置数
        private const float TrailOpacity = 0.55f;       //拖尾首段不透明度,往后线性降到 0
        private const float TrailScaleEnd = 0.35f;      //拖尾末段缩放
        private const float GlowSize = 48f;             //光晕直径,像素
        private const float GlowOpacity = 0.6f;
        private const float TrailDustChance = 0.4f;     //每帧出拖尾粉尘的概率
        private const float TrailDustRadius = 5f;       //拖尾粉尘散布半径,像素
        private const float TrailDustInherit = 0.2f;    //拖尾粉尘初速取自火球速度的比例
        private const float TrailDustScaleMin = 0.9f;
        private const float TrailDustScaleMax = 1.3f;
        private const int BurstDust = 14;               //碎开时的粉尘数
        private const float BurstDustSpeed = 4f;        //碎开粉尘初速上限,像素每帧
        private const float BurstDustScaleMin = 1.1f;
        private const float BurstDustScaleMax = 1.6f;
        private const int BurstSparks = 6;              //碎开时的火花数
        private const float SparkSpread = 0.6f;         //火花顺着火球来向飞出,方向的随机偏转上限,弧度
        private const float SparkSpeedMin = 3f;         //火花初速,像素每帧
        private const float SparkSpeedMax = 7f;
        private const float SparkScaleMin = 0.05f;
        private const float SparkScaleMax = 0.09f;
        private const int SparkLifetime = 18;           //火花寿命,帧
        private const int FizzleDust = 5;               //寿命耗尽时散出的粉尘数
        private const float FizzleDustSpeed = 2f;       //散出粉尘初速上限,像素每帧
        private const float LaunchPitchMin = 1.2f;      //出膛声音高,CEUtils.PlaySound 以 1 为原调
        private const float LaunchPitchMax = 1.4f;
        private const float LaunchVolume = 0.3f;
        private const int LaunchInstances = 3;          //出膛声同时最多几个
        private const float HitPitchMin = 1.2f;         //碎裂声音高
        private const float HitPitchMax = 1.5f;
        private const float HitVolume = 0.35f;
        private const int HitInstances = 4;             //碎裂声同时最多几个
        private const int HitSoundVariants = 2;         //moonlighthit0 与 moonlighthit1
        private static readonly Color TrailColor = new Color(170, 204, 255);
        private static readonly Color GlowColor = new Color(115, 150, 253);
        private static readonly Color SparkColor = new Color(190, 220, 255);
        private static readonly Vector3 LightRGB = new Vector3(0.35f, 0.5f, 0.9f);  //照明,RGB 0-1

        private ref float Age => ref Projectile.localAI[0];

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailRecordMode;
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifeFrames;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = IgnoredDefense;
        }

        public override void AI() {
            Age++;
            if (Age == 1f) {
                CEUtils.PlaySound("LunarStar", Main.rand.NextFloat(LaunchPitchMin, LaunchPitchMax), Projectile.Center, LaunchInstances, LaunchVolume);
            }
            if (++Projectile.frameCounter >= FrameTicks) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Frames;
            }
            if (Age >= HomingDelay) {
                if (Projectile.owner == Main.myPlayer && (!HasTarget(out _) || (Age - HomingDelay) % RetargetInterval == 0f)) {
                    Retarget();
                }
                if (HasTarget(out NPC target)) {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * HomingSpeed;
                    Projectile.velocity = (Projectile.velocity * (HomingInertia - 1f) + desired) / HomingInertia;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, LightRGB * Projectile.Opacity);
            if (!Main.dedServ && Main.rand.NextFloat() < TrailDustChance) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(TrailDustRadius, TrailDustRadius), DustID.IceTorch, Projectile.velocity * TrailDustInherit);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(TrailDustScaleMin, TrailDustScaleMax);
            }
        }

        //目标只在拥有者端选,换了才同步
        private void Retarget() {
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange, CEUtils.HomingWithTileBlockingFilter);
            float index = target == null ? NoTarget : target.whoAmI;
            if (index != Projectile.ai[0]) {
                Projectile.ai[0] = index;
                Projectile.netUpdate = true;
            }
        }

        private bool HasTarget(out NPC target) {
            int index = (int)Projectile.ai[0];
            target = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
            return target != null && target.CanBeChasedBy(Projectile);
        }

        //击杀会同步到各端,碎裂特效放在这里各端都看得到;timeLeft 为 0 是寿命耗尽
        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            if (timeLeft <= 0) {
                Scatter(FizzleDust, FizzleDustSpeed, TrailDustScaleMin, TrailDustScaleMax);
                return;
            }
            CEUtils.PlaySound("moonlighthit" + Main.rand.Next(HitSoundVariants), Main.rand.NextFloat(HitPitchMin, HitPitchMax), Projectile.Center, HitInstances, HitVolume);
            Scatter(BurstDust, BurstDustSpeed, BurstDustScaleMin, BurstDustScaleMax);
            Vector2 forward = Projectile.oldVelocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < BurstSparks; i++) {
                Vector2 velocity = forward.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, velocity, SparkColor, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, velocity.ToRotation(), SparkLifetime);
            }
        }

        private void Scatter(int count, float speed, float scaleMin, float scaleMax) {
            for (int i = 0; i < count; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, Main.rand.NextVector2Circular(speed, speed));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(scaleMin, scaleMax);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Frames, 0, Projectile.frame);
            Vector2 origin = new Vector2(HeadX, HeadY);
            float opacity = Projectile.Opacity;
            //拖尾用同一张图的 A=0 叠加,越旧越淡越小;从最旧的画起
            for (int i = TrailLength - 1; i >= 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float progress = i / (float)TrailLength;
                Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color color = TrailColor with { A = 0 } * (TrailOpacity * (1f - progress) * opacity);
                float scale = MathHelper.Lerp(1f, TrailScaleEnd, progress) * Projectile.scale;
                Main.EntitySpriteDraw(texture, position, frame, color, Projectile.oldRot[i], origin, scale, SpriteEffects.None);
            }
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, center, null, GlowColor with { A = 0 } * (GlowOpacity * opacity), 0f, glow.Size() * 0.5f, GlowSize / glow.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, frame, Color.White * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
