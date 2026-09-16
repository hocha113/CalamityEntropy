using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Dusts;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Icicle : ModProjectile
    {
        //冰锥本体贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Icicle")]
        internal static Asset<Texture2D> IcicleTex;
        public bool d = false;
        public int j = 0;
        public int counter;
        public float speed = 0;
        public int ycount = 0;
        List<Vector2> odp = new List<Vector2>();
        public int mct = 0;
        public float js;
        List<float> odr = new List<float>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 2f;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
        }

        public override void AI() {

            int jspeed = 22;
            float gravity = 1f;
            odr.Add(Projectile.rotation);
            odp.Add(Projectile.Center);
            if (odp.Count > 12) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
            if (counter == 0) {

                float basej = 0.01f;
                float bspeed = jspeed;
                int count = 0;
                while (basej > 0) {
                    basej += bspeed;
                    bspeed -= gravity;
                    count++;
                }
                mct = count;
                speed = CEUtils.getDistance(Projectile.Center, new Vector2(Projectile.ai[0], Projectile.ai[1])) / count;
                js = jspeed;
            }
            counter++;
            NPC target = Projectile.FindTargetWithinRange(1200, false);
            if (ycount < mct || target == null) {
                Vector2 jv = (new Vector2(Projectile.ai[0], Projectile.ai[1]) - Projectile.Center).ToRotation().ToRotationVector2() * speed;
                jv.Y -= js;
                Projectile.velocity = jv;
                js -= gravity;
                ycount++;

                if (ycount == mct && target != null) {
                    Projectile.velocity = new Vector2(0, 0);
                }
                if (counter == 1) {
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
            }
            else {
                if (Projectile.penetrate == -1) {
                    Projectile.penetrate = 1;
                }
                Projectile.velocity += (target.Center - Projectile.Center).ToRotation().ToRotationVector2() * 2f;
                Projectile.velocity *= 0.9f;
            }

            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.3f, false);

        }
        public override bool PreDraw(ref Color lightColor) {
            /*float size = 6;
            float sizej = size / odp.Count;
            Color cl = new Color(18, 50, 117);
            for (int i = odp.Count - 1; i >= 1; i--)
            {
                Util.drawLine(Main.spriteBatch, ModContent.Request<Texture2D>("CalamityEntropy/Extra/white").Value, this.odp[i], this.odp[i - 1], cl * ((float)i / (float)odp.Count), size);
                size -= sizej;
            }*/
            Texture2D tx = IcicleTex.Value;
            float x = 0f;
            for (int i = 0; i < odp.Count; i++) {
                Color tc = Color.White;
                if (Projectile.ai[2] == 1) {
                    tc = new Color(255, 0, 255);
                }
                Main.spriteBatch.Draw(tx, odp[i] - Main.screenPosition, null, tc * x * 0.6f, odr[i], new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                x += 1 / 14f;
            }
            if (Projectile.ai[2] == 1) {
                Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(-2, -2), null, new Color(255, 0, 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(-2, 0), null, new Color(255, 0, 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(-2, 2), null, new Color(255, 0, 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(0, -2), null, new Color(255, 0, 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(0, 2), null, new Color(255, 0, 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(2, -2), null, new Color(255, 0, 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(2, 0), null, new Color(255, 0, 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(2, 2), null, new Color(255, 0, 255), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);

            }
            return true;

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (ycount < mct) {
                return;
            }
            if (Projectile.ai[2] == 1) {
                target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 480);
            }
            target.immune[Projectile.owner] = 0;
            for (int i = 0; i < 20; i++) {
                Dust.NewDust(Projectile.Center, 12, 12, ModContent.DustType<IcePiece1>());
            }
            target.AddBuff(BuffID.Frostburn, 1080);
            Main.player[Projectile.owner].AddBuff(ModContent.BuffType<CosmicFreeze>(), 600);
            var r = Main.rand;
            for (int i = 0; i < 6; i++) {
                Projectile.NewProjectile(Main.player[Projectile.owner].GetSource_FromAI(), Projectile.Center, new Vector2(r.Next(0, 16) - 8, r.Next(0, 16) - 8), ModContent.ProjectileType<IceSpikeSmall>(), (int)(Projectile.damage * 0.3f), 1);
            }
            for (int i = 0; i < 3; i++) {
                var rd = Main.rand;
                int pj = Projectile.NewProjectile(Main.player[Projectile.owner].GetSource_FromAI(), target.Center + new Vector2(0, 400) + new Vector2(rd.Next(-160, 161), rd.Next(-60, 161)), new Vector2(0, 20), ModContent.ProjectileType<IceEdge>(), Projectile.damage, 0);
                Main.projectile[pj].rotation = (target.Center - Main.projectile[pj].Center).ToRotation();

            }

            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/CryogenHit", 3), Projectile.Center);
        }
    }


}