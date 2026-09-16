using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class WyrmDash : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void OnSpawn(IEntitySource source) {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<AbyssalCrack>(), Projectile.damage / 4, 0, Projectile.owner);
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.light = 2f;
            Projectile.timeLeft = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ignoreWater = true;
        }
        public float alpha = 0;
        public bool playSound = true;
        public override void AI() {
            if (playSound) {
                playSound = false;
                CEUtils.PlaySound("beast_ghostdash1", 0.6f, Projectile.Center, 2, 1);
            }
            if (Projectile.timeLeft < 6) {
                alpha -= 1f / 6f;
            }
            else {
                if (alpha < 1) {
                    alpha += 1f / 6f;
                }
            }
            var player = Projectile.owner.ToPlayer();

            player.velocity = Projectile.velocity;
            player.Center = Projectile.Center;
            if (Projectile.timeLeft > 12) {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            player.Entropy().immune = 20;
            player.itemTime = 36;
            player.itemAnimation = 36;
            if (Projectile.timeLeft < 2) {
                player.velocity *= 0.2f;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.velocity *= 0f;
            if (Projectile.timeLeft > 8) {
                Projectile.timeLeft = 8;
            }
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return projHitbox.Center().getRectCentered(400 * Projectile.scale, 400 * Projectile.scale).Intersects(targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<LifeOppress>(600);
            CEUtils.PlaySound("xhit", Main.rand.NextFloat(0.6f, 1.1f), Projectile.Center, 8, volume: 0.7f);
            CEUtils.PlaySound("DevourerDeathImpact", Main.rand.NextFloat(0.8f, 1f), Projectile.Center, 8, volume: 0.7f);
            CalamityEntropy.Instance.screenShakeAmp = (Projectile.ai[0] * 0.7f);
            for (int i = 0; i < 1 + (int)(Projectile.ai[0] * 0.34f); i++) {
                //AbyssalLine旧版粒子系统 spawn,现走BasePRT,参数照抄
                PRTLoader.NewParticle<PRT_AbyssalLine>(target.Center, Vector2.Zero, Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());  //AbyssalLine带lifetime的Configure是CalamityPorts签名
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = CEUtils.getExtraTex("wyrmdash");
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * alpha, Projectile.rotation, new Vector2(tex.Size().X / 5f * 4f, tex.Size().Y / 2), Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

    }

}