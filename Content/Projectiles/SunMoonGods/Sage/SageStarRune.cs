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
    /// 星符,出生即锁向,航线预告结束后沿线直飞,贴图用星月套装的月牙印记
    /// ai[0] 朝向角,ai[1] 预告帧数(0 用默认),localAI[0] 存活帧
    /// </summary>
    public class SageStarRune : GodHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Armor/Starmoon/StarmoonMark";
        public override GodKind Kind => GodKind.Moon;

        /// <summary>预告线画出去的长度</summary>
        private const float LaneLength = 1500f;
        private const float DrawScale = 1.5f;

        private float Age => Projectile.localAI[0];
        private int Telegraph => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : SageDirector.RingTelegraph;
        private bool Flying => Age >= Telegraph;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1800;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 22;
            Projectile.height = 22;
        }

        public override void AI() {
            //timeLeft 不过线,首帧各端自己定
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = Telegraph + SageDirector.RingFlyFrames;
            }
            Projectile.localAI[0]++;
            Vector2 dir = Projectile.ai[0].ToRotationVector2();
            //月牙尖朝上的贴图,转到航向上
            Projectile.rotation = Projectile.ai[0] + MathHelper.PiOver2;
            if (!Flying) {
                Projectile.velocity = Vector2.Zero;
                return;
            }
            if (Age == Telegraph) {
                SageVfx.Cue("moonlightswordattack0", Projectile.Center, 1.2f, 0.45f, 4);
            }
            Projectile.velocity = dir * SageDirector.RingSpeed;
            Lighting.AddLight(Projectile.Center, 0.4f, 0.5f, 1f);
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center - dir * Main.rand.NextFloat(0f, 24f), DustID.MagicMirror, -dir * 1.5f, 100, default, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Flying) {
                return false;
            }
            float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
            float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
            return Vector2.DistanceSquared(Projectile.Center, new Vector2(x, y)) <= SageDirector.RuneHitRadius * SageDirector.RuneHitRadius;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 pivot = tex.Size() / 2f;
            Vector2 dir = Projectile.ai[0].ToRotationVector2();
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float appear = GodDraw.Span(Age, 0f, 8f) * GodDraw.Span(Projectile.timeLeft, 0f, 12f);
            if (appear <= 0.01f) {
                return false;
            }

            GodDraw.BeginAdditive();
            if (!Flying) {
                //航线预告随进度变亮,末段闪烁
                float p = Age / Telegraph;
                float flicker = p > 0.75f ? 0.6f + 0.4f * MathF.Sin(Age * 1.6f) : 1f;
                Vector2 to = Projectile.Center + dir * LaneLength;
                GodDraw.Line(Projectile.Center, to, 9f + 6f * p, GodDraw.Add(SageVfx.StarViolet, 0.14f * appear), 0.06f);
                GodDraw.Line(Projectile.Center, to, 1.5f + 3f * p, GodDraw.Add(SageVfx.StarBlue, (0.25f + 0.6f * p) * flicker * appear), 0.06f);
            }
            else {
                for (int k = 5; k >= 1; k--) {
                    float t = 1f - k / 6f;
                    Main.spriteBatch.Draw(tex, pos - dir * (15f * k), null, GodDraw.Add(SageVfx.StarBlue, 0.5f * t * appear), Projectile.rotation, pivot,
                        DrawScale * (0.6f + 0.4f * t), SpriteEffects.None, 0f);
                }
                GodDraw.Centered(GodDraw.Spindle, Projectile.Center - dir * 30f, GodDraw.Add(SageVfx.StarBlue, 0.5f * appear), Projectile.rotation, new Vector2(0.45f, 1.5f));
            }
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SageVfx.StarBlue, 0.5f * appear), 0f, 0.42f);
            GodDraw.End();

            float scale = DrawScale * (Flying ? 1f : MathHelper.Lerp(0.6f, 1f, GodDraw.Span(Age, 0f, 10f)));
            Main.spriteBatch.Draw(tex, pos, null, Color.White * appear, Projectile.rotation, pivot, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}