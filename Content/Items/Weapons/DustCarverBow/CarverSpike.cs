using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.DustCarverBow
{
    public class CarverSpike : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = Projectile.height = 32;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 4;
            Projectile.timeLeft = 256;
        }

        public override void AI() {
            //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
            PRTLoader.NewParticle<PRT_AltSpark>(Projectile.Center - Projectile.velocity * 4, Projectile.velocity * 4, new Color(200, 0, 0), Main.rand.NextFloat(0.2f, 0.4f)).Configure(false, 8);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("GrassSwordHit2", Main.rand.NextFloat(1.2f, 1.6f), target.Center, volume: 0.6f);
            for (int i = 0; i < 6; i++)
                //命中burst AltSpark,Configure(false,life)对齐CalamityPorts
                PRTLoader.NewParticle<PRT_AltSpark>(target.Center, Projectile.velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.2f, 2), new Color(200, 0, 0), Main.rand.NextFloat(0.3f, 0.6f)).Configure(false, 18);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = CEExtraAssets.Glow2;
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(180, 0, 0), Projectile.rotation, tex.Size() / 2f, Projectile.scale * new Vector2(1f, 0.08f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 90, 90), Projectile.rotation, tex.Size() / 2f, Projectile.scale * new Vector2(0.8f, 0.025f), SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            //Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
}
