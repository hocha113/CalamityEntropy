using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Sage
{
    /// <summary>
    /// 十字星爆:原地亮出十字标记,预告走完沿四向爆成星芒,只在爆开段有判定
    /// 星芒两端收尖,判定只取中段,藏在可见亮体里
    /// ai[0] 十字的朝向角,ai[1] 预告帧数(0 用默认),localAI[0] 存活帧
    /// </summary>
    public class SageCrossBurst : GodHostileProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override GodKind Kind => GodKind.Moon;

        /// <summary>判定取臂长的这个比例,再往外可见体已经细过判定宽</summary>
        private const float HitLengthRatio = 0.75f;
        private static readonly Vector2[] armPoints = new Vector2[9];

        private float Age => Projectile.localAI[0];
        private int Telegraph => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : SageDirector.CrossTelegraph;

        /// <summary>爆开程度 0..1,预告段为 0,爆开前 4 帧爬满,燄灭段收回</summary>
        private float Ignite {
            get {
                float activeAge = Age - Telegraph;
                if (activeAge < 0f) {
                    return 0f;
                }
                return GodDraw.Span(activeAge, 0f, 4f) * GodDraw.Span(activeAge, SageDirector.CrossActiveFrames + SageDirector.CrossFadeFrames, SageDirector.CrossActiveFrames);
            }
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            //timeLeft 不过线,首帧各端自己定
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = Telegraph + SageDirector.CrossActiveFrames + SageDirector.CrossFadeFrames;
            }
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.ai[0];
            if (Age == Telegraph) {
                SageVfx.Burst(Projectile.Center, 1.5f);
                SageVfx.Stardust(Projectile.Center, 10, 3f, 9f);
                SageVfx.Shake(Projectile.Center, 3f, 1600f);
                SageVfx.Cue("truemoonlighthit", Projectile.Center, 1.1f, 0.5f, 3);
            }
            float ignite = Ignite;
            if (ignite > 0.2f) {
                Lighting.AddLight(Projectile.Center, 0.6f * ignite, 0.7f * ignite, 1.4f * ignite);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Ignite < 0.7f) {
                return false;
            }
            float reach = SageDirector.CrossArmLength * HitLengthRatio;
            float point = 0f;
            for (int k = 0; k < 2; k++) {
                Vector2 arm = (Projectile.rotation + MathHelper.PiOver2 * k).ToRotationVector2() * reach;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - arm, Projectile.Center + arm,
                    SageDirector.CrossHitHalfWidth * 2f, ref point)) {
                    return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            float ignite = Ignite;
            float time = Main.GlobalTimeWrappedHourly;
            GodDraw.BeginAdditive();
            if (Age < Telegraph) {
                float p = Age / Telegraph;
                float fadeIn = GodDraw.Span(Age, 0f, 8f);
                float flicker = p > 0.8f ? 0.6f + 0.4f * MathF.Sin(Age * 1.7f) : 1f;
                for (int k = 0; k < 2; k++) {
                    Vector2 arm = (Projectile.rotation + MathHelper.PiOver2 * k).ToRotationVector2() * SageDirector.CrossArmLength;
                    GodDraw.Line(Projectile.Center - arm, Projectile.Center + arm, 8f + 6f * p, GodDraw.Add(SageVfx.StarViolet, 0.12f * fadeIn), 0.2f);
                    GodDraw.Line(Projectile.Center - arm, Projectile.Center + arm, 1.5f + 2f * p, GodDraw.Add(SageVfx.StarBlue, (0.2f + 0.6f * p) * flicker * fadeIn), 0.2f);
                }
                //中心的星核随预告收紧变亮
                GodDraw.Centered(GodDraw.BloomRing, Projectile.Center, GodDraw.Add(SageVfx.StarBlue, (0.3f + 0.6f * p) * fadeIn), 0f, GodDraw.RingScale(MathHelper.Lerp(46f, 14f, p)));
                GodDraw.Centered(GodDraw.Star, Projectile.Center, GodDraw.Add(SageVfx.StarWhite, (0.3f + 0.7f * p) * fadeIn), Projectile.rotation, 0.12f + 0.16f * p);
            }
            else if (ignite > 0.01f) {
                float half = SageDirector.CrossDrawHalfWidth * ignite;
                for (int k = 0; k < 2; k++) {
                    Vector2 arm = (Projectile.rotation + MathHelper.PiOver2 * k).ToRotationVector2() * SageDirector.CrossArmLength;
                    for (int i = 0; i < armPoints.Length; i++) {
                        armPoints[i] = Projectile.Center + arm * (i / (float)(armPoints.Length - 1) * 2f - 1f);
                    }
                    GodDraw.Strip(GodDraw.SoftLine, armPoints, t => half * 2.4f * Taper(t) + 1f, t => GodDraw.Add(SageVfx.StarViolet, 0.5f * ignite));
                    GodDraw.Strip(GodDraw.SoftLine, armPoints, t => half * 1.4f * Taper(t) + 1f, t => GodDraw.Add(SageVfx.StarBlue, 0.95f * ignite));
                    GodDraw.Strip(GodDraw.SoftLine, armPoints, t => half * 0.6f * Taper(t) + 0.5f, t => GodDraw.Add(SageVfx.StarWhite, ignite));
                }
                GodDraw.Centered(GodDraw.Flash, Projectile.Center, GodDraw.Add(SageVfx.StarBlue, 0.7f * ignite), time * 0.5f, 0.6f * ignite);
                GodDraw.Centered(GodDraw.Star, Projectile.Center, GodDraw.Add(SageVfx.StarWhite, ignite), Projectile.rotation, 0.5f * ignite);
            }
            GodDraw.End();
            return false;
        }

        /// <summary>沿臂的粗细:中段饱满,过了四分之三处才迅速收尖</summary>
        private static float Taper(float t) {
            float f = MathF.Abs(t * 2f - 1f);
            return 1f - f * f * f * f;
        }
    }
}