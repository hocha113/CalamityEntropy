using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Prophet
{
    public class ProphetRuneAlt : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 500;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;

        }
        public Vector2 lastPos;
        public float counter = 0;
        public Vector2 dC;
        public override void AI() {
            NPC owner = ((int)Projectile.ai[0]).ToNPC();
            counter += 1;

            float dist = 5 * (counter > 20 ? 20 : counter);
            float rotJ = Projectile.ai[1];
            if (counter < 150) {
                Projectile.Center = owner.Center + (Main.GameUpdateCount * 0.12f + rotJ).ToRotationVector2() * dist;
                dC = owner.Center;
            }
            else {
                dist = 100 + (counter - 150) * 18f;
                Projectile.Center = dC + (Main.GameUpdateCount * 0.025f + rotJ).ToRotationVector2() * dist;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 8 * 60);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D light = CEExtraAssets.lightball;
            Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition, null, Color.White * (counter > 20 ? 1 : counter / 20f) * 0.8f, Projectile.rotation, light.Size() / 2, Projectile.scale * 0.8f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D tx = CEUtils.getExtraTex("runes/rune" + ((int)Projectile.ai[2]).ToString());
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White * (counter > 20 ? 1 : counter / 20f) * (0.8f + (float)(Math.Cos(Main.GameUpdateCount * 0.26f) * 0.2f)), 0, tx.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

    }

}