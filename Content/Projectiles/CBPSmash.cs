using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class CBPSmash : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 1024;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 4;
            Projectile.friendly = true;
        }
        public override void AI() {
            Projectile.tileCollide = Projectile.velocity.Length() > 4;
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 16) {
                oldPos.RemoveAt(0);
            }
            if (Projectile.ai[2]++ > 60) {
                Projectile.velocity.Y += 0.02f;
            }

            Projectile.GetOwner().Entropy().screenPos = Projectile.Center;
            Projectile.GetOwner().Entropy().screenShift = float.Lerp(Projectile.GetOwner().Entropy().screenShift, 0.4f, 0.1f);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float scale = 3 * Projectile.scale;
            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            for (int i = 0; i < oldPos.Count; i++) {
                float c = (i + 1f) / oldPos.Count;
                DrawEnergyBall(oldPos[i], scale * c, Projectile.Opacity * c);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public static void DrawEnergyBall(Vector2 pos, float size, float alpha) {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(100, 100, 160) * alpha, 0, tex.Size() * 0.5f, size * 0.24f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(30, 30, 80) * alpha, 0, tex.Size() * 0.5f, size * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
        }
        public override void OnKill(int timeLeft) {
            ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.Zero, 64), Projectile.Center);
            //AdditiveBlend走Configure分桶,旧版粒子系统 Before层那套
            PRTLoader.NewParticle<PRT_HeavenfallStar3>(Projectile.GetOwner().Center, Vector2.Zero, new Color(100, 100, 255), 12).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);  //AdditiveBlend走Configure分桶,旧版粒子系统 Before层那套
        }
    }


}
