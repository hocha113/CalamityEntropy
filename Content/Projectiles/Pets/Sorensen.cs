using CalamityEntropy.Content.Buffs.Pets;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Pets
{
    public class Sorensen : ModProjectile
    {
        public int counter = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 24;
            Projectile.height = 58;
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(Projectile.GetTexture(), 6, ((counter / 4) % 6), false), Color.White, Projectile.rotation, new Vector2(68, 75), Projectile.scale, Projectile.GetOwner().Center.X > Projectile.Center.X ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            return false;

        }
        public override bool PreAI() {
            Player player = Main.player[Projectile.owner];

            player.zephyrfish = false;
            return true;
        }
        void MoveToTarget(Vector2 targetPos) {
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 2400) {
                Projectile.Center = Main.player[Projectile.owner].Center;
            }
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 120)
                Projectile.velocity += (targetPos - Projectile.Center).normalize() * 1.2f;
            Projectile.velocity *= 0.95f;
        }
        public override void AI() {
            counter++;
            Player player = Main.player[Projectile.owner];

            MoveToTarget(player.Center + new Vector2(0, -80) + new Vector2(-140 * player.direction, 0));

            if (!player.dead && player.HasBuff(ModContent.BuffType<ChaosTyrant>())) {
                Projectile.timeLeft = 2;
            }
            CEUtils.AddLight(Projectile.Center, Color.LightYellow, 4);
        }


    }
}
