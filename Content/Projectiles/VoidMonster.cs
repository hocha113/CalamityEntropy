using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VoidMonster : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 56;
            Projectile.height = 56;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 10;
        }
        public Vector2 leglu; public Vector2 legld; public Vector2 legru; public Vector2 legrd;
        public Vector2 tleglu; public Vector2 tlegld; public Vector2 tlegru; public Vector2 tlegrd;
        public override void OnSpawn(IEntitySource source)
        {
            leglu = Projectile.Center + new Vector2(-100, -100);
            legld = Projectile.Center + new Vector2(-100, 100);
            legru = Projectile.Center + new Vector2(100, -100);
            legrd = Projectile.Center + new Vector2(100, 100);
            tleglu = Projectile.Center + new Vector2(-100, -100);
            tlegld = Projectile.Center + new Vector2(-100, 100);
            tlegru = Projectile.Center + new Vector2(100, -100);
            tlegrd = Projectile.Center + new Vector2(100, 100);
        }

        public override void AI()
        {
            if (Projectile.ai[0] < 3)
            {
                leglu = Projectile.Center + new Vector2(-100, -100);
                legld = Projectile.Center + new Vector2(-100, 100);
                legru = Projectile.Center + new Vector2(100, -100);
                legrd = Projectile.Center + new Vector2(100, 100);
                tleglu = Projectile.Center + new Vector2(-100, -100);
                tlegld = Projectile.Center + new Vector2(-100, 100);
                tlegru = Projectile.Center + new Vector2(100, -100);
                tlegrd = Projectile.Center + new Vector2(100, 100);
            }
            Vector2 tlu = Projectile.Center + new Vector2(-160, -160) + Projectile.velocity * 10;
            Vector2 tld = Projectile.Center + new Vector2(-160, 160) + Projectile.velocity * 10;
            Vector2 tru = Projectile.Center + new Vector2(160, -160) + Projectile.velocity * 10;
            Vector2 trd = Projectile.Center + new Vector2(160, 160) + Projectile.velocity * 10;
            int legsmoving = 4;
            if (leglu == tleglu)
            {
                legsmoving -= 1;
            }
            if (legld == tlegld)
            {
                legsmoving -= 1;
            }
            if (legru == tlegru)
            {
                legsmoving -= 1;
            }
            if (legrd == tlegrd)
            {
                legsmoving -= 1;
            }
            if (checkMoving(tlu, leglu) && legsmoving < 3)
            {
                tleglu = tlu;
                legsmoving++;
            }
            if (checkMoving(tld, legld) && legsmoving < 3)
            {
                tlegld = tld;
                legsmoving++;
            }
            if (checkMoving(tru, legru) && legsmoving < 3)
            {
                tlegru = tru;
                legsmoving++;
            }
            if (checkMoving(trd, legrd) && legsmoving < 3)
            {
                tlegrd = trd;
                legsmoving++;
            }

            if (CEUtils.getDistance(Projectile.Center, Projectile.owner.ToPlayer().Center) > 1200)
            {
                Projectile.Center = Projectile.owner.ToPlayer().Center;

            }
            if (CEUtils.getDistance(tleglu, leglu) > LegSpeed)
            {
                leglu += (tleglu - leglu).SafeNormalize(Vector2.Zero) * LegSpeed;
            }
            else
            {
                leglu = tleglu;
            }

            if (CEUtils.getDistance(tlegld, legld) > LegSpeed)
            {
                legld += (tlegld - legld).SafeNormalize(Vector2.Zero) * LegSpeed;
            }
            else
            {
                legld = tlegld;
            }

            if (CEUtils.getDistance(tlegru, legru) > LegSpeed)
            {
                legru += (tlegru - legru).SafeNormalize(Vector2.Zero) * LegSpeed;
            }
            else
            {
                legru = tlegru;
            }

            if (CEUtils.getDistance(tlegrd, legrd) > LegSpeed)
            {
                legrd += (tlegrd - legrd).SafeNormalize(Vector2.Zero) * LegSpeed;
            }
            else
            {
                legrd = tlegrd;
            }

            if (CEUtils.getDistance(leglu, tlu) > 1000)
            {
                leglu = tlu;
                tleglu = tlu;
            }
            if (CEUtils.getDistance(legld, tld) > 1000)
            {
                legld = tld;
                tlegld = tld;
            }
            if (CEUtils.getDistance(legru, tru) > 1000)
            {
                legru = tru;
                tlegru = tru;
            }
            if (CEUtils.getDistance(legrd, trd) > 1000)
            {
                legrd = trd;
                tlegrd = trd;
            }
            Projectile.ai[0]++;
            if (CEUtils.getDistance(Projectile.Center, Projectile.owner.ToPlayer().Center) > 140)
            {
                Projectile.velocity += (Projectile.owner.ToPlayer().Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 1;
                Projectile.velocity *= 0.98f;
            }
            Player player = Projectile.owner.ToPlayer();
            NPC target = null;
            if (player.HasMinionAttackTargetNPC)
            {
                target = Main.npc[player.MinionAttackTargetNPC];
                float betw = Vector2.Distance(target.Center, Projectile.Center);
                if (betw > 2000f)
                {
                    target = null;
                }

            }
            if (target == null || !target.active)
            {
                NPC t = Projectile.FindTargetWithinRange(1000, false);
                if (t != null)
                {
                    target = t;
                }
            }
            if (target != null && Projectile.ai[0] % 10 == 0 && Main.myPlayer == Projectile.owner)
            {
                float rot = (float)(target.Center - Projectile.Center).RotatedBy(0.7f * Math.Cos(Projectile.ai[0] * 0.08f)).ToRotation();
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, rot.ToRotationVector2() * 46, ModContent.ProjectileType<VoidMonsterShoot>(), Projectile.damage, 5, Projectile.owner);
            }
        }
        public static int MaxDistanceFromLegToTarget = 320;
        public static int LegSpeed = 50;
        public bool checkMoving(Vector2 a, Vector2 b)
        {
            return CEUtils.getDistance(a, b) > MaxDistanceFromLegToTarget;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
        public void draw()
        {
            Vector2 a, b, c;

            a = Projectile.Center;
            b = Projectile.Center + new Vector2(-100, -100);
            c = leglu;
            drawLeg(a, b, c);

            a = Projectile.Center;
            b = Projectile.Center + new Vector2(-100, 100);
            c = legld;
            drawLeg(a, b, c);

            a = Projectile.Center;
            b = Projectile.Center + new Vector2(100, -100);
            c = legru;
            drawLeg(a, b, c);

            a = Projectile.Center;
            b = Projectile.Center + new Vector2(100, 100);
            c = legrd;
            drawLeg(a, b, c);
            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White, 0, tx.Size() / 2, 1, SpriteEffects.None, 0);
        }

        public void drawLeg(Vector2 root, Vector2 p1, Vector2 end)
        {
            float size = 20;
            int counts = 40;
            float p = 0;
            Vector2 lastp = root;
            for (int i = 0; i < counts; i++)
            {
                Vector2 a = Vector2.Lerp(root, p1, p);
                Vector2 b = Vector2.Lerp(p1, end, p);
                Vector2 c = Vector2.Lerp(a, b, p);
                CEUtils.drawLine(Main.spriteBatch, ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white").Value, lastp, c, Color.White, size, 2);
                lastp = c;
                size -= 20f / (float)counts;
                p += 1f / (float)counts;
            }
        }

        /*public void drawLeg(Vector2 root, Vector2 p1, Vector2 end)
        {
            float size = 20;
            int counts = 40;
            float p = 0;
            Vector2 lastp = root;

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<Vertex> ve = new List<Vertex>();
            Color clr = Color.White;
            for (int i = 0; i < counts; i++)
            {
                Vector2 a = Vector2.Lerp(root, p1, p);
                Vector2 b = Vector2.Lerp(p1, end, p);
                Vector2 c = Vector2.Lerp(a, b, p);
                ve.Add(new Vertex(c - Main.screenPosition + (c - lastp).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 30,
                      new Vector3((float)i / (float)counts, 1, 1),
                      clr));
                ve.Add(new Vertex(c - Main.screenPosition + (c - lastp).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 30,
                      new Vector3((float)i / (float)counts  , 0, 1),
                      clr));
                lastp = c;
                size -= 20f / (float)counts;
                p += 1f / (float)counts;
            }
            
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3)             {
                Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/").Value;
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }*/

    }

}