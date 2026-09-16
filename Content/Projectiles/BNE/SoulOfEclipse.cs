using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.BNE
{
    public class SoulOfEclipse : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 360;
            Projectile.light = 0.25f;
        }
        public override void AI() {
            if (Projectile.timeLeft < 340 - Projectile.ai[0]) {
                Projectile.velocity += (Projectile.owner.ToPlayer().Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 3.2f;
                Projectile.velocity *= 0.94f;
                if (CEUtils.getDistance(Projectile.Center, Projectile.owner.ToPlayer().Center) < 64) {
                    Projectile.owner.ToPlayer().Entropy().serviceWhipDamageBonus += 0.022f;
                    Projectile.owner.ToPlayer().Heal(4);
                    CEUtils.PlaySound("soulshine", 1, Projectile.Center, volume: 0.4f);
                    for (int i = 0; i < 32; i++) {
                        Dust.NewDust(Projectile.owner.ToPlayer().Center, 1, 1, DustID.OrangeStainedGlass, Main.rand.NextFloat(-8, 8), Main.rand.NextFloat(-8, 8));
                    }
                    Projectile.Kill();
                }
            }
            else {
                Projectile.velocity += (Projectile.owner.ToPlayer().Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.06f;
                Projectile.velocity *= 0.976f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            Vector2 top = Projectile.Center;
            Vector2 sparkVelocity2 = Projectile.velocity * -0.1f;
            int sparkLifetime2 = Main.rand.Next(20, 24);
            float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
            Color sparkColor2 = Color.Lerp(Color.OrangeRed, Color.Gold, Main.rand.NextFloat(0, 1));
            //PRT_LineCal Configure(false,lifetime)对齐Calamity LineParticle
            PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));  //跟AltSpark成对出现时寿命/速度系数是旧代码原值
        }
        public float alpha = 1;
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tex, 4, (int)(Projectile.Entropy().counter / 4) % 4), Color.White * alpha, Projectile.rotation + MathHelper.PiOver2, new Vector2(25, 32), Projectile.scale, SpriteEffects.None);
            return false;
        }


    }

}