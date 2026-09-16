using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class HolyBeamRevelation : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 80;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 3;
            Projectile.ArmorPenetration = 12800;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.DisableCrit();
        }
        public override void AI() {

            if (playSound) {
                playSound = false;
                CEUtils.PlaySound("angel_blast1", 1, Projectile.Center, 8, 0.6f);
            }
            Projectile.Center = Projectile.owner.ToPlayer().Center + Projectile.owner.ToPlayer().gfxOffY * Vector2.UnitY;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Player owner = Projectile.owner.ToPlayer();
            if (Projectile.velocity.X > 0) {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
        }
        public bool playSound = true;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 1600, targetHitbox, 64);
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }
        public override bool PreDraw(ref Color lightColor) {
            float alpha = 1;
            if (Projectile.timeLeft > 74) {
                alpha = (float)(80 - Projectile.timeLeft) / 6f;
            }
            if (Projectile.timeLeft < 20) {
                alpha = (float)Projectile.timeLeft / 20f;
            }
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 1600, tx.Height), Color.White * ((Projectile.timeLeft / 3 % 2 == 0 ? 1 : 0.8f)), Projectile.rotation, new Vector2(0, tx.Height / 2f), Projectile.scale * new Vector2(1, alpha), SpriteEffects.None, 0); ;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

    }

}