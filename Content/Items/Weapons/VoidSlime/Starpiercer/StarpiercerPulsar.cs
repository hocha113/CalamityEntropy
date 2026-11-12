using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Starpiercer
{
    /// <summary>
    /// 穿天星点的脉冲星弹丸。直线高速飞行,碰到物块即散;命中敌人时向前迸出 45° 的弧形冲击波,并放出一道电弧
    /// 冲击波与电弧都从命中点出发,冲击波不打这个敌人,电弧先电它再跳到下一个
    /// localAI[0] 已过更新次数
    /// </summary>
    public class StarpiercerPulsar : ModProjectile
    {
        public const float Speed = 16f;                 //飞行速度,像素每次更新
        private const int ExtraUpdates = 1;             //每帧额外更新次数,合计 32 像素每帧
        private const int Size = 14;                    //判定边长,像素
        private const int LifeUpdates = 120;            //寿命,更新次数,合计 60 帧,约 120 格
        private const float ShockwaveRatio = 0.5f;      //冲击波伤害占弹丸的比例,策划没写,任务文件定为 50%
        private const float ArcRatio = 0.2f;            //电弧伤害占弹丸的比例,弹丸即面板伤害
        private const float SpinSpeed = 0.2f;           //弹体自转,弧度每次更新
        private const float PulseAmplitude = 0.12f;     //弹体缩放起伏的幅度,比例
        private const float PulseSpeed = 0.5f;          //弹体缩放起伏角频率,弧度每次更新
        private const int TrailRecordMode = 2;          //ProjectileID.Sets.TrailingMode 取值,逐次记下旧位置与朝向
        private const int TrailLength = 10;             //拖尾段数,也是记下的旧位置数
        private const float TrailOpacity = 0.6f;        //拖尾首段不透明度,往后线性降到 0
        private const float TrailScaleEnd = 0.5f;       //拖尾末段缩放,不窄于弹体的一半
        private const float GlowSize = 48f;             //光晕直径,像素
        private const float GlowOpacity = 0.6f;
        private const float TrailDustChance = 0.3f;     //每次更新出一颗电花粉尘的概率
        private const float TrailDustScale = 0.8f;
        private const int BurstDust = 14;               //散开时的粉尘数
        private const float BurstDustSpeed = 5f;        //散开粉尘初速上限,像素每帧
        private const int BurstSparks = 8;              //散开时的火花数
        private const float SparkSpread = 0.7f;         //火花顺着弹丸来向飞出,方向的随机偏转上限,弧度
        private const float SparkSpeedMin = 3f;         //火花初速,像素每帧
        private const float SparkSpeedMax = 8f;
        private const float SparkScaleMin = 0.05f;
        private const float SparkScaleMax = 0.09f;
        private const int SparkLifetime = 16;           //火花寿命,帧
        private static readonly Color GlowColor = new Color(115, 150, 253);
        private static readonly Color TrailColor = new Color(170, 204, 255);
        private static readonly Color SparkColor = new Color(223, 248, 255);
        private static readonly Vector3 LightRGB = new Vector3(0.45f, 0.6f, 1f);    //照明,RGB 0-1

        private ref float Age => ref Projectile.localAI[0];

        public override void SetStaticDefaults() {
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
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifeUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            Age++;
            Projectile.rotation += SpinSpeed;
            Lighting.AddLight(Projectile.Center, LightRGB);
            if (!Main.dedServ && Main.rand.NextFloat() < TrailDustChance) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Vector2.Zero);
                dust.noGravity = true;
                dust.scale = TrailDustScale;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            IEntitySource source = Projectile.GetSource_FromThis();
            Projectile.NewProjectile(source, Projectile.Center, forward, ModContent.ProjectileType<StarpiercerShockwave>(), (int)(Projectile.damage * ShockwaveRatio), Projectile.knockBack, Projectile.owner, target.whoAmI);
            Projectile.NewProjectile(source, Projectile.Center, forward * StarpiercerArc.Speed, ModContent.ProjectileType<StarpiercerArc>(), (int)(Projectile.damage * ArcRatio), 0f, Projectile.owner, target.whoAmI, StarpiercerArc.NoTarget);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < BurstDust; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Main.rand.NextVector2Circular(BurstDustSpeed, BurstDustSpeed));
                dust.noGravity = true;
            }
            Vector2 forward = Projectile.oldVelocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < BurstSparks; i++) {
                Vector2 velocity = forward.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, velocity, SparkColor, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, velocity.ToRotation(), SparkLifetime);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float pulse = 1f + PulseAmplitude * MathF.Sin(Age * PulseSpeed);
            //拖尾用同一张图的 A=0 叠加,越旧越淡越小;从最旧的画起
            for (int i = TrailLength - 1; i >= 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float progress = i / (float)TrailLength;
                Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color color = TrailColor with { A = 0 } * (TrailOpacity * (1f - progress));
                float scale = MathHelper.Lerp(1f, TrailScaleEnd, progress) * Projectile.scale;
                Main.EntitySpriteDraw(texture, position, null, color, Projectile.oldRot[i], origin, scale, SpriteEffects.None);
            }
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, center, null, GlowColor with { A = 0 } * GlowOpacity, 0f, glow.Size() * 0.5f, GlowSize * Projectile.scale / glow.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, null, Color.White, Projectile.rotation, origin, Projectile.scale * pulse, SpriteEffects.None);
            return false;
        }
    }
}
