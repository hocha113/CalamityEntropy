using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Slash : ModProjectile
    {
        //斩击贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Slash")]
        internal static Asset<Texture2D> SlashTex;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public bool sPlayerd = false;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 2f;
            Projectile.timeLeft = 30;
            Projectile.penetrate = -1;
        }
        public override void AI() {
            if (!sPlayerd) {
                sPlayerd = true;
                SoundStyle s = new("CalamityEntropy/Assets/Sounds/swing" + Main.rand.Next(1, 4));
                s.Volume = 0.6f;
                s.Pitch = 1f;
                SoundEngine.PlaySound(s, Projectile.Center);
            }
            if (Projectile.ai[2] < 6) {
                Projectile.ai[0] += 55;
            }
            else {
                Projectile.ai[1] = Projectile.ai[1] + (Projectile.ai[0] - Projectile.ai[1]) * 0.3f;
            }
            Projectile.ai[2]++;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {

            Texture2D tx = SlashTex.Value;
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2((Projectile.ai[0] + Projectile.ai[1]) / 2 - 55 * 3, 0).RotatedBy(Projectile.rotation), null, Color.White, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, new Vector2((Projectile.ai[0] - Projectile.ai[1]) / tx.Width, 1), SpriteEffects.None, 0);

            return false;
        }


    }


}