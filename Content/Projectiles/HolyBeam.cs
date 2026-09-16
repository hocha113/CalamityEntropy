using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class HolyBeam : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 3000;

        }
        public float counter = 0;
        public int drawcount = 0;
        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 480;
            Projectile.MaxUpdates = 6;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 5;
        }
        float opc = 1;
        public float rotSpeed = 0;
        public float num = -1;
        public override void AI() {
            if (num == -1)
                num = Main.GameUpdateCount / 16;
            if (counter == 0) {
                SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/angel_blast1"), Projectile.Center);
            }
            counter++;
            rotSpeed += 0.0001f * (num % 2 == 0 ? 1 : -1);
            rotSpeed *= 0.996f;
            Projectile.velocity = Projectile.velocity.RotatedBy(rotSpeed);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (counter > 400) {
                opc -= 1f / 80f;
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.normalize() * 900, targetHitbox, 100);
        }

        public override bool PreDraw(ref Color lightColor) {
            drawcount++;

            SpriteBatch spriteBatch = Main.spriteBatch;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D warn = CEExtraAssets.vlbw;

            spriteBatch.Draw(warn, Projectile.Center - Main.screenPosition, null, Color.Yellow * opc, Projectile.rotation, warn.Size() / 2 * new Vector2(0, 1), new Vector2(10, 1.2f) * Projectile.scale * 1.46f * new Vector2(0.5f, opc), SpriteEffects.None, 0);
            spriteBatch.Draw(warn, Projectile.Center - Main.screenPosition, null, ((drawcount / 2) % 2 == 0 ? Color.White : Color.Yellow) * opc, Projectile.rotation, warn.Size() / 2 * new Vector2(0, 1), new Vector2(10, 1) * Projectile.scale * 1.46f * new Vector2(0.5f, opc), SpriteEffects.None, 0);


            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }

}