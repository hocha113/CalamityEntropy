using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public abstract class PoopProj : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.light = 0f;
            Projectile.timeLeft = 600 * 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        public bool canDamageEnemies = true;
        public virtual bool BreakWhenHitNPC => true;
        public int life = 5;
        public int getFrame => 5 - life;
        public virtual int damageChance => 100;
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(life);
            writer.Write(kill);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            life = reader.ReadInt32();
            kill = reader.ReadBoolean();
        }
        public bool kill = false;

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
            fallThrough = false;
            return true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (BreakWhenHitNPC) {
                kill = true;
            }
            Projectile.netUpdate = true;
        }
        public virtual int Damage => 80;
        public bool shooted = false;
        public int immute = 0;
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }

        public override void AI() {
            if (Projectile.timeLeft % 60 == 0) {
                Projectile.netUpdate = true;
            }
            if (kill) {
                for (int i = 0; i < 64; i++) {
                    Vector2 dustVel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(1, 3);
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, dustVel.X, dustVel.Y);
                }
                Projectile.Kill();
                return;
            }
            Player player = Projectile.owner.ToPlayer();
            if (player.Entropy().holdingPoop && !shooted) {
                Projectile.Center = player.Center + new Vector2(0, -50) + player.gfxOffY * Vector2.UnitY;
                return;
            }
            if (!shooted && Projectile.owner == Main.myPlayer) {
                shooted = true;
                Projectile.velocity = (Main.MouseWorld - player.Center).SafeNormalize(new Vector2(0, -1)) * 18;
            }
            if (life <= 0) {
                Projectile.Kill();
                return;
            }
            if (Projectile.velocity.Y == 0) {
                Projectile.velocity.X *= 0.6f;
                if (canDamageEnemies) {
                    CEUtils.PlaySound("poopland", 1, Projectile.Center);
                    for (int i = 0; i < 16; i++) {
                        Vector2 dustVel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(1, 3);
                        Dust.NewDust(Projectile.position + new Vector2(0, Projectile.height), Projectile.width, 1, dustType, dustVel.X, dustVel.Y);
                    }
                    canDamageEnemies = false;
                }
            }
            Projectile.velocity.Y += 0.82f;
            if (Projectile.velocity.Y > 15) {
                Projectile.velocity.Y = 15;
            }

            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.Hitbox.Intersects(Projectile.Hitbox)) {
                    PushNPC(npc);
                }
            }
            immute--;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.active && p.ModProjectile is not PoopProj && p.damage > 0 && p.ModProjectile is not BlueFlies && p.ModProjectile is not Flame && p.type != ProjectileID.SilverCoin && p.type != ProjectileID.GoldCoin && p.type != ProjectileID.PlatinumCoin) {
                    bool? f = ProjectileLoader.CanDamage(p);
                    if (f.HasValue && f.Value) {
                        if (p.Colliding(p.getRect(), Projectile.getRect())) {
                            if (p.ModProjectile is PoopBombProjectile pb) {
                                if (pb.Exp) {
                                    kill = true;
                                }
                            }
                            else {
                                if (p.ModProjectile is FartCloud fc) {
                                    if (fc.Exp) {
                                        kill = true;
                                    }
                                }
                                else {
                                    if (p.hostile) {
                                        p.Kill();
                                        DamageMe();
                                    }
                                    if (p.friendly) {
                                        if (immute <= 0) {
                                            DamageMe();
                                            immute = 10;
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }
        public override bool ShouldUpdatePosition() {
            return !Projectile.owner.ToPlayer().Entropy().holdingPoop || shooted;
        }
        public override void OnKill(int timeLeft) {
            if (!shooted) {
                Projectile.owner.ToPlayer().Entropy().holdingPoop = false;
            }
        }
        public virtual bool damageNPCAfterLand => false;
        public virtual void DamageMe() {
            Projectile.netUpdate = true;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile is PoopWulfrumProjectile w && CEUtils.getDistance(Projectile.Center, p.Center) < PoopWulfrumProjectile.shieldDistance) {
                    if (w.shield > 0) {
                        w.shield -= 8;
                        p.netUpdate = true;
                        return;
                    }
                }
            }
            CEUtils.PlaySound("pop_impact_13", 1, Projectile.Center);
            if (Main.rand.Next(0, 101) <= damageChance) {
                life--;
                if (life <= 0) {
                    Projectile.Kill();
                }
            }
            for (int i = 0; i < 24; i++) {
                Vector2 dustVel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(1, 3);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, dustVel.X, dustVel.Y);
            }
            Projectile.netUpdate = true;
        }

        public virtual int dustType => DustID.Poop;

        public virtual void PushNPC(NPC npc) {
            float pushStrenth = 1;
            if (npc.noGravity) {
                npc.velocity += (npc.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * pushStrenth;
            }
            else {
                if (npc.Center.X < Projectile.Center.X) {
                    npc.velocity.X -= pushStrenth;
                }
                else {
                    npc.velocity.X += pushStrenth;
                }
            }
        }
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.owner.ToPlayer().Entropy().holdingPoop && !shooted) {
                return false;
            }
            if (!canDamageEnemies && !damageNPCAfterLand) {
                return false;
            }
            return null;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + new Vector2(0, 14), CEUtils.GetCutTexRect(tex, 5, getFrame), lightColor, Projectile.rotation, new Vector2(tex.Height / 2, tex.Height / 2), 1, SpriteEffects.None);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.velocity = Vector2.Zero;
            return false;
        }

    }


}