using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.TwinDragonsPearl
{
    /// <summary>
    /// 金色飞龙幻影:从宝珠外 18 格处沿圆弧穿过宝珠,再飞约 10 格后溶散。
    /// Center 固定在生成时的宝珠处;ai[0] = 弦方向(生成点指向宝珠,弧度),ai[1] = 弯向(±1)。
    /// 各节按弧长排在同一条圆弧上,各端由同样的输入算出同样的轨迹
    /// </summary>
    public class TwinDragonsPearlWyvern : ModProjectile
    {
        #region 调参旋钮
        //节段顺序:头、身、腿、身、腿、身2、身3、尾;值为图集帧号(原版 NPC 87..92 依次排列)
        private static readonly int[] PartFrames = { 0, 2, 1, 2, 1, 3, 4, 5 };
        //图集单帧尺寸(像素):原版 76x156 画布裁去空列空行
        public const int FrameWidth = 58;
        public const int FrameHeight = 152;
        //旋转原点(像素):原版画布中心在裁后帧里的位置
        public static readonly Vector2 FrameOrigin = new Vector2(20f, 78f);
        //绘制缩放;节段间距(像素):原版 42 乘缩放
        public const float DrawScale = 0.85f;
        public const float SegmentGap = 42f * DrawScale;
        public const float HalfGap = SegmentGap / 2f;
        //弦长(像素,18 格)
        public const float ChordLength = 18f * 16f;
        //起点切线与弦的夹角(弧度),越大弧越弯
        public const float BendAngle = 0.45f;
        //穿过宝珠后再飞的弧长(像素,10 格)
        public const float OvershootLength = 10f * 16f;
        //头部沿弧线的速度(像素/帧)
        public const float HeadSpeed = 22f;
        //节段过起点后淡入、近终点淡出的弧长(像素)
        public const float FadeInLength = 40f;
        public const float FadeOutLength = 56f;
        //可见度高于此值的节段才判定伤害
        public const float HitVisibility = 0.5f;
        //碰撞线宽(像素);吻端在头节点前、尾尖在尾节点后的弧长(像素,按原版图量得乘缩放)
        public const float HitWidth = 26f;
        public const float SnoutReach = 64f;
        public const float TailReach = 62f;
        //耀斑灼烧时长(帧,2 秒)
        public const int FlareBurnTime = 120;
        //实体层不透明度;发光层强度与放大倍率;头部光晕半径(像素)
        public const float BodyOpacity = 0.8f;
        public const float GlowOpacity = 0.45f;
        public const float GlowScale = 1.08f;
        public const float HeadGlowRadius = 46f;
        //光照强度
        public const float LightStrength = 0.8f;
        //俯冲音效:音量、音高浮动、同时实例上限(每秒 6 只,压低免得吵)
        public const float DiveVolume = 0.3f;
        public const float DivePitchVariance = 0.25f;
        public const int DiveMaxInstances = 3;
        //命中火花数量与速度范围(像素/帧)、沿飞行方向的散开角(弧度)
        public const int HitSparkCount = 7;
        public const float HitSparkMinSpeed = 4f;
        public const float HitSparkMaxSpeed = 11f;
        public const float HitSparkSpread = 0.5f;
        //命中闪光的 Glow2 缩放(1 约合 100 像素半径)
        public const float HitFlashScale = 0.3f;
        //溶散金尘每节每帧的出现概率
        public const float DissolveDustChance = 0.3f;
        //屏外剔除余量(像素):整条弧离 Center 最远约 460;占位判定边长(像素),实际判定走 Colliding;每隔几节打一处光
        public const int CullFluff = 1200;
        public const int HitboxSize = 16;
        public const int LightStride = 3;
        //冲击环:起始缩放、出生裂口与穿珠时的最终缩放(环贴图边长 156 像素)、寿命(帧)
        public const float RingStartScale = 0.05f;
        public const float RiftRingScale = 0.35f;
        public const float PearlRingScale = 0.5f;
        public const int RingLife = 14;
        //火花缩放与寿命(帧),命中时稍大稍久;命中闪光寿命(帧)
        public const float SparkScale = 0.04f;
        public const int SparkLife = 12;
        public const float HitSparkScale = 0.05f;
        public const int HitSparkLife = 14;
        public const int HitFlashLife = 10;
        //头部金尘:散布半径(像素)、后飘速度(像素/帧)、缩放范围、寿命(帧)
        public const float GlitterRadius = 8f;
        public const float GlitterBack = 2f;
        public const float GlitterMinScale = 0.2f;
        public const float GlitterMaxScale = 0.35f;
        public const int GlitterLife = 14;
        //溶散金尘:散布半径(像素)、顺飞行方向的速度与随机扰动(像素/帧)、缩放范围、寿命(帧)
        public const float DissolveRadius = 14f;
        public const float DissolveDrift = 3f;
        public const float DissolveJitter = 1.5f;
        public const float DissolveMinScale = 0.2f;
        public const float DissolveMaxScale = 0.4f;
        public const int DissolveLife = 18;
        //光尘与闪光的不透明度;头部光晕不透明度;Glow2 可见半径占边长的比例
        public const float MoteOpacity = 0.9f;
        public const float HeadGlowOpacity = 0.5f;
        public const float GlowVisibleFrac = 0.4f;
        #endregion

        //圆弧半径(像素)、起点到宝珠的弧长(像素)、头部溶散处的弧长(像素)
        public static readonly float ArcRadius = ChordLength / (2f * MathF.Sin(BendAngle));
        public static readonly float ArcToPearl = 2f * BendAngle * ArcRadius;
        public static readonly float ArcEnd = ArcToPearl + OvershootLength;
        //尾节越过终点所需的帧数
        public static readonly int Lifetime = (int)MathF.Ceiling((ArcEnd + (PartFrames.Length - 1) * SegmentGap) / HeadSpeed) + 1;

        public static readonly Color GoldBody = new Color(255, 236, 190);
        public static readonly Color GoldGlow = new Color(255, 170, 50);
        public static readonly Color GoldSpark = new Color(255, 220, 120);
        //火花贴图的宽、长缩放比
        public static readonly Vector2 SparkSquash = new Vector2(0.4f, 1f);

        private int age;
        //每个 NPC 只出一次命中火花;纯表现,各端各自判定
        private readonly bool[] sparked = new bool[Main.maxNPCs];

        private float Chord => Projectile.ai[0];
        private float Bend => Projectile.ai[1] >= 0f ? 1f : -1f;
        private float HeadArc => age * HeadSpeed;
        private Vector2 Start => Projectile.Center - Chord.ToRotationVector2() * ChordLength;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = CullFluff;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanCutTiles() => false;

        /// <summary>弧长 s 处的位置与前进方向角;s = 0 是生成点,s = ArcToPearl 是宝珠</summary>
        private void Sample(float s, out Vector2 pos, out float heading) {
            float b = Bend;
            float h0 = Chord - b * BendAngle;
            heading = h0 + b * s / ArcRadius;
            pos = Start + new Vector2(MathF.Sin(heading) - MathF.Sin(h0), MathF.Cos(h0) - MathF.Cos(heading)) * (ArcRadius * b);
        }

        private static float Visibility(float s) {
            float fadeIn = MathHelper.Clamp(s / FadeInLength, 0f, 1f);
            float fadeOut = MathHelper.Clamp((ArcEnd - s) / FadeOutLength, 0f, 1f);
            return fadeIn * fadeOut;
        }

        public override void AI() {
            float prevHead = HeadArc;
            age++;
            float head = HeadArc;
            Sample(head, out Vector2 headPos, out float headHeading);
            Projectile.direction = MathF.Cos(headHeading) >= 0f ? 1 : -1;
            int last = PartFrames.Length - 1;
            for (int k = 0; k <= last; k += LightStride) {
                float s = head - k * SegmentGap;
                Sample(s, out Vector2 p, out _);
                Lighting.AddLight(p, GoldGlow.ToVector3() * LightStrength * Visibility(s));
            }
            if (Main.dedServ) {
                return;
            }
            Vector2 forward = headHeading.ToRotationVector2();
            if (age == 1) {
                SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Volume = DiveVolume, PitchVariance = DivePitchVariance, MaxInstances = DiveMaxInstances }, Start);
                Sample(0f, out Vector2 rift, out float riftHeading);
                PRTLoader.NewParticle<PRT_PulseRing>(rift, Vector2.Zero, GoldGlow, RingStartScale).Configure(RiftRingScale, RingLife);
                for (int i = 0; i < HitSparkCount; i++) {
                    Vector2 vel = riftHeading.ToRotationVector2().RotatedByRandom(HitSparkSpread) * Main.rand.NextFloat(HitSparkMinSpeed, HitSparkMaxSpeed);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(rift, vel, GoldSpark, SparkScale).Configure(false, SparkLife, SparkSquash, true);
                }
            }
            //头部穿过宝珠的一帧在宝珠处荡一圈金环
            if (prevHead < ArcToPearl && head >= ArcToPearl) {
                PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, GoldGlow, RingStartScale).Configure(PearlRingScale, RingLife);
            }
            float headVis = Visibility(head);
            if (headVis > 0f) {
                PRTLoader.NewParticle<PRT_Light>(headPos + CEUtils.randomPointInCircle(GlitterRadius), -forward * GlitterBack, GoldSpark * headVis, Main.rand.NextFloat(GlitterMinScale, GlitterMaxScale)).Configure(MoteOpacity, lifetime: GlitterLife);
            }
            for (int k = 0; k <= last; k++) {
                float s = head - k * SegmentGap;
                if (s < ArcEnd - FadeOutLength || s > ArcEnd || Main.rand.NextFloat() > DissolveDustChance) {
                    continue;
                }
                Sample(s, out Vector2 p, out float h);
                Vector2 drift = h.ToRotationVector2() * DissolveDrift + CEUtils.randomPointInCircle(DissolveJitter);
                PRTLoader.NewParticle<PRT_Light>(p + CEUtils.randomPointInCircle(DissolveRadius), drift, GoldSpark, Main.rand.NextFloat(DissolveMinScale, DissolveMaxScale)).Configure(MoteOpacity, lifetime: DissolveLife);
            }
            HitSparks(head, headVis);
        }

        /// <summary>头部擦过敌怪时沿飞行方向溅出金色火花;不走 OnHitNPC,另外的客户端也看得到</summary>
        private void HitSparks(float head, float headVis) {
            if (headVis < HitVisibility) {
                return;
            }
            Sample(head + SnoutReach, out Vector2 snout, out float heading);
            Sample(head, out Vector2 neck, out _);
            Vector2 forward = heading.ToRotationVector2();
            foreach (NPC npc in Main.ActiveNPCs) {
                if (sparked[npc.whoAmI] || npc.friendly || npc.dontTakeDamage) {
                    continue;
                }
                float point = 0f;
                if (!Collision.CheckAABBvLineCollision(npc.TopLeft, npc.Size, snout, neck, HitWidth, ref point)) {
                    continue;
                }
                sparked[npc.whoAmI] = true;
                for (int i = 0; i < HitSparkCount; i++) {
                    Vector2 vel = forward.RotatedByRandom(HitSparkSpread) * Main.rand.NextFloat(HitSparkMinSpeed, HitSparkMaxSpeed);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(npc.Center, vel, GoldSpark, HitSparkScale).Configure(false, HitSparkLife, SparkSquash, true);
                }
                PRTLoader.NewParticle<PRT_ShineParticle>(npc.Center, Vector2.Zero, GoldGlow, HitFlashScale).Configure(MoteOpacity, true, PRTDrawModeEnum.AdditiveBlend, 0f, HitFlashLife);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float head = HeadArc;
            int last = PartFrames.Length - 1;
            for (int k = 0; k <= last; k++) {
                float s = head - k * SegmentGap;
                if (Visibility(s) < HitVisibility) {
                    continue;
                }
                float front = k == 0 ? s + SnoutReach : s + HalfGap;
                float back = k == last ? s - TailReach : s - HalfGap;
                Sample(front, out Vector2 a, out _);
                Sample(back, out Vector2 b, out _);
                float point = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), a, b, HitWidth, ref point)) {
                    return true;
                }
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<FlareBurn>(), FlareBurnTime);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            //弦朝左时整条水平翻转,背鳍始终朝上;翻转只翻纹理坐标,原点要跟着镜像
            bool flip = Chord.ToRotationVector2().X < 0f;
            SpriteEffects fx = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = flip ? new Vector2(FrameWidth - FrameOrigin.X, FrameOrigin.Y) : FrameOrigin;
            float head = HeadArc;
            int last = PartFrames.Length - 1;
            for (int k = last; k >= 0; k--) {
                DrawPart(tex, k, head - k * SegmentGap, origin, fx, GoldBody * BodyOpacity, DrawScale);
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int k = last; k >= 0; k--) {
                DrawPart(tex, k, head - k * SegmentGap, origin, fx, GoldGlow * GlowOpacity, DrawScale * GlowScale);
            }
            Texture2D glow = CEExtraAssets.Glow2;
            float headVis = Visibility(head);
            if (glow != null && headVis > 0f) {
                Sample(head + HalfGap, out Vector2 hp, out _);
                Main.EntitySpriteDraw(glow, hp - Main.screenPosition, null, GoldGlow * (HeadGlowOpacity * headVis), 0f, glow.Size() / 2f, HeadGlowRadius / (glow.Width * GlowVisibleFrac), SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private void DrawPart(Texture2D tex, int k, float s, Vector2 origin, SpriteEffects fx, Color color, float scale) {
            float vis = Visibility(s);
            if (vis <= 0f) {
                return;
            }
            Sample(s, out Vector2 pos, out float heading);
            Rectangle frame = new Rectangle(0, PartFrames[k] * FrameHeight, FrameWidth, FrameHeight);
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color * vis, heading + MathHelper.PiOver2, origin, scale, fx);
        }
    }
}
