using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.StarryNight
{
    /// <summary>
    /// 暗夜繁星的彗星:从高空斜落,50 格内追踪敌人;落到施法时玩家所在高度之前穿墙(仿原版狂星),之后撞墙即散
    /// ai[0] 目标 NPC 索引(-1 为无),主人端选定并同步;ai[1] 开始撞墙的世界 Y 坐标
    /// 大小两型只差 <see cref="Look"/> 里的参数;贴图朝右,旋转原点是白色星核
    /// </summary>
    public abstract class StarryNightComet : ModProjectile
    {
        #region 调参旋钮
        //时间单位为更新(extraUpdates 1,每帧 2 次),距离 px,速度 px/更新
        public const int ExtraUpdates = 1;
        //存在时间(更新),约 2 秒;出生淡入、寿终淡出(更新)
        public const int Life = 240;
        public const int FadeInUpdates = 10;
        public const int FadeOutUpdates = 20;
        //追踪距离:策划定 50 格;目标跑出这个距离就放弃
        public const float HomingRange = 800f;
        public const float LoseRange = 1000f;
        //主人端每隔这么多更新补找一次目标
        public const int RetargetInterval = 6;
        //常速转弯半径(px),角速度 = 速度 / 半径;贴近目标时角速度最多再乘这么多倍,免得绕圈
        public const float TurnRadius = 180f;
        public const float CloseRange = 240f;
        public const float CloseTurnBoost = 3f;
        //拖尾粒子在彗星消失后的淡出(帧);PRT_TrailParticle 按 剩余帧/12 算亮度,12 正好满亮
        public const int TrailFadeFrames = 12;
        //星核闪光的脉动幅度与角频率(弧度/秒)
        public const float TwinklePulse = 0.15f;
        public const float TwinkleRate = 9f;
        //飞行星屑:散布半径(px)、顺飞行方向的余速比例、寿命(帧)、自转(弧度/帧)、光晕倍率
        public const float SparkleRadius = 5f;
        public const float SparkleDrag = 0.06f;
        public const int SparkleLife = 16;
        public const float SparkleSpin = 0.08f;
        public const float SparkleBloom = 1.3f;
        //出生星芒相对消散闪光的缩放
        public const float SpawnFlashRatio = 0.6f;
        //消散:冲击环起始缩放与寿命(帧);闪光寿命(帧)与光晕倍率
        public const float RingStartScale = 0.1f;
        public const int RingLife = 14;
        public const int FlashLife = 12;
        public const float FlashBloom = 1.6f;
        //消散火花:命中敌人顺飞行方向、撞墙往回溅的散开角(弧度),速度(px/帧),寿命(帧)
        public const float SparkSpread = 0.7f;
        public const float TileSparkSpread = 1.2f;
        public const float SparkMinSpeed = 3f;
        public const float SparkMaxSpeed = 9f;
        public const int SparkLife = 12;
        //消散光尘:数量、散布半径(px)、速度扰动(px/帧)、缩放范围、寿命(帧)
        public const int MoteCount = 4;
        public const float MoteRadius = 8f;
        public const float MoteJitter = 1.5f;
        public const float MoteMinScale = 0.2f;
        public const float MoteMaxScale = 0.34f;
        public const int MoteLife = 16;
        //撞击音效:贤者自己的陨星落点音,同时实例上限
        public const string ImpactSound = "moonlighthit0";
        public const int ImpactMaxInstances = 4;
        #endregion

        //火花贴图的宽、长缩放比
        public static readonly Vector2 SparkSquash = new(0.3f, 1f);

        /// <summary>大小两型各自的参数,在子类顶部赋值</summary>
        public sealed class Look
        {
            //飞行速度(px/更新)、判定边长(px)、贴图里白色星核中心(px)
            public float Speed { get; init; }
            public int Hitbox { get; init; }
            public Vector2 Pivot { get; init; }
            //头部光晕颜色、半径(px)、强度;光照强度(乘光晕颜色)
            public Color Glow { get; init; }
            public float GlowRadius { get; init; }
            public float GlowIntensity { get; init; }
            public float Light { get; init; }
            //星核闪光(Star2 贴图 128px)顺飞行方向与横向的缩放、强度
            public Vector2 TwinkleScale { get; init; }
            public float TwinkleIntensity { get; init; }
            //拖尾颜色、宽度缩放(PRT_TrailParticle 外带半宽 12px 乘它)、点数(每更新一个点,长度约点数乘速度)
            public Color Trail { get; init; }
            public float TrailScale { get; init; }
            public int TrailPoints { get; init; }
            //飞行星屑间隔(更新)与缩放范围
            public int SparkleInterval { get; init; }
            public float SparkleMinScale { get; init; }
            public float SparkleMaxScale { get; init; }
            //消散:冲击环终缩放(环贴图 156px)、闪光缩放、火花数与缩放、火花颜色
            public float RingScale { get; init; }
            public float FlashScale { get; init; }
            public int SparkCount { get; init; }
            public float SparkScale { get; init; }
            public Color Spark { get; init; }
            //撞击音高范围(1 为原调)与音量
            public float ImpactPitchMin { get; init; }
            public float ImpactPitchMax { get; init; }
            public float ImpactVolume { get; init; }
        }

        protected abstract Look Style { get; }

        private int age;
        private bool hitTile;
        private Vector2 lastDir = Vector2.UnitY;
        private PRT_TrailParticle trail;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Style.Hitbox;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = Life;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            age++;
            //ai[1] 随生成包同步,各端在同一高度开始撞墙
            if (!Projectile.tileCollide && Projectile.Center.Y >= Projectile.ai[1]) {
                Projectile.tileCollide = true;
            }
            Home();
            lastDir = Projectile.velocity.SafeNormalize(lastDir);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = MathHelper.Clamp(Math.Min(age / (float)FadeInUpdates, Projectile.timeLeft / (float)FadeOutUpdates), 0f, 1f);
            Lighting.AddLight(Projectile.Center, Style.Glow.ToVector3() * Style.Light * Projectile.Opacity);
            if (!Main.dedServ) {
                Visuals();
            }
        }

        private void Home() {
            NPC target = null;
            int index = (int)Projectile.ai[0];
            if (index >= 0 && index < Main.maxNPCs) {
                NPC npc = Main.npc[index];
                if (npc.CanBeChasedBy(Projectile) && Projectile.DistanceSQ(npc.Center) < LoseRange * LoseRange) {
                    target = npc;
                }
            }
            //只有主人补找目标,换了就同步;别的端只跟 ai[0]
            if (target == null && Projectile.owner == Main.myPlayer && age % RetargetInterval == 0) {
                target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange);
                float next = target?.whoAmI ?? -1;
                if (next != Projectile.ai[0]) {
                    Projectile.ai[0] = next;
                    Projectile.netUpdate = true;
                }
            }
            if (target == null) {
                return;
            }
            float boost = MathHelper.Lerp(CloseTurnBoost, 1f, Utils.GetLerpValue(0f, CloseRange, Projectile.Distance(target.Center), true));
            float heading = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), Projectile.AngleTo(target.Center), Style.Speed / TurnRadius * boost);
            Projectile.velocity = heading.ToRotationVector2() * Style.Speed;
        }

        private void Visuals() {
            if (age == 1) {
                //生成线进屏(缩小视野或高分辨率)时才看得见
                PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Color.White, Style.FlashScale * SpawnFlashRatio).Configure(Style.Glow, FlashLife, 0f, FlashBloom);
            }
            //系统满员或暂停时 NewParticle 返回未激活的实例,下次再试
            if (trail == null || !trail.active) {
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Style.Trail, Style.TrailScale);
                trail.maxLength = Style.TrailPoints;
                trail.ShouldKillWhenOffScreen = false;
                trail.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend);
            }
            //活着就把寿命往后推,彗星消失后留下的轨迹自己淡掉
            trail.Lifetime = trail.Time + TrailFadeFrames;
            trail.Position = Projectile.Center;
            trail.Color = Style.Trail * Projectile.Opacity;
            //AI 在本次位移之前跑,加上速度才对得上本帧画出的头
            trail.AddPoint(Projectile.Center + Projectile.velocity);
            if (age % Style.SparkleInterval == 0) {
                PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center + CEUtils.randomPointInCircle(SparkleRadius), Projectile.velocity * SparkleDrag, Color.White,
                    Main.rand.NextFloat(Style.SparkleMinScale, Style.SparkleMaxScale)).Configure(Style.Glow, SparkleLife, SparkleSpin, SparkleBloom);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            hitTile = true;
            return true;
        }

        public override void OnKill(int timeLeft) {
            //寿终时本体已淡出,只剩拖尾自己淡掉
            if (Main.dedServ || timeLeft <= 0) {
                return;
            }
            for (int i = 0; i < MoteCount; i++) {
                Vector2 vel = lastDir * Main.rand.NextFloat(SparkMinSpeed) + CEUtils.randomPointInCircle(MoteJitter);
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(MoteRadius), vel, Style.Glow,
                    Main.rand.NextFloat(MoteMinScale, MoteMaxScale)).Configure(1f, lifetime: MoteLife);
            }
            CEUtils.PlaySound(ImpactSound, Main.rand.NextFloat(Style.ImpactPitchMin, Style.ImpactPitchMax), Projectile.Center, ImpactMaxInstances, Style.ImpactVolume);
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Color.White, Style.FlashScale).Configure(Style.Glow, FlashLife, 0f, FlashBloom);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Style.Glow, RingStartScale).Configure(Style.RingScale, RingLife);
            Vector2 spray = hitTile ? -lastDir : lastDir;
            float spread = hitTile ? TileSparkSpread : SparkSpread;
            for (int i = 0; i < Style.SparkCount; i++) {
                Vector2 vel = spray.RotatedByRandom(spread) * Main.rand.NextFloat(SparkMinSpeed, SparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, Style.Spark, Style.SparkScale).Configure(false, SparkLife, SparkSquash, true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            float o = Projectile.Opacity;
            //光晕和星核闪光在下,像素本体压在上面保持清晰
            GodDraw.BeginAdditive();
            Texture2D glow = GodDraw.Glow;
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(Style.Glow, Style.GlowIntensity * o), 0f, Style.GlowRadius * 2f / glow.Width);
            float pulse = 1f + TwinklePulse * MathF.Sin(Main.GlobalTimeWrappedHourly * TwinkleRate + Projectile.identity);
            GodDraw.Centered(GodDraw.ThinStar, Projectile.Center, GodDraw.Add(Color.White, Style.TwinkleIntensity * o), Projectile.rotation, Style.TwinkleScale * pulse);
            GodDraw.End();
            //自发光不吃环境光
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * o, Projectile.rotation, Style.Pivot, 1f, SpriteEffects.None);
            return false;
        }
    }
}
