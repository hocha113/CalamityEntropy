using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess
{
    /// <summary>
    /// 日冕光芒,根部跟着本体:细线预告 → 点亮并旋转 → 收细熄灭,只在点亮段有判定
    /// ai[0] 基准角,ai[1] 本体下标,ai[2] 模式与转向(±1 日冕之舞,±2 炎阳天轮),localAI[0] 存活帧
    /// </summary>
    public class SunCoronaRay : GodHostileProjectile
    {
        public const float ModeDance = 1f;
        public const float ModeStorm = 2f;
        /// <summary>光芒根部离本体中心的距离,这一段不画也不判定</summary>
        private const float RootGap = 36f;

        public override string Texture => CEUtils.WhiteTexPath;
        public override GodKind Kind => GodKind.Sun;
        public override int DebuffType => ModContent.BuffType<SunblazeBurn>();
        public override int DebuffTime => SunDirector.BurnFrames;

        private float Age => Projectile.localAI[0];
        private bool Storm => Math.Abs(Projectile.ai[2]) >= ModeStorm;
        private int SpinDir => Projectile.ai[2] < 0f ? -1 : 1;
        private int TelegraphFrames => Storm ? SunDirector.StormWindup - SunDirector.StormRayBeat : SunDirector.RayTelegraphFrames;
        private int ActiveFrames => Storm ? SunDirector.StormActiveFrames : SunDirector.RayActiveFrames;
        private float Length => Storm ? SunDirector.StormRayLength : SunDirector.RayLength;

        /// <summary>点亮程度 0..1,预告段为 0,点亮前 6 帧爬满,熄灭段收回</summary>
        private float Ignite {
            get {
                float activeAge = Age - TelegraphFrames;
                if (activeAge < 0f) {
                    return 0f;
                }
                return GodDraw.Span(activeAge, 0f, 6f) * GodDraw.Span(activeAge, ActiveFrames + SunDirector.RayFadeFrames, ActiveFrames);
            }
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
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
                Projectile.timeLeft = TelegraphFrames + ActiveFrames + SunDirector.RayFadeFrames;
            }
            Projectile.localAI[0]++;
            NPC owner = OwnerNpc<SunGoddess>(Projectile.ai[1]);
            if (owner == null) {
                Projectile.Kill();
                return;
            }
            Projectile.Center = owner.Center + SunGoddess.HaloOffset;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.ai[0] + SpinDir * Sweep(Age - TelegraphFrames);

            float ignite = Ignite;
            if (ignite > 0.2f) {
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                for (float d = 120f; d < Length; d += 160f) {
                    Lighting.AddLight(Projectile.Center + dir * d, 1f * ignite, 0.65f * ignite, 0.25f * ignite);
                }
            }
        }

        /// <summary>点亮后累计转过的角度,预告段恒为 0,预告线就是起转位置</summary>
        private float Sweep(float activeAge) {
            if (activeAge <= 0f) {
                return 0f;
            }
            if (!Storm) {
                return SunDirector.RaySweep * GodDraw.Smooth(activeAge / ActiveFrames);
            }
            float spinUp = SunDirector.StormSpinUp;
            return activeAge < spinUp
                ? SunDirector.StormAngularSpeed * activeAge * activeAge / (2f * spinUp)
                : SunDirector.StormAngularSpeed * (activeAge - spinUp * 0.5f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Ignite < 0.7f) {
                return false;
            }
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center + dir * RootGap,
                Projectile.Center + dir * (Length * 0.96f), SunDirector.RayHitHalfWidth * 2f, ref point);
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
                GodDraw.Line(from, to, 10f + 8f * p, GodDraw.Add(SunVfx.Ember, 0.1f * fadeIn), 0.06f);
                GodDraw.Line(from, to, 1.5f + 2.5f * p, GodDraw.Add(SunVfx.Gold, (0.2f + 0.55f * p) * flicker * fadeIn), 0.06f);
            }
            else if (ignite > 0.01f) {
                float half = SunDirector.RayDrawHalfWidth * ignite;
                float pulse = 0.92f + 0.08f * MathF.Sin(Age * 0.5f + Projectile.whoAmI);
                GodDraw.Line(from, to, half * 1.7f, GodDraw.Add(SunVfx.Crimson, 0.55f * ignite), 0.1f);
                GodDraw.Line(from, to, half * pulse, GodDraw.Add(SunVfx.Ember, 0.95f * ignite), 0.1f);
                GodDraw.Line(from, to, half * 0.42f, GodDraw.Add(SunVfx.Core, ignite), 0.1f);

                //火流纹理沿光芒向外滚,两端收细
                float scroll = -Age * 0.035f;
                float repeat = Length / 300f;
                Vector2[] line = { from, Vector2.Lerp(from, to, 0.12f), Vector2.Lerp(from, to, 0.88f), to };
                GodDraw.Strip(GodDraw.FireStreak, line,
                    t => half * 1.25f * MathF.Sin(MathHelper.Clamp(t, 0.02f, 0.98f) * MathHelper.Pi) + 2f,
                    t => GodDraw.Add(SunVfx.Gold, 0.7f * ignite),
                    scroll, scroll + repeat);

                GodDraw.Centered(GodDraw.Flash, from, GodDraw.Add(SunVfx.Gold, 0.5f * ignite), Age * 0.03f, 0.42f * ignite);
            }
            GodDraw.End();
            return false;
        }
    }
}