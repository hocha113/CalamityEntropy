using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AmethystFrisbeeProjectile : ModProjectile
    {
        List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.light = 0.1f;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ArmorPenetration = 10;
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            hitbox = Projectile.Center.getRectCentered(Projectile.scale * 40, Projectile.scale * 40);
        }
        public float counter { get { return Projectile.localAI[0]; } set { Projectile.localAI[0] = value; } }
        public float grav { get { return Projectile.localAI[2]; } set { Projectile.localAI[2] = value; } }
        public float gravity = 2.4f;
        public bool hited {
            get { return Projectile.ai[1] > 0; }
            set { Projectile.ai[1] = 1; }
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AmethystFrisbee";
        public bool homing = false;
        public override void AI() {
            Projectile.rotation += Projectile.velocity.X * 0.03f;
            counter++;
            if (Projectile.ai[0] == 0) {
                Projectile.localNPCHitCooldown = 20;
                if (hited && Projectile.ai[2] == 0) {
                    Projectile.ai[2]++;
                    Vector2 targetPosition = Projectile.GetOwner().Center;
                    float speed = 0;
                    int jspeed = 38;
                    float js;
                    int mct = 0;
                    float basej = 0.01f;
                    float bspeed = jspeed;
                    int count = 0;
                    while (basej > 0) {
                        basej += bspeed;
                        bspeed -= gravity;
                        count++;
                    }
                    mct = count;
                    speed = CEUtils.getDistance(Projectile.Center, Projectile.GetOwner().Center) / count;
                    js = jspeed;
                    Vector2 jv = (Projectile.GetOwner().Center - Projectile.Center).ToRotation().ToRotationVector2() * speed;
                    jv.Y -= js;
                    Projectile.velocity = jv;

                    Projectile.netUpdate = true;
                }
                if (hited) {
                    if (Projectile.Distance(Projectile.GetOwner().Center) < Projectile.velocity.Length() + 54) {
                        if (Projectile.GetOwner().HeldItem.ModItem is AmethystFrisbee af) {
                            af.altShotCount = 16;
                        }
                        Projectile.Kill();
                    }
                    if (homing) {
                        if (Projectile.localAI[1] < 0.12f) {
                            Projectile.localAI[1] += 0.0025f;
                        }
                        Projectile.velocity *= 1f - Projectile.localAI[1];
                        Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * (Projectile.localAI[1] * 56);

                    }
                    else {
                        Projectile.velocity.Y += gravity;
                        Projectile.velocity *= 0.997f;
                        Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * 0.3f;

                    }
                }
                else {
                    if (Projectile.Distance(Projectile.owner.ToPlayer().Center) > 1400) {
                        hited = true;
                    }
                    Projectile.velocity.Y += grav;
                    if (grav < 2f) {
                        grav += 0.05f;
                    }
                    if (counter % 8 == 0) {
                        CEUtils.PlaySound("spin" + Main.rand.Next(1, 3).ToString(), 1, Projectile.Center, 8, 0.5f);
                    }
                }
            }
            else {
                Projectile.localNPCHitCooldown = 5;
                if (counter > 30) {
                    Projectile.velocity *= 0.92f;
                    Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * 3f;
                    if (Projectile.Distance(Projectile.GetOwner().Center) < Projectile.velocity.Length() + 40) {
                        Projectile.Kill();
                    }
                }
                else {
                    if (counter % 6 == 0) {
                        CEUtils.PlaySound("spin" + Main.rand.Next(1, 3).ToString(), 1, Projectile.Center);
                    }
                }
            }
            odp.Add(Projectile.Center);
            if (odp.Count > 8) {
                odp.RemoveAt(0);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Projectile.damage = (int)(Projectile.damage * 0.9f);
            if (!hited) {
                CEUtils.PlaySound("shield", pos: Projectile.Center);
                CEUtils.PlaySound("SarosDiskThrow1", pos: Projectile.Center);
            }
            else {
                homing = true;
            }
            CEUtils.PlaySound("bne_hit2", pos: Projectile.Center);
            if (!hited && Projectile.IsEmpowered()) {
                CEUtils.PlaySound("crystalShieldBreak", pos: Projectile.Center);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<AmethystExplosion>(), Projectile.damage, 1, Projectile.owner);
            }
            hited = true;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            if (!hited) {
                CEUtils.PlaySound("shield", pos: Projectile.Center);
            }
            else {
                homing = true;
            }
            hited = true;
        }
        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (!hited) {
                CEUtils.PlaySound("shield", pos: Projectile.Center);
            }
            else {
                homing = true;
                if (Projectile.velocity.Y == 0 && oldVelocity.Y != 0) {
                    Projectile.velocity.Y = oldVelocity.Y * -0.8f;
                }
                if (Projectile.velocity.X == 0 && oldVelocity.X != 0) {
                    Projectile.velocity.X = oldVelocity.X * -0.8f;
                }
                Projectile.tileCollide = false;
            }
            hited = true;
            if (Projectile.ai[0] == 1) {
                counter = 41;
            }

            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            odp.Add(Projectile.Center);
            if (odp.Count > 2) {
                List<Vector2> poses = new List<Vector2>();
                for (int i = 1; i < odp.Count; i++) {
                    for (float h = 0.1f; h <= 1; h += 0.1f) {
                        poses.Add(Vector2.Lerp(odp[i - 1], odp[i], h));
                    }
                }
                Color b = Color.MediumPurple * 1.2f;
                Color w = Color.White;
                List<ColoredVertex> vep = new List<ColoredVertex>();
                List<ColoredVertex> vew = new List<ColoredVertex>();
                float ds = Main.GlobalTimeWrappedHourly * 4;
                for (int i = 1; i < poses.Count; i++) {
                    float p = i / (poses.Count - 1f);
                    float a = p;

                    vep.Add(new ColoredVertex(poses[i] - Main.screenPosition + (poses[i] - poses[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 24 * p * Projectile.scale,
                          new Vector3(p + ds, 1, 1),
                        b * a));
                    vep.Add(new ColoredVertex(poses[i] - Main.screenPosition + (poses[i] - poses[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 24 * p * Projectile.scale,
                          new Vector3(p + ds, 0, 1),
                          b * a));
                    vew.Add(new ColoredVertex(poses[i] - Main.screenPosition + (poses[i] - poses[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 24 * p * Projectile.scale,
                          new Vector3(p + ds, 1, 1),
                        w * a));
                    vew.Add(new ColoredVertex(poses[i] - Main.screenPosition + (poses[i] - poses[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 24 * p * Projectile.scale,
                          new Vector3(p + ds, 0, 1),
                          w * a));
                }
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (vep.Count >= 3) {
                    Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
                    gd.Textures[0] = CEExtraAssets.Streak2;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vep.ToArray(), 0, vep.Count - 2);
                    gd.Textures[0] = CEExtraAssets.Streak1;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vew.ToArray(), 0, vew.Count - 2);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }
            odp.RemoveAt(odp.Count - 1);
            Texture2D tx = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }

    }

}