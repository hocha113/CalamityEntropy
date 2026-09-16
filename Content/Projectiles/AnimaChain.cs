using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class AnimaChain : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0;
            Projectile.timeLeft = 300;
        }
        float trap = 0;
        NPC target { get { return ((int)Projectile.ai[0]).ToNPC(); } }
        bool playsound = true;
        public override void AI() {
            if (Projectile.timeLeft == 270) {
                CEUtils.PlaySound("chains_rattle", 1, Projectile.Center);
            }
            if (trap < 1) {
                trap += 0.1f;
            }
            if (trap >= 1) {
                target.velocity += (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 2;
                if (r > 0) {
                    r -= 0.01f;
                }
                if (playsound) {
                    playsound = false;
                    CEUtils.PlaySound("chain2", 1, Projectile.Center);

                }
                if (target.active) {
                    target.Entropy().AnimaTrapped = 2;
                }
                else {
                    if (Projectile.timeLeft > 2) {
                        Projectile.timeLeft = 2;
                    }
                }
            }
        }
        float r = 1;
        public override void OnSpawn(IEntitySource source) {
            Projectile.rotation = CEUtils.randomRot();
        }
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("chains_break", 1, Projectile.Center);
        }
        int l = 0;
        int rtime = 0;
        public override bool PreDraw(ref Color lightColor) {
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < 3; i++) {
                points.Add(Projectile.Center + new Vector2(300, 0).RotatedBy(MathHelper.ToRadians(i * 120) + Projectile.rotation));
            }
            SpriteBatch sb = Main.spriteBatch;
            Effect shader = CEEffectAssets.RedAdd;
            float redAlpha = 0;

            if (Projectile.timeLeft < 180) {
                int value = Projectile.timeLeft + 320;
                double frequency = 1.0 / (value + 1);
                double time = Projectile.timeLeft * 110;
                double phase = time * frequency;
                int r = (int)(Math.Sin(phase) > 0 ? 1 : 0);
                if (r != l) {
                    rtime = 5;
                }
                l = r;
                if (rtime > 0) {
                    rtime--;
                    redAlpha = 0.8f;
                }
            }
            shader.Parameters["strength"].SetValue(redAlpha);
            shader.CurrentTechnique.Passes["EffectPass"].Apply();
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.GameViewMatrix.TransformationMatrix);

            foreach (Vector2 p in points) {
                Vector2 startPos = p;
                Vector2 endPos = Vector2.Lerp(startPos, target.Center, trap);
                int spacing = 18;
                Texture2D tx = CEUtils.getExtraTex("anima_chain");

                int distance = ((int)Math.Sqrt(Math.Pow(endPos.X - startPos.X, 2) + Math.Pow(endPos.Y - startPos.Y, 2)));
                float rot = (endPos - startPos).ToRotation();
                float px = startPos.X;
                float py = startPos.Y;
                int num = ((int)(distance / spacing));
                Vector2 addVec = new Vector2((endPos.X - startPos.X) / num, (endPos.Y - startPos.Y) / num);
                addVec.Normalize();
                float adx = (endPos.X - startPos.X) / num;
                float ady = (endPos.Y - startPos.Y) / num;
                Vector2 drawPos = new Vector2(px, py);
                for (int i = 0; i <= num; i++) {
                    if (((float)i / (float)num) < 0.3f) {
                        shader.Parameters["alpha"].SetValue((float)i / ((float)num * 0.3f));
                    }
                    else {
                        shader.Parameters["alpha"].SetValue(1);
                    }
                    Main.EntitySpriteDraw(tx, drawPos - Main.screenPosition, null, Color.White, rot, new Vector2(tx.Width / 2, tx.Height / 2), (new Vector2(1, 1)), SpriteEffects.None, 0);

                    drawPos.X += addVec.X * spacing;
                    drawPos.Y += addVec.Y * spacing;
                }
            }
            if (trap >= 1) {
                shader.Parameters["alpha"].SetValue(1);
                Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
                Main.EntitySpriteDraw(tex, target.Center - Main.screenPosition, null, Color.White, 0, tex.Size() / 2, Projectile.scale, SpriteEffects.None);
            }
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }

}