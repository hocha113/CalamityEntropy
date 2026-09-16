using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkSnowgrave : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 5);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Snowgrave");
        public override EBookProjectileEffect getEffect() {
            return new SnowgraveBMEffect();
        }
        public override Color tooltipColor => new Color(160, 160, 255);
    }
    public class SnowgraveBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            int sgtype = ModContent.ProjectileType<Snowgrave>();
            if (projectile.GetOwner().ownedProjectileCounts[sgtype] < 1) {
                projectile.GetOwner().Entropy().SnowgraveChargeTime = 20;
                if (projectile.GetOwner().Entropy().SnowgraveCharge >= 1 && projectile.ModProjectile is EBookBaseProjectile ebp && ebp.ShooterModProjectile is EntropyBookHeldProjectile eb) {
                    projectile.GetOwner().Entropy().SnowgraveCharge = 0;
                    projectile.GetOwner().Entropy().SnowgraveChargeTime = 0;
                    Projectile.NewProjectile(projectile.GetSource_FromAI(), target.Center, Vector2.Zero, sgtype, EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 10, projectile.DamageType), 0.4f, projectile.owner);
                }
            }
        }
    }
    public class Snowgrave : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Particles/SnowPiece";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.width = 360;
            Projectile.height = 2048;
            Projectile.timeLeft = 360;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 2;
            Projectile.ArmorPenetration = 6400;
        }

        public void SpawnSnow() {
            Vector2 scaledSize = Main.Camera.ScaledSize;
            Vector2 scaledPosition = Main.Camera.ScaledPosition;
            for (int i = 0; (float)i < 42; i++) {
                try {

                    int num5 = Main.rand.Next((int)scaledSize.X + 1500) - 750;
                    int num6 = (int)scaledPosition.Y - Main.rand.Next(50);
                    if (Main.player[Main.myPlayer].velocity.Y > 0f)
                        num6 -= (int)Main.player[Main.myPlayer].velocity.Y;

                    if (Main.rand.Next(5) == 0)
                        num5 = Main.rand.Next(500) - 500;
                    else if (Main.rand.Next(5) == 0)
                        num5 = Main.rand.Next(500) + (int)scaledSize.X;

                    if (num5 < 0 || (float)num5 > scaledSize.X)
                        num6 += Main.rand.Next((int)((double)scaledSize.Y * 0.8)) + (int)((double)scaledSize.Y * 0.1);
                    float cloudAlpha = 1.2f;
                    num5 += (int)scaledPosition.X;
                    int num7 = num5 / 16;
                    int num8 = num6 / 16;
                    int num9 = Dust.NewDust(new Vector2(num5, num6), 10, 10, DustID.Snow);
                    Main.dust[num9].scale += cloudAlpha * 0.2f;
                    Main.dust[num9].velocity.Y = 3f + (float)Main.rand.Next(30) * 0.1f;
                    Main.dust[num9].velocity.Y *= Main.dust[num9].scale;
                    float windSpeedCurrent = 1.6f;
                    if (!Main.raining) {
                        Main.dust[num9].velocity.X = windSpeedCurrent + (float)Main.rand.Next(-10, 10) * 0.1f;
                        Main.dust[num9].velocity.X += windSpeedCurrent * 15f;
                    }
                    else {
                        Main.dust[num9].velocity.X = (float)Math.Sqrt(Math.Abs(windSpeedCurrent)) * (float)Math.Sign(windSpeedCurrent) * (cloudAlpha + 0.5f) * 10f + Main.rand.NextFloat() * 0.2f - 0.1f;
                        Main.dust[num9].velocity.Y *= 0.5f;
                    }

                    Main.dust[num9].velocity.Y *= 1f + 0.3f * cloudAlpha;
                    Main.dust[num9].scale += cloudAlpha * 0.2f;

                    Main.dust[num9].velocity *= 1f + cloudAlpha * 0.5f;


                    continue;
                } catch {
                }
            }
        }
        public override void AI() {
            Main.LocalPlayer.Entropy().snowgrave = 16;
            if (!Main.dedServ)
                SpawnSnow();
            if (Projectile.localAI[0]++ == 0)
                CEUtils.PlaySound("Snowgrave", 1, Projectile.Center, 2, 4);

            float wave = (0.4f + 0.6f * (float)(Math.Sin(Main.GameUpdateCount * 0.35f) * 0.5f + 0.5f));
            Player owner = Projectile.GetOwner();
            void SpawnSnowPiece(Vector2 pos) {
                //owner绑射弹寿命,SnowPiece里ownedProjectileCounts<1才开始Opacity衰减
                var snow = PRTLoader.NewParticle<PRT_SnowPiece>(pos, new Vector2(0, -80), Color.White, 1);
                snow.owner = owner;
                snow.Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, -1);
            }

            for (float i = 0.2f; i <= 1; i += 0.2f) {
                SpawnSnowPiece(Projectile.Center + new Vector2(-30, -Math.Abs(i) * 40) + new Vector2(wave * i * 160, 1024));
                SpawnSnowPiece(Projectile.Center + new Vector2(-30, -Math.Abs(i) * 40) + new Vector2(0, -40) + new Vector2(wave * i * 160, 1024));
                SpawnSnowPiece(Projectile.Center + new Vector2(30, -Math.Abs(i) * 40) + new Vector2(wave * i * 160, 1024));
                SpawnSnowPiece(Projectile.Center + new Vector2(30, -Math.Abs(i) * 40) + new Vector2(0, -40) + new Vector2(wave * i * 160, 1024));

                SpawnSnowPiece(Projectile.Center + new Vector2(-30, -Math.Abs(i) * 40) + new Vector2(-wave * i * 160, 1024));
                SpawnSnowPiece(Projectile.Center + new Vector2(-30, -Math.Abs(i) * 40) + new Vector2(0, -40) + new Vector2(-wave * i * 160, 1024));
                SpawnSnowPiece(Projectile.Center + new Vector2(30, -Math.Abs(i) * 40) + new Vector2(-wave * i * 160, 1024));
                SpawnSnowPiece(Projectile.Center + new Vector2(30, -Math.Abs(i) * 40) + new Vector2(0, -40) + new Vector2(-wave * i * 160, 1024));
            }

            //大雾块AdditiveBlend桶,owner同上
            var storm = PRTLoader.NewParticle<PRT_SnowStorm>(Projectile.Center + new Vector2(Main.rand.NextFloat(-260, 260), 1024), new Vector2(0, -20), Color.White, 10);
            storm.owner = owner;
            storm.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
        }
        public override bool? CanHitNPC(NPC target) {
            return Projectile.timeLeft < 330;
        }

        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }

}