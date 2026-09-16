using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class DarkSoul : EBookBaseProjectile
    {
        //拖尾贴图,加载期就位,绘制时不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/DarkSoul")]
        internal static Asset<Texture2D> TrailTex;
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public Vector2 dscp = Vector2.Zero;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void ApplyHoming() {

        }
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 260;
            Projectile.ArmorPenetration = 12;
        }
        public int counter = 0;
        public bool std = false;

        public override void AI() {
            base.AI();
            if (Projectile.timeLeft < 3) {
                return;
            }
            counter++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (counter < 32) {
                Projectile.velocity *= 0.95f;
            }
            else {
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1200);
                if (target != null) {
                    if (l < 6) {
                        l += l < 2 ? 0.014f : 0.01f;
                    }
                    Projectile.velocity = new Vector2(Projectile.velocity.Length() + 1.4f, 0).RotatedBy(CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), l / 6f, false));
                    Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), 0.3f * l, true));
                }
                Projectile.velocity *= 0.97f;
            }
        }

        public override bool? CanHitNPC(NPC target) {
            if (counter < 32) {
                return false;
            }
            return base.CanHitNPC(target);
        }
        public override void PostAI() {
            base.PostAI();
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 16) {
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
        public float l = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            if (Projectile.timeLeft > 3) {
                for (int i = 0; i < 32; i++) {
                    //GlowSpark旧PRT/EParticle,Configure尾参统一签名那套
                    PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 7), Color.Red, Main.rand.NextFloat(0.1f, 0.16f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                }
                for (int i = 0; i < odp.Count; i++) {
                    for (int i_ = 0; i_ < 6; i_++) {
                        PRTLoader.NewParticle<PRT_GlowSpark>(odp[i], CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 7) * ((float)i / odp.Count), Color.Red, Main.rand.NextFloat(0.1f, 0.16f) * ((float)i / odp.Count)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    }
                }
                CEUtils.PlaySound("soulexplode", 1.2f, Projectile.Center, maxIns: 4, volume: 0.8f);
                Projectile.timeLeft = 2;
                Projectile.Resize(256, 256);
                CEUtils.SetShake(target.Center, 6);
                //DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
                PRTLoader.NewParticle<PRT_DirectionalPulseRing>(target.Center, Vector2.Zero, Color.DarkRed, 0.1f).Configure(new Vector2(2f, 2f), 0, 1 * 0.85f, 36);
                PRTLoader.NewParticle<PRT_DetailedExplosionCal>(target.Center, Vector2.Zero, Color.DarkRed, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 1 * 0.65f, 26);
            }
        }
        public override Color baseColor => new Color(255, 255, 255);
        public void drawT() {
            if (Projectile.timeLeft < 3) {
                return;
            }
            var mp = this;
            if (mp.odp.Count > 1) {
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = this.color;
                float a = 0;
                float lr = 0;
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 14 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 14 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                          b * a));
                    lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = TrailTex.Value;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                }


            }

        }
    }

}