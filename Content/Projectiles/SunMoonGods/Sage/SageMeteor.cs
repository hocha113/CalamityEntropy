using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Sage
{
    /// <summary>
    /// 流星,出生即锁向,航线预告结束后沿线坠落,弹体是像素彗星
    /// ai[0] 朝向角,ai[1] 预告帧数(0 用默认),localAI[0] 存活帧
    /// </summary>
    public class SageMeteor : GodHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/StarTrail";
        public override GodKind Kind => GodKind.Moon;

        /// <summary>预告线画出去的长度,等于流星的全部航程</summary>
        private const float LaneLength = SageDirector.MeteorSpeed * SageDirector.MeteorFlyFrames;
        /// <summary>彗头中心在贴图里的像素坐标,贴图朝右</summary>
        private static readonly Vector2 HeadPivot = new Vector2(74f, 69f);
        private const float DrawScale = 1.7f;

        private float Age => Projectile.localAI[0];
        private int Telegraph => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : SageDirector.MeteorTelegraph;
        private bool Flying => Age >= Telegraph;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 28;
            Projectile.height = 28;
        }

        public override void AI() {
            //timeLeft 不过线,首帧各端自己定
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = Telegraph + SageDirector.MeteorFlyFrames;
            }
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.ai[0];
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            if (!Flying) {
                Projectile.velocity = Vector2.Zero;
                return;
            }
            if (Age == Telegraph) {
                SageVfx.Cue("moonlighthit0", Projectile.Center, 0.9f, 0.4f, 4);
            }
            Projectile.velocity = dir * SageDirector.MeteorSpeed;
            Lighting.AddLight(Projectile.Center, 0.4f, 0.6f, 1.2f);
            if (!Main.dedServ) {
                Dust d = Dust.NewDustPerfect(Projectile.Center - dir * Main.rand.NextFloat(0f, 50f) + Main.rand.NextVector2Circular(8f, 8f), DustID.MagicMirror, -dir * 2f, 100, default,
                    Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Flying) {
                return false;
            }
            float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
            float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
            return Vector2.DistanceSquared(Projectile.Center, new Vector2(x, y)) <= SageDirector.MeteorHitRadius * SageDirector.MeteorHitRadius;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float appear = GodDraw.Span(Age, 0f, 10f) * GodDraw.Span(Projectile.timeLeft, 0f, 10f);
            if (appear <= 0.01f) {
                return false;
            }

            GodDraw.BeginAdditive();
            if (!Flying) {
                //航线预告随进度变粗变亮,末段闪烁
                float p = Age / Telegraph;
                float flicker = p > 0.82f ? 0.6f + 0.4f * MathF.Sin(Age * 1.6f) : 1f;
                Vector2 to = Projectile.Center + dir * LaneLength;
                GodDraw.Line(Projectile.Center, to, 14f + 8f * p, GodDraw.Add(SageVfx.StarViolet, 0.13f * appear), 0.06f);
                GodDraw.Line(Projectile.Center, to, 2f + 3.5f * p, GodDraw.Add(SageVfx.StarBlue, (0.22f + 0.55f * p) * flicker * appear), 0.06f);
                GodDraw.Centered(GodDraw.ThinStar, Projectile.Center, GodDraw.Add(SageVfx.StarWhite, (0.4f + 0.6f * p) * appear), Age * 0.08f, 0.2f + 0.2f * p);
            }
            else {
                //拖尾:细长光带加同一张彗星图的残影,宽度不低于彗头的一半
                GodDraw.Line(Projectile.Center - dir * 260f, Projectile.Center, 15f, GodDraw.Add(SageVfx.StarBlue, 0.5f * appear), 0.45f);
                GodDraw.Line(Projectile.Center - dir * 170f, Projectile.Center, 7f, GodDraw.Add(SageVfx.StarWhite, 0.7f * appear), 0.45f);
                for (int k = 3; k >= 1; k--) {
                    float t = 1f - k / 4f;
                    Main.spriteBatch.Draw(tex, pos - dir * (26f * k), null, GodDraw.Add(SageVfx.StarBlue, 0.45f * t * appear), Projectile.rotation, HeadPivot,
                        DrawScale * (0.75f + 0.25f * t), SpriteEffects.None, 0f);
                }
                GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SageVfx.StarBlue, 0.6f * appear), 0f, 0.6f);
            }
            GodDraw.End();

            if (Flying) {
                Main.spriteBatch.Draw(tex, pos, null, Color.White * appear, Projectile.rotation, HeadPivot, DrawScale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}