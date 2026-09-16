using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VoidImpact : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 116;
            Projectile.height = 116;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 5;
            Projectile.scale = 2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("flashback", 1.4f, Projectile.Center, 3, CEUtils.WeapSound * 0.4f);
            float[] rots = new float[] { MathHelper.PiOver4, -MathHelper.PiOver4, MathHelper.PiOver4 * 3, MathHelper.PiOver4 * -3 };
            for (int i = 0; i < rots.Length; i++) {
                float r = rots[i] + Projectile.rotation;
                //VoidImpactParticle命中反馈,旧PRT/EParticle VoidImpact
                PRTLoader.NewParticle<PRT_VoidImpactParticle>(target.Center, r.ToRotationVector2() * 9, Color.White, 1.8f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, r, 46);  //VoidImpactParticle命中反馈,旧EParticle VoidImpact
                PRTLoader.NewParticle<PRT_VoidImpactParticle>(target.Center, r.ToRotationVector2() * 12, Color.White, 2f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, r, 46);
            }
            Projectile.damage = 0;
        }
        public override void AI() {
            if (Projectile.ai[2] == 0)
                Projectile.ai[2] = 1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 64) {
                oldPos.RemoveAt(0);
            }
            if (Projectile.damage == 0) {
                Projectile.ai[2] *= 0.98f;
                Projectile.velocity *= 0.97f;
            }
        }

        public List<Vector2> oldPos = new List<Vector2>();
        public override bool PreDraw(ref Color lightColor) {
            lightColor = Color.White * Projectile.ai[2];

            if (Projectile.timeLeft < 30) {
                lightColor *= ((float)Projectile.timeLeft / 30f);
            }
            float scale = 0;
            float scale2 = 1 + (float)Math.Cos(Main.GameUpdateCount * 0.52f) * 0.12f;
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 0; i < oldPos.Count; i++) {
                scale += 1f / oldPos.Count;
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, lightColor * ((float)i / (float)oldPos.Count) * 0.6f, Projectile.rotation, tex.Size() / 2, Projectile.scale * Projectile.ai[2] * scale * scale2 * 0.6f, SpriteEffects.None, 0);
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale * scale * scale2 * Projectile.ai[2], SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale * scale * scale2 * 0.8f * Projectile.ai[2], SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale * scale * scale2 * 0.6f * Projectile.ai[2], SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;

        }
    }


}