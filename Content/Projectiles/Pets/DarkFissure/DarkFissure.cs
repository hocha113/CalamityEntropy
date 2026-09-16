using CalamityEntropy.Content.Buffs.Pets;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Pets.DarkFissure
{
    public class DarkFissure : ModProjectile
    {
        //飞行首帧文件名不带序号(DarkFissure.png 同时是弹幕主贴图),数组标签只认「路径+数字」,首帧单独加载
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DarkFissure/DarkFissure")]
        internal static Asset<Texture2D> FlyFrame1;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DarkFissure/DarkFissure", 2, 5, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FlyFramesRest;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DarkFissure/s/DarkFissure")]
        internal static Asset<Texture2D> FlyHatFrame1;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DarkFissure/s/DarkFissure", 2, 5, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FlyHatFramesRest;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DarkFissure/walk", 1, 6, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] WalkFrames;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/DarkFissure/s/walk", 1, 6, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] WalkHatFrames;
        public float counter = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            base.SetStaticDefaults();

        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 24;
            Projectile.height = 40;
        }
        public override bool PreDraw(ref Color lightColor) {
            bool hat = Projectile.owner.ToPlayer().Entropy().PetsHat;

            if (Main.gameMenu) {
                Texture2D txd = FlyFrame1.Value;
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            Player player = Main.player[Projectile.owner];
            if (counter > 36) {
                counter -= 36;
            }
            Texture2D tx;
            if (Projectile.ai[1] == 1) {
                //飞行组共 6 帧:首帧 + 后 5 帧,按下标拼回原来的取帧顺序
                Texture2D[] rest = hat ? FlyHatFramesRest : FlyFramesRest;
                int frame = ((int)counter / 6) % (rest.Length + 1);
                tx = frame == 0 ? (hat ? FlyHatFrame1 : FlyFrame1).Value : rest[frame - 1];
            }
            else {
                Texture2D[] frames = hat ? WalkHatFrames : WalkFrames;
                tx = frames[(((int)counter / 6) % frames.Length)];
            }
            if (Projectile.velocity.X > -2 && Projectile.velocity.X < 2f) {
                if (player.Center.X > Projectile.Center.X) {
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
        void MoveToTarget(Vector2 targetPos) {
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 1400) {
                Projectile.Center = Main.player[Projectile.owner].Center - new Vector2(0, 50);
            }
            if (Projectile.ai[1] == 1) {
                counter++;
                Projectile.tileCollide = false;
                Projectile.rotation = MathHelper.ToRadians((Projectile.velocity.X * 1.4f));
                if (CEUtils.getDistance(Projectile.Center, targetPos) > 90) {
                    Vector2 px = targetPos - Projectile.Center;
                    px.Normalize();
                    Projectile.velocity += px * 1.2f;

                    Projectile.velocity *= 0.96f;

                }
                if (Projectile.Center.Y < targetPos.Y - 16 && CEUtils.getDistance(Projectile.Center, targetPos) < 100 && !(CEUtils.isAir(Projectile.owner.ToPlayer().Center + new Vector2(0, Projectile.owner.ToPlayer().height / 2 + 2), true))) {
                    Projectile.ai[1] = 0;
                }
                if (Projectile.velocity.X > 0) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }
            }
            else {
                if (Projectile.velocity.Y == 0) {
                    counter += Math.Abs(Projectile.velocity.X / 4);
                }
                Projectile.tileCollide = true;
                Projectile.rotation = 0;
                Projectile.velocity.Y += 0.5f;
                if (CEUtils.getDistance(targetPos, Projectile.Center) > 340 || (Math.Abs(targetPos.Y - Projectile.Center.Y) > 60 && Projectile.owner.ToPlayer().velocity.Y == 0)) {
                    Projectile.ai[1] = 1;
                }
                else if (CEUtils.getDistance(targetPos * new Vector2(1, 0), Projectile.Center * new Vector2(1, 0)) > 120) {
                    if (targetPos.X > Projectile.Center.X) {
                        Projectile.velocity.X += 1f;
                    }
                    else {
                        Projectile.velocity.X -= 1f;
                    }
                    Projectile.velocity.X *= 0.95f;
                }
                else {
                    Projectile.velocity.X *= 0.9f;
                }
                if (targetPos.X > Projectile.Center.X) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }

                if (Math.Abs(Projectile.velocity.X) > 0.3f && !CEUtils.isAir(Projectile.Center + (Projectile.velocity * new Vector2(1, 0)).SafeNormalize(Vector2.Zero) * 14 + new Vector2(0, 18))) {
                    Projectile.velocity.Y -= 1.5f;
                }
            }

        }
        public override bool PreAI() {
            Player player = Main.player[Projectile.owner];

            player.zephyrfish = false;
            return true;
        }

        public override void AI() {

            Player player = Main.player[Projectile.owner];
            MoveToTarget(player.Center + new Vector2(0, 0));
            if (!player.dead && (player.HasBuff(ModContent.BuffType<DevourerAndTheApostles>()) || player.HasBuff(ModContent.BuffType<WeakGravity>()))) {
                Projectile.timeLeft = 2;
            }

        }


    }
}
