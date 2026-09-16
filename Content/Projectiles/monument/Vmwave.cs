using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.monument
{
    public class Vmwave : ModProjectile
    {
        //冲击波三帧(wave1~3),加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/monument/wave", 1, 3, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] WaveFrames;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 9;
            Projectile.penetrate = -1;
        }

        public override void AI() {
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tx1 = WaveFrames[0];
            Texture2D tx2 = WaveFrames[1];
            Texture2D tx3 = WaveFrames[2];
            Texture2D draw = tx1;
            if (Projectile.timeLeft < 7) {
                draw = tx2;
            }
            if (Projectile.timeLeft < 4) {
                draw = tx3;
            }
            if (Projectile.timeLeft < 1) {
                return false;
            }

            Main.spriteBatch.Draw(draw, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(0, draw.Width) / 2, 1, SpriteEffects.None, 0);

            return false;
        }
    }


}