using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PlunderBlaze
{
    /// <summary>
    /// 劫掠怒焰的暗影火焰。照搬原版喷火器火焰 ProjectileID.Flames 的 AI_193 与 Main.DrawProj_Flamethrower,换成蓝紫配色,贴图借原版 Projectile_85
    /// 被物块挡住会停在墙前燃尽,遇水熄灭;最多烧 4 个敌人,每个只烧一次,每烧一个伤害降一成半;命中挂 8 秒虚空之火,无视 30 防御
    /// ai[0] 本轮里的序号,是 5 的倍数时播放喷射声;localAI[0] 已过更新次数
    /// </summary>
    public class PlunderBlazeFlame : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flames;

        public const float Speed = 9.1f;                //每次更新移动的距离,像素,原版喷火器 7 的 1.3 倍
        private const int Frames = 7;                   //贴图竖排帧数
        private const int ExtraUpdates = 2;             //每帧额外更新次数
        private const float SlowdownUpdates = 60f;      //此后每次更新减速并淡出,更新次数
        private const float LifeUpdates = 72f;          //存活的更新次数,合计 24 帧
        private const float Slowdown = 0.95f;           //减速期每次更新保留的速度比例,贴墙时同样
        private const int Size = 6;                     //碰撞物块用的边长,像素
        private const float InflateStart = 10f;         //伤害判定向四周扩的距离,像素,随寿命涨到下一个值
        private const float InflateEnd = 40f;
        private const int MaxHits = 4;                  //最多烧到的敌人数
        private const float PierceFalloff = 0.85f;      //每烧到一个敌人后伤害保留的比例
        private const int IgnoredDefense = 30;          //无视的防御
        private const int VoidFireFrames = 480;         //命中施加的虚空之火时长,帧,8 秒
        private const float LethalMargin = 1.25f;       //这一击伤害乘以它不小于剩余生命就当作致死,补联机时客户端血量的偏差
        private const int MarkFrames = 10;              //致死标记的时长,帧
        private const float SoundEveryShots = 5f;       //每几发播一次喷射声
        private const float ShotPitch = -0.25f;         //喷射声音高
        private const float ShotPitchBand = 0.15f;      //喷射声音高随机幅度,连喷不单调
        private const int SoundInstances = 3;           //喷射声同时最多几个
        //粉尘,照搬原版 AI_193,火把粉尘换成同一种更新方式的紫火把粉尘
        private const float FlameDustChance = 0.25f;    //每次更新出火焰粉尘的概率
        private const float FlameDustUntil = 50f;       //此前出火焰粉尘,此后出烟,更新次数
        private const float FlameDustRadius = 60f;      //火焰粉尘的散布半径,像素,随寿命从下一个比例涨满
        private const float FlameDustRadiusStart = 0.5f;
        private const int DustBox = 4;                  //粉尘生成框边长,像素
        private const float FlameDustInherit = 0.2f;    //火焰粉尘初速取自火焰速度的比例
        private const int FlameDustAlpha = 100;
        private const int BigDustOneIn = 4;             //每几颗火焰粉尘里有一颗是不受重力的大火苗
        private const float BigDustScale = 4.5f;        //大火苗的缩放倍数
        private const float BigDustSpeed = 2.4f;        //大火苗的速度倍数
        private const float SmallDustScale = 2.25f;     //小火苗的缩放倍数
        private const float SmallDustSpeed = 1.2f;      //小火苗的速度倍数
        private const float DustFollowFade = 45f;       //火焰粉尘跟随火焰的力度在这么多次更新内降到下限
        private const float DustFollowRamp = 6f;        //刚喷出的这么多次更新内跟随力度从下限升满
        private const float DustFollowMin = 0.1f;
        private const float SmokeChance = 0.5f;         //每次更新出烟的概率
        private const float SmokeRadius = 50f;          //烟的散布半径,像素
        private const float SmokeSpeed = 7f;            //烟背离玩家飘散的速度,像素每帧
        private const float SmokeSpread = MathHelper.Pi / 16f;  //烟飘散方向的随机偏转,弧度
        private const float SmokeBackOffset = 2f;       //烟生成点朝玩家回退的帧数
        private const int SmokeAlpha = 150;
        private const float SmokeScaleMin = 1.1f;       //烟的缩放倍数
        private const float SmokeScaleMax = 1.3f;
        //绘制,照搬原版 DrawProj_Flamethrower 的分段与叠层
        private const float BandIgnite = 0.1f;          //寿命进度分段: 淡入到点火色
        private const float BandCore = 0.2f;            //点火色过渡到核心色
        private const float BandCoreEnd = 0.35f;        //核心色保持到这里
        private const float BandBody = 0.7f;            //核心色过渡到火身色
        private const float BandTail = 0.85f;           //火身色过渡到余烬色,之后淡出
        private const float AppearUntil = 0.2f;         //叠层不透明度在这段进度里从 0 涨满
        private const float ScaleGrowStart = 0.2f;      //火团缩放在这段进度里从下限涨到 1
        private const float ScaleGrowEnd = 0.5f;
        private const float ScaleMin = 0.25f;
        private const float FrameShiftStart = 0.5f;     //此后帧从第 3 帧渐变到第 5 帧
        private const float FirstFrame = 3f;
        private const float LastFrame = 5f;
        private const float StretchUpdates = 20f;       //火团沿速度反方向拉开的长度,更新次数,寿命内封顶
        private const float StepNormal = 0.2f;          //叠层步长
        private const float StepLate = 0.175f;          //快燃尽时的叠层步长,叠得更密
        private const float StepLateAfter = 50f;        //此后改用密步长,更新次数
        private const float SpinRate = 2f;              //叠层自转速度系数,弧度每秒
        private const float DeepAlphaBoost = 80f;       //外层额外的不透明度,0-255
        private const float HaloOpacity = 0.25f;        //每层前方虚影的不透明度
        private const float OuterHaloLead = 0.5f;       //外层虚影前移的步长比例
        private const float InnerHaloLead = 0.2f;       //内层虚影前移的步长比例
        private const float InnerScale = 0.75f;         //内层缩放
        private static readonly Color IgniteColor = new Color(140, 90, 255, 200);   //刚喷出
        private static readonly Color CoreColor = new Color(90, 150, 255, 70);      //最热的一段
        private static readonly Color BodyColor = new Color(150, 80, 255, 100);     //火身
        private static readonly Color TailColor = new Color(70, 40, 140, 100);      //余烬
        private static readonly Color SmokeColor = new Color(60, 50, 80);           //烟粉尘
        private static readonly Vector3 LightRGB = new Vector3(0.45f, 0.25f, 0.8f); //照明,RGB 0-1

        private ref float Life => ref Projectile.localAI[0];

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = MaxHits;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = IgnoredDefense;
        }

        public override void AI() {
            Life++;
            if (Life >= LifeUpdates) {
                Projectile.Kill();
                return;
            }
            if (Life == 1f && !Main.dedServ && Projectile.ai[0] % SoundEveryShots == 0f) {
                SoundEngine.PlaySound(SoundID.Item34 with { Pitch = ShotPitch, PitchVariance = ShotPitchBand, MaxInstances = SoundInstances }, Projectile.Center);
            }
            //遇水熄灭,同原版
            if (Projectile.wet && !Projectile.lavaWet) {
                Projectile.Kill();
                return;
            }
            if (Life >= SlowdownUpdates) {
                Projectile.velocity *= Slowdown;
            }
            Lighting.AddLight(Projectile.Center, LightRGB * Utils.Remap(Life, SlowdownUpdates, LifeUpdates, 1f, 0f));
            SpawnDust();
        }

        private void SpawnDust() {
            float life = Life;
            if (life < FlameDustUntil && Main.rand.NextFloat() < FlameDustChance) {
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(FlameDustRadius, FlameDustRadius) * Utils.Remap(life, 0f, LifeUpdates, FlameDustRadiusStart, 1f);
                Dust dust = Dust.NewDustDirect(position, DustBox, DustBox, DustID.PurpleTorch, Projectile.velocity.X * FlameDustInherit, Projectile.velocity.Y * FlameDustInherit, FlameDustAlpha);
                if (Main.rand.NextBool(BigDustOneIn)) {
                    dust.noGravity = true;
                    dust.scale *= BigDustScale;
                    dust.velocity *= BigDustSpeed;
                }
                else {
                    dust.scale *= SmallDustScale;
                    dust.velocity *= SmallDustSpeed;
                }
                dust.velocity += Projectile.velocity * Utils.Remap(life, 0f, DustFollowFade, 1f, DustFollowMin) * Utils.Remap(life, 0f, DustFollowRamp, DustFollowMin, 1f);
            }
            if (life >= FlameDustUntil && Main.rand.NextFloat() < SmokeChance) {
                Vector2 away = (Projectile.Center - Main.player[Projectile.owner].Center).SafeNormalize(Vector2.Zero).RotatedByRandom(SmokeSpread) * SmokeSpeed;
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(SmokeRadius, SmokeRadius) - away * SmokeBackOffset;
                Dust smoke = Dust.NewDustDirect(position, DustBox, DustBox, DustID.Smoke, 0f, 0f, SmokeAlpha, SmokeColor);
                smoke.noGravity = true;
                smoke.velocity = away;
                smoke.scale *= Main.rand.NextFloat(SmokeScaleMin, SmokeScaleMax);
            }
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            int inflate = (int)Utils.Remap(Life, 0f, LifeUpdates, InflateStart, InflateEnd);
            hitbox.Inflate(inflate, inflate);
        }

        //火团隔着墙烧不到,同原版
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!projHitbox.Intersects(targetHitbox)) {
                return false;
            }
            return Collision.CanHit(Projectile.Center, 0, 0, targetHitbox.Center.ToVector2(), 0, 0);
        }

        //同原版: 保留速度只为拉开火团,位置退回抵掉本次位移,停在墙前慢慢燃尽
        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.velocity = oldVelocity * Slowdown;
            Projectile.position -= Projectile.velocity;
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            NPC victim = target.realLife >= 0 ? Main.npc[target.realLife] : target;
            if (!PlunderBlazeGlobalNPC.Eligible(victim)) {
                return;
            }
            //最后一击在拥有者客户端结算、掉落在服务器结算;标记用减益挂,NPC.AddBuff 的同步包先于伤害包到达
            modifiers.ModifyHitInfo += (ref NPC.HitInfo hit) => {
                if (hit.InstantKill || hit.Damage * LethalMargin >= victim.life) {
                    victim.AddBuff(ModContent.BuffType<PlunderBlazeMark>(), MarkFrames);
                }
            };
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<VoidFire>(), VoidFireFrames);
            Projectile.damage = (int)(Projectile.damage * PierceFalloff);
        }

        public override bool PreDraw(ref Color lightColor) {
            float life = Life;
            float progress = Utils.Remap(life, 0f, LifeUpdates, 0f, 1f);
            if (progress >= 1f) {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Frames, 0, (int)Utils.Remap(progress, FrameShiftStart, 1f, FirstFrame, LastFrame));
            Vector2 origin = frame.Size() * 0.5f;
            float fade = Utils.Remap(life, SlowdownUpdates, LifeUpdates, 1f, 0f);
            float scale = Utils.Remap(progress, ScaleGrowStart, ScaleGrowEnd, ScaleMin, 1f) * Projectile.scale;
            float step = life > StepLateAfter ? StepLate : StepNormal;
            float appear = Utils.Remap(progress, 0f, AppearUntil, 0f, 1f);
            Color band = BandColor(progress);
            Vector2 back = Projectile.velocity * -Math.Min(life, StretchUpdates);
            Vector2 center = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            //从尾到头叠一串旋转的火团,外层暗而实,内层亮而小;半透明的 A 让它在默认批次里半叠加
            for (int pass = 0; pass < 2; pass++) {
                for (float k = 1f; k >= 0f; k -= step) {
                    float weight = (1f - k) * appear;
                    Vector2 position = center + back * k;
                    Color color = band * weight;
                    float spin = 1f / step * (k + 1f) * time * SpinRate;
                    float spinA = Projectile.rotation + k * MathHelper.PiOver2 + spin;
                    float spinB = Projectile.rotation - k * MathHelper.PiOver2 - spin;
                    if (pass == 0) {
                        color.G /= 2;
                        color.A = (byte)Math.Min(color.A + DeepAlphaBoost * weight, 255f);
                        Main.EntitySpriteDraw(texture, position + back * step * OuterHaloLead, frame, color * (fade * HaloOpacity), spinA + MathHelper.PiOver4, origin, scale, SpriteEffects.None);
                        Main.EntitySpriteDraw(texture, position, frame, color * fade, spinB, origin, scale, SpriteEffects.None);
                    }
                    else {
                        Main.EntitySpriteDraw(texture, position + back * step * InnerHaloLead, frame, color * (fade * HaloOpacity), spinA + MathHelper.PiOver2, origin, scale * InnerScale, SpriteEffects.None);
                        Main.EntitySpriteDraw(texture, position, frame, color * fade, spinB + MathHelper.PiOver2, origin, scale * InnerScale, SpriteEffects.None);
                    }
                }
            }
            return false;
        }

        private static Color BandColor(float progress) {
            if (progress < BandIgnite) {
                return Color.Lerp(Color.Transparent, IgniteColor, Utils.GetLerpValue(0f, BandIgnite, progress, true));
            }
            if (progress < BandCore) {
                return Color.Lerp(IgniteColor, CoreColor, Utils.GetLerpValue(BandIgnite, BandCore, progress, true));
            }
            if (progress < BandCoreEnd) {
                return CoreColor;
            }
            if (progress < BandBody) {
                return Color.Lerp(CoreColor, BodyColor, Utils.GetLerpValue(BandCoreEnd, BandBody, progress, true));
            }
            if (progress < BandTail) {
                return Color.Lerp(BodyColor, TailColor, Utils.GetLerpValue(BandBody, BandTail, progress, true));
            }
            return Color.Lerp(TailColor, Color.Transparent, Utils.GetLerpValue(BandTail, 1f, progress, true));
        }
    }
}
