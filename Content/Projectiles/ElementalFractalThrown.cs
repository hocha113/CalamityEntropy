using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.DamageClasses;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ElementalFractalThrown : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 26;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60 * 2;
            Projectile.scale *= 1.6f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 2;
            Projectile.tileCollide = false;
        }
        public override void AI() {
            Projectile.Opacity = 0.4f + 0.6f * Projectile.timeLeft / 120f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++) {
                float prog = ((float)i / ProjectileID.Sets.TrailCacheLength[Type]);
                Color clr = Color.White * 0.36f * (1 - prog);
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, clr, Projectile.oldRot[i], (int)Projectile.ai[1], 0.4f + 0.6f * (1 - prog));
            }
            Main.spriteBatch.ExitShaderRegion();

            Draw(Projectile.Center, Color.White, Projectile.rotation, Math.Sign(Projectile.velocity.X));
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            hit.DamageType = NoDRMelee.Instance;
            CEUtils.PlaySound("sf_hit", 1, Projectile.Center, volume: 0.6f);
            target.AddBuff(ModContent.BuffType<ElementalMix>(), 400);
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.Keybrand, new ParticleOrchestraSettings {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
        }
        public override void OnKill(int timeLeft) {
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.Keybrand, new ParticleOrchestraSettings {
                PositionInWorld = Projectile.Center,
                MovementVector = Vector2.Zero
            });
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/ElementalFractalGlow";
        public void Draw(Vector2 pos, Color lightColor, float rotation, int dir, float scale = 1) {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            var tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, null, lightColor * Projectile.Opacity, rot, tex.Size() * 0.5f, Projectile.scale * scale, effect);
        }
    }


}