using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AnnihilateArrowSplit : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float angle;
        public float speed = 30;
        public bool htd = false;
        public float exps = 0;
        public Vector2 dscp = Vector2.Zero;
        float alpha = 1f;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.ArmorPenetration = 12;
            Projectile.extraUpdates = 4;
            Projectile.arrow = true;
            Projectile.ArmorPenetration = 120;
        }
        public int counter = 0;
        public bool std = false;
        public int homingTime = 60;
        public bool s = true;
        public override void AI() {
            if (Projectile.damage < 3) {
                Projectile.damage = 3;
            }
            hcounter++;
            if (s) {
                s = false;
                spawnrot = Projectile.velocity.ToRotation();
            }
            if (Projectile.timeLeft < 50) {
                alpha -= 0.02f;
                if (alpha < 0) {
                    alpha = 0;
                }
            }
            if (Main.myPlayer != Projectile.owner) {
                foreach (NPC n in Main.ActiveNPCs) {
                    if (!n.friendly && n.life > 5 && !n.dontTakeDamage && Projectile.Colliding(Projectile.Hitbox, n.Hitbox))
                        Projectile.Kill();
                }
            }
            counter++;
            Projectile.ai[0]++;
            if (htd) {
                if (odp.Count > 0) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);

                }
                Projectile.velocity = Vector2.Zero;
            }
            else {
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation - MathHelper.PiOver2);
                if (odp.Count > 26) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }

                NPC target = Projectile.FindTargetWithinRange(1600, false);
                if (target != null && CEUtils.getDistance(target.Center, Projectile.Center) < 200 && counter > 16) {
                    homingTime = 0;
                    Projectile.velocity *= 0.9f;
                    Vector2 v = target.Center - Projectile.Center;
                    v.Normalize();

                    Projectile.velocity += v * 1.5f;
                }
            }
            exps *= 0.9f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.velocity.Length() > 3) {
                Projectile.velocity *= 0.995f - homing * 0.018f;
            }
            if (counter > 4 && !htd) {
                if (homing < 6) {
                    homing += 0.014f;
                }
                NPC target = Projectile.FindTargetWithinRange(2600);

                if (target != null) {
                    if (Projectile.timeLeft < 60) {
                        Projectile.timeLeft = 60;
                    }
                    Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * homing;
                }
            }
        }
        public int hcounter = 0;
        public override bool? CanHitNPC(NPC target) {
            if (hcounter < 6) {
                return false;
            }
            return base.CanHitNPC(target);
        }
        float homing = 0;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (htd) {
                return false;
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (!htd) {
                Projectile.timeLeft = 20;
                htd = true;
                exps = 1;
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation - MathHelper.PiOver2);
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation - MathHelper.PiOver2);
            }
        }
        public int tofs;
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(140, 140, 255);
            return result * completionRatio * alpha;
        }

        public Color TrailColor2(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255);
            return result * completionRatio * alpha;
        }
        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 38 * Projectile.scale, completionRatio);
        }
        public float TrailWidth2(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 20 * Projectile.scale, completionRatio);
        }
        float spawnrot = 0;
        public override bool PreDraw(ref Color lightColor) {
            drawT();
            return false;
        }

        public void drawT() {
            tofs++;
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            Vector2 position = base.Projectile.Center - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
            Vector2 origin = value.Size() * 0.5f;
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.SylvestaffStreakAsset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth2, TrailColor2, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            if (!htd) {
                Main.EntitySpriteDraw(value, position, null, base.Projectile.GetAlpha(Color.White) * alpha, Projectile.rotation, origin, base.Projectile.scale, SpriteEffects.None);
            }
        }

    }

}