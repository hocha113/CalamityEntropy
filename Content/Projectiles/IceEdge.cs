using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class IceEdge : ModProjectile
    {
        List<Vector2> odp = new List<Vector2>();
        List<float> odr = new List<float>();
        bool htd = false;
        float exps = 0;
        int frame = 1;
        int framejc = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 74;
            Projectile.height = 74;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.timeLeft = 110;
        }

        public override void AI() {
            Projectile.ai[0]++;
            if (htd) {
                if (odp.Count > 0) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }
                Projectile.velocity = Vector2.Zero;
            }
            else {
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation);
                if (odp.Count > 9) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }
                Projectile.velocity = new Vector2(28, 0).RotatedBy(Projectile.rotation);
            }
            if (exps > 0) {
                exps++;
                framejc++;
                if (framejc > 2) {
                    framejc = 0;
                    frame++;
                    if (frame > 5) {
                        frame = 5;

                    }
                    if (frame < 1) {
                        frame = 1;
                    }
                }
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (base.Colliding(projHitbox, targetHitbox) == null) {
                return base.Colliding(projHitbox, targetHitbox);
            }
            return (!htd) && (bool)base.Colliding(projHitbox, targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Frostburn, 1080);
            Main.player[Projectile.owner].AddBuff(ModContent.BuffType<CosmicFreeze>(), 600);
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
            Texture2D tx = Projectile.GetTexture();
            float x = 0f;
            for (int i = 0; i < odp.Count; i++) {
                Main.spriteBatch.Draw(tx, odp[i] - Main.screenPosition, null, Color.White * x * 0.4f, odr[i], new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);
                x += 1 / 10f;
            }
            return true;
        }

    }

}