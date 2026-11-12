using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SpacetimeRift
{
    /// <summary>
    /// 裂隙箭首次命中时的引力爆炸,只由主人端生成;开头两帧结算圆形范围伤害,之后只播向内坍缩的动画
    /// ai[0]=1 表示这支箭瞬移过 ai[1]、ai[2]=瞬移起点,爆炸顺带画一道从起点到这里的裂隙 localAI[0]=已存活帧数
    /// </summary>
    public class SpacetimeRiftBlast : ModProjectile
    {
        //伤害只在开头几帧结算(帧)
        private const int DamageFrames = 2;
        //判定半径 7 格(像素)
        private const float Radius = 112f;
        //坍缩动画时长(帧),倒数第 2 帧收拢成一点闪光
        private const int Lifetime = 16;
        private const int PopFrame = Lifetime - 2;
        //HollowCircleSoftEdge 原图 2048 像素,圆环外缘半径 1024 像素;坍缩环从判定半径缩到 0.1 倍,实体层与加色金边的不透明度
        private const float RingRadiusAtOne = 1024f;
        private const float RingEnd = 0.1f;
        private const float RingOpacity = 0.75f;
        private const float RingGlowOpacity = 0.5f;
        //Glow2 原图 256 像素,缩放 1 时柔光团的可见半径约 44 像素;环内紫色暗场的不透明度
        private const float GlowRadiusAtOne = 44f;
        private const float FieldOpacity = 0.35f;
        //吸入的光点(GlowSpark 原图 2048 像素): 数量、出生在判定半径的多少倍上、存活帧数、大小
        //PRT_GlowSparkCal 每帧把速度乘 0.95,初速按存活帧数内刚好飘到中心反推
        private const int MoteCount = 8;
        private const float MoteStart = 0.95f;
        private const int MoteLife = 12;
        private const float MoteScale = 0.03f;
        private const float SparkDrag = 0.95f;
        //收拢闪光(Glow2)的紫色外晕与白金核心大小、存活帧数
        private const float PopScale = 0.5f;
        private const float PopCoreScale = 0.22f;
        private const int PopLife = 8;
        //裂隙: 折线段数、折点偏离直线的最大距离(像素)、外层与内层宽度(像素)、持续帧数
        private const int RiftSegments = 7;
        private const float RiftJag = 10f;
        private const float RiftWidth = 10f;
        private const float RiftCoreWidth = 3f;
        private const int RiftLife = 14;
        //爆炸声用虚空史莱姆引力聚变的冲击声;每秒可能十来下,压低音量、音高随机、同时至多 3 个
        private const float SoundVolume = 0.32f;
        private const float SoundPitchMin = 1.2f;
        private const float SoundPitchMax = 1.4f;
        private const int SoundInstances = 3;

        //起点裂口光(Glow2)沿裂隙方向压扁的缩放
        private static readonly Vector2 TearScale = new(0.08f, 0.3f);
        private static readonly Vector2 MoteSquash = new(0.4f, 1.4f);
        private static readonly Color VoidPurple = new(91, 35, 159);
        private static readonly Color RiftPurple = new(136, 35, 159);
        private static readonly Color RiftGold = new(255, 214, 120);
        private static readonly Color CoreWhite = new(255, 246, 220);
        private static readonly Vector3 BlastLight = new(0.55f, 0.3f, 0.8f);

        //裂隙折点的偏移,第一帧各端各自随机,只管好看
        private float[] riftJag;

        private bool HasRift => Projectile.ai[0] == 1f;

        private Vector2 RiftStart => new(Projectile.ai[1], Projectile.ai[2]);

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() {
            if (Projectile.localAI[0] > DamageFrames)
                return false;
            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => EGlobalProjectile.CircleIntersectsRectangle(Projectile.Center, Radius, targetHitbox);

        public override bool CanHitPvp(Player target) => false;

        public override bool? CanCutTiles() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f && !Main.dedServ)
                Burst();
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == PopFrame && !Main.dedServ) {
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, RiftPurple, PopScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, PopLife);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, CoreWhite, PopCoreScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, PopLife);
            }
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, BlastLight * (Projectile.timeLeft / (float)Lifetime));
        }

        private void Burst() {
            Vector2 center = Projectile.Center;
            CEUtils.PlaySound("energyImpact", Main.rand.NextFloat(SoundPitchMin, SoundPitchMax), center, SoundInstances, SoundVolume);
            float speed = Radius * MoteStart * (1f - SparkDrag) / (1f - MathF.Pow(SparkDrag, MoteLife));
            float start = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < MoteCount; i++) {
                Vector2 direction = (start + MathHelper.TwoPi * i / MoteCount).ToRotationVector2();
                Color color = i % 2 == 0 ? RiftGold : RiftPurple;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(center + direction * (Radius * MoteStart), -direction * speed, color, MoteScale).Configure(false, MoteLife, MoteSquash);
            }
            riftJag = new float[RiftSegments];
            for (int i = 1; i < RiftSegments; i++)
                riftJag[i] = Main.rand.NextFloat(-RiftJag, RiftJag);
        }

        public override bool PreDraw(ref Color lightColor) {
            float t = Projectile.localAI[0] / Lifetime;
            float collapse = t * t;
            Vector2 center = Projectile.Center - Main.screenPosition;
            float ringRadius = Radius * MathHelper.Lerp(1f, RingEnd, collapse);
            Texture2D glow = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(glow, center, null, VoidPurple * (FieldOpacity * (1f - t)), 0f, glow.Size() / 2f, ringRadius / GlowRadiusAtOne, SpriteEffects.None);
            Texture2D ring = CEExtraAssets.HollowCircleSoftEdge;
            float ringScale = ringRadius / RingRadiusAtOne;
            Main.EntitySpriteDraw(ring, center, null, Color.Lerp(RiftPurple, RiftGold, collapse) * RingOpacity, 0f, ring.Size() / 2f, ringScale, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, center, null, (RiftGold with { A = 0 }) * (RingGlowOpacity * collapse), 0f, ring.Size() / 2f, ringScale, SpriteEffects.None);
            if (HasRift)
                DrawRift();
            return false;
        }

        private void DrawRift() {
            float life = Projectile.localAI[0] / RiftLife;
            if (life >= 1f || riftJag == null)
                return;
            float fade = 1f - life;
            Vector2 start = RiftStart;
            Vector2 end = Projectile.Center;
            Vector2 normal = (end - start).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            Vector2 previous = start;
            for (int i = 1; i <= RiftSegments; i++) {
                Vector2 point = Vector2.Lerp(start, end, i / (float)RiftSegments);
                if (i < RiftSegments)
                    point += normal * riftJag[i];
                CEUtils.drawLine(previous, point, (RiftPurple with { A = 0 }) * fade, RiftWidth * fade);
                CEUtils.drawLine(previous, point, RiftGold * fade, RiftCoreWidth * fade);
                previous = point;
            }
            Texture2D glow = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(glow, start - Main.screenPosition, null, (RiftGold with { A = 0 }) * fade, (end - start).ToRotation(), glow.Size() / 2f, TearScale * (1f + life), SpriteEffects.None);
        }
    }
}
