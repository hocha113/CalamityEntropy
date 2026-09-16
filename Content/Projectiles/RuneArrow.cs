using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class RuneArrow : ModProjectile
    {
        List<Vector2> odp = new List<Vector2>();
        List<float> odr = new List<float>();
        bool htd = false;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.ArmorPenetration = 30;
        }

        public override void AI() {


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
                odr.Add(Projectile.rotation);
                if (odp.Count > 16) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }

                NPC target = Projectile.FindTargetWithinRange(1100, false);
                if (target != null) {
                    Projectile.velocity *= 0.9f;
                    Vector2 v = target.Center - Projectile.Center;
                    v.Normalize();

                    Projectile.velocity += v * 3f;
                }
            }
            //PRT_RuneParticle字段(homing/target)旧初始化器拆成spawn后直赋
            PRTLoader.NewParticle<PRT_RuneParticle>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-0.6f, 0.6f), Color.White, Projectile.scale * 0.76f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);

            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (htd) {
                return false;
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            for (int i = 0; i < 16; i++) {
                //RuneParticle字段(homing/target)旧初始化器拆成spawn后直赋
                PRTLoader.NewParticle<PRT_RuneParticle>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-5f, 5f), Color.White, Projectile.scale * 0.6f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
            }
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 300);
            if (!htd) {
                Projectile.timeLeft = 20;
                htd = true;
            }
            CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3).ToString(), Main.rand.NextFloat(0.7f, 1.3f), target.Center, 10, 0.4f);
        }
        public override bool PreDraw(ref Color lightColor) {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            Color cl = Color.Lerp(Color.Black, Color.White, Projectile.ai[0] / 30f);
            float c = 0;


            c = 0;
            if (odp.Count > 1) {
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.White * 0.7f;
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 20 * 0.6f,
                          new Vector3((float)0, 1, 1),
                          b));
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 20 * 0.6f,
                      new Vector3((float)0, 0, 1),
                      b));
                for (int i = 1; i < odp.Count; i++) {


                    c += 1f / odp.Count;
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 20 * 0.6f,
                          new Vector3((float)i / odp.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 20 * 0.6f,
                          new Vector3((float)i / odp.Count, 0, 1),
                          b));

                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.rvslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                if (!htd) {
                    Texture2D light = CEExtraAssets.lightball;
                    Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition, null, Color.White * 0.4f, Projectile.rotation, light.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                if (htd) {
                    return false;

                }
                Main.spriteBatch.Draw(TextureAssets.Projectile[Projectile.type].Value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, TextureAssets.Projectile[Projectile.type].Value.Size() / 2, Projectile.scale * 0.6f, SpriteEffects.None, 0);

            }
            odp.RemoveAt(odp.Count - 1);
            odr.RemoveAt(odr.Count - 1);
            return false;
        }

    }

}