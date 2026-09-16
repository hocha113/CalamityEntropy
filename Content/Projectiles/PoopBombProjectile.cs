using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopBombProjectile : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.light = 0f;
            Projectile.timeLeft = 180 * 60;
        }
        public bool canDamageEnemies = true;
        public bool BreakWhenHitNPC => true;
        public int life = 5;
        public int getFrame => 5 - life;
        public int damageChance => 100;
        public override void OnSpawn(IEntitySource source) {
            Projectile.damage *= 6;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(life);
            writer.Write(kill);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            life = reader.ReadInt32();
            kill = reader.ReadBoolean();
        }
        public bool kill = false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (BreakWhenHitNPC) {
                kill = true;
            }
            Projectile.netUpdate = true;
        }
        public virtual int Damage => 80;
        public bool shooted = false;
        public int immute = 0;
        public override void AI() {
            bool onPlat = false;
            expCounter += 0.2f / 30f;
            if (expCounter > 1) {
                if (!Exp) {
                    Projectile.hostile = true;
                    CEUtils.PlaySound("boss explosions 0", 1, Projectile.Center);
                    Projectile.timeLeft = 4;
                    Projectile.Resize(256, 256);
                    for (int i = 0; i < 30; i++) {
                        Dust smokeDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                        smokeDust.velocity *= 2.8f;
                    }

                    for (int j = 0; j < 20; j++) {
                        Dust fireDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 3.5f);
                        fireDust.noGravity = true;
                        fireDust.velocity *= 7f;
                        fireDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1.5f);
                        fireDust.velocity *= 6f;
                    }

                    for (int k = 0; k < 2; k++) {
                        float speedMulti = 0.4f;
                        if (k == 1) {
                            speedMulti = 0.8f;
                        }
                        speedMulti *= 2;
                        Gore smokeGore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, default, Main.rand.Next(GoreID.Smoke1, GoreID.Smoke3 + 1));
                        smokeGore.velocity *= speedMulti;
                        smokeGore.velocity += Vector2.One;
                        smokeGore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, default, Main.rand.Next(GoreID.Smoke1, GoreID.Smoke3 + 1));
                        smokeGore.velocity *= speedMulti;
                        smokeGore.velocity.X -= 1f;
                        smokeGore.velocity.Y += 1f;
                        smokeGore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, default, Main.rand.Next(GoreID.Smoke1, GoreID.Smoke3 + 1));
                        smokeGore.velocity *= speedMulti;
                        smokeGore.velocity.X += 1f;
                        smokeGore.velocity.Y -= 1f;
                        smokeGore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, default, Main.rand.Next(GoreID.Smoke1, GoreID.Smoke3 + 1));
                        smokeGore.velocity *= speedMulti;
                        smokeGore.velocity -= Vector2.One;
                    }
                }
                expCounter = 1;
                Exp = true;
            }
            if (!CEUtils.isAir(Projectile.Center + new Vector2(0, Projectile.height / 2 + 1), true)) {
                onPlat = true;
                if (Projectile.velocity.Y > 0) {
                    Projectile.velocity.Y = 0;
                }
            }
            if (kill) {
                Projectile.Kill();
                return;
            }
            Player player = Projectile.owner.ToPlayer();
            if (player.Entropy().holdingPoop && !shooted) {
                Projectile.Center = player.Center + new Vector2(0, -36) + player.gfxOffY * Vector2.UnitY;
                return;
            }
            if (!shooted && Projectile.owner == Main.myPlayer) {
                shooted = true;
                Projectile.velocity = (Main.MouseWorld - player.Center).SafeNormalize(new Vector2(0, -1)) * 18;
            }
            if (Projectile.velocity.Y == 0) {
                Projectile.velocity.X *= 0.6f;
                if (canDamageEnemies) {
                    canDamageEnemies = false;
                }
            }
            if (!onPlat) {
                Projectile.velocity.Y += 0.82f;
                if (Projectile.velocity.Y > 15) {
                    Projectile.velocity.Y = 15;
                }
            }
        }
        public float expCounter = 0;
        public bool Exp = false;
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
            modifiers.FinalDamage *= 0.12f;
        }
        public override bool ShouldUpdatePosition() {
            if (Exp) {
                return false;
            }
            return !Projectile.owner.ToPlayer().Entropy().holdingPoop || shooted;
        }
        public override void OnKill(int timeLeft) {
            if (!shooted) {
                Projectile.owner.ToPlayer().Entropy().holdingPoop = false;
            }
        }
        public override bool? CanHitNPC(NPC target) {
            if (Exp) {
                return true;
            }
            return false;
        }
        public override bool CanHitPlayer(Player target) {
            if (Exp) {
                return true;
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Exp) {
                return false;
            }
            float l = 0.5f + (float)(Math.Cos(expCounter * 0.08f * Projectile.timeLeft));
            Color c = Color.Lerp(lightColor, Color.Red, l);
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, c, Projectile.rotation, new Vector2(tex.Height / 2, tex.Height / 2), 1, SpriteEffects.None);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.velocity = Vector2.Zero;
            return false;
        }

    }


}