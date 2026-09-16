using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class UrnOfSoulsHoldout : ModProjectile
    {
        public static SoundEffect loopSnd;
        public override void SetStaticDefaults() {
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public LoopSound snd;
        public override void AI() {
            Player owner = Projectile.owner.ToPlayer();
            if (Projectile.ai[0] == 0) {
                CEUtils.PlaySound("flamethrower start", 1, Projectile.Center);
            }
            if (!Main.dedServ) {
                if (Projectile.ai[0] == 70) {
                    snd = new LoopSound(loopSnd);
                    snd.play();
                    snd.setVolume(0);
                }
                if (Projectile.ai[0] >= 70) {
                    snd.setVolume_Dist(Projectile.Center, 80, 800, 1);
                    snd.timeleft = 3;
                }
            }
            if (Projectile.ai[0]++ > 27) {
                if (Projectile.ai[0] % 2 == 0) {
                    if (owner.CheckMana(3, true)) {
                        if (Main.myPlayer == Projectile.owner) {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedByRandom(0.2f), ModContent.ProjectileType<UrnSoulFire>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                        }
                    }
                    else {
                        Projectile.Kill();
                    }
                }
            }
            if (!owner.channel) {
                Projectile.Kill();
                return;
            }
            else {
                Projectile.timeLeft = 2;
            }

            if (Main.myPlayer == Projectile.owner) {
                Vector2 nv = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.One) * 8;
                if (nv != Projectile.velocity) {
                    Projectile.netUpdate = true;
                }
                Projectile.velocity = nv;

            }
            if (Projectile.velocity.X > 0) {
                Projectile.direction = 1;
                owner.direction = 1;
            }
            else {
                Projectile.direction = -1;
                owner.direction = -1;
            }
            Player player = Projectile.owner.ToPlayer();
            Projectile.Center = owner.MountedCenter + player.gfxOffY * Vector2.UnitY + Projectile.velocity.SafeNormalize(Vector2.Zero) * 28;
            owner.itemRotation = Projectile.rotation * Projectile.direction;
            Projectile.rotation = Projectile.velocity.ToRotation();
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            if (Projectile.velocity.X > 0) {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public float counter = 0;
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("urnclose", 1, Projectile.Center);
            CEUtils.PlaySound("flamethrower end", 1, Projectile.Center);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            lightColor = Color.White;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.ToRadians(90), texture.Size() / 2, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }

}