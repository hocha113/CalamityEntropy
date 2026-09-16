using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ShadowSpawner : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Throwing, penetrate: -1);
            Projectile.timeLeft = 64;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override void AI() {
            Projectile.Center = ((int)Projectile.ai[0]).ToNPC().Center;
            if (Projectile.timeLeft % 4 == 0 && Projectile.owner == Main.myPlayer) {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShadowKnife>(), Projectile.damage, 1, Projectile.owner, (Projectile.timeLeft / 64f) * MathHelper.TwoPi);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }
    public class ShadowShoot : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 120;
        }

        public override void AI() {
            Projectile.rotation += 0.16f;
            NPC target = Projectile.FindTargetWithinRange(1600, false);
            if (target != null && Projectile.ai[1]++ > 8) {
                Projectile.velocity *= 0.96f;
                Vector2 v = target.Center - Projectile.Center;
                v.Normalize();

                Projectile.velocity += v * 2f;
            }
            for (int i = 0; i < 4; i++) {
                ShadowMetaball.SpawnParticle(Projectile.Center + CEUtils.randomPointInCircle(8), CEUtils.randomPointInCircle(2), Main.rand.NextFloat(20, 28));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<ShadowSpawner>(), Projectile.damage, 2, Projectile.owner, target.whoAmI);
        }
    }
    public class ShadowKnife : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 36;
            Projectile.extraUpdates = 1;
        }
        public int counter = 0;
        public override bool? CanHitNPC(NPC target) {
            if (counter < 12) {
                return false;
            }
            return null;
        }
        private Vector2 lastPos = Vector2.Zero;
        public Vector2 spawnPos = Vector2.Zero;
        public PRT_StarTrailParticle spt = null;
        public PRT_StarTrailParticle spt2 = null;
        public override void AI() {
            if (counter == 0) {
                lastPos = spawnPos = Projectile.Center;
            }
            counter++;
            if (counter <= 24f) {
                Projectile.Center = spawnPos + Projectile.ai[0].ToRotationVector2() * CEUtils.Parabola(counter / 24f, 220);
            }
            if (counter == 24) {
                Projectile.velocity = Projectile.Center - lastPos;
            }
            if (counter > 12) {
                for (int i = 0; i < 6; i++) {
                    Dust.NewDustDirect(Projectile.Center + CEUtils.randomPointInCircle(8), 1, 1, Terraria.ID.DustID.CorruptTorch, 0, 0).noGravity = true;
                }
                if (spt == null) {
                    //StarTrailParticle星尘拖尾,旧EParticle StarTrail
                    spt = PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, Vector2.Zero, Color.MediumPurple, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    spt.maxLength = 18;
                    spt.maxLength = 18;
                }
                spt.Velocity = Projectile.velocity * 0.2f;
                spt.Lifetime = 30;
                spt.Position = Projectile.Center;
                if (spt2 == null) {
                    //StarTrailParticle星尘拖尾,旧EParticle StarTrail
                    spt2 = PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, Vector2.Zero, Color.LightBlue, 0.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    spt2.maxLength = 18;
                }
                spt2.Velocity = Projectile.velocity * 0.2f;
                spt2.Lifetime = 30;
                spt2.Position = Projectile.Center;
            }
            Projectile.rotation = Projectile.ai[0] + MathHelper.Pi;
            lastPos = Projectile.Center;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }

    }
}
