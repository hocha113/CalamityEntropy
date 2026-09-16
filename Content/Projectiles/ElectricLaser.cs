using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ElectricLaser : ModProjectile
    {
        public Vector2 endPos => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            Projectile.ArmorPenetration = 256;
            Projectile.timeLeft = 16;
        }
        public int frame = 0;
        public int framecounter = 0;
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void AI() {
            framecounter++;
            if (framecounter % 2 == 0) { frame++; }
        }
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.timeLeft < 15) {
                return false;
            }
            return base.CanHitNPC(target);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, endPos, targetHitbox, 6);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend, SamplerState.LinearWrap);
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle rect = new Rectangle(64 * frame, 0, 64, (int)CEUtils.getDistance(Projectile.Center, endPos));
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, rect, Color.White, (endPos - Projectile.Center).ToRotation() - MathHelper.PiOver2, new Vector2(32, 0), 1, SpriteEffects.None);
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

}