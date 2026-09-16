using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class RuneTorrentRanger : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public override string Texture => CEUtils.WhiteTexPath;
        public float nowSpeed = 0;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.width = Projectile.height = 64;
            Projectile.MaxUpdates = 4;
            Projectile.timeLeft = 180;
            Projectile.penetrate = 6;
            Projectile.ArmorPenetration = 8;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }
        public bool r = true; public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 300);
        }
        public override void AI() {
            if (r) {
                r = false;
                if (Projectile.ai[1] == 0) {
                    Projectile.localAI[0] = Main.rand.NextFloat(0.0001f, MathHelper.Pi * 128);
                }
                else {
                    Projectile.localAI[0] = Main.GameUpdateCount % 256 * MathHelper.Pi;
                }
            }
            nowSpeed = Projectile.velocity.Length();
            Projectile.localAI[0] += nowSpeed;
            Projectile.velocity = Projectile.velocity.normalize() * nowSpeed;
            Projectile.Center += Projectile.velocity.RotatedBy(Math.Cos(Projectile.localAI[0] * 0.014f) * 0.4f);
            odp.Add(Projectile.Center);
            if (odp.Count > 80) {
                odp.RemoveAt(0);
            }
            spawnParticleCount += nowSpeed;
            if (spawnParticleCount > 22) {
                spawnParticleCount -= 22;
                //PRT_RuneParticle字段(homing/target)旧初始化器拆成spawn后直赋
                PRTLoader.NewParticle<PRT_RuneParticle>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-0.6f, 0.6f), Color.White, Projectile.scale * 0.76f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);  //RuneParticle字段(homing/target)旧初始化器拆成spawn后直赋
            }
        }
        public float spawnParticleCount = 0;
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {

            return false;
        }
        public void Draw() {
            if (odp.Count > 1) {
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                for (int i = 1; i < odp.Count; i++) {
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 6 * Projectile.scale,
                          new Vector3((float)i / odp.Count, 1, 1),
                          Color.Lerp(new Color(110, 120, 255), Color.White, ((float)i / odp.Count)) * ((float)i / odp.Count) * 0.86f));
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 6 * Projectile.scale,
                          new Vector3((float)i / odp.Count, 0, 1),
                          Color.Lerp(new Color(110, 120, 255), Color.White, ((float)i / odp.Count)) * ((float)i / odp.Count) * 0.86f));

                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = Projectile.GetTexture();
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
        }

    }

}