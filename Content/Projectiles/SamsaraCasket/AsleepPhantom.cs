using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class AsleepPhantom : ModProjectile
    {
        List<Vector2> odp = new List<Vector2>();
        List<float> odr = new List<float>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            CEUtils.recordOldPosAndRots(Projectile, ref odp, ref odr, 5);
        }

        public override bool PreDraw(ref Color lightColor) {
            float alpha = 1;
            if (Projectile.timeLeft < 10) {
                alpha = (float)Projectile.timeLeft / 20;
            }
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 0; i < odp.Count; i++) {
                Main.spriteBatch.Draw(tex, odp[i] - Main.screenPosition, null, lightColor * ((float)i / (float)odp.Count) * 0.4f, odr[i] + MathHelper.PiOver4, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

    }

}