using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Sage
{
    /// <summary>
    /// 月刃,沿一条叶形回旋轨迹飞出又折回,起飞前整条轨迹先画出来
    /// 轨迹由出生点和出射角解析算出,各端一致
    /// ai[0] 出射角,ai[1] 卷向 ±1,ai[2] 起飞前的预告帧数,localAI[0] 存活帧
    /// </summary>
    public class SageCrescent : GodHostileProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override GodKind Kind => GodKind.Moon;

        private const int PathPoints = 40;
        /// <summary>月刃贴图缩到直径约 100px,旋转扫过的圆半径略大于判定半径</summary>
        private const float BladeScale = 0.2f;
        private static readonly Vector2[] pathBuffer = new Vector2[PathPoints + 1];

        private Vector2 origin;
        private bool initialized;

        private float Age => Projectile.localAI[0];
        private int Delay => (int)Projectile.ai[2];
        private float FlightAge => Age - Delay;
        private float Curl => Projectile.ai[1] < 0f ? -1f : 1f;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>起飞 t 帧后的位置:纵向走半个正弦到远端再回来,横向走一整个正弦,去程和回程分在两侧</summary>
        private Vector2 PathPoint(float t) {
            float tau = MathHelper.Clamp(t / SageDirector.CrescentFlightFrames, 0f, 1f);
            Vector2 dir = Projectile.ai[0].ToRotationVector2();
            Vector2 normal = dir.RotatedBy(MathHelper.PiOver2) * Curl;
            return origin + dir * (SageDirector.CrescentReach * MathF.Sin(MathHelper.Pi * tau))
                + normal * (SageDirector.CrescentBulge * MathF.Sin(MathHelper.TwoPi * tau));
        }

        public override void AI() {
            if (!initialized) {
                initialized = true;
                origin = Projectile.Center;
                //timeLeft 不过线,首帧各端自己定
                Projectile.timeLeft = Delay + SageDirector.CrescentFlightFrames;
            }
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
            if (FlightAge < 0f) {
                return;
            }
            if (FlightAge == 0f) {
                SageVfx.Cue("moonlightswordattack1", Projectile.Center, 1f, 0.6f, 3);
            }
            Projectile.Center = PathPoint(FlightAge);
            Projectile.rotation += 0.36f * Curl;
            Lighting.AddLight(Projectile.Center, 0.5f, 0.6f, 1.2f);
            if (!Main.dedServ && Main.rand.NextBool(2)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), DustID.MagicMirror, Main.rand.NextVector2Circular(1f, 1f), 100, default, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (FlightAge < 0f || Projectile.timeLeft < 6) {
                return false;
            }
            float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
            float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
            return Vector2.DistanceSquared(Projectile.Center, new Vector2(x, y)) <= SageDirector.CrescentHitRadius * SageDirector.CrescentHitRadius;
        }

        public override bool PreDraw(ref Color lightColor) {
            if (!initialized) {
                return false;
            }
            bool flying = FlightAge >= 0f;
            float appear = GodDraw.Span(Age, 0f, 10f) * GodDraw.Span(Projectile.timeLeft, 0f, 8f);
            Texture2D blade = CEUtils.getExtraTex("swordslash");

            GodDraw.BeginAdditive();
            //整条回旋轨迹:起飞前逐渐变亮,起飞后留一条淡线
            float p = flying ? 1f : Age / Math.Max(Delay, 1);
            float flicker = !flying && p > 0.8f ? 0.6f + 0.4f * MathF.Sin(Age * 1.7f) : 1f;
            float pathGlow = (flying ? 0.3f : 0.25f + 0.55f * p) * flicker * appear;
            for (int i = 0; i <= PathPoints; i++) {
                pathBuffer[i] = PathPoint(SageDirector.CrescentFlightFrames * i / (float)PathPoints);
            }
            GodDraw.Strip(GodDraw.SoftLine, pathBuffer, t => 9f, t => GodDraw.Add(SageVfx.StarViolet, 0.35f * pathGlow));
            GodDraw.Strip(GodDraw.SoftLine, pathBuffer, t => 2.5f, t => GodDraw.Add(SageVfx.StarBlue, pathGlow));

            if (flying) {
                //沿轨迹回溯的残影,和刃体同一张图
                for (int k = 5; k >= 1; k--) {
                    float t = 1f - k / 6f;
                    Vector2 at = PathPoint(FlightAge - k * 1.6f);
                    Main.spriteBatch.Draw(blade, at - Main.screenPosition, null, GodDraw.Add(SageVfx.StarBlue, 0.4f * t * appear),
                        Projectile.rotation - 0.36f * Curl * k * 1.6f, blade.Size() / 2f, BladeScale * (0.7f + 0.3f * t), SpriteEffects.None, 0f);
                }
                GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SageVfx.StarBlue, 0.55f * appear), 0f, 0.9f);
            }
            else {
                GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SageVfx.StarBlue, 0.4f * p * appear), 0f, 0.5f);
                GodDraw.Centered(GodDraw.ThinStar, Projectile.Center, GodDraw.Add(SageVfx.StarWhite, p * appear), Age * 0.1f, 0.3f);
            }
            GodDraw.End();

            if (flying) {
                //实体:暗色描边垫底,再压银白刃身,贴图是白底真透明,要进直通道批次
                GodDraw.BeginStraightAlpha();
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Main.spriteBatch.Draw(blade, pos, null, GodDraw.Straight(SageVfx.Rim, 0.9f * appear), Projectile.rotation, blade.Size() / 2f, BladeScale * 1.14f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(blade, pos, null, GodDraw.Straight(SageVfx.MoonSilver, appear), Projectile.rotation, blade.Size() / 2f, BladeScale, SpriteEffects.None, 0f);
                GodDraw.End();
            }
            return false;
        }
    }
}