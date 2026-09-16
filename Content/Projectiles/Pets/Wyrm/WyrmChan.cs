using CalamityEntropy.Content.Buffs.Pets;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Pets.Wyrm
{
    public class WyrmChan : ModProjectile
    {
        //本体与眼睛两组帧的首帧文件名都不带序号(WyrmChan.png 同时是弹幕主贴图),
        //数组标签只认「路径+数字」,首帧单独加载
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Wyrm/WyrmChan")]
        internal static Asset<Texture2D> BodyFrame1;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Wyrm/WyrmChan", 2, 3, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] BodyFramesRest;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Wyrm/Eye")]
        internal static Asset<Texture2D> EyeFrame1;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Wyrm/Eye", 2, 3, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] EyeFramesRest;
        public int counter = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 92;
            Projectile.height = 92;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Main.gameMenu) {
                Texture2D txd = BodyFrame1.Value;
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            //两组各 4 帧:首帧 + 后 3 帧,本体和眼睛用同一帧序号
            int frame = (counter / 6) % (BodyFramesRest.Length + 1);
            Texture2D tx = frame == 0 ? BodyFrame1.Value : BodyFramesRest[frame - 1];
            Texture2D tx2 = frame == 0 ? EyeFrame1.Value : EyeFramesRest[frame - 1];
            if (Projectile.velocity.X > -2 && Projectile.velocity.X < 2f) {
                if (Main.player[Projectile.owner].Center.X > Projectile.Center.X) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }
            }
            if (Projectile.direction == 1) {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);
                if (lightColor.R + lightColor.G + lightColor.B < 255) {
                    int gr = (255 - (lightColor.R + lightColor.G + lightColor.B));
                    Main.EntitySpriteDraw(tx2, Projectile.Center - Main.screenPosition, null, new Color(255, 255, 255) * ((float)gr / 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);
                }
            }
            else {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
                if (lightColor.R + lightColor.G + lightColor.B < 255) {
                    int gr = (255 - (lightColor.R + lightColor.G + lightColor.B));
                    Main.EntitySpriteDraw(tx2, Projectile.Center - Main.screenPosition, null, new Color(255, 255, 255) * ((float)gr / 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
                }
            }



            return false;

        }
        void MoveToTarget(Vector2 targetPos) {
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 1400) {
                Projectile.Center = Main.player[Projectile.owner].Center;
            }
            Projectile.rotation = MathHelper.ToRadians((Projectile.velocity.X * 1.4f));
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 34) {
                Vector2 px = targetPos - Projectile.Center;
                px.Normalize();
                Projectile.velocity += px * 0.6f;

                Projectile.velocity *= 0.98f;

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

        public override void AI() {
            counter++;
            Player player = Main.player[Projectile.owner];
            MoveToTarget(player.Center + new Vector2(0, -60) + new Vector2(-80 * player.direction, 0));
            if (!player.dead && player.HasBuff(ModContent.BuffType<WyrmChanBuff>())) {
                Projectile.timeLeft = 2;
            }
            if (Projectile.wet) {
                Projectile.extraUpdates = 1;
            }
            else {
                Projectile.extraUpdates = 0;
            }
        }


    }
}
