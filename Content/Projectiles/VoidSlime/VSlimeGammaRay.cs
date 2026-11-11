using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using System;
using Terraria;
using Terraria.ID;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 伽玛射线,根部跟着本体:细线预告 → 点亮 → 收细熄灭,只在点亮段有判定
    /// ai[0] 模式(0 朝天的紫色射线,1 顺时针扫一圈的蓝色射线),ai[1] 本体下标,ai[2] 基准角,localAI[0] 存活帧
    /// </summary>
    public class VSlimeGammaRay : VSlimeHostileProjectile
    {
        public const float ModeSky = 0f;
        public const float ModePulsar = 1f;
        /// <summary>射线根部离本体中心的距离,这一段不画也不判定</summary>
        private const float RootGap = 60f;

        public override string Texture => CEUtils.WhiteTexPath;

        private float Age => Projectile.localAI[0];
        private bool Pulsar => Projectile.ai[0] >= ModePulsar;
        private int TelegraphFrames => Pulsar ? VSlimeDirector.DevourBeamCharge : VSlimeDirector.RainChargeFrames;
        private int ActiveFrames => Pulsar ? VSlimeDirector.DevourBeamFrames : VSlimeDirector.SkyRayActiveFrames;
        private float Length => Pulsar ? VSlimeDirector.DevourBeamLength : VSlimeDirector.SkyRayLength;
        private float HitHalfWidth => Pulsar ? VSlimeDirector.BlueRayHitHalfWidth : VSlimeDirector.SkyRayHitHalfWidth;
        private float DrawHalfWidth => Pulsar ? VSlimeDirector.BlueRayDrawHalfWidth : VSlimeDirector.SkyRayDrawHalfWidth;
        private Color Outer => Pulsar ? VSlimeVfx.StarDeepBlue : VSlimeVfx.VoidPurple;
        private Color Body => Pulsar ? VSlimeVfx.StarBlue : VSlimeVfx.VoidBright;
        private Color Core => Pulsar ? VSlimeVfx.StarIce : VSlimeVfx.StarCore;

        /// <summary>点亮程度 0..1,预告段为 0,点亮前 6 帧爬满,熄灭段收回</summary>
        private float Ignite {
            get {
                float activeAge = Age - TelegraphFrames;
                if (activeAge < 0f) {
                    return 0f;
                }
                return GodDraw.Span(activeAge, 0f, 6f) * GodDraw.Span(activeAge, ActiveFrames + VSlimeDirector.SkyRayFadeFrames, ActiveFrames);
            }
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
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
                Projectile.timeLeft = TelegraphFrames + ActiveFrames + VSlimeDirector.SkyRayFadeFrames;
            }
            Projectile.localAI[0]++;
            VoidSlimeNPC boss = OwnerBoss(Projectile.ai[1]);
            if (boss == null) {
                Projectile.Kill();
                return;
            }
            Projectile.Center = boss.NPC.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.ai[2] + Sweep(Age - TelegraphFrames);

            float ignite = Ignite;
            if (ignite > 0.2f) {
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                Vector3 light = Body.ToVector3() * ignite;
                for (float d = 120f; d < Length; d += 180f) {
                    Lighting.AddLight(Projectile.Center + dir * d, light);
                }
            }
            if (Age == TelegraphFrames) {
                VSlimeVfx.Cue("void_laser", Projectile.Center, Pulsar ? 1.2f : 0.9f, 0.8f, 2);
                VSlimeVfx.Shake(Projectile.Center, 5f, 2400f);
            }
        }

        /// <summary>点亮后累计转过的角度,预告段恒为 0,预告线就是起转位置;屏幕坐标里角度递增是顺时针</summary>
        private float Sweep(float activeAge) {
            if (!Pulsar || activeAge <= 0f) {
                return 0f;
            }
            float ramp = VSlimeDirector.DevourBeamRamp;
            float omega = MathHelper.TwoPi / (VSlimeDirector.DevourBeamFrames - ramp * 0.5f);
            activeAge = Math.Min(activeAge, VSlimeDirector.DevourBeamFrames);
            return activeAge < ramp
                ? omega * activeAge * activeAge / (2f * ramp)
                : omega * (activeAge - ramp * 0.5f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Ignite < 0.7f) {
                return false;
            }
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center + dir * RootGap,
                Projectile.Center + dir * (Length * 0.97f), HitHalfWidth * 2f, ref point);
        }

        public override bool PreDraw(ref Color lightColor) {
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 from = Projectile.Center + dir * RootGap;
            Vector2 to = Projectile.Center + dir * Length;
            float ignite = Ignite;

            GodDraw.BeginAdditive();
            if (Age < TelegraphFrames) {
                float p = Age / TelegraphFrames;
                float flicker = p > 0.8f ? 0.6f + 0.4f * MathF.Sin(Age * 1.7f) : 1f;
                float fadeIn = GodDraw.Span(Age, 0f, 8f);
                GodDraw.Line(from, to, 12f + 10f * p, GodDraw.Add(Outer, 0.12f * fadeIn), 0.06f);
                GodDraw.Line(from, to, 1.5f + 2.5f * p, GodDraw.Add(Body, (0.2f + 0.6f * p) * flicker * fadeIn), 0.06f);
                if (Pulsar) {
                    DrawSpinHint(p * fadeIn);
                }
            }
            else if (ignite > 0.01f) {
                float half = DrawHalfWidth * ignite;
                float pulse = 0.92f + 0.08f * MathF.Sin(Age * 0.5f + Projectile.whoAmI);
                GodDraw.Line(from, to, half * 1.8f, GodDraw.Add(Outer, 0.6f * ignite), 0.08f);
                GodDraw.Line(from, to, half * pulse, GodDraw.Add(Body, 0.95f * ignite), 0.08f);
                GodDraw.Line(from, to, half * 0.4f, GodDraw.Add(Core, ignite), 0.08f);

                //能流纹理沿射线向外滚,两端收细
                float scroll = -Age * 0.04f;
                float repeat = Length / 320f;
                Vector2[] line = { from, Vector2.Lerp(from, to, 0.1f), Vector2.Lerp(from, to, 0.9f), to };
                GodDraw.Strip(GodDraw.FireStreak, line,
                    t => half * 1.2f * MathF.Sin(MathHelper.Clamp(t, 0.02f, 0.98f) * MathHelper.Pi) + 2f,
                    t => GodDraw.Add(Body, 0.6f * ignite),
                    scroll, scroll + repeat);
                GodDraw.Centered(GodDraw.Flash, from, GodDraw.Add(Body, 0.6f * ignite), Age * 0.03f, 0.5f * ignite);
            }
            GodDraw.End();
            return false;
        }

        /// <summary>预告线前方的一串渐淡短划,标出顺时针的扫向</summary>
        private void DrawSpinHint(float strength) {
            const int Ticks = 5;
            for (int i = 1; i <= Ticks; i++) {
                float angle = Projectile.rotation + 0.07f * i;
                Vector2 dir = angle.ToRotationVector2();
                float fade = 1f - i / (float)(Ticks + 1);
                GodDraw.Line(Projectile.Center + dir * 260f, Projectile.Center + dir * 420f, 2f, GodDraw.Add(Body, 0.5f * fade * strength), 0.3f);
            }
        }
    }
}
