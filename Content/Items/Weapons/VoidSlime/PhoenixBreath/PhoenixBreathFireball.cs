using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.PhoenixBreath
{
    /// <summary>
    /// 凤凰吐息的烈焰火球,右键发射。不穿透,碰到物块即碎;命中敌人后顺着飞行方向向前散出三发追踪火焰弹
    /// localAI[0] 已飞行帧数
    /// </summary>
    public class PhoenixBreathFireball : ModProjectile
    {
        public const float LaunchSpeed = 15f;           //出膛速度,像素每帧
        private const int Frames = 4;                   //贴图竖排帧数
        private const int FrameTicks = 4;               //每帧停留,帧
        private const int Size = 20;                    //判定边长,像素
        private const int LifeFrames = 120;             //寿命,帧,2 秒
        private const int FadeFrames = 12;              //寿命最后这些帧里淡出
        private const int TrailRecordMode = 2;          //ProjectileID.Sets.TrailingMode 取值,逐帧记下旧位置与朝向
        private const int EmberCount = 3;               //命中后散出的火焰弹数
        private const float EmberDamageRatio = 0.66f;   //火焰弹伤害占火球的比例
        private const float EmberHalfSpread = 0.35f;    //火焰弹扇面半角,弧度,约 20°
        private const float HeadX = 19f;                //火球头部圆心在单帧(28×20,朝 +X)上的像素坐标,绘制原点
        private const float HeadY = 10f;
        private const int TrailLength = 8;              //拖尾段数,也是记下的旧位置数
        private const float TrailOpacity = 0.6f;        //拖尾首段不透明度,往后线性降到 0
        private const float TrailScaleEnd = 0.5f;       //拖尾末段缩放,不窄于弹体的一半
        private const float GlowSize = 56f;             //光晕直径,像素
        private const float GlowOpacity = 0.55f;
        private const float TrailDustChance = 0.5f;     //每帧出拖尾粉尘的概率
        private const float TrailDustRadius = 6f;       //拖尾粉尘散布半径,像素
        private const float TrailDustInherit = 0.2f;    //拖尾粉尘初速取自火球速度的比例
        private const float TrailDustScaleMin = 1.1f;
        private const float TrailDustScaleMax = 1.6f;
        private const int BurstDust = 16;               //碎开时的粉尘数
        private const float BurstDustSpeed = 5f;        //碎开粉尘初速上限,像素每帧
        private const float BurstDustScaleMin = 1.3f;
        private const float BurstDustScaleMax = 2f;
        private const int BurstSparks = 6;              //碎开时的火花数
        private const float SparkSpread = 0.7f;         //火花顺着火球来向飞出,方向的随机偏转上限,弧度
        private const float SparkSpeedMin = 3f;         //火花初速,像素每帧
        private const float SparkSpeedMax = 8f;
        private const float SparkScaleMin = 0.05f;
        private const float SparkScaleMax = 0.1f;
        private const int SparkLifetime = 18;           //火花寿命,帧
        private const int FizzleDust = 6;               //寿命耗尽时散出的粉尘数
        private const float FizzleDustSpeed = 2f;       //散出粉尘初速上限,像素每帧
        private const float LaunchPitch = 1.3f;         //出膛声音高,CEUtils.PlaySound 以 1 为原调
        private const float LaunchVolume = 0.45f;
        private const int LaunchInstances = 3;          //出膛声同时最多几个
        private const float BurstPitch = 0.3f;          //碎裂声的音高偏移,原版爆炸声取尖一些
        private const float BurstVolume = 0.5f;
        private static readonly Color GlowColor = new Color(252, 102, 25);
        private static readonly Color TrailColor = new Color(255, 145, 28);
        private static readonly Color SparkColor = new Color(255, 215, 70);
        private static readonly Vector3 LightRGB = new Vector3(1f, 0.5f, 0.15f);   //照明,RGB 0-1

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
            Projectile.timeLeft = LifeFrames;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            Age++;
            if (Age == 1f) {
                CEUtils.PlaySound("YharonFireball1", LaunchPitch, Projectile.Center, LaunchInstances, LaunchVolume);
            }
            if (++Projectile.frameCounter >= FrameTicks) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Frames;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, LightRGB * Projectile.Opacity);
            if (!Main.dedServ && Main.rand.NextFloat() < TrailDustChance) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(TrailDustRadius, TrailDustRadius), DustID.Torch, Projectile.velocity * TrailDustInherit);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(TrailDustScaleMin, TrailDustScaleMax);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            PhoenixBreath.ApplyHitDebuffs(Main.player[Projectile.owner], target);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int damage = (int)(Projectile.damage * EmberDamageRatio);
            int type = ModContent.ProjectileType<PhoenixBreathEmber>();
            for (int i = 0; i < EmberCount; i++) {
                float angle = MathHelper.Lerp(-EmberHalfSpread, EmberHalfSpread, i / (EmberCount - 1f));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, forward.RotatedBy(angle) * PhoenixBreathEmber.LaunchSpeed, type, damage, Projectile.knockBack * EmberDamageRatio, Projectile.owner, target.whoAmI, PhoenixBreathEmber.NoTarget);
            }
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
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = BurstPitch, Volume = BurstVolume }, Projectile.Center);
            Scatter(BurstDust, BurstDustSpeed, BurstDustScaleMin, BurstDustScaleMax);
            Vector2 forward = Projectile.oldVelocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < BurstSparks; i++) {
                Vector2 velocity = forward.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, velocity, SparkColor, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, velocity.ToRotation(), SparkLifetime);
            }
        }

        private void Scatter(int count, float speed, float scaleMin, float scaleMax) {
            for (int i = 0; i < count; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame, Main.rand.NextVector2Circular(speed, speed));
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
            Main.EntitySpriteDraw(glow, center, null, GlowColor with { A = 0 } * (GlowOpacity * opacity), 0f, glow.Size() * 0.5f, GlowSize * Projectile.scale / glow.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, frame, Color.White * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
