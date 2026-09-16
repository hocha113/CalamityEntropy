using CalamityEntropy.Content.Buffs.Pets;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Pets.MelonCat
{
    public class MelonCatProj : ModProjectile
    {
        //贴图在加载期一次就位,不再在 PreDraw 里逐帧请求;flying/walk 是横向多帧的雪碧图
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/MelonCat/MelonCatProj")]
        internal static Asset<Texture2D> MenuTex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/MelonCat/flying")]
        internal static Asset<Texture2D> FlyingTex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/MelonCat/walk")]
        internal static Asset<Texture2D> WalkTex;
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
            Projectile.height = 44;
        }
        public override bool PreDraw(ref Color lightColor) {

            if (Main.gameMenu) {
                Texture2D txd = MenuTex.Value;
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            Player player = Main.player[Projectile.owner];
            int tc = 4;
            Texture2D tx;
            if (Projectile.ai[1] == 1) {
                tx = FlyingTex.Value;
            }
            else {
                tx = WalkTex.Value;
                tc = 6;
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
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tx, tc, (int)(counter / 2f % tc)), lightColor, Projectile.rotation, new Vector2(tx.Width / tc, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

            }
            else {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tx, tc, (int)(counter / 2f % tc)), lightColor, Projectile.rotation, new Vector2(tx.Width / tc, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
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
                    Projectile.velocity += px * 0.8f;

                    Projectile.velocity *= 0.98f;

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
                else if (CEUtils.getDistance(targetPos * new Vector2(1, 0), Projectile.Center * new Vector2(1, 0)) > 80) {
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

                if (Math.Abs(Projectile.velocity.X) > 0.3f && !CEUtils.isAir(Projectile.Center + (Projectile.velocity * new Vector2(1, 0)).SafeNormalize(Vector2.Zero) * 14 + new Vector2(0, 23))) {
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
            if (!player.dead && player.HasBuff(ModContent.BuffType<MelonCatBuff>())) {
                Projectile.timeLeft = 2;
            }

        }


    }
}
