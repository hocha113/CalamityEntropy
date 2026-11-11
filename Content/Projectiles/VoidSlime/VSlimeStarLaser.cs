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
    /// 主序星的一轮放射激光,一发弹幕管整圈:白色预警线 1 秒 → 点亮 → 熄灭,只在点亮段有判定
    /// 激光从恒星表面起算,预警线的位置就是激光的位置
    /// ai[0] 基准角,ai[1] 本体下标,ai[2] 激光道数,localAI[0] 存活帧
    /// </summary>
    public class VSlimeStarLaser : VSlimeHostileProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        private float Age => Projectile.localAI[0];
        private int Count => Math.Max(1, (int)Projectile.ai[2]);
        /// <summary>激光起点到圆心的距离,略收进恒星表面</summary>
        private float innerRadius = VSlimeDirector.MainStarRadius;

        /// <summary>点亮程度 0..1,预警段为 0</summary>
        private float Ignite {
            get {
                float activeAge = Age - VSlimeDirector.StarLaserTelegraph;
                if (activeAge < 0f) {
                    return 0f;
                }
                return GodDraw.Span(activeAge, 0f, 4f)
                    * GodDraw.Span(activeAge, VSlimeDirector.StarLaserActive + VSlimeDirector.StarLaserFade, VSlimeDirector.StarLaserActive);
            }
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
                Projectile.timeLeft = VSlimeDirector.StarLaserTelegraph + VSlimeDirector.StarLaserActive + VSlimeDirector.StarLaserFade;
            }
            Projectile.localAI[0]++;
            VoidSlimeNPC boss = OwnerBoss(Projectile.ai[1]);
            if (boss == null) {
                Projectile.Kill();
                return;
            }
            Projectile.Center = boss.NPC.Center;
            Projectile.velocity = Vector2.Zero;
            innerRadius = Math.Max(60f, boss.StarRadius - 12f);
            if (Age == VSlimeDirector.StarLaserTelegraph) {
                VSlimeVfx.Cue("lasershoot", Projectile.Center, 1.3f, 0.6f, 2);
                VSlimeVfx.Shake(Projectile.Center, 4f, 2400f);
            }
        }

        /// <summary>目标相对圆心的方位折到最近的一道激光上,按垂距判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Ignite < 0.7f) {
                return false;
            }
            Vector2 offset = targetHitbox.Center.ToVector2() - Projectile.Center;
            float dist = offset.Length();
            float reach = Math.Max(targetHitbox.Width, targetHitbox.Height) * 0.5f;
            if (dist < innerRadius - reach || dist > VSlimeDirector.StarLaserLength + reach) {
                return false;
            }
            float step = MathHelper.TwoPi / Count;
            float relative = MathHelper.WrapAngle(offset.ToRotation() - Projectile.ai[0]);
            float nearest = MathF.Round(relative / step) * step + Projectile.ai[0];
            Vector2 dir = nearest.ToRotationVector2();
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center + dir * innerRadius,
                Projectile.Center + dir * VSlimeDirector.StarLaserLength, VSlimeDirector.StarLaserHitHalfWidth * 2f, ref point);
        }

        public override bool PreDraw(ref Color lightColor) {
            float ignite = Ignite;
            int telegraph = VSlimeDirector.StarLaserTelegraph;
            float step = MathHelper.TwoPi / Count;

            GodDraw.BeginAdditive();
            for (int i = 0; i < Count; i++) {
                Vector2 dir = (Projectile.ai[0] + step * i).ToRotationVector2();
                Vector2 from = Projectile.Center + dir * innerRadius;
                Vector2 to = Projectile.Center + dir * VSlimeDirector.StarLaserLength;
                if (Age < telegraph) {
                    float p = Age / telegraph;
                    float fadeIn = GodDraw.Span(Age, 0f, 8f);
                    float flicker = p > 0.8f ? 0.6f + 0.4f * MathF.Sin(Age * 1.7f) : 1f;
                    GodDraw.Line(from, to, 6f + 5f * p, GodDraw.Add(VSlimeVfx.StarBlue, 0.1f * fadeIn), 0.04f);
                    GodDraw.Line(from, to, 1.2f + 1.6f * p, GodDraw.Add(Color.White, (0.25f + 0.6f * p) * flicker * fadeIn), 0.04f);
                }
                else if (ignite > 0.01f) {
                    float half = VSlimeDirector.StarLaserDrawHalfWidth * ignite;
                    GodDraw.Line(from, to, half * 1.8f, GodDraw.Add(VSlimeVfx.StarDeepBlue, 0.55f * ignite), 0.05f);
                    GodDraw.Line(from, to, half, GodDraw.Add(VSlimeVfx.StarBlue, 0.95f * ignite), 0.05f);
                    GodDraw.Line(from, to, half * 0.4f, GodDraw.Add(VSlimeVfx.StarIce, ignite), 0.05f);
                }
            }
            GodDraw.End();
            return false;
        }
    }
}
