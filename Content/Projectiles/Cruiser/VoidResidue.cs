using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Cruiser
{

    public class VoidResidue : ModProjectile
    {
        List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 160);
        }
        public override void SetDefaults() {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 600;
        }
        public bool setv = true;
        public override void AI() {
            Projectile.velocity *= 0.996f;
            if (Projectile.timeLeft < 20) {
                Projectile.Opacity -= 0.05f;
            }
            for (int i = 0; i < 2; i++) {
                //PRT_Void字段直赋对齐旧VoidParticles,Opacity/ad/multShrink Configure管不了
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, CEUtils.randomPointInCircle(3), Color.White, 1f);
                p.Opacity = 0.3f * Main.rand.NextFloat();
            }
            Projectile.rotation += Projectile.velocity.Length() * 0.01f * (Projectile.velocity.X > 0 ? 1 : -1);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D outline = Projectile.GetTexture(); //ModContent.Request<Texture2D>("CalamityEntropy/Content/Projectiles/Cruiser/VoidResidueOutline").Value;
            for (float i = 0; i < 360; i += 16) {
                Main.spriteBatch.Draw(outline, Projectile.Center - Main.screenPosition + MathHelper.ToRadians(i).ToRotationVector2() * 2, null, Color.White * Projectile.Opacity, Projectile.rotation, outline.Size() / 2, 1, SpriteEffects.None, 0);
            }
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            Texture2D t = Projectile.GetTexture();
            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, t.Size() / 2, 1, SpriteEffects.None, 0);

            return false;
        }
    }

}