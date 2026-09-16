using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.ApsychosProjs
{
    public class ApsychosFire : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 100000;
            Projectile.penetrate = 1;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(BuffID.OnFire3, 180);
        }
        public float scale = 0;
        public float p = 0;

        public override void AI() {
            p += 1 / Projectile.ai[0];
            if (p > 1)
                Projectile.Kill();
            scale = float.Min(1, p * 4) + p * 0.2f;
            if (p > 0.65f)
                scale += (p - 0.65f) * 1.2f;
            Projectile.velocity *= 0.96f;
        }
        public override bool? CanDamage() {
            return p < 0.95f;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            float d = 0.2f;
            Color clr = new Color(255, 220, 30);
            Color clr2 = new Color(255, 220, 30);
            float alpha = 1;
            Color color1 = Projectile.ai[1] == 1 ? new Color(255, 160, 0) : Color.DeepSkyBlue;
            Color color2 = Projectile.ai[1] == 1 ? Color.OrangeRed : Color.MediumPurple;
            if (p > 1 - d) {
                alpha = Utils.Remap(p, 1 - d, 1, 1, 0);
                clr = Color.Lerp(clr, color1, Utils.Remap(p, 1 - d, 1, 0, 1)) * alpha;
            }
            for (float i = 0.2f; i < 1; i += 0.2f) {
                clr = Color.Lerp(color1, Color.Black, i) * alpha;
                clr2 = Color.Lerp(color2, Color.Black, i) * alpha;
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition - Projectile.velocity * i, null, clr * 0.6f * (1 - i), Main.GlobalTimeWrappedHourly * 16, tex.Size() * 0.5f, scale * Projectile.scale * 1.1f * (1 - i), SpriteEffects.None, 0);
                Main.spriteBatch.UseAdditive();
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition - Projectile.velocity * i, null, clr2 * (1 - i), Main.GlobalTimeWrappedHourly * 16, tex.Size() * 0.5f, scale * Projectile.scale * 0.8f * (1 - i), SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
            clr = color1 * alpha;
            clr2 = color2 * alpha;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, clr * 0.6f, Main.GlobalTimeWrappedHourly * 16, tex.Size() * 0.5f, scale * Projectile.scale * 1.1f, SpriteEffects.None, 0);
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, clr2, Main.GlobalTimeWrappedHourly * 16, tex.Size() * 0.5f, scale * Projectile.scale * 0.8f, SpriteEffects.None, 0);
            for (float i = 0.2f; i < 1; i += 0.2f) {
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition - Projectile.velocity * i, null, Color.White * alpha * (1 - i), Main.GlobalTimeWrappedHourly * 16, tex.Size() * 0.5f, scale * Projectile.scale * 0.45f * (1 - i), SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * alpha, Main.GlobalTimeWrappedHourly * 16, tex.Size() * 0.5f, scale * Projectile.scale * 0.45f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }


}
