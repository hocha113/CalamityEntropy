using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FractalShadow : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 46;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 320 * 8;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 8;
            Projectile.tileCollide = false;
        }
        bool init = true;
        public float counter = 0;
        public float rotSpeed;
        public float pg = 0;
        public override void AI() {
            Player player = Projectile.GetOwner();
            player.Entropy().MouseWorldListener = true;
            if (init) {
                rotSpeed = Projectile.ai[1] * 0.1f;
                Projectile.rotation = Projectile.ai[0];
                init = false;
            }
            NPC fTarget = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1600);
            if (counter < 46 * Projectile.MaxUpdates) {
                Projectile.velocity *= 0.986f;
                pg = counter / (46 * Projectile.MaxUpdates);
                Projectile.rotation += rotSpeed * (1 - pg);
                rotSpeed *= 0.99f;
                if (fTarget != null) {
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (fTarget.Center - Projectile.Center).ToRotation(), 0.0154f * pg, false);
                }
            }
            if (counter == 46 * Projectile.MaxUpdates) {
                if (fTarget != null) {
                    Projectile.rotation = (fTarget.Center - Projectile.Center).ToRotation();
                }
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 8.4f;
            }
            counter++;
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++) {
                float prog = ((float)i / ProjectileID.Sets.TrailCacheLength[Type]);
                Color clr = Color.White * 0.36f * (1 - prog);
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, clr, Projectile.oldRot[i], (int)Projectile.ai[1]);
            }

            Main.spriteBatch.ExitShaderRegion();
            Draw(Projectile.Center, Color.White, Projectile.rotation, (int)Projectile.ai[1]);
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Projectile.damage = (int)(Projectile.damage * 0.86f);
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/BrilliantFractalGlow";
        public void Draw(Vector2 pos, Color lightColor, float rotation, int dir) {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            var tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, null, lightColor, rot, tex.Size() * 0.5f, Projectile.scale, effect);
        }
    }


}