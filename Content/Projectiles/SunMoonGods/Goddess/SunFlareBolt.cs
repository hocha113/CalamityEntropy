using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess
{
    /// <summary>
    /// 火羽弹,匀速直飞,弹体用烈阳之光的贴图
    /// ai[0] 寿命覆盖(0 用默认),ai[1] 起飞前隐身等待的帧数,localAI[0] 存活帧
    /// </summary>
    public class SunFlareBolt : GodHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/BlazingSunlight";
        public override GodKind Kind => GodKind.Sun;
        public override int DebuffType => ModContent.BuffType<SunblazeBurn>();
        public override int DebuffTime => SunDirector.BurnFrames;
        public override int DefaultTimeLeft => SunDirector.FlareBoltLife;

        private float Age => Projectile.localAI[0];
        private int Delay => (int)Projectile.ai[1];

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 5;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool ShouldUpdatePosition() => Age > Delay;

        public override void AI() {
            //timeLeft 不过线,首帧各端自己定
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                int life = Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : SunDirector.FlareBoltLife;
                Projectile.timeLeft = life + Delay;
            }
            Projectile.localAI[0]++;
            float flightAge = Age - Delay;
            Projectile.Opacity = MathHelper.Clamp(flightAge / 8f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 14f, 0f, 1f);
            if (flightAge <= 0f) {
                return;
            }
            if (++Projectile.frameCounter >= 5) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Projectile.rotation += 0.05f;
            Lighting.AddLight(Projectile.Center, 0.9f, 0.55f, 0.18f);
            if (!Main.dedServ && Main.rand.NextBool(6)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.SolarFlare, -Projectile.velocity * 0.2f, 100, default, Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f ? null : false;

        public override void OnKill(int timeLeft) {
            if (timeLeft > 0) {
                SunVfx.Embers(Projectile.Center, 5, 1.5f, 4f);
            }
        }

        /// <summary>残影和弹体同一张图,残影宽度不低于弹体的一半</summary>
        public override bool PreDraw(ref Color lightColor) {
            float opacity = Projectile.Opacity;
            if (opacity <= 0.01f) {
                return false;
            }
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() / 2f;

            GodDraw.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.spriteBatch.Draw(tex, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, frame, GodDraw.Add(SunVfx.Ember, 0.45f * t * opacity),
                    Projectile.rotation, origin, Projectile.scale * (0.55f + 0.4f * t), SpriteEffects.None, 0f);
            }
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SunVfx.Ember, 0.5f * opacity), 0f, 0.34f * Projectile.scale);
            GodDraw.End();

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}