using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Sage
{
    /// <summary>
    /// 满月光炮,根部跟着本体的嘴:预告画出起始线和整片扫掠扇面 → 点亮停顿 → 匀速扫过扇面 → 收细燄灭
    /// 只在点亮段有判定,扫掠的起止和方向在生成时就定死
    /// ai[0] 起始角,ai[1] 本体下标,ai[2] 扫掠方向 ±1,localAI[0] 存活帧
    /// </summary>
    public class SageMoonBeam : GodHostileProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override GodKind Kind => GodKind.Moon;

        private float Age => Projectile.localAI[0];
        private int SweepDir => Projectile.ai[2] < 0f ? -1 : 1;
        private static float TotalSweep => SageDirector.BeamAngularSpeed * SageDirector.BeamSweepFrames;

        /// <summary>点亮程度 0..1,预告段为 0,点亮前 6 帧爬满,燄灭段收回</summary>
        private float Ignite {
            get {
                float activeAge = Age - SageDirector.BeamTelegraph;
                if (activeAge < 0f) {
                    return 0f;
                }
                int active = SageDirector.BeamStaticFrames + SageDirector.BeamSweepFrames;
                return GodDraw.Span(activeAge, 0f, 6f) * GodDraw.Span(activeAge, active + SageDirector.BeamFadeFrames, active);
            }
        }

        /// <summary>生成 age 帧后光柱的朝向,状态拿它让本体的头跟着转</summary>
        public static float AngleAt(float startAngle, int sweepDir, float age) {
            float sweepAge = MathHelper.Clamp(age - SageDirector.BeamTelegraph - SageDirector.BeamStaticFrames, 0f, SageDirector.BeamSweepFrames);
            return startAngle + sweepDir * SageDirector.BeamAngularSpeed * sweepAge;
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3200;
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
                Projectile.timeLeft = SageDirector.BeamLife;
                SageVfx.Cue("lasercharge", Projectile.Center, 1f, 0.8f, 2);
            }
            Projectile.localAI[0]++;
            NPC owner = OwnerNpc<StarmoonSage>(Projectile.ai[1]);
            if (owner == null) {
                Projectile.Kill();
                return;
            }
            Projectile.rotation = AngleAt(Projectile.ai[0], SweepDir, Age);
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Projectile.Center = owner.Center + dir * SageDirector.MuzzleDistance;
            Projectile.velocity = Vector2.Zero;

            if (Age == SageDirector.BeamTelegraph) {
                SageVfx.Burst(Projectile.Center, 2.4f);
                SageVfx.Shake(Projectile.Center, 9f);
                SageVfx.Flash(Projectile.Center, 0.25f);
                SageVfx.SkyFlash(1f);
                SageVfx.Cue("lasershoot", Projectile.Center, 0.8f, 1f, 2);
            }
            float ignite = Ignite;
            if (ignite > 0.2f) {
                for (float d = 100f; d < SageDirector.BeamLength; d += 200f) {
                    Lighting.AddLight(Projectile.Center + dir * d, 0.6f * ignite, 0.75f * ignite, 1.5f * ignite);
                }
                if (!Main.dedServ) {
                    if (Age % 6f == 0f) {
                        SageVfx.Shake(Projectile.Center, 2.5f * ignite, 2400f);
                    }
                    for (int i = 0; i < 3; i++) {
                        Vector2 at = Projectile.Center + dir * Main.rand.NextFloat(40f, 1500f) + dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-50f, 50f);
                        Dust dust = Dust.NewDustPerfect(at, DustID.MagicMirror, dir * Main.rand.NextFloat(4f, 10f), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
                        dust.noGravity = true;
                    }
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Ignite < 0.7f) {
                return false;
            }
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + dir * (SageDirector.BeamLength * 0.98f), SageDirector.BeamHitHalfWidth * 2f, ref point);
        }

        public override bool PreDraw(ref Color lightColor) {
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 from = Projectile.Center;
            Vector2 to = from + dir * SageDirector.BeamLength;
            float ignite = Ignite;
            float time = Main.GlobalTimeWrappedHourly;

            GodDraw.BeginAdditive();
            if (Age < SageDirector.BeamTelegraph) {
                float p = Age / SageDirector.BeamTelegraph;
                float fadeIn = GodDraw.Span(Age, 0f, 10f);
                float flicker = p > 0.8f ? 0.6f + 0.4f * MathF.Sin(Age * 1.7f) : 1f;
                //扇面:从起始线到扫掠终点的整片区域,终点再画一条细线
                float sectorStart = SweepDir > 0 ? Projectile.ai[0] : Projectile.ai[0] - TotalSweep;
                float reach = SageDirector.BeamLength * 0.5f;
                GodDraw.RingStrip(GodDraw.SoftLine, from, reach * 0.5f, reach * 0.5f, GodDraw.Add(SageVfx.StarViolet, 0.22f * fadeIn), sectorStart, TotalSweep, 24);
                Vector2 endDir = (Projectile.ai[0] + SweepDir * TotalSweep).ToRotationVector2();
                GodDraw.Line(from, from + endDir * SageDirector.BeamLength, 2f, GodDraw.Add(SageVfx.StarViolet, 0.45f * fadeIn), 0.04f);
                //起始线:外宽带标出光柱的实际宽度
                GodDraw.Line(from, to, SageDirector.BeamDrawHalfWidth, GodDraw.Add(SageVfx.StarBlue, (0.1f + 0.14f * p) * fadeIn), 0.04f);
                GodDraw.Line(from, to, 2.5f + 4f * p, GodDraw.Add(SageVfx.MoonSilver, (0.3f + 0.6f * p) * flicker * fadeIn), 0.04f);
                GodDraw.Centered(GodDraw.Glow, from, GodDraw.Add(SageVfx.StarBlue, 0.7f * p * fadeIn), 0f, 0.6f + 0.8f * p);
                GodDraw.Centered(GodDraw.Star, from, GodDraw.Add(SageVfx.StarWhite, p * fadeIn), time * 2f, 0.2f + 0.3f * p);
            }
            else if (ignite > 0.01f) {
                float half = SageDirector.BeamDrawHalfWidth * ignite;
                float pulse = 0.94f + 0.06f * MathF.Sin(Age * 0.6f);
                GodDraw.Line(from, to, half * 1.8f, GodDraw.Add(SageVfx.StarViolet, 0.5f * ignite), 0.03f);
                GodDraw.Line(from, to, half * 1.15f * pulse, GodDraw.Add(SageVfx.StarBlue, 0.95f * ignite), 0.03f);
                GodDraw.Line(from, to, half * 0.55f, GodDraw.Add(SageVfx.StarWhite, ignite), 0.03f);

                //能流纹理沿光柱向外滚
                float scroll = -Age * 0.05f;
                float repeat = SageDirector.BeamLength / 420f;
                Vector2[] line = { from, Vector2.Lerp(from, to, 0.03f), Vector2.Lerp(from, to, 0.97f), to };
                GodDraw.Strip(GodDraw.FireStreak, line, t => half * 1.1f, t => GodDraw.Add(SageVfx.MoonSilver, 0.6f * ignite), scroll, scroll + repeat);

                GodDraw.Centered(GodDraw.Flash, from, GodDraw.Add(SageVfx.StarBlue, 0.8f * ignite), time, 1.1f * ignite);
                GodDraw.Centered(GodDraw.Star, from, GodDraw.Add(SageVfx.StarWhite, ignite), -time * 1.5f, 0.9f * ignite);
                GodDraw.Centered(GodDraw.Glow, from, GodDraw.Add(SageVfx.StarBlue, 0.8f * ignite), 0f, 2.2f);
            }
            GodDraw.End();
            return false;
        }
    }
}