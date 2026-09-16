using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Projectiles
{
    public class SilvaSoul : EBookBaseProjectile
    {
        //拖尾贴图,加载期就位,绘制时不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SilvaSoul")]
        internal static Asset<Texture2D> TrailTex;
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public Vector2 dscp = Vector2.Zero;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 480;
            Projectile.ArmorPenetration = 12;
        }
        public int counter = 0;
        public bool std = false;
        public float l = 0;

        public override void AI() {
            base.AI();
            //LifeLeaf bramble/silva装饰,旧PRT/EParticle LifeLeaf
            PRTLoader.NewParticle<PRT_LifeLeaf>(Projectile.Center, CEUtils.randomVec(6), Color.White, Main.rand.NextFloat(0.6f, 1.4f))
                .Configure(1, false, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());
            if (Projectile.timeLeft < 3) {
                return;
            }
            counter++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (counter < 32) {
                Projectile.velocity *= 0.96f;
            }
            else {
                Player target = Projectile.GetOwner();
                if (l < 9) {
                    l += 0.03f;
                }
                Projectile.velocity = new Vector2(Projectile.velocity.Length() + 1.4f, 0).RotatedBy(CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), 0.5f * l, false));
                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), 1f * l.ToRadians(), true));
                if (Projectile.getRect().Intersects(target.getRect())) {
                    for (int i = 0; i < 42; i++) {
                        //GlowSpark旧EParticle,Configure尾参统一签名那套
                        PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 7), Color.Gold, Main.rand.NextFloat(0.08f, 0.12f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    }
                    CEUtils.PlaySound("soulshine", 1f, Projectile.Center, maxIns: 6, volume: 0.6f);
                    Projectile.Kill();
                    Projectile.GetOwner().Heal(Projectile.GetOwner().statLifeMax2 / 200 + 3);
                    Projectile.GetOwner().Entropy().temporaryArmor += 6f;
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
            if (odp.Count > 12) {
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

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 12 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 12 * Projectile.scale,
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