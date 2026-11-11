using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Conqueror
{
    /// <summary>
    /// 禁戒能量球,每刺两发,绕同一条轴线反相摆动成双螺旋
    /// position 与 velocity 是轴线上的点和速度,球体画在轴线旁的偏移处,判定盒随之平移,物块碰撞仍按轴线算
    /// ai[0] 螺旋侧 ±1,ai[1] 已飞帧数,ai[2] 已命中次数(拥有者累加,其余端据此补命中星芒)
    /// </summary>
    public class SandseaConquerorOrb : ModProjectile
    {
        public const float DamageMult = 0.5f;           //伤害占面板的比例,策划原文
        public const float Speed = 12f;                 //轴向速度,像素/帧
        private const int Life = 54;                    //寿命,帧,射程约 648 像素
        private const int MaxHits = 4;                  //穿透 3 次即可命中 4 次,策划原文
        private const int HitCooldown = 30;             //局部无敌帧,策划原文
        private const float PierceDecay = 0.8f;         //每命中一次伤害乘 0.8,策划原文 20% 衰减
        private const int BodySize = 24;                //判定盒边长,像素,比暗芯加亮环略宽,本体贴图 26
        private const int TileSize = 8;                 //物块碰撞盒边长,像素,取小让贴地平刺不蹭地
        private const float HelixAmplitude = 14f;       //摆幅,像素
        private const float HelixPeriod = 14f;          //绕轴一周的帧数
        private const float HelixRamp = 8f;             //摆幅从 0 放开到满的帧数
        private const float DepthScale = 0.12f;         //近大远小的幅度
        private const int FadeFrames = 8;               //末段淡出帧数
        private const int DamageCutoff = 4;             //剩余帧数不多于它时不再伤人
        private const float SpinSpeed = 0.1f;           //日珥自转,弧度/帧
        private const float BodySpinRatio = 0.35f;      //本体自转相对日珥的比例,方向相反
        private const int DustChance = 3;               //每帧 1/3 概率掉一粒沙尘
        private const float DustSpread = 6f;            //沙尘出生范围,像素
        private const float DustDrag = 0.15f;           //沙尘初速占球速的比例,向后
        private const float DustScaleMin = 0.8f;
        private const float DustScaleMax = 1.2f;
        private const int RibbonSamples = 18;           //光带采样数
        private const float RibbonStep = 0.5f;          //相邻采样相隔的帧数,共 9 帧约 108 像素
        private const float RibbonAlpha = 0.5f;
        private const float RibbonScaleTail = 0.07f;    //Glow2 贴图 256 像素,尾端约 18 像素
        private const float RibbonScaleHead = 0.2f;     //头端约 51 像素
        private const int GhostCount = 4;               //日珥残影数
        private const float GhostStep = 2f;             //相邻残影相隔的帧数
        private const float GhostAlpha = 0.55f;
        private const float GhostScaleTail = 0.6f;      //残影最小也有本体的六成
        private const float GhostScaleHead = 0.92f;
        private const float GlowAlpha = 0.5f;
        private const float GlowScale = 0.34f;          //Glow2 贴图 256 像素,约 87 像素的晕
        private const string FlareSuffix = "Flare";     //日珥贴图文件名后缀,SandseaConquerorOrbFlare.png
        private static readonly Color RibbonColor = new Color(255, 140, 50, 170);   //A 留大半,亮背景上仍是橙而不发白
        private static readonly Color GhostColor = new Color(255, 200, 140, 150);
        private static readonly Color GlowColor = new Color(255, 150, 60, 90);
        private static readonly Vector3 LightColor = new Vector3(0.9f, 0.5f, 0.15f);

        private int seenHits;   //本端已播过星芒的命中次数

        private float Side => Projectile.ai[0] < 0f ? -1f : 1f;

        public override void SetDefaults() {
            Projectile.width = BodySize;
            Projectile.height = BodySize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = MaxHits;
            Projectile.timeLeft = Life;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.noEnchantmentVisuals = true;
        }

        /// <summary>某一帧球体相对轴线的偏移,depth 为正表示转到靠近观察者的一侧;只依赖 ai 与速度,各端算出来一样</summary>
        private Vector2 HelixOffset(float age, out float depth) {
            float phase = age * MathHelper.TwoPi / HelixPeriod;
            float amplitude = HelixAmplitude * Utils.GetLerpValue(0f, HelixRamp, age, true);
            Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            depth = Side * MathF.Cos(phase);
            return normal * (Side * amplitude * MathF.Sin(phase));
        }

        public override void AI() {
            Projectile.ai[1]++;
            Vector2 center = Projectile.Center + HelixOffset(Projectile.ai[1], out float depth);
            //远侧的球排进「弹幕之后」的绘制队列,交叉处被近侧那发压住
            Projectile.hide = depth < 0f;
            Projectile.rotation += SpinSpeed * Side;
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);
            Projectile.EmitEnchantmentVisualsAt(center - Projectile.Size / 2f, Projectile.width, Projectile.height);
            Lighting.AddLight(center, LightColor * Projectile.Opacity);

            int hits = (int)Projectile.ai[2];
            if (hits > seenHits) {
                seenHits = hits;
                SandseaConquerorVfx.OrbHitStar(center, Projectile.velocity);
            }
            if (!Main.dedServ && Main.rand.NextBool(DustChance)) {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(DustSpread, DustSpread), DustID.Sandnado, -Projectile.velocity * DustDrag, 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
                dust.noGravity = true;
            }
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            Vector2 offset = HelixOffset(Projectile.ai[1], out _);
            hitbox.Offset((int)offset.X, (int)offset.Y);
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
            width = TileSize;
            height = TileSize;
            return true;
        }

        public override bool? CanDamage() {
            return Projectile.timeLeft > DamageCutoff ? null : false;
        }

        /// <summary>
        /// 命中钩子只在拥有者端跑,ai[2] 过网后其余端在 AI 或 OnKill 里补星芒
        /// 立刻发同步包而不等 netUpdate,最后一次命中当帧就会消失,等不到下一次发包;衰减照原版剑气,先结算再降
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Vector2 center = Projectile.Center + HelixOffset(Projectile.ai[1], out _);
            Rectangle box = target.Hitbox;
            Vector2 point = new Vector2(MathHelper.Clamp(center.X, box.Left, box.Right), MathHelper.Clamp(center.Y, box.Top, box.Bottom));
            SandseaConquerorVfx.OrbHitStar(point, Projectile.velocity);
            Projectile.ai[2]++;
            seenHits = (int)Projectile.ai[2];
            Projectile.damage = (int)(Projectile.damage * PierceDecay);
            CEUtils.SyncProj(Projectile.whoAmI);
        }

        public override void OnKill(int timeLeft) {
            Vector2 center = Projectile.Center + HelixOffset(Projectile.ai[1], out _);
            int hits = (int)Projectile.ai[2];
            if (hits > seenHits) {
                seenHits = hits;
                SandseaConquerorVfx.OrbHitStar(center, Projectile.velocity);
            }
            //自然到期的已经淡没,撞物块或打满命中次数的才播消散
            if (timeLeft > FadeFrames) {
                SandseaConquerorVfx.OrbPop(center);
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            if (Projectile.hide) {
                behindProjectiles.Add(index);
            }
        }

        /// <summary>光带、日珥残影、光晕、日珥、本体依次画在默认批次里,加色靠颜色的低 A 值,不切批次</summary>
        public override bool PreDraw(ref Color lightColor) {
            float opacity = Projectile.Opacity;
            if (opacity <= 0f) {
                return false;
            }
            Texture2D body = TextureAssets.Projectile[Type].Value;
            Texture2D flare = this.getTextureAlt(FlareSuffix);
            Texture2D glow = CEExtraAssets.Glow2;
            float age = Projectile.ai[1];
            float spin = Projectile.rotation;
            Vector2 axis = Projectile.Center - Main.screenPosition;

            for (int i = RibbonSamples; i >= 1; i--) {
                float back = i * RibbonStep;
                if (age < back) {
                    continue;
                }
                Vector2 offset = HelixOffset(age - back, out float depth);
                float t = 1f - i / (float)(RibbonSamples + 1);
                float scale = (1f + DepthScale * depth) * MathHelper.Lerp(RibbonScaleTail, RibbonScaleHead, t) * Projectile.scale;
                Main.EntitySpriteDraw(glow, axis - Projectile.velocity * back + offset, null, RibbonColor * (t * RibbonAlpha * opacity), 0f, glow.Size() / 2f, scale, SpriteEffects.None);
            }
            for (int i = GhostCount; i >= 1; i--) {
                float back = i * GhostStep;
                if (age < back) {
                    continue;
                }
                Vector2 offset = HelixOffset(age - back, out float depth);
                float t = 1f - i / (float)(GhostCount + 1);
                float scale = (1f + DepthScale * depth) * MathHelper.Lerp(GhostScaleTail, GhostScaleHead, t) * Projectile.scale;
                Main.EntitySpriteDraw(flare, axis - Projectile.velocity * back + offset, null, GhostColor * (t * GhostAlpha * opacity), back * SpinSpeed * Side - spin, flare.Size() / 2f, scale, SpriteEffects.None);
            }

            Vector2 drawPos = axis + HelixOffset(age, out float nowDepth);
            float bodyScale = (1f + DepthScale * nowDepth) * Projectile.scale;
            Main.EntitySpriteDraw(glow, drawPos, null, GlowColor * (GlowAlpha * opacity), 0f, glow.Size() / 2f, GlowScale * bodyScale, SpriteEffects.None);
            Main.EntitySpriteDraw(flare, drawPos, null, Color.White * opacity, -spin, flare.Size() / 2f, bodyScale, SpriteEffects.None);
            Main.EntitySpriteDraw(body, drawPos, null, Color.White * opacity, spin * BodySpinRatio, body.Size() / 2f, bodyScale, SpriteEffects.None);
            return false;
        }
    }
}
