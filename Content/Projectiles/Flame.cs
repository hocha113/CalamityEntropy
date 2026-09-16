using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Flame : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60 * 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        public override void AI() {
            Projectile.velocity *= 0.976f;
            if (Projectile.ai[1] >= 0) {
                if (((int)Projectile.ai[1]).ToProj_Identity().active) {
                    Projectile.Center = ((int)Projectile.ai[1]).ToProj_Identity().Center;
                }
                else {
                    Projectile.ai[1] = -1;
                }
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindProjectiles.Add(index);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix); ;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tex, 6, (((100000 - Projectile.timeLeft) / 4) % 6)), lightColor, Projectile.rotation, new Vector2(50, 50), Projectile.scale * (0.4f + 0.6f * ((float)Projectile.penetrate / 5f)), SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix); ;
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("firedeath hiss", 1, Projectile.Center);
        }
    }


}