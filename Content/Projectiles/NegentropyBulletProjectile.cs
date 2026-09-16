using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class NegentropyBulletProjectile : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = 1;
            Projectile.friendly = true; Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.alpha = 255;
            Projectile.light = 0.5f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.MaxUpdates = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.light = 0.4f;
            AIType = ProjectileID.Bullet;
        }
        public int portalcount = 3;
        public override bool PreDraw(ref Color lightColor) {
            if (CEUtils.getDistance(Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f, Projectile.Center) > 1600)
                return false;
            lightColor = Color.White;
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (float i = 0; i <= MathHelper.TwoPi; i += MathHelper.TwoPi / 6f) {
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + (i + Main.GlobalTimeWrappedHourly * 16f).ToRotationVector2() * 2f, null, Color.White * 0.6f, Projectile.rotation, new Vector2(tex.Width, tex.Height / 2), Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(tex.Width, tex.Height / 2), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        public void portalParticle(Vector2 pos) {
            if (PRTLoader.PRT_InGame_World_Inds.Count > 256 && Main.rand.NextBool())
                return;
            float rj = CEUtils.randomRot();
            for (int i = 0; i < 360; i += 90) {
                //AbyssalLine旧版粒子系统 spawn,现走BasePRT,参数照抄
                var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(pos, Vector2.Zero, Color.LightBlue, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, MathHelper.ToRadians(i) + rj, 26);
                __prt.lx = 1f;
                __prt.xadd = 0.1f;
            }
            for (int i = 0; i < 360; i += 90) {
                float r = MathHelper.ToRadians(i);
                var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(pos, (r + rj).ToRotationVector2() * 2f, Color.LightBlue, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, r + rj, 26);
                __prt.lx = 3f;
                __prt.xadd = 0.06f;
            }
            for (int i = 0; i < 360; i += 90) {
                var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(pos, Vector2.Zero, Color.Black, 1).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, MathHelper.ToRadians(i) + rj, 26);
                __prt.lx = 0.7f;
                __prt.xadd = 0.09f;
                __prt.spawnColor = Color.Black;
                __prt.endColor = Color.Black;
            }
            for (int i = 0; i < 360; i += 90) {
                float r = MathHelper.ToRadians(i);
                //AbyssalLine带lifetime的Configure是CalamityPorts签名
                var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(pos, (r + rj).ToRotationVector2() * 2f, Color.Black, 1).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, r + rj, 26);
                __prt.lx = 2.5f;
                __prt.xadd = 0.055f;
                __prt.spawnColor = Color.Black;
                __prt.endColor = Color.Black;
            }
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            portalTime--;
            if (portalTime == 0) {
                Projectile.damage = ((int)(Projectile.damage * 0.4f));
                NPC target = ptarget;
                portalParticle(Projectile.Center);
                CEUtils.PlaySound("portal_emerge", 1.6f, Projectile.Center, 12, 0.22f);
                Projectile.Center = target.Center + Projectile.velocity.normalize() * -250;//CEUtils.randomRot().ToRotationVector2() * 400;
                Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * Projectile.velocity.Length();
                Projectile.netUpdate = true;
                Projectile.ResetLocalNPCHitImmunity();
                Projectile.numHits = 0;
                if (Projectile.TryGetGlobalProjectile<ScorchingGProj>(out var sgp)) {
                    sgp.NPCHited.Clear();
                    if (sgp.trail != null)
                        sgp.trail.trailPositions.Clear();
                }
                Projectile.timeLeft = 60 * Projectile.MaxUpdates;
                portalParticle(Projectile.Center);
            }
        }
        public NPC ptarget = null;
        public int portalTime = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (PRTLoader.PRT_InGame_World_Inds.Count < 128 || Main.rand.NextBool()) {
                var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, Color.Blue, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), 24);
                __prt.spawnColor = new Color(200, 160, 255);
                __prt.endColor = Color.Black;
                __prt.lx = 0.9f;
                __prt.xadd = 0.5f;
            }
            Projectile.damage = (int)(Projectile.damage * 0.9f);
            CEUtils.PlaySound("ystn_hit", 1.6f, Projectile.Center, 1, 0.24f);
            if (portalcount > 0 && portalTime < 0) {
                portalcount--;
                portalTime = 18;
                ptarget = target;
            }
        }
    }
}