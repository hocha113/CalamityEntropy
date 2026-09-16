using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class LightSoul : EBookBaseProjectile
    {
        //拖尾贴图,加载期就位,绘制时不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/LightSoul")]
        internal static Asset<Texture2D> TrailTex;
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public Vector2 dscp = Vector2.Zero;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Ranged;
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

        // 2026-08-31 平衡案:光明能量不再回血/消除弹幕,改为直接飞向玩家并回复5点魔力
        public override void AI() {
            base.AI();
            if (Projectile.timeLeft < 3) {
                return;
            }
            counter++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Player owner = Projectile.GetOwner();
            if (owner == null || !owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            if (counter < 24) {
                Projectile.velocity *= 0.95f;
            }
            else {
                if (l < 6) {
                    l += 0.06f;
                }
                Projectile.velocity += (owner.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * (1.2f + l * 0.4f);
                if (Projectile.velocity.Length() > 22) {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 22;
                }
                if (Projectile.getRect().Intersects(owner.getRect())) {
                    for (int i = 0; i < 12; i++) {
                        //GlowSpark旧PRT/EParticle,Configure尾参统一签名那套
                        PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 7), Color.White, Main.rand.NextFloat(0.08f, 0.12f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    }
                    CEUtils.PlaySound("soulexplode", 1.2f, Projectile.Center, maxIns: 2, volume: 0.4f);
                    if (Projectile.owner == Main.myPlayer) {
                        owner.statMana = Math.Min(owner.statManaMax2, owner.statMana + 5);
                        owner.ManaEffect(5);
                    }
                    Projectile.Kill();
                    return;
                }
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

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 9 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 9 * Projectile.scale,
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