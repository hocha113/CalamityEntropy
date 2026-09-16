using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.ApsychosProjs
{
    public class ApsychosTailShoot : ModProjectile
    {
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 200 * 8;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 8;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(BuffID.OnFire3, 180);
        }
        public override void AI() {
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 90) {
                oldPos.RemoveAt(0);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = Projectile.ai[0];
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float scale = 1 * Projectile.scale;
            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            for (int i = 0; i < oldPos.Count; i++) {
                float c = (i + 1f) / oldPos.Count;
                DrawEnergyBall(oldPos[i], scale * c, Projectile.Opacity * c);
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(Color.White));
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public void DrawEnergyBall(Vector2 pos, float size, float alpha) {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(160, 160, 255) * alpha, 0, tex.Size() * 0.5f, size * 0.3f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(34, 34, 180) * alpha, 0, tex.Size() * 0.5f, size * 0.4f, SpriteEffects.None, 0);
        }
    }
}
