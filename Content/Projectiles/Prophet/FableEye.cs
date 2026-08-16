using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Prophet
{
    public class FableEye : ModProjectile
    {
        public float w = 0f;
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9000;
        }
        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.light = 1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 1500;
            Projectile.penetrate = -1;
        }
        public bool playsound = true;
        public static SoundEffect sound = null;
        public LoopSound sd;
        public float RotateSpeed = 0;
        public float rspd = 0;
        public override void AI()
        {
            if (playsound)
            {
                playsound = false;
                if (!Main.dedServ)
                {
                    sd = new LoopSound(sound);
                    sd.play();
                    sd.instance.Volume = 0;
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            if (sd != null)
            {
                sd.setVolume_Dist(Projectile.Center, 400, 1500, w * 0.64f);
                sd.timeleft = 4;
                sd.instance.Pitch = w;
            }
            if (Projectile.ai[0] < 20)
            {
                w = 0.1f;
            }
            if (Projectile.ai[0] < 120)
            {
                w += 0.01f;
            }
            if (Projectile.ai[0] > 440)
            {
                w -= 0.01f;
                if (w <= 0)
                {
                    Projectile.Kill();
                }
            }
            if (Projectile.ai[0] > 80)
            {

                if (Projectile.ai[0] > 440)
                {
                    RotateSpeed *= 0.98f;
                }
                else
                {
                    int plr = Player.FindClosest(Projectile.Center, 3600, 3600);
                    if (plr >= 0)
                    {
                        Player player = plr.ToPlayer();
                        RotateSpeed += rspd * CEUtils.getRotateAngle(Projectile.velocity.ToRotation(), (player.Center - Projectile.Center).ToRotation(), 0.042f);
                    }
                    if (Projectile.timeLeft % 10 == 0)
                        GeneralParticleHandler.SpawnParticle(new PulseRing(Projectile.Center, Vector2.Zero, Color.SkyBlue * 0.32f, 0.2f, 4f, 64));
                    RotateSpeed *= 0.977f;
                    foreach (var player in Main.ActivePlayers)
                    {
                        float d = Utils.Remap(player.Distance(Projectile.Center), 0, 600, 6, 0);

                        Vector2 vel = (player.Center - Projectile.Center).normalize() * d;
                        if (Math.Abs(player.velocity.Y) < 1.2f)
                            vel.Y = 0;
                        player.position += vel;
                        if (CEUtils.CheckSolidTile(player.getRect()))
                        {
                            player.position -= vel;
                        }
                    }
                }
            }
            if (rspd < 1)
                rspd += 0.01f;
            Projectile.rotation += RotateSpeed;
            Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(Projectile.rotation);
            if (Projectile.ai[0] > 40 && CEUtils.getDistance(Main.LocalPlayer.Center, Projectile.Center) < 4000)
            {
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 7;
            }
            Projectile.ai[0]++;
        }
        public override bool CanHitPlayer(Player target)
        {
            return Projectile.ai[0] > 100;
        }
        public List<Vector2> getSamplePoints()
        {
            List<Vector2> p = new List<Vector2>();
            for (int i = 0; i < 1624; i++)
            {
                p.Add(Projectile.Center + Projectile.velocity * i * 6);
            }
            return p;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var p = getSamplePoints();
            return CEUtils.LineThroughRect(Projectile.Center, p[p.Count - 1], targetHitbox, (int)(100 * w));
        }
        float yx = 0;
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 8 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            yx += 0.036f;
            List<Vector2> points = this.getSamplePoints();
            points.Insert(0, Projectile.Center - Projectile.velocity);
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * w, 0, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            if (points.Count < 2)
            {
                return false;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            {
                Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/MegaStreakBacking2").Value;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(60, 60, 170);

                float p = -Main.GlobalTimeWrappedHourly * 2;
                for (int i = 1; i < points.Count; i++)
                {
                    float wd = 1;
                    if (i < 48)
                    {
                        wd = new Vector2(1, 0).RotatedBy((i / 48f) * MathHelper.PiOver2).Y;
                    }
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 120 * Projectile.scale * w * wd,
                          new Vector3((float)i / points.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 120 * Projectile.scale * w * wd,
                          new Vector3((float)i / points.Count, 0, 1),
                          b));
                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3)
                {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }

            Main.spriteBatch.End();
            var effect = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/fableeyelaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["yofs"].SetValue(yx);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes["fableeyelaser"].Apply();
            {
                Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/TurbulentNoise").Value;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.SkyBlue * 0.66f;
                float p = -Main.GlobalTimeWrappedHourly;
                for (int i = 1; i < points.Count; i++)
                {
                    float wd = 1;
                    if (i < 48)
                    {
                        wd = new Vector2(1, 0).RotatedBy((i / 48f) * MathHelper.PiOver2).Y;
                    }
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 54 * Projectile.scale * w * wd,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 54 * Projectile.scale * w * wd,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width) * 0.3f;
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3)
                {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            effect.Parameters["yofs"].SetValue(-yx);
            {
                Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/EternityStreak").Value;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(255, 255, 255) * 0.66f;
                float p = -Main.GlobalTimeWrappedHourly * 2;
                for (int i = 1; i < points.Count; i++)
                {
                    float wd = 1;
                    if (i < 48)
                    {
                        wd = new Vector2(1, 0).RotatedBy((i / 48f) * MathHelper.PiOver2).Y;
                    }
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 54 * Projectile.scale * w * wd,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 54 * Projectile.scale * w * wd,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width) * 0.3f;
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3)
                {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}