using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.AbyssalWraith;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.AbyssalWraithProjs
{

    public class HomingLightBall : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;

        }
        public float counter = 0;
        public int drawcount = 0;
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 600;

        }
        public List<Vector2> odp = new List<Vector2>();
        public override void AI()
        {
            odp.Add(Projectile.Center);
            if (odp.Count > 36)
            {
                odp.RemoveAt(0);
            }
            if (((int)Projectile.ai[2]).ToNPC().active && ((int)Projectile.ai[2]).ToNPC().ModNPC is AbyssalWraith aw)
            {
                if (aw.deathAnm)
                {
                    Projectile.Kill();
                }
            }
            if ((((int)Projectile.ai[2]).ToNPC().active && ((int)Projectile.ai[2]).ToNPC().HasValidTarget))
            {
                Player target = ((int)Projectile.ai[2]).ToNPC().target.ToPlayer();
                if (CEUtils.getDistance(Projectile.Center, target.Center) > 200)
                {
                    Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.5f;
                    Projectile.velocity *= 0.96f;
                }
            }
            if (Projectile.timeLeft < 100)
            {
                opc -= 0.01f;
            }
        }
        public override bool CanHitPlayer(Player target)
        {
            return Projectile.timeLeft > 70;
        }
        float opc = 1;
        public override bool PreDraw(ref Color lightColor)
        {
            drawcount++;
            SpriteBatch spriteBatch = Main.spriteBatch;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D warn = CEExtraAssets.vlbw;
            Texture2D t = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 0; i < odp.Count; i++)
            {
                float alpha = (float)i / (float)odp.Count;
                spriteBatch.Draw(t, odp[i] - Main.screenPosition, null, Color.White * opc * alpha, 0, t.Size() / 2, new Vector2(1, 1) * Projectile.scale, SpriteEffects.None, 0);

            }
            spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White * opc, MathHelper.ToRadians(drawcount * 4f), t.Size() / 2, new Vector2(1.5f, 1) * Projectile.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White * opc, MathHelper.ToRadians((drawcount + 64) * 14f), t.Size() / 2, new Vector2(1.5f, 1) * Projectile.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White * opc, MathHelper.ToRadians((drawcount + 154) * 34f), t.Size() / 2, new Vector2(1.5f, 1) * Projectile.scale, SpriteEffects.None, 0);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }

}