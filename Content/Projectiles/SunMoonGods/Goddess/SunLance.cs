using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess
{
    /// <summary>
    /// 日矛,出生即锁向,航线预告结束后沿线直刺,贴图用原版以太长枪
    /// ai[0] 朝向角,ai[1] 预告帧数(0 用默认),localAI[0] 存活帧
    /// </summary>
    public class SunLance : GodHostileProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FairyQueenLance;
        public override GodKind Kind => GodKind.Sun;
        public override int DebuffType => ModContent.BuffType<SunblazeBurn>();
        public override int DebuffTime => SunDirector.BurnFrames;

        /// <summary>预告线画出去的长度</summary>
        private const float LaneLength = 1700f;

        private float Age => Projectile.localAI[0];
        private int Telegraph => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : SunDirector.LanceTelegraphFrames;
        private bool Flying => Age >= Telegraph;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 24;
            Projectile.height = 24;
        }

        public override void AI() {
            //timeLeft 不过线,首帧各端自己定
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = Telegraph + SunDirector.LanceFlyFrames;
            }
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.ai[0];
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            if (!Flying) {
                Projectile.velocity = Vector2.Zero;
                return;
            }
            Projectile.velocity = dir * SunDirector.LanceSpeed;
            Lighting.AddLight(Projectile.Center, 1f, 0.7f, 0.25f);
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center - dir * Main.rand.NextFloat(0f, 40f), DustID.SolarFlare, -dir * 2f, 100, default, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Flying) {
                return false;
            }
            Vector2 half = Projectile.rotation.ToRotationVector2() * (SunDirector.LanceHitLength * 0.5f);
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - half, Projectile.Center + half,
                SunDirector.LanceHitHalfWidth * 2f, ref point);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float appear = GodDraw.Span(Age, 0f, 14f) * GodDraw.Span(Projectile.timeLeft, 0f, 12f);
            if (appear <= 0.01f) {
                return false;
            }

            GodDraw.BeginAdditive();
            if (!Flying) {
                //航线预告随进度变粗变亮,末段闪烁
                float p = Age / Telegraph;
                float flicker = p > 0.82f ? 0.6f + 0.4f * MathF.Sin(Age * 1.6f) : 1f;
                Vector2 from = Projectile.Center - dir * 60f;
                Vector2 to = Projectile.Center + dir * LaneLength;
                GodDraw.Line(from, to, 12f + 8f * p, GodDraw.Add(SunVfx.Ember, 0.14f * appear), 0.08f);
                GodDraw.Line(from, to, 2f + 4f * p, GodDraw.Add(SunVfx.Gold, (0.25f + 0.55f * p) * flicker * appear), 0.08f);
            }
            else {
                for (int k = 5; k >= 1; k--) {
                    float t = 1f - k / 6f;
                    Main.spriteBatch.Draw(tex, pos - dir * (24f * k), null, GodDraw.Add(SunVfx.Ember, 0.5f * t * appear), Projectile.rotation, origin,
                        Projectile.scale * (0.7f + 0.3f * t), SpriteEffects.None, 0f);
                }
            }
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SunVfx.Ember, 0.45f * appear), 0f, 0.5f);
            GodDraw.End();

            //实体:四向暗红描边垫底,再压金白枪身
            float scale = Projectile.scale * (Flying ? 1f : MathHelper.Lerp(0.7f, 1f, GodDraw.Span(Age, Telegraph - 6f, Telegraph)));
            for (int i = 0; i < 4; i++) {
                Vector2 offset = (MathHelper.PiOver2 * i + Projectile.rotation).ToRotationVector2() * 2f;
                Main.spriteBatch.Draw(tex, pos + offset, null, SunVfx.Crimson * appear, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(tex, pos, null, new Color(255, 236, 176) * appear, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}