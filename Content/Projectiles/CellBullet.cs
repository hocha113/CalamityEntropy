using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class CellBullet : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 300;
        }
        public bool setRot = true;
        public override void AI() {
            if (trailAlpha < 1) {
                trailAlpha += 0.05f;
            }
            if (setRot) {
                Projectile.rotation = CEUtils.randomRot();
            }
            Projectile.rotation += 0.001f;
            if (Projectile.velocity.Length() < 50) {
                Projectile.velocity *= 1.01f;
            }
        }
        float trailAlpha = 0;
        public override bool PreDraw(ref Color lightColor) {
            Texture2D t = CEUtils.getExtraTex("slash3");
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White * 0.8f * trailAlpha, Projectile.velocity.ToRotation(), new Vector2(t.Width, t.Height / 2), new Vector2(Projectile.velocity.Length() * 0.07f, 1f) * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            lightColor = Color.White;
            return base.PreDraw(ref lightColor);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 160);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 160);
        }
    }


}