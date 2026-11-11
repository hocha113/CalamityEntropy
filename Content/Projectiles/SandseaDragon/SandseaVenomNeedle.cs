using CalamityEntropy.Content.NPCs.SandseaDragon;
using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SandseaDragon
{
    /// <summary>
    /// 毒针:出生时就锁定方向,先悬停蓄势 20 帧,再沿锁定方向从慢到快直飞,碰物块消失,不追踪
    /// 初速度的方向就是飞行方向;localAI[0] 存活帧,localAI[2] 是否已经飞到过空处
    /// 贴图尖端朝右上,绘制旋转补 45 度
    /// </summary>
    public class SandseaVenomNeedle : SandseaHostileProjectile
    {
        public override int DebuffType => BuffID.Venom;
        public override int DebuffTime => SandseaDirector.VenomFrames;
        public override int DefaultTimeLeft => SandseaDirector.NeedleLife;

        private float Age => Projectile.localAI[0];
        private bool Charging => Age <= SandseaDirector.NeedleChargeFrames;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 14;
            Projectile.height = 14;
        }

        public override bool ShouldUpdatePosition() => !Charging;

        public override void AI() {
            //timeLeft 不过线,首帧各端自己定
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = SandseaDirector.NeedleLife;
                SandseaVfx.Sound(SoundID.Item17 with { Volume = 0.7f, PitchVariance = 0.2f, MaxInstances = 6 }, Projectile.Center);
            }
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.Opacity = MathHelper.Clamp(Age / 10f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);

            if (!Charging) {
                if (Projectile.velocity.Length() < SandseaDirector.NeedleMaxSpeed) {
                    Projectile.velocity *= SandseaDirector.NeedleAccel;
                }
                //末冲贴着地面时针会生在沙里,先让它飞出来,之后再认物块
                bool buried = SandseaTerrain.Buried(Projectile.Center);
                if (!buried) {
                    Projectile.localAI[2] = 1f;
                }
                else if (Projectile.localAI[2] == 1f) {
                    Projectile.Kill();
                    return;
                }
            }

            Lighting.AddLight(Projectile.Center, 0.35f, 0.2f, 0.5f);
            if (!Main.dedServ && Main.rand.NextBool(Charging ? 3 : 5)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.Venom, -Projectile.velocity * 0.15f, 120, default, Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
            }
        }

        public override bool? CanDamage() => !Charging && Projectile.Opacity > 0.6f ? null : false;

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, Main.rand.NextVector2Circular(3f, 3f), 100, default, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }
        }

        /// <summary>残影和弹体同一张图,蓄势时弹体来回胀缩</summary>
        public override bool PreDraw(ref Color lightColor) {
            float opacity = Projectile.Opacity;
            if (opacity <= 0.01f) {
                return false;
            }
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero || Charging) {
                    continue;
                }
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Color trail = SandseaVfx.Venom * (0.5f * t * opacity);
                trail.A = (byte)(trail.A * 0.4f);
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, null, trail,
                    Projectile.rotation, origin, Projectile.scale * (0.6f + 0.4f * t), SpriteEffects.None, 0);
            }
            float pulse = Charging ? 1f + 0.18f * MathF.Sin(Age * 0.9f) : 1f;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * opacity, Projectile.rotation, origin,
                Projectile.scale * pulse, SpriteEffects.None, 0);
            return false;
        }
    }
}
