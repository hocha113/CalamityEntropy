using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ProjectionStar : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1.5f;
            Projectile.timeLeft = 36;
        }
        public override void AI() {
            Projectile.rotation += 0.16f;
            Projectile.velocity *= 0.96f;
            Dust.NewDust(Projectile.Center - new Vector2(4, 4), 8, 8, DustID.PinkStarfish, 0, 0);
            Dust.NewDust(Projectile.Center - new Vector2(4, 4), 8, 8, DustID.YellowStarDust, 0, 0);
        }

        public override void OnKill(int timeLeft) {
            if (Main.myPlayer == Projectile.owner) {
                float rot = CEUtils.randomRot();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, rot.ToRotationVector2() * 14, ModContent.ProjectileType<ProjectionStarSplit>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, rot.ToRotationVector2() * -14, ModContent.ProjectileType<ProjectionStarSplitAlt>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

            }
            for (int i = 0; i < 36; i++) {
                Vector2 speed = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(1, 3);
                Dust.NewDust(Projectile.Center - new Vector2(4, 4), 8, 8, DustID.PinkStarfish, speed.X, speed.Y);
                speed = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(1, 3);
                Dust.NewDust(Projectile.Center - new Vector2(4, 4), 8, 8, DustID.YellowStarDust, speed.X, speed.Y);

            }
            ParticleOrchestrator.RequestParticleSpawn(true, ParticleOrchestraType.Keybrand, new ParticleOrchestraSettings() { IndexOfPlayerWhoInvokedThis = (byte)Projectile.owner, MovementVector = Vector2.Zero, PositionInWorld = Projectile.Center });
            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/lasershoot") { Volume = 0.35f }, Projectile.Center);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;

            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tx.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }


}