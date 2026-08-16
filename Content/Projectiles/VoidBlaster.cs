using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VoidBlaster : ModProjectile
    {
        int frame = 0;
        public float Rot = 0;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
        public float rp = 0;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(rp);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            rp = reader.ReadSingle();
        }
        public bool sspl = false;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 1000;
        }
        public float back = -36;
        public int up = 0;

        public override void AI()
        {
            back *= 0.9f;
            if (frame > 8)
            {
                up -= 6;
            }
            if (Projectile.ai[0] > 0 && Projectile.ai[0] % 3 == 0)
            {
                if (frame < 8)
                {
                    frame++;
                }

                if (Projectile.ai[0] > 200)
                {
                    frame++;
                    if (frame > 9)
                        if (cl < 1)
                            cl += 0.2f;
                    if (frame > 11)
                    {
                        Projectile.Kill();
                        SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/vbdisapear"), Projectile.Center);
                    }
                }
            }
            if (!sspl)
            {
                SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/vbapear"), Projectile.Center);
                sspl = true;
            }
            if (Projectile.ai[0] == 14)
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    if (up == 0)
                    {
                        int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 60, Vector2.Zero, ModContent.ProjectileType<VoidLaser>(), Projectile.damage, 0, Projectile.owner, 0, 0, Projectile.identity);
                        p.ToProj().rotation = Projectile.rotation;
                        p.ToProj().scale = Projectile.scale;
                        p.ToProj().netUpdate = true;
                    }
                }

                if (!Main.dedServ && Projectile.owner == Main.myPlayer)
                {
                    SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/laser"), Projectile.Center);
                }

            }
            if (Projectile.ai[0] != 13 || !Projectile.owner.ToPlayer().channel)
            {
                Projectile.ai[0]++;
            }
            if (Projectile.ai[0] >= 14 && Projectile.ai[0] < 180)
            {
                Rot = float.Lerp(Rot, 1.4f, 0.2f);
            }
            else
            {
                Rot *= 0.92f;
            }
            Vector2 c = Projectile.owner.ToPlayer().Center;
            if (Projectile.Entropy().OnProj != -1)
            {
                c = Projectile.Entropy().OnProj.ToProj().Center;
            }
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.rotation = (Main.MouseWorld - Projectile.Center).ToRotation();
                Projectile.netUpdate = true;
                rp = Projectile.rotation;

            }
            else
            {
                Projectile.rotation = rp;
            }
            if (Projectile.ai[0] < 13 && !Projectile.owner.ToPlayer().channel)
            {
                Projectile.Kill();
            }
            if (frame <= 4 || true)
            {
                Projectile.Center = Projectile.Center + (Projectile.owner.ToPlayer().Center + new Vector2(Projectile.ai[1], Projectile.ai[2]) - Projectile.Center) * 0.1f;
            }
            ct++;
            if (frame <= 4)
            {
                Projectile.timeLeft = 1000;
            }
            lw *= 0.9f;
            lw -= 0.05f;
            if (lw < 0)
                lw = 0;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        public int ct = 0;
        public float cl = 0;
        public float LX = -1;
        public float lw = 1;
        public override bool PreDraw(ref Color lightColor)
        {
            if (LX == -1)
                LX = Projectile.Center.X;
            LX = float.Lerp(LX, Projectile.Center.X, 0.4f);
            Texture2D t1 = CEUtils.RequestTex("CalamityEntropy/Content/Projectiles/VB/VEHA");
            Texture2D t2 = CEUtils.RequestTex("CalamityEntropy/Content/Projectiles/VB/VEHB");
            Texture2D t3 = CEUtils.RequestTex("CalamityEntropy/Content/Projectiles/VB/VES");
            int dir = Projectile.rotation.ToRotationVector2().X > 0 ? 1 : -1;
            SpriteEffects ef = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Color clr = Color.Lerp(Color.White, new Color(0, 0, 255, 0), cl);
            Main.EntitySpriteDraw(t2, Projectile.Center - Main.screenPosition, null, clr, Projectile.rotation, t1.Size() / 2f, Projectile.scale, ef);
            Main.EntitySpriteDraw(t1, Projectile.Center - Main.screenPosition, null, clr, Projectile.rotation + dir * (-Rot * ((Rot > 1.2f ? 1 : 0) * 0.02f * ((float)(Math.Sin((float)Main.GameUpdateCount * 0.4f))) + 1f)), t1.Size() / 2f, Projectile.scale, ef);
            Main.EntitySpriteDraw(t3, Projectile.Center - Main.screenPosition, null, clr, new Vector2(LX - Projectile.Center.X, 12).ToRotation() - MathHelper.PiOver2, new Vector2(t3.Width / 2f, 0), Projectile.scale * 2, SpriteEffects.None);

            Main.spriteBatch.UseBlendState(CEUtils.ColorInverse);
            if (lw > 0.001f)
            {
                CEUtils.drawLine(Projectile.Center + new Vector2(0, -2000), Projectile.Center + new Vector2(0, 2000), Color.White, lw * 110);
            }
            Main.spriteBatch.ExitShaderRegion();
            /*
            if (frame >= 12)
            {
                return false;
            }
            Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Content/Projectiles/VB/VoidBlaster" + frame.ToString()).Value;
            SpriteEffects ef = SpriteEffects.None;
            if (Projectile.rotation.ToRotationVector2().X < 0)
            {
                ef = SpriteEffects.FlipVertically;
            }
            if (frame == 8)
            {
                tx = ModContent.Request<Texture2D>("CalamityEntropy/Content/Projectiles/VB/VoidBlaster" + ((ct / 3) % 3 + 6).ToString()).Value;
            }
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * back + new Vector2(0, up), null, Color.White, Projectile.rotation, new Vector2(165, 144) / 2, Projectile.scale, ef, 0);
            */
            return false;
        }
    }

}