using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.SpiritFountain;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SpiritFountainShoots
{
    public class SpiritBullet : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;
            Projectile.timeLeft = 1240;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 4;
            Projectile.friendly = false;
        }

        public override void AI() {
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 3) {
                oldPos.RemoveAt(0);
            }
            if (Projectile.localAI[0]++ == 0) {
                Projectile.Opacity = 0;
            }
            Projectile.Opacity = float.Lerp(Projectile.Opacity, 1, 0.05f);
            if (Projectile.localAI[2]++ > ((int)120 * (Projectile.ai[1] + 1))) {
                if (Projectile.localAI[2] == 2 + ((int)120 * (Projectile.ai[1] + 1))) {
                    CEUtils.PlaySound("Dizzy", 1, Projectile.Center);
                    int p = Player.FindClosest(Projectile.Center, 99999, 99999);
                    Projectile.velocity = ((Projectile.ai[2] != 0 ? (((int)Projectile.ai[0]).ToNPC().Center) : (Projectile.Center + Projectile.velocity * 16)) - Projectile.Center).normalize() * 10 * (1f / (1 + Projectile.ai[1] * 0.5f));
                }
                //HeavySmokeCal Configure是Calamity原构造顺序,跟PRT/EParticle统一尾参不是一回事
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center + CEUtils.randomVec(1) + Projectile.velocity * Main.rand.NextFloat(), CEUtils.randomVec(1), new Color(160, 160, 255), 0.24f).Configure(1, 28, 0.1f, true, 0, true);  //形体烟Cal+后面EHeavySmoke发光层的话后者Additive走Configure

            }
            else {
                Projectile.velocity *= 0.98f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (((int)(Projectile.ai[0])).ToNPC().ModNPC is SpiritFountain sf) {
                if (sf.ClearMyProjs > 0) {
                    Projectile.Kill();
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float scale = 1 * Projectile.scale;
            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            for (int i = 0; i < oldPos.Count; i++) {
                float c = (i + 1f) / oldPos.Count;
                DrawEnergyBall(oldPos[i], scale * c, Projectile.Opacity * c);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public void DrawEnergyBall(Vector2 pos, float size, float alpha) {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(90, 90, 165) * alpha, Projectile.rotation, tex.Size() * 0.5f, new Vector2(1 + (Projectile.velocity.Length() * 0.2f), 1) * size * 0.25f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(40, 40, 255) * alpha, Projectile.rotation, tex.Size() * 0.5f, new Vector2(1 + (Projectile.velocity.Length() * 0.2f), 1) * size * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
        }
    }


}
