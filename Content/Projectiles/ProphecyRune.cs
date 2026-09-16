using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ProphecyRune : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 400;

        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            for (int i = 0; i < 9; i++) {
                //PRT_RuneParticle字段(homing/target)旧初始化器拆成spawn后直赋
                PRTLoader.NewParticle<PRT_RuneParticle>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-5f, 5f), Color.White, Projectile.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);  //RuneParticle字段(homing/target)旧初始化器拆成spawn后直赋
            }
            CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3).ToString(), Main.rand.NextFloat(0.7f, 1.3f), target.Center, 10, 0.4f);
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 300);
        }
        public override bool? CanHitNPC(NPC target) {
            if (counter < 100) {
                return false;
            }
            return null;
        }
        public float rotCount = 0;
        public float counter = 0;
        public override void AI() {
            rotCount += 0.32f * ((100 - counter) * 0.01f);
            counter++;
            if (counter > 100) {
                NPC target = CEUtils.findTarget(Projectile.GetOwner(), Projectile, 2800);
                if (target != null) {
                    Projectile.velocity += (target.Center - Projectile.Center).normalize() * 1.9f;
                    Projectile.velocity *= 0.96f;
                }
            }
            else {
                NPC target = CEUtils.findTarget(Projectile.GetOwner(), Projectile, 2800);
                if (target != null) {
                    Projectile.Center = target.Center + (Projectile.ai[0]).ToRotationVector2().RotatedBy(rotCount) * counter * 1.8f;
                }
                else {
                    Projectile.Kill();
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D light = CEExtraAssets.lightball;
            Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition, null, Color.White * (counter > 100 ? 1 : counter / 100f) * 0.8f, Projectile.rotation, light.Size() / 2, Projectile.scale * 0.8f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D tx = CEUtils.getExtraTex("runes/rune" + ((int)Projectile.ai[2]).ToString());
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White * (counter > 100 ? 1 : counter / 100f) * (0.8f + (float)(Math.Cos(Main.GameUpdateCount * 0.2f) * 0.2f)), 0, tx.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

    }

}