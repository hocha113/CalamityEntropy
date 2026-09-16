using CalamityEntropy.Content.Dusts;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.monument
{
    public class VoidMonumentProj : ModProjectile
    {
        //方尖碑帧动画(f1~f18,按 ai[1] 取帧),加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/monument/f", 1, 18, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        SoundStyle hitSound = new("CalamityEntropy/Assets/Sounds/vb_hit");
        SoundStyle hs = new("CalamityEntropy/Assets/Sounds/vbuse");
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 6000;

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 256;
            Projectile.height = 256;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1.2f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 2;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            Projectile.ai[0]++;
            if (Projectile.ai[0] % 3 == 0) {
                Projectile.ai[1]++;
                if (Projectile.ai[1] > 17) {
                    Projectile.ai[1] = 0;
                }
            }
            if (player.channel) {
                Projectile.timeLeft = 10;
            }
            else {
                if (Projectile.ai[1] == 0 || Projectile.ai[1] == 6 || Projectile.ai[1] == 10 || Projectile.ai[1] == 14) {
                    Projectile.Kill();
                }
            }

            if (Projectile.ai[1] == 3 || Projectile.ai[1] == 7 || Projectile.ai[1] == 11 || Projectile.ai[1] == 16) {
                if (Projectile.ai[0] % 3 == 0) {
                }
            }
            Vector2 playerRotatedPoint = player.RotatedRelativePoint(player.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner) {
                if (Projectile.ai[1] == 0 || Projectile.ai[1] == 6 || Projectile.ai[1] == 10 || Projectile.ai[1] == 14) {
                    HandleChannelMovement(player, playerRotatedPoint);
                }
            }
            if (Projectile.ai[1] == 14 && Projectile.ai[0] % 3 == 0) {
                int pj = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(0, 60), Vector2.Zero, ModContent.ProjectileType<MonumentGround>(), Projectile.damage, 0, Projectile.owner, 0, 8);
                if (Projectile.velocity.X < 0) {
                    pj.ToProj().ai[2] = -1;
                    pj.ToProj().rotation = MathHelper.ToRadians(-120);
                    pj.ToProj().netUpdate = true;
                }
                else {
                    pj.ToProj().ai[2] = 1;
                    pj.ToProj().rotation = MathHelper.ToRadians(-60);
                    pj.ToProj().netUpdate = true;
                }
            }
            Projectile.rotation = 0;
            if (Projectile.velocity.X < 0) {
                Projectile.rotation = (float)Math.PI;
            }
            Projectile.Center = player.Center + Projectile.rotation.ToRotationVector2() * 8 * Projectile.scale + new Vector2(0, -20);
            if (Projectile.velocity.X > 0) {
                player.direction = 1;
            }
            else {
                player.direction = 0;
            }
            player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            Lighting.AddLight(Projectile.Center, 0.6f, 0.6f, 3f);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCsAndTiles.Add(index);
        }
        public void HandleChannelMovement(Player player, Vector2 playerRotatedPoint) {
            float speed = 1f;
            Vector2 newVelocity = (Main.MouseWorld - playerRotatedPoint).SafeNormalize(Vector2.UnitX * player.direction) * speed;

            if (Projectile.velocity.X != newVelocity.X || Projectile.velocity.Y != newVelocity.Y) {
                Projectile.netUpdate = true;
            }
            Projectile.velocity = newVelocity;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Player player = Main.player[Projectile.owner];
            if (Projectile.ai[0] % 3 == 0) {
                if (Projectile.ai[1] == 3 || Projectile.ai[1] == 7 || Projectile.ai[1] == 11 || Projectile.ai[1] == 16) {
                    int bsize = ((int)(144 * Projectile.scale));
                    Vector2 c = player.Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
                    return new Rectangle((int)c.X - bsize / 2, (int)c.Y - bsize / 2, bsize, bsize).Intersects(targetHitbox);
                }
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.immune[Projectile.owner] = 1;
            if (Projectile.owner == Main.myPlayer) {
                CalamityEntropy.Instance.screenShakeAmp = 2;
            }
            SoundEngine.PlaySound(SoundID.Item62, Projectile.position);
            for (int i = 0; i < 20; i++) {
                Dust.NewDust(Projectile.Center, 8, 8, ModContent.DustType<Vmpiece>());
            }
            if (Projectile.owner == Main.myPlayer) {
                int pj = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Vmwave>(), 0, 0, Projectile.owner);
                if (Projectile.velocity.X < 0) {
                    Main.projectile[pj].rotation = (float)Math.PI;
                    Main.projectile[pj].netUpdate = true;
                }
            }
        }

        public override bool PreDraw(ref Color dc) {
            Texture2D tx = Frames[(int)Projectile.ai[1]];
            SpriteEffects se = SpriteEffects.None;
            if (Projectile.velocity.X < 0) {
                se = SpriteEffects.FlipVertically;
            }
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tx.Size() / 2, new Vector2(Projectile.scale, Projectile.scale), se, 0);
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }
    }


}