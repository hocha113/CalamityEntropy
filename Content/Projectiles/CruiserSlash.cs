using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class CruiserSlash : ModProjectile
    {
        public bool sPlayerd = false;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.light = 30f;
            Projectile.timeLeft = 88;
            Projectile.penetrate = -1;
            Projectile.ArmorPenetration = 1024;
        }
        public int ct = 0;
        public override void AI() {
            ct++;
            if (Main.dedServ) {
                sPlayerd = true;
            }
            if (ct > 60) {
                if (!sPlayerd) {
                    sPlayerd = true;
                    if (CEUtils.getDistance(Projectile.Center, Main.LocalPlayer.Center) < 600) {
                        SoundStyle s = new("CalamityEntropy/Assets/Sounds/swing" + Main.rand.Next(1, 4));
                        s.Volume = 1f;
                        s.Pitch = 0.8f;
                        SoundEngine.PlaySound(s, Projectile.Center);
                    }
                }
                if (Projectile.ai[2] < 6) {
                    Projectile.ai[0] += 100;
                }
                else {
                    Projectile.ai[1] = Projectile.ai[1] + (Projectile.ai[0] - Projectile.ai[1]) * 0.3f;
                }
                Projectile.ai[2]++;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.timeLeft < 16) {
                return false;
            }
            if (ct < 60 || ct > 64)
                return false;
            return CEUtils.LineThroughRect(Projectile.Center + Projectile.rotation.ToRotationVector2() * 380, Projectile.Center + Projectile.rotation.ToRotationVector2() * -380, targetHitbox, 12);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D t1 = CEExtraAssets.lightball;

            if (ct < 60) {
                SpriteBatch sb = Main.spriteBatch;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Texture2D t = CEExtraAssets.a_circle;
                Main.EntitySpriteDraw(t, Projectile.Center - Main.screenPosition, null, new Color(180, 180, 255) * ((float)ct / 60f) * 0.8f, Projectile.rotation, t.Size() / 2f, new Vector2(6.4f, 0.2f), SpriteEffects.None); ;
                sb.Draw(t1, Projectile.Center - Main.screenPosition, null, Color.DarkBlue * ((float)ct / 60f) * 0.6f, 0, new Vector2(t1.Width, t1.Height) / 2, 50f * (60 - ct) / 128, SpriteEffects.None, 0);
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
            return false;
        }


    }


}