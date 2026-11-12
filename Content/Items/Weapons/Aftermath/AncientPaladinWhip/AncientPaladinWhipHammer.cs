using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.AncientPaladinWhip
{
    //幻影圣骑士锤:从目标身边旋转飞开约10格,再折返砸向它,只打这一个目标,命中即碎
    //ai[0]=目标NPC下标 ai[1]=飞开方向(弧度) ai[2]=目标NPC类型,下标被别的NPC复用时据此认出
    //localAI[0]=已存活帧数 localAI[1]=淡出进度(帧),0为未淡出
    public class AncientPaladinWhipHammer : ModProjectile
    {
        //伤害倍率,对应「鞭子弹幕伤害的500%」
        public const float DamageMult = 5f;
        private const float HammerKnockback = 4f;
        //飞开距离(像素,约10格)与用时(帧),匀减速到0,初速=2×距离/用时
        private const float FlyOutDist = 160f;
        private const int FlyOutFrames = 22;
        private const float FlyOutStartSpeed = 2f * FlyOutDist / FlyOutFrames;
        //飞开方向在「目标中心→生成点」两侧的随机偏角(弧度)
        private const float FlyOutSpread = 0.6f;
        //折返:初速、每帧加速、最高速(像素/帧)
        private const float ReturnSpeed = 6f;
        private const float ReturnAccel = 1.6f;
        private const float ReturnMaxSpeed = 34f;
        //自转速度(弧度/帧):飞开、折返、淡出
        private const float SpinOut = 0.55f;
        private const float SpinBack = 0.4f;
        private const float SpinFade = 0.2f;
        //淡入、淡出用时与最长存活(帧),淡出时每帧速度保留比例
        private const int FadeInFrames = 6;
        private const int FadeFrames = 15;
        private const int Lifetime = 150;
        private const float FadeDrag = 0.9f;
        //画面:原版圣骑士锤贴图放大倍数、本体不透明度、残影数与最浓残影不透明度
        private const float DrawScale = 1.15f;
        private const float BodyAlpha = 0.75f;
        private const int TrailLength = 8;
        private const float TrailAlpha = 0.45f;
        //金色底光:Glow2原图256像素,0.3倍约77像素
        private const float GlowScale = 0.3f;
        private const float GlowAlpha = 0.6f;
        //飞出与砸中的闪光缩放和寿命(帧)
        private const float LaunchShineScale = 0.35f;
        private const int LaunchShineLife = 10;
        private const float SmashShineScale = 0.8f;
        private const int SmashShineLife = 12;
        private const float SmashRingStart = 0.1f;
        private const float SmashRingEnd = 0.8f;
        private const int SmashRingLife = 16;
        //砸中火花:数量、沿锤子来向的单侧散角(弧度)、速度区间(像素/帧)、缩放区间、寿命区间(帧);金尘数与速度
        private const int SmashSparks = 10;
        private const float SparkSpread = 0.9f;
        private const float SparkSpeedMin = 6f;
        private const float SparkSpeedMax = 13f;
        private const float SparkScaleMin = 0.7f;
        private const float SparkScaleMax = 1.2f;
        private const int SparkLifeMin = 20;
        private const int SparkLifeMax = 28;
        private const int SmashDust = 12;
        private const float DustSpeed = 5f;

        private static readonly Color BodyColor = new(255, 236, 170);
        private static readonly Color TrailColor = new(255, 210, 110);
        private static readonly Color GlowColor = new(255, 200, 90, 0);
        private static readonly Color GlintColor = new(255, 245, 200);
        private static readonly Color RingColor = new(255, 210, 110);
        private static readonly Color SparkHot = new(255, 235, 150);
        private static readonly Color SparkDeep = new(255, 180, 60);
        private static readonly Vector3 HammerLight = new(0.8f, 0.65f, 0.3f);

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.PaladinsHammerFriendly;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
        }

        //只在仆从主人的客户端调用;从仆从命中处贴着目标碰撞箱生成
        public static void Spawn(Projectile minion, NPC target, int whipDamage) {
            Vector2 spawn = Vector2.Clamp(minion.Center, target.TopLeft, target.BottomRight);
            Vector2 away = spawn - target.Center;
            float baseAngle = away.LengthSquared() > 1f ? away.ToRotation() : Main.rand.NextFloat(MathHelper.TwoPi);
            float angle = baseAngle + Main.rand.NextFloat(-FlyOutSpread, FlyOutSpread);
            int damage = (int)(whipDamage * DamageMult);
            Projectile.NewProjectile(minion.GetSource_FromThis(), spawn, Vector2.Zero, ModContent.ProjectileType<AncientPaladinWhipHammer>(), damage, HammerKnockback, minion.owner, target.whoAmI, angle, target.type);
        }

        private NPC Target() {
            int index = (int)Projectile.ai[0];
            if (index < 0 || index >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[index];
            return npc.active && npc.life > 0 && npc.type == (int)Projectile.ai[2] ? npc : null;
        }

        private float Opacity() {
            if (Projectile.localAI[1] > 0f)
                return 1f - (Projectile.localAI[1] - 1f) / FadeFrames;
            return Utils.GetLerpValue(0f, FadeInFrames, Projectile.localAI[0], true);
        }

        public override void AI() {
            NPC target = Target();
            float age = ++Projectile.localAI[0];
            if (age == 1f && !Main.dedServ)
                LaunchFx();
            if (Projectile.localAI[1] == 0f && (target == null || Projectile.timeLeft <= FadeFrames))
                Projectile.localAI[1] = 1f;
            if (Projectile.localAI[1] > 0f) {
                Projectile.velocity *= FadeDrag;
                Projectile.rotation += SpinFade;
                if (++Projectile.localAI[1] > FadeFrames + 1)
                    Projectile.Kill();
                return;
            }
            if (age <= FlyOutFrames) {
                //叠加目标速度,飞开轨迹跟着目标走
                float speed = FlyOutStartSpeed * (1f - (age - 1f) / FlyOutFrames);
                Projectile.velocity = Projectile.ai[1].ToRotationVector2() * speed + target.velocity;
                Projectile.rotation += SpinOut;
            }
            else {
                Vector2 toTarget = target.Center - Projectile.Center;
                float speed = Math.Min(ReturnSpeed + ReturnAccel * (age - FlyOutFrames), ReturnMaxSpeed);
                Projectile.velocity = toTarget.SafeNormalize(Vector2.Zero) * Math.Min(speed, toTarget.Length());
                Projectile.rotation += SpinBack;
            }
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, HammerLight * Opacity());
        }

        public override bool? CanHitNPC(NPC target) {
            if (target.whoAmI != (int)Projectile.ai[0] || Projectile.localAI[1] > 0f || Projectile.localAI[0] <= FlyOutFrames)
                return false;
            return null;
        }

        public override bool? CanCutTiles() => false;

        //击杀同步到各端都会跑这里;淡出中死掉是目标没了或超时,不放砸击
        public override void OnKill(int timeLeft) {
            if (Main.dedServ || Projectile.localAI[1] > 0f)
                return;
            Vector2 center = Projectile.Center;
            SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = 0.8f, PitchVariance = 0.15f, MaxInstances = 3 }, center);
            PRTLoader.NewParticle<PRT_ShineParticle>(center, Vector2.Zero, GlintColor, SmashShineScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, SmashShineLife);
            PRTLoader.NewParticle<PRT_PulseRing>(center, Vector2.Zero, RingColor, SmashRingStart).Configure(SmashRingEnd, SmashRingLife);
            float heading = Projectile.velocity.LengthSquared() > 0.01f ? Projectile.velocity.ToRotation() : Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < SmashSparks; i++) {
                float rot = heading + Main.rand.NextFloat(-SparkSpread, SparkSpread);
                Vector2 velocity = rot.ToRotationVector2() * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                Color color = Color.Lerp(SparkHot, SparkDeep, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_AltSpark>(center, velocity, color, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(false, Main.rand.Next(SparkLifeMin, SparkLifeMax));
            }
            for (int i = 0; i < SmashDust; i++) {
                Dust dust = Dust.NewDustPerfect(center, DustID.Enchanted_Gold, Main.rand.NextVector2Circular(DustSpeed, DustSpeed), 0, default, Main.rand.NextFloat(1f, 1.4f));
                dust.noGravity = true;
            }
        }

        private void LaunchFx() {
            SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.7f, Pitch = 0.2f, PitchVariance = 0.15f, MaxInstances = 3 }, Projectile.Center);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, GlintColor, LaunchShineScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, LaunchShineLife);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glow = CEUtils.getExtraTex("Glow2");
            Vector2 origin = tex.Size() / 2f;
            float opacity = Opacity();
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float k = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, null, TrailColor * (TrailAlpha * k * opacity), Projectile.oldRot[i], origin, DrawScale, SpriteEffects.None);
            }
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, center, null, GlowColor * (GlowAlpha * opacity), 0f, glow.Size() / 2f, GlowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, null, BodyColor * (BodyAlpha * opacity), Projectile.rotation, origin, DrawScale, SpriteEffects.None);
            return false;
        }
    }
}
