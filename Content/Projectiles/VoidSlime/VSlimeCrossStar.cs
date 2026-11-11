using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 十字星爆炸:从背景深处逼近,这段只有落点标记;到达玩家层的那一拍沿四向爆成星芒,只在爆开段有判定
    /// 星芒两端收尖,判定只取中段,藏在可见亮体里
    /// ai[0] 十字的朝向角,ai[1] 逼近帧数(0 用默认),localAI[0] 存活帧
    /// </summary>
    public class VSlimeCrossStar : VSlimeHostileProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        private static readonly Vector2[] armPoints = new Vector2[9];

        private float Age => Projectile.localAI[0];
        private int Approach => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : VSlimeDirector.CrossApproachFrames;

        /// <summary>爆开程度 0..1,逼近段为 0,爆开前 3 帧爬满,熄灭段收回</summary>
        private float Ignite {
            get {
                float activeAge = Age - Approach;
                if (activeAge < 0f) {
                    return 0f;
                }
                return GodDraw.Span(activeAge, 0f, 3f) * GodDraw.Span(activeAge, VSlimeDirector.CrossActiveFrames + VSlimeDirector.CrossFadeFrames, VSlimeDirector.CrossActiveFrames);
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
                Projectile.timeLeft = Approach + VSlimeDirector.CrossActiveFrames + VSlimeDirector.CrossFadeFrames;
            }
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.ai[0];
            if (Age == Approach) {
                VSlimeVfx.StarBurst(Projectile.Center, 1.5f);
                VSlimeVfx.Scatter(Projectile.Center, DustID.BlueTorch, 10, 3f, 9f);
                VSlimeVfx.Shake(Projectile.Center, 3f, 1600f);
                VSlimeVfx.Cue("truemoonlighthit", Projectile.Center, 1.2f, 0.45f, 3);
            }
            float ignite = Ignite;
            if (ignite > 0.2f) {
                Lighting.AddLight(Projectile.Center, 0.5f * ignite, 0.75f * ignite, 1.4f * ignite);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Ignite < 0.7f) {
                return false;
            }
            float reach = VSlimeDirector.CrossArmLength * VSlimeDirector.CrossHitLengthRatio;
            float point = 0f;
            for (int k = 0; k < 2; k++) {
                Vector2 arm = (Projectile.rotation + MathHelper.PiOver2 * k).ToRotationVector2() * reach;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - arm, Projectile.Center + arm,
                    VSlimeDirector.CrossHitHalfWidth * 2f, ref point)) {
                    return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            float ignite = Ignite;
            float time = Main.GlobalTimeWrappedHourly;
            GodDraw.BeginAdditive();
            if (Age < Approach) {
                float p = Age / Approach;
                float fadeIn = GodDraw.Span(Age, 0f, 8f);
                float flicker = p > 0.8f ? 0.6f + 0.4f * MathF.Sin(Age * 1.7f) : 1f;
                //落点标记:四臂细线标出爆开后的范围,外环随逼近收紧
                for (int k = 0; k < 2; k++) {
                    Vector2 arm = (Projectile.rotation + MathHelper.PiOver2 * k).ToRotationVector2() * VSlimeDirector.CrossArmLength;
                    GodDraw.Line(Projectile.Center - arm, Projectile.Center + arm, 7f + 5f * p, GodDraw.Add(VSlimeVfx.StarDeepBlue, 0.14f * fadeIn), 0.2f);
                    GodDraw.Line(Projectile.Center - arm, Projectile.Center + arm, 1.5f + 2f * p, GodDraw.Add(VSlimeVfx.StarBlue, (0.2f + 0.6f * p) * flicker * fadeIn), 0.2f);
                }
                GodDraw.Centered(GodDraw.BloomRing, Projectile.Center, GodDraw.Add(VSlimeVfx.StarBlue, (0.3f + 0.6f * p) * fadeIn), 0f, GodDraw.RingScale(MathHelper.Lerp(54f, 14f, p)));
                //远处的星点由小变大,读作从深处靠近
                float near = VSlimeDirector.EaseIn(p);
                GodDraw.Centered(GodDraw.Star, Projectile.Center, GodDraw.Add(VSlimeVfx.StarIce, (0.25f + 0.75f * p) * fadeIn), Projectile.rotation, 0.06f + 0.26f * near);
                GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(VSlimeVfx.StarBlue, 0.4f * p * fadeIn), 0f, 0.2f + 0.5f * near);
            }
            else if (ignite > 0.01f) {
                float half = VSlimeDirector.CrossDrawHalfWidth * ignite;
                for (int k = 0; k < 2; k++) {
                    Vector2 arm = (Projectile.rotation + MathHelper.PiOver2 * k).ToRotationVector2() * VSlimeDirector.CrossArmLength;
                    for (int i = 0; i < armPoints.Length; i++) {
                        armPoints[i] = Projectile.Center + arm * (i / (float)(armPoints.Length - 1) * 2f - 1f);
                    }
                    GodDraw.Strip(GodDraw.SoftLine, armPoints, t => half * 2.4f * Taper(t) + 1f, t => GodDraw.Add(VSlimeVfx.StarDeepBlue, 0.55f * ignite));
                    GodDraw.Strip(GodDraw.SoftLine, armPoints, t => half * 1.4f * Taper(t) + 1f, t => GodDraw.Add(VSlimeVfx.StarBlue, 0.95f * ignite));
                    GodDraw.Strip(GodDraw.SoftLine, armPoints, t => half * 0.6f * Taper(t) + 0.5f, t => GodDraw.Add(VSlimeVfx.StarIce, ignite));
                }
                GodDraw.Centered(GodDraw.Flash, Projectile.Center, GodDraw.Add(VSlimeVfx.StarBlue, 0.7f * ignite), time * 0.5f, 0.6f * ignite);
                GodDraw.Centered(GodDraw.Star, Projectile.Center, GodDraw.Add(VSlimeVfx.StarIce, ignite), Projectile.rotation, 0.5f * ignite);
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
