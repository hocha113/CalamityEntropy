using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class BlueFlies : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180 * 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;

        }
        public override void AI() {
            Player player = Projectile.owner.ToPlayer();
            NPC target = CEUtils.findTarget(player, Projectile, 800, false);
            if (target != null) {
                Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.6f;
            }
            else {
                Vector2 targetPosp;
                int index = 0;
                int maxFlies = 0;
                bool flag = true;
                foreach (Projectile proj in Main.ActiveProjectiles) {
                    if (proj.type == Projectile.type) {
                        if (proj.whoAmI == Projectile.whoAmI) {
                            flag = false;
                        }
                        if (flag) {
                            index++;
                        }
                        maxFlies++;

                    }
                }
                targetPosp = player.Center + MathHelper.ToRadians(((float)(index + 1) / (float)(maxFlies)) * 360).ToRotationVector2().RotatedBy(player.Entropy().CasketSwordRot * 0.3f) * 48;
                Projectile.velocity += (targetPosp - Projectile.Center).SafeNormalize(Vector2.Zero) * 2f;
                Projectile.velocity *= 0.9f;
            }
            Projectile.velocity *= 0.968f;
        }


        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tex, 2, ((Projectile.timeLeft / 4) % 2 == 0 ? 0 : 1)), lightColor, Projectile.rotation, new Vector2(32, 32), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }


}