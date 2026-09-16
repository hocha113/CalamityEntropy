using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items.Weapons.Fractal;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FractalStarBlade : ModProjectile
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
            if (counter < 46 * Projectile.MaxUpdates) {
                Projectile.velocity *= 0.986f;
                pg = counter / (46 * Projectile.MaxUpdates);
                Projectile.rotation += rotSpeed * (1 - pg);
                rotSpeed *= 0.99f;
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (player.Entropy().MouseWorld - Projectile.Center).ToRotation(), 0.022f * pg, false);

            }
            else {
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (homing == null || (homing != null && !homing.active)) {
                    homing = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 2400);
                }
                else if (!hited) {
                    Projectile.velocity += (homing.Center - Projectile.Center).normalize() * 1f;
                    Projectile.velocity *= 0.92f;
                }
            }
            if (counter == 46 * Projectile.MaxUpdates) {
                Projectile.rotation = (player.Entropy().MouseWorld - Projectile.Center).ToRotation();
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 12;
            }
            counter++;

        }
        public NPC homing = null;

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
        public bool hited = false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            hited = true;
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 360);
            Projectile.damage = (int)(Projectile.damage * 0.86f);
            if (Projectile.timeLeft > 30 * Projectile.MaxUpdates) {
                Projectile.timeLeft = 30 * Projectile.MaxUpdates;
            }
            for (int i = 0; i < 3; i++) {
                Vector2 pos = target.Center + new Vector2(0, -900) + CEUtils.randomPointInCircle(400);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, (target.Center - pos).normalize() * 42, ModContent.ProjectileType<AstralStarMelee>(), Projectile.damage / 4, Projectile.owner).ToProj();
            }
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/StarlitFractalGlow";
        public void Draw(Vector2 pos, Color lightColor, float rotation, int dir) {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            var tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, null, lightColor, rot, tex.Size() * 0.5f, Projectile.scale, effect);
        }
    }


}