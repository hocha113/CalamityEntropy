using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FractalShoot : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.MaxUpdates = 4;
            Projectile.friendly = true;
            Projectile.penetrate = 4;
            Projectile.tileCollide = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft < 32)
                Projectile.Opacity -= 1 / 32f;
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 26)
                oldPos.RemoveAt(0);
            if (Projectile.localAI[0]++ > 10 && Projectile.numHits == 0) {
                Projectile.HomingToNPCNearby(0.7f, 0.651f);
                Projectile.HomingToNPCNearby(0.7f, 0.651f);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= 0.5f * (Projectile.timeLeft / 120f) + 0.5f;
        }

        public override void OnKill(int timeLeft) {
            for (int i = 0; i < 12; i++) {
                //GlowSpark旧PRT/EParticle,Configure尾参统一签名那套
                PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 7), Color.LightGoldenrodYellow, Main.rand.NextFloat(0.06f, 0.1f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);  //GlowSpark旧EParticle,Configure尾参统一签名那套
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseAdditive();
            if (oldPos.Count > 1) {
                for (int i = 0; i < oldPos.Count; i++) {
                    float alpha = i / (oldPos.Count - 1f);
                    Main.EntitySpriteDraw(Projectile.GetTexture(), oldPos[i] - Main.screenPosition, null, lightColor * Projectile.Opacity * alpha, Projectile.rotation, Projectile.GetTexture().Size() * 0.5f, alpha, SpriteEffects.None);
                }
            }
            Main.EntitySpriteDraw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, Projectile.GetTexture().Size() * 0.5f, 1, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }


}