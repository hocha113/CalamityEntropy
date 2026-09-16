using CalamityEntropy.Content.Buffs.Pets;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Pets.DoG
{
    public class DoG : ModProjectile
    {
        //无帽版首帧文件名不带序号(DoG.png 同时是弹幕主贴图),数组标签只认「路径+数字」,首帧单独加载
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DoG/DoG")]
        internal static Asset<Texture2D> Frame1;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DoG/DoG", 2, 4, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FramesRest;
        //戴帽版从 1 开始编号,且只有 4 帧
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DoG/s/DoG", 1, 4, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] HatFrames;
        public int counter = 0;
        public bool say = true;
        public float sayCount = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 64;
            Projectile.height = 64;
        }
        public override bool PreDraw(ref Color lightColor) {
            bool hat = Projectile.owner.ToPlayer().Entropy().PetsHat;
            if (Main.gameMenu) {
                Texture2D txd = Frame1.Value;
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            Texture2D tx;
            if (hat) {
                tx = HatFrames[(counter / 6) % HatFrames.Length];
            }
            else {
                //无帽版共 5 帧:首帧 + 后 4 帧,按下标拼回原来的取帧顺序
                int frame = (counter / 6) % (FramesRest.Length + 1);
                tx = frame == 0 ? Frame1.Value : FramesRest[frame - 1];
            }
            if (Projectile.velocity.X > -2 && Projectile.velocity.X < 2f) {
                if (Main.player[Projectile.owner].Center.X > Projectile.Center.X) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }
            }
            if (Projectile.direction == -1) {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

            }
            else {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
            }


            return false;

        }
        public bool std = false;
        void MoveToTarget(Vector2 targetPos) {
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 1400) {
                Projectile.Center = Main.player[Projectile.owner].Center;
            }
            Projectile.rotation = MathHelper.ToRadians((Projectile.velocity.X * 1.4f));
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 34) {
                Vector2 px = targetPos - Projectile.Center;
                px.Normalize();
                Projectile.velocity += px * 0.84f;

                Projectile.velocity *= 0.935f;

            }
            else {
                Projectile.velocity *= 0.8f;

            }
            if (Projectile.velocity.X > 0) {
                Projectile.direction = 1;
            }
            else {
                Projectile.direction = -1;
            }

        }
        public override bool PreAI() {
            Player player = Main.player[Projectile.owner];

            player.zephyrfish = false;

            return true;
        }
        public static string ConvertToUnicodeString(string text) {
            string result = "";
            foreach (char c in text) {
                result += "\\u" + ((int)c).ToString("x4");
            }
            return result;
        }
        public override void AI() {
            counter++;
            Player player = Main.player[Projectile.owner];
            MoveToTarget(player.Center + new Vector2(0, -60) + new Vector2(-30 * player.direction, 0));
            if (!player.dead && (player.HasBuff(ModContent.BuffType<DevourerAndTheApostles>()) || player.HasBuff(ModContent.BuffType<DoGBuff>()))) {
                Projectile.timeLeft = 2;
            }



        }
    }
}
