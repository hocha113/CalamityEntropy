using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VoidshadeHeld : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 55;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public int attackType { get { return (int)Projectile.ai[0]; } }
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRots = new List<float>();
        public List<float> oldScale = new List<float>();
        public bool init = false;
        public float rotSpeed = 0;
        public float counter = 0;
        public float cspeed = 0;
        public float c = 0;
        public float dash = 30;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (counter > 6 && counter < 60) {
                return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 92 * Projectile.scale * getScale(), targetHitbox, 36);
            }
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (dash >= 0 && attackType == 3) {
                dash = -32;
                Projectile.owner.ToPlayer().velocity *= -0.05f;
                Projectile.owner.ToPlayer().Entropy().voidshadeBoostTime = 90;
            }
            else {
                CEUtils.PlaySound("antivoidhit", Main.rand.NextFloat(0.8f, 1.2f), target.Center, volume: CEUtils.WeapSound);
                Color impactColor = Color.LightBlue;
                float impactParticleScale = Main.rand.NextFloat(1.4f, 1.6f);

                //PRT_SparkleCal bloom/color在Configure里,旧Calamity SparkleParticle两色构造
                PRTLoader.NewParticle<PRT_SparkleCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, impactColor, impactParticleScale).Configure(Color.Blue, 8, 0, 2.5f);


                float sparkCount = 32;
                for (int i = 0; i < sparkCount; i++) {
                    float p = Main.rand.NextFloat();
                    Vector2 sparkVelocity2 = (target.Center - Projectile.Center).normalize().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(12, 36 * (2 - p));
                    int sparkLifetime2 = (int)((2 - p) * 7);
                    float sparkScale2 = 0.6f + (1 - p);
                    Color sparkColor2 = Color.Lerp(Color.DeepSkyBlue, Color.Purple, p);
                    if (Main.rand.NextBool()) {
                        //AltSpark Configure(bool,int)是Ports签名,不是opacity/glow/mode那套
                        PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                    }
                    else {
                        PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (Projectile.frame == 7 ? 1f : 0.65f), Main.rand.NextBool() ? Color.AliceBlue : Color.SkyBlue, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2 * (Projectile.frame == 7 ? 1.2f : 1f)));
                    }
                }
            }
        }
        public bool st = true;
        public bool vsboost = false;
        public bool spawnProj = true;
        public override void AI() {
            if (Projectile.localAI[1]++ == 0) {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            Player player = Projectile.owner.ToPlayer();
            if (counter < 60) {
                player.itemTime = 2;
                player.itemAnimation = 2;
            }

            float speed = player.GetTotalAttackSpeed(Projectile.DamageType);
            if (attackType == 3) {
                if (counter > 9) {
                    dash -= speed;

                    if (dash > 0) {
                        player.velocity = Projectile.velocity * 2 * speed;
                    }
                    else {
                        if (dash > -30) {
                            player.velocity *= 0.88f;
                        }
                    }
                }
                if (counter >= 20 && spawnProj) {
                    spawnProj = false;
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, Projectile.velocity, ModContent.ProjectileType<WohLaser>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner, 0, 0, 2).ToProj().DamageType = Projectile.DamageType;
                    }
                }
                player.heldProj = Projectile.whoAmI;
                if (!init) {
                    init = true;
                    Projectile.direction = (Projectile.velocity.X > 0 ? 1 : -1);
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(16 * Projectile.direction);
                }
                if (counter < 12) {
                    cspeed += 0.01f * speed;
                }
                else {
                    cspeed -= 0.004f * speed;
                }
                Projectile.Center = player.Center + Projectile.rotation.ToRotationVector2() * c * 18 * getScale() - Projectile.rotation.ToRotationVector2() * 60 * getScale();
                c += cspeed * speed;
                Projectile.rotation += -0.003f * c * Projectile.direction * speed;
                counter += speed;
                if (counter > 60) {
                    Projectile.Kill();
                    player.itemTime = 1;
                    player.itemAnimation = 1;
                }
                if (counter < 44) {
                    oldPos.Add(Projectile.Center + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.ToRadians(-10) * Projectile.direction) * 130 * getScale() * Projectile.scale);

                    oldRots.Add(Projectile.rotation);
                    oldScale.Add(1);
                }
            }
            else {
                if (counter >= 1 && sb && player.Entropy().voidshadeBoostTime > 0) {
                    sb = false;
                    player.Entropy().voidshadeBoostTime = 0;
                    vsboost = true;
                    Projectile.damage *= 2;
                }
                if (counter >= 16 && st) {
                    st = false;
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, Projectile.velocity * 0.52f, ModContent.ProjectileType<VoidImpact>(), (int)(Projectile.damage), Projectile.knockBack, Projectile.owner);
                    }
                }
                if (!init) {
                    init = true;
                    Projectile.direction = (Projectile.velocity.X > 0 ? 1 : -1);
                    if (Projectile.direction == 1) {
                        if (attackType == 0) {
                            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(180 - 20);
                        }
                        else {
                            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.ToRadians(180 - 20);
                        }
                    }
                    else {
                        if (attackType != 0) {
                            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.ToRadians(180 - 20);
                        }
                        else {
                            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(180 - 20);
                        }
                    }
                }
                counter += speed;

                if (counter < 8) {
                    if (attackType == 0) {
                        rotSpeed += -0.06f * Projectile.direction * speed;
                    }
                    else {
                        rotSpeed += 0.06f * Projectile.direction * speed;
                    }
                }
                else {
                    rotSpeed *= (float)Math.Pow(0.9f, 1.0 / speed);
                }
                if (counter > 60) {
                    Projectile.Kill();
                    player.itemTime = 1;
                    player.itemAnimation = 1;
                }
                Projectile.rotation += rotSpeed * Projectile.direction;
                Player owner = player;
                if (counter <= 50) {
                    if (Projectile.velocity.X > 0) {
                        owner.direction = 1;
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI / 2));
                    }
                    else {
                        owner.direction = -1;
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI / 2));
                    }
                }
                Projectile.Center = player.RotatedRelativePoint(player.MountedCenter);
                player.heldProj = Projectile.whoAmI;

                if (counter < 36) {
                    if (counter % 2 == 1) {
                        oldPos.Add(Projectile.Center);
                        oldPos.Add(Projectile.Center);
                        oldPos.Add(Projectile.Center);
                    }
                    else {
                        oldPos.Add(Projectile.Center + player.velocity / 2);
                        oldPos.Add(Projectile.Center + player.velocity / 2);
                        oldPos.Add(Projectile.Center + player.velocity / 2);
                    }
                    oldRots.Add(Projectile.rotation - rotSpeed * Projectile.direction - rotSpeed * Projectile.direction * 0.6666f);
                    oldScale.Add(float.Lerp(getScale(), LastScale, 0.6666f) * Projectile.scale);
                    oldRots.Add(Projectile.rotation - rotSpeed * Projectile.direction - rotSpeed * Projectile.direction * 0.3333f);
                    oldScale.Add(float.Lerp(getScale(), LastScale, 0.3333f) * Projectile.scale);
                    oldRots.Add(Projectile.rotation - rotSpeed * Projectile.direction);
                    oldScale.Add(getScale() * Projectile.scale);
                    LastScale = getScale();
                }

            }

            for (int i = 0; i < 3; i++) {
                if ((oldPos.Count > 32 && counter > 6) || (counter >= 34 && oldPos.Count > 0)) {
                    oldPos.RemoveAt(0);
                    oldRots.RemoveAt(0);
                    oldScale.RemoveAt(0);
                }
            }
        }
        public float LastScale = 0;
        public float trailOffset = 0;
        public float getScale() {
            /*float x = (float)counter / 60f;
            x = Math.Clamp(x, 0, 1);

            double deviation = Math.Abs(x - 0.5);

            float value = (float)Math.Exp(-deviation * 6);
*/
            if (attackType == 3) {
                return 2;
            }
            return 1.2f + (float)Math.Sqrt(Math.Abs(rotSpeed / Projectile.owner.ToPlayer().GetTotalAttackSpeed(Projectile.DamageType) * 6 * (vsboost ? 1.6f : 1)));
        }
        public bool sb = true;
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            trailOffset += 0.06f;
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Texture2D trail;
            if (attackType == 3) {
                goto drawBlade;
            }
            var r = Main.rand;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<ColoredVertex> ve = new List<ColoredVertex>();

            for (int i = 0; i < oldRots.Count; i++) {
                Color b = Color.Lerp(Color.Purple * 0.01f, Color.Purple, ((float)(i)) / (float)oldRots.Count) * 1f;
                if (attackType == 3) {
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 16 * Projectile.scale * getScale() * ((float)(oldPos.Count - i - 1) / (float)oldPos.Count),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2().RotatedBy(-MathHelper.PiOver2) * 16 * Projectile.scale * getScale() * ((float)(oldPos.Count - i - 1) / (float)oldPos.Count),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 0, 1),
                          b));
                }
                else {
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (36 * oldScale[i] + 60 * oldScale[i] * (1 - (float)(i) / (float)oldRots.Count) * 0.5f),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (36 * oldScale[i] + 60 * oldScale[i] - 60 * oldScale[i] * (1 - ((float)(i) / (float)oldRots.Count)) * 0.5f),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 0, 1),
                          b));
                }
            }

            if (ve.Count >= 3) {
                trail = CEExtraAssets.white;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            ve = new List<ColoredVertex>();

            for (int i = 0; i < oldRots.Count; i++) {
                Color b = Color.Lerp(Color.White * 0.01f, Color.White, ((float)(i)) / (float)oldRots.Count) * 1f;
                if (attackType == 3) {
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 16 * Projectile.scale * getScale() * ((float)(oldPos.Count - i - 1) / (float)oldPos.Count),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2().RotatedBy(-MathHelper.PiOver2) * 16 * Projectile.scale * getScale() * ((float)(oldPos.Count - i - 1) / (float)oldPos.Count),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                          b));
                }
                else {
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (36 * oldScale[i] + 60 * oldScale[i] * (1 - (float)(i) / (float)oldRots.Count) * 0.5f),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (36 * oldScale[i] + 60 * oldScale[i] - 60 * oldScale[i] * (1 - ((float)(i) / (float)oldRots.Count)) * 0.5f),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 0, 1),
                          b));
                }
            }

            if (ve.Count >= 3) {
                trail = CEExtraAssets.SwordSlashTexture;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

drawBlade:
            if (counter <= 60) {
                Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
                int dir = 1;
                if (attackType == 0) {
                    dir = -1;
                }
                if (attackType == 3) {
                    if (Projectile.velocity.X < 0) {
                        dir *= -1;
                    }
                }
                Main.EntitySpriteDraw(tex, Projectile.Center + (Projectile.rotation.ToRotationVector2() * -4) * getScale() * Projectile.scale - Main.screenPosition, null, Color.White, Projectile.rotation + (dir > 0 ? MathHelper.ToRadians(50) : MathHelper.ToRadians(130)), (dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height)), Projectile.scale * getScale(), (dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            }
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}