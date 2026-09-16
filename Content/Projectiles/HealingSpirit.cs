using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class HealingSpirit : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public Vector2 dscp = Vector2.Zero;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 900;
            Projectile.MaxUpdates = 6;
        }
        public int counter = 0;
        public bool std = false;
        public float l = 0;

        public override void AI() {
            if (Projectile.timeLeft < 3) {
                return;
            }
            counter++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (counter < 18 * Projectile.MaxUpdates) {
                Projectile.velocity *= 0.99f;
            }
            else {
                Player target = Projectile.GetOwner();
                if (l < 9) {
                    l += 0.002f;
                }
                Projectile.velocity = new Vector2(Projectile.velocity.Length() + 0.3f, 0).RotatedBy(CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), 0.5f * l, false));
                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), 1f.ToRadians() * l, true));
                if (Projectile.getRect().Intersects(target.getRect())) {
                    for (int i = 0; i < 6; i++) {
                        //GlowSpark旧PRT/EParticle,Configure尾参统一签名那套
                        PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 7), Color.Gray, Main.rand.NextFloat(0.08f, 0.12f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);  //GlowSpark旧EParticle,Configure尾参统一签名那套
                    }
                    CEUtils.PlaySound("soulshine", 1f, Projectile.Center, maxIns: 6, volume: 0.3f);
                    Projectile.Kill();
                    Projectile.GetOwner().Entropy().HealFloat(GrudgeCard.HealAmount);
                    Projectile.GetOwner().Entropy().temporaryArmor += GrudgeCard.TempDefense;
                    Projectile.GetOwner().HealMana(10);
                    return;
                }

                Projectile.velocity *= 0.97f;
            }
        }

        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override void PostAI() {
            base.PostAI();
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 20) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public int tofs;
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255) * completionRatio;
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 12 * Projectile.scale, completionRatio);
        }

        public override bool PreDraw(ref Color lightColor) {
            drawT();
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
        }
        public void drawT() {
            if (Projectile.timeLeft < 3) {
                return;
            }
            var mp = this;
            if (mp.odp.Count > 1) {
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.White;
                float a = 0;
                float lr = 0;
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 8 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 8 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                          b * a));
                    lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = this.getTextureAlt("Body");
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, overridePos: odp[odp.Count - 1]));
                }


            }

        }
    }

}
