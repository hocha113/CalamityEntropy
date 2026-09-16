using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles
{
    public class IceEdge2 : EBookBaseProjectile
    {
        //冰刃两层贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/IceEdge")]
        internal static Asset<Texture2D> IceEdgeTex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/IceEdge2")]
        internal static Asset<Texture2D> IceEdge2Tex;
        List<Vector2> odp = new List<Vector2>();
        List<float> odr = new List<float>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.width = 74;
            Projectile.height = 74;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.timeLeft = 160;
            Projectile.MaxUpdates = 3;
        }
        public override bool ShouldUpdatePosition() {
            return white <= 0;
        }
        public override void AI() {
            base.AI();
            if (Projectile.ai[0] == 0) {
                CEUtils.PlaySound("bne_hit2", 1, Projectile.Center, 1, 0.36f);
                Projectile.rotation = CEUtils.randomRot();
            }
            if (op < 1) {
                op += 0.1f;
            }
            if (white > 0) {
                white -= 0.025f;
            }
            Projectile.ai[0]++;
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 9) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.1f, false);

        }
        public float op = 0;
        public float white = 1;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Frostburn, 400);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode);
            //PRT_DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center + Projectile.velocity * 3, Vector2.Zero, new Color(170, 170, 255), 0.1f).Configure(new Vector2(2f, 2f), 0, 0.5f, 20);

            PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center + Projectile.velocity * 6, Vector2.Zero, new Color(140, 140, 255), 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 0.36f, 16);

            float sparkCount = 14;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = new Vector2(Main.rand.NextFloat(10, 20), 0).RotateRandom(1f).RotatedBy(Projectile.velocity.ToRotation());
                int sparkLifetime2 = Main.rand.Next(26, 35);
                float sparkScale2 = Main.rand.NextFloat(1.2f, 1.6f);
                Color sparkColor2 = Color.Lerp(Color.SkyBlue, Color.LightSkyBlue, Main.rand.NextFloat(0, 1));
                //跟AltSpark成对出现时寿命/速度系数是旧代码原值
                PRTLoader.NewParticle<PRT_LineCal>(Projectile.Center + Projectile.velocity * 3, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));

            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tx = IceEdgeTex.Value;
            Texture2D tx2 = IceEdge2Tex.Value;
            float x = 0f;
            for (int i = 0; i < odp.Count; i++) {
                Main.spriteBatch.Draw(tx, odp[i] - Main.screenPosition, null, Color.White * x * 0.3f, odr[i], new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                x += 1 / 10f;
            }
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White * op, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tx2, Projectile.Center - Main.screenPosition, null, Color.White * op * white, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

    }

}