using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class FlowerSkeleton0 : ModProjectile
    {
        //帧动画数组(FlowerSkeleton0~9),加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/FlowerSkeleton", 0, 10, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        int framecounter = 4;
        int frame = 0;
        public override void AI() {
            Projectile.ArmorPenetration = HorizonssKey.getArmorPen();
            framecounter--;
            if (framecounter == 0) {
                frame++;
                framecounter = 4;
                if (frame > 9) {
                    Projectile.Kill();
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (HorizonssKey.getVoidTouchLevel() > 0) {
                EGlobalNPC.AddVoidTouch(target, 80, HorizonssKey.getVoidTouchLevel(), 800, 16);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Frames[frame];
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

    }

}