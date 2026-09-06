using CalamityEntropy.Assets.Register;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VPVoidLightBall : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;

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
            Projectile.timeLeft = 170;

        }
        public List<Vector2> odp = new List<Vector2>();
        public int counter1 = 60;
        public override void AI()
        {
            if (((int)(Projectile.ai[2])).ToNPC().active == false)
            {
                Projectile.Kill();
                return;
            }
            counter1--;
            if (counter == 0)
            {
                Projectile.velocity = ((int)(Projectile.ai[2])).ToNPC().velocity * 2;
            }
            if (counter1 > 0)
            {
                Projectile.Center = ((int)(Projectile.ai[2])).ToNPC().Center + ((int)(Projectile.ai[2])).ToNPC().rotation.ToRotationVector2() * 90;
                return;
            }
            odp.Add(Projectile.Center);
            if (odp.Count > 36)
            {
                odp.RemoveAt(0);
            }
            if (Projectile.ai[0] == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/light_bolt_delayed"));
            }
            if (Projectile.ai[0] == 60)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/light_bolt"));
            }

            Projectile.velocity *= 0.94f;
            Projectile.ai[0] += (Projectile.ai[1] == 2 ? 1f : 1);
            counter += (Projectile.ai[1] == 2 ? 1f : 1);
            if (counter > 60)
            {
                Projectile.velocity *= 0;
            }
            else
            {
                if ((((int)Projectile.ai[2]).ToNPC().active && ((int)Projectile.ai[2]).ToNPC().HasValidTarget) || Projectile.ai[1] == 2)
                {
                    Player target = ((int)Projectile.ai[2]).ToNPC().target.ToPlayer();
                    if (Projectile.ai[1] == 0)
                    {
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 0.12f, false);
                    }
                    if (counter <= 1)
                    {
                        if (Projectile.ai[1] == 2)
                        {
                            Projectile.rotation = CEUtils.randomRot();
                            rotSpeed = (CEUtils.randomRot() - MathHelper.Pi) * 0.05f;
                            Projectile.netUpdate = true;
                            Projectile.timeLeft = 220;
                        }
                        else
                        {
                            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 1, false);
                        }

                    }
                }
            }
            if (Projectile.ai[1] == 2 && counter == 60)
            {
                rotSpeed = 0;
            }
            Projectile.rotation += rotSpeed;
            rotSpeed *= 0.92f;
            if (counter >= 90)
            {
                opc -= 1f / 20f;
            }
        }
        float opc = 1;
        float rotSpeed = 0;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return counter > 60 && counter < 90 && CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 2400, targetHitbox, 24);
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(rotSpeed);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            rotSpeed = reader.ReadSingle();
        }
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
            if (counter <= 60)
            {
                spriteBatch.Draw(warn, Projectile.Center - Main.screenPosition, null, Color.White * opc * (counter / 60f), Projectile.rotation, warn.Size() / 2 * new Vector2(0, 1), new Vector2(9f * counter / 60f * (Projectile.ai[1] == 1 ? 3 : 1), 1 * (Projectile.ai[1] == 1 ? 0.4f : 1)) * Projectile.scale, SpriteEffects.None, 0);
            }
            else
            {
                spriteBatch.Draw(warn, Projectile.Center - Main.screenPosition, null, Color.Purple * opc, Projectile.rotation, warn.Size() / 2 * new Vector2(0, 1), new Vector2(20, 1.2f) * Projectile.scale * 1.46f * new Vector2(1, opc), SpriteEffects.None, 0);
                spriteBatch.Draw(warn, Projectile.Center - Main.screenPosition, null, ((drawcount / 2) % 2 == 0 ? Color.White : Color.Purple) * opc, Projectile.rotation, warn.Size() / 2 * new Vector2(0, 1), new Vector2(20, 1) * Projectile.scale * 1.46f * new Vector2(1, opc), SpriteEffects.None, 0);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }

}