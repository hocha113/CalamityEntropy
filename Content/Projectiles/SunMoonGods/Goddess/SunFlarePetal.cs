using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess
{
    /// <summary>
    /// 日珥花瓣,沿固定圆弧飞行并拖出会灼伤的火带,轨迹由出生点和出射角解析算出,各端一致
    /// ai[0] 出射角,ai[1] 卷向 ±1,ai[2] 起飞前的预告帧数,localAI[0] 存活帧
    /// </summary>
    public class SunFlarePetal : GodHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/BlazingSunlight";
        public override GodKind Kind => GodKind.Sun;
        public override int DebuffType => ModContent.BuffType<SunblazeBurn>();
        public override int DebuffTime => SunDirector.BurnFrames;

        /// <summary>预告弧线画出去的飞行帧数</summary>
        private const int TelegraphSpan = 84;
        /// <summary>寿命末尾火带收回头部用的帧数</summary>
        private const int RetractFrames = 24;
        /// <summary>火带从头部起算,只有这个比例的长度参与判定</summary>
        private const float HitTrailRatio = 0.6f;

        private readonly List<Vector2> trail = new();
        private Vector2 origin;
        private bool initialized;

        private float Age => Projectile.localAI[0];
        private int Delay => (int)Projectile.ai[2];
        private float FlightAge => Age - Delay;
        private float Curl => Projectile.ai[1] < 0f ? -1f : 1f;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 5;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.scale = 1.15f;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>起飞 t 帧后的位置:航向按 卷向×速度/半径 匀速转,积分得过出生点的圆</summary>
        private Vector2 PathPoint(float t) {
            float a = Projectile.ai[0];
            float angle = a + Curl * SunDirector.BloomPetalSpeed / SunDirector.BloomPetalRadius * t;
            return origin + Curl * SunDirector.BloomPetalRadius * new Vector2(MathF.Sin(angle) - MathF.Sin(a), MathF.Cos(a) - MathF.Cos(angle));
        }

        public override void AI() {
            if (!initialized) {
                initialized = true;
                origin = Projectile.Center;
                //timeLeft 不过线,首帧各端自己定
                Projectile.timeLeft = Delay + SunDirector.BloomPetalFlyFrames;
            }
            Projectile.localAI[0]++;
            if (++Projectile.frameCounter >= 5) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Projectile.rotation += 0.06f * Curl;

            trail.Clear();
            if (FlightAge < 0f) {
                return;
            }
            Projectile.Center = PathPoint(FlightAge);
            //寿命末尾火带收回头部,不在消失那一帧整条断掉
            int points = (int)(SunDirector.BloomTrailPoints * MathHelper.Clamp(Projectile.timeLeft / (float)RetractFrames, 0f, 1f));
            for (int j = 0; j <= points; j++) {
                float t = FlightAge - j;
                if (t < 0f) {
                    break;
                }
                trail.Add(PathPoint(t));
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.6f, 0.2f);
            if (!Main.dedServ && trail.Count > 2 && Main.rand.NextBool(2)) {
                Vector2 at = trail[Main.rand.Next(trail.Count)];
                Dust d = Dust.NewDustPerfect(at + Main.rand.NextVector2Circular(8f, 8f), DustID.SolarFlare, Main.rand.NextVector2Circular(1f, 1f), 100, default, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (FlightAge < 0f) {
                return false;
            }
            if (projHitbox.Intersects(targetHitbox)) {
                return true;
            }
            //火带尾段收细到比判定还窄,那一段不判定
            int hitCount = (int)(trail.Count * HitTrailRatio);
            for (int i = 2; i < hitCount; i += 2) {
                if (CEUtils.LineThroughRect(trail[i - 2], trail[i], targetHitbox, (int)(SunDirector.BloomHitHalfWidth * 2f))) {
                    return true;
                }
            }
            return false;
        }

        public override void OnKill(int timeLeft) {
            SunVfx.Embers(Projectile.Center, 8, 2f, 5f);
        }

        public override bool PreDraw(ref Color lightColor) {
            if (!initialized) {
                return false;
            }
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            float fadeOut = GodDraw.Span(Projectile.timeLeft, 0f, 10f);

            if (FlightAge < 0f) {
                DrawTelegraph(tex, frame);
                return false;
            }

            if (trail.Count >= 2) {
                float scroll = Age * 0.03f;
                //火流贴图的亮体只占贴图高度的五分之一,条带要给到可见宽度的五倍
                //暗红实体带垫底,亮背景上也有轮廓
                GodDraw.BeginStraightAlpha();
                GodDraw.Strip(GodDraw.FireStreak, trail,
                    t => MathHelper.Lerp(56f, 18f, t),
                    t => GodDraw.Straight(SunVfx.Crimson, 0.85f * (1f - t * t)),
                    scroll, scroll + 1.3f);
                GodDraw.End();

                GodDraw.BeginAdditive();
                //软边光带是火带的主体,亮体宽度盖住判定宽
                GodDraw.Strip(GodDraw.SoftLine, trail,
                    t => MathHelper.Lerp(22f, 12f, t),
                    t => GodDraw.Add(SunVfx.Ember, 0.85f * (1f - t * t)));
                GodDraw.Strip(GodDraw.FireStreak, trail,
                    t => MathHelper.Lerp(44f, 12f, t),
                    t => GodDraw.Add(SunVfx.Gold, 0.9f * (1f - t)),
                    scroll * 1.6f, scroll * 1.6f + 1.3f);
                GodDraw.Strip(GodDraw.SoftLine, trail,
                    t => MathHelper.Lerp(5f, 1f, t),
                    t => GodDraw.Add(SunVfx.Core, 0.8f * (1f - t)));
                GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SunVfx.Ember, 0.6f * fadeOut), 0f, 0.5f);
                GodDraw.End();
            }

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * fadeOut, Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        /// <summary>起飞前沿将要飞的弧画一条细线,越接近起飞越亮</summary>
        private void DrawTelegraph(Texture2D tex, Rectangle frame) {
            float p = MathHelper.Clamp(Age / Math.Max(Delay, 1), 0f, 1f);
            float flicker = p > 0.8f ? 0.6f + 0.4f * MathF.Sin(Age * 1.7f) : 1f;
            trail.Clear();
            for (int t = 0; t <= TelegraphSpan; t += 4) {
                trail.Add(PathPoint(t));
            }
            GodDraw.BeginAdditive();
            GodDraw.Strip(GodDraw.SoftLine, trail,
                t => 2f + 2.5f * p,
                t => GodDraw.Add(SunVfx.Gold, (0.18f + 0.5f * p) * flicker * (1f - t)));
            GodDraw.Centered(GodDraw.Glow, origin, GodDraw.Add(SunVfx.Ember, 0.5f * p), 0f, 0.3f + 0.2f * p);
            GodDraw.End();
            trail.Clear();

            Main.EntitySpriteDraw(tex, origin - Main.screenPosition, frame, Color.White * p, Projectile.rotation, frame.Size() / 2f, Projectile.scale * (0.5f + 0.5f * p), SpriteEffects.None, 0);
        }
    }
}