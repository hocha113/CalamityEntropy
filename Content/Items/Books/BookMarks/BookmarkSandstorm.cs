using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkSandstorm : BookMark, IPriceFromRecipe
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Sandstorm");
        public override EBookProjectileEffect getEffect() {
            return new SandstormBMEffect();
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.SandBlock, 40)
                .AddIngredient(ItemID.AntlionMandible)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        public override Color tooltipColor => new Color(246, 201, 122);
        public static void ShootProjectile(int count, Player player, EntropyBookHeldProjectile book) {
            if (count > 0)
                CEUtils.PlaySound("corruptwhip_hit2", 1, player.Center, 10, count / 4f + 0.4f);
            for (int i = 0; i < count; i++) {
                int dustAmt = 16;
                for (int j = 0; j < dustAmt; j++) {
                    Vector2 vel = (Main.MouseWorld - player.MountedCenter).normalize().RotatedByRandom(0.22f) * 24 * Main.rand.NextFloat(0.3f, 1);
                    Vector2 dustRotate = vel;
                    int sand = Dust.NewDust(player.Center + vel * 4, 0, 0, DustID.Sand, 0, 0, 0, default, 1.2f);
                    Main.dust[sand].noGravity = true;
                    Main.dust[sand].noLight = true;
                    Main.dust[sand].scale *= 1.4f;
                    Main.dust[sand].velocity = vel;
                }
                book.ShootSingleProjectile(ModContent.ProjectileType<SandBullet>(), player.MountedCenter, (Main.MouseWorld - player.MountedCenter).normalize() * 12 + CEUtils.randomPointInCircle(2) + new Vector2(0, -0.5f), 1, 1, 1, fixedBaseDamage: 20);
            }
        }
    }
    public class SandBullet : EBookBaseProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.alpha = 255;
        }

        public override void SendExtraAI(BinaryWriter writer) {
            base.SendExtraAI(writer);
            writer.Write(Projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            base.ReceiveExtraAI(reader);
            Projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI() {
            base.AI();
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.frame = 0;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 4f) {
                Projectile.alpha -= 50;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;

                int sandyDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Sand, 0f, 0f, 100, default, 1f);
                Main.dust[sandyDust].noGravity = true;
                Main.dust[sandyDust].velocity *= 0f;
            }
            if (Projectile.localAI[0] > 14)
                Projectile.velocity.Y += 0.9f;
        }

        public override void OnKill(int timeLeft) {
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = (int)(32 * Projectile.scale);
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            int dustAmt = 36;
            for (int i = 0; i < dustAmt; i++) {
                Vector2 dustRotate = Vector2.Normalize(Projectile.velocity) * new Vector2((float)Projectile.width / 2f, (float)Projectile.height) * 0.75f;
                dustRotate = dustRotate.RotatedBy((double)((float)(i - (dustAmt / 2 - 1)) * MathHelper.TwoPi / (float)dustAmt), default) + Projectile.Center;
                Vector2 dustDirection = dustRotate - Projectile.Center;
                int killSand = Dust.NewDust(dustRotate + dustDirection, 0, 0, DustID.Sand, dustDirection.X, dustDirection.Y, 100, default, 1.2f);
                Main.dust[killSand].noGravity = true;
                Main.dust[killSand].noLight = true;
                Main.dust[killSand].velocity = dustDirection * 0.5f;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            lightColor.G = (byte)(255 * Projectile.Opacity);
            lightColor.B = (byte)(255 * Projectile.Opacity);
            CEUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
    public class SandstormBMEffect : EBookProjectileEffect
    {

    }
}