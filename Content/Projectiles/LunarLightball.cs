using CalamityEntropy.Content.Buffs.PortsDoT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class LunarLightball : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float angle;
        public float speed = 30;
        public bool htd = false;
        public float exps = 0;
        public Vector2 dscp = Vector2.Zero;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.ArmorPenetration = 40;
            Projectile.extraUpdates = 1;
        }
        public int counter = 0;
        public bool std = false;
        public int homingTime = 60;
        public override bool? CanHitNPC(NPC target) {
            if (counter < 16) {
                return false;
            }
            return base.CanHitNPC(target);
        }
        public override void AI() {
            counter++;
            Projectile.ai[0]++;
            if (htd) {
                alpha_ = (float)Projectile.timeLeft / 34f;
                if (odp.Count > 0) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);

                }
                Projectile.velocity = Vector2.Zero;
            }
            else {
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation - MathHelper.PiOver2);
                if (odp.Count > 34) {
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
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            exps *= 0.9f;

            if (Projectile.velocity.Length() > 3) {
                Projectile.velocity *= 0.995f - homing * 0.018f;
            }
            if (counter > 2 && !htd) {
                if (homing < 4) {
                    homing += 0.015f;
                }
                NPC target = Projectile.FindTargetWithinRange(2600);

                if (target != null) {
                    if (Projectile.timeLeft < 60) {
                        Projectile.timeLeft = 60;
                    }
                    Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * homing * 2;
                }
            }
        }
        float homing = 0;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (htd) {
                return false;
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 180);
            if (!htd) {
                Projectile.timeLeft = 34;
                htd = true;
                exps = 1;
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation - MathHelper.PiOver2);
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation - MathHelper.PiOver2);
            }
        }
        public int tofs;
        float alpha_ = 1;
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
            if (htd) {
                tx = CEUtils.getExtraTex("LunarSpark");
            }
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (!htd) {
                float ap = 1f / (float)odp.Count;
                for (int i = 0; i < odp.Count; i++) {
                    Main.spriteBatch.Draw(tx, odp[i] - Main.screenPosition, null, new Color(106, 255, 180) * ap * 0.5f, odr[i] + MathHelper.PiOver2, tx.Size() / 2, new Vector2(1 + (Projectile.velocity.Length() * 0.03f), (1f / (1 + (Projectile.velocity.Length() * 0.03f)))) * 1.2f, SpriteEffects.None, 0);
                    ap += 1f / (float)odp.Count;
                }
            }
            Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, new Color(106, 255, 180) * (htd ? 1.2f : 0.5f) * alpha_, Projectile.rotation, tx.Size() / 2, new Vector2(1 + (Projectile.velocity.Length() * 0.03f), (1f / (1 + (Projectile.velocity.Length() * 0.03f)))) * 1.2f, SpriteEffects.None);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }


    }

}
