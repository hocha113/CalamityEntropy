using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkStinger : BookMark, IPriceFromRecipe
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Green;
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Stinger");
        public override Color tooltipColor => Color.Green;
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Stinger, 5)
                .AddIngredient(ItemID.FallenStar, 2)
                .AddTile(TileID.Bookcases)
                .Register();
        }
        public override EBookProjectileEffect getEffect() {
            return new StingerBMEffect();
        }
    }

    public class StingerBMEffect : EBookProjectileEffect
    {
        public static int type = -1;
        public override void BookUpdate(Projectile projectile, bool ownerClient) {
            if (ownerClient) {
                if (type == -1)
                    type = ModContent.ProjectileType<Stinger>();
                int counter = (int)projectile.Entropy().counter;
                if (counter % 70 == 0 || counter % 70 == 5 || counter % 70 == 10) {
                    if (projectile.ModProjectile is EntropyBookHeldProjectile eb) {
                        NPC target = CEUtils.FindTarget_HomingProj(projectile, projectile.Center, 2000);
                        if (target != null) {
                            eb.ShootSingleProjectile(type, projectile.Center, (target.Center + target.velocity * 4 - projectile.Center).normalize() * 22, 1, randomRotMult: 0, fixedBaseDamage: 4);
                        }
                    }
                }
            }
        }
    }
    public class Stinger : EBookBaseProjectile
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.width = Projectile.height = 22;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 160;
        }
        public override void OnKill(int timeLeft) {
            base.OnKill(timeLeft);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 16; i++) {
                int num98 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.CorruptGibs, 0f, 0f, 0, default(Color), 0.9f);
                Main.dust[num98].noGravity = true;
                Main.dust[num98].velocity = CEUtils.randomPointInCircle(4);
            }
        }
        public override void AI() {
            base.AI();
            int num98 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.CorruptGibs, 0f, 0f, 0, default(Color), 0.9f);
            Main.dust[num98].noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
}