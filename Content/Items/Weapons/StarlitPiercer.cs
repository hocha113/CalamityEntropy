using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class StarlitPiercer : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 40;
            Item.DamageType = DamageClass.Melee;
            Item.width = 58;
            Item.noUseGraphic = true;
            Item.height = 58;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ModContent.RarityType<Lunarblight>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<StarlitPiercerHeld>();
            Item.shootSpeed = 16f;
        }

        public override bool MeleePrefix() {
            return true;
        }
    }
    public class StarlitPiercerHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/StarlitPiercer";
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= 1.4f;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
        }
        public float counter = 0;
        public float scale;
        public float dCounter = 0;
        public float offset = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(0.7f, 1.3f), target.Center, volume: CEUtils.WeapSound * 0.8f);
            for (int i = 0; i < 3; i++) {
                //PRT_StarTrailParticle hit burst,Configure设opacity+PRTDrawMode
                //EParticle→PRT,spawn参数迁移纪律:数值一个不改
                PRTLoader.NewParticle<PRT_StarTrailParticle>(target.Center, Projectile.velocity.normalize().RotatedByRandom(0.4f) * Main.rand.NextFloat(16, 36), Color.White).Configure(Main.rand.NextFloat(0.6f, 1.6f), true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
        }
        public float starAlpha = 0;
        public override void AI() {
            var player = Projectile.GetOwner();
            player.heldProj = Projectile.whoAmI;
            player.Entropy().MouseWorldListener = true;
            float cMax = player.itemTimeMax * Projectile.MaxUpdates;
            float zMax = cMax * (Main.zenithWorld ? 0.06f : 0.22f);
            if (counter == 0) {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            counter++;
            if (counter <= zMax * (Main.zenithWorld ? 10 : 3)) {
                if (dCounter == 0) {
                    CEUtils.PlaySound("powerwhip", 1.6f, Projectile.Center, volume: CEUtils.WeapSound);
                    if (Projectile.owner == Main.myPlayer) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.velocity * 3, Projectile.velocity * 1.2f, ModContent.ProjectileType<FriendlyAstralShoot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                }
                dCounter++;
                if (dCounter >= zMax) {
                    dCounter = 0;
                    for (int i = 0; i < Projectile.localNPCImmunity.Length; i++) {
                        Projectile.localNPCImmunity[i] = 0;
                    }

                    Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((player.Entropy().MouseWorld - player.MountedCenter).ToRotation());
                }
                scale = 1.4f + CEUtils.Parabola(dCounter / zMax, 0.2f);
                offset = CEUtils.Parabola(dCounter / zMax, 40);
                starAlpha = offset / 40f;
            }
            else {
                scale = 1.4f;
                offset = 0;
                starAlpha = 0;
            }
            player.itemAnimation = player.itemTime = 2;
            if (counter >= cMax) {
                player.itemTime = player.itemAnimation = 0;
                Projectile.Kill();
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.X > 0) {
                player.direction = 1;
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                player.direction = -1;
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            Projectile.Center = player.MountedCenter + player.gfxOffY * Vector2.UnitY + Projectile.velocity.normalize() * (offset - 16);
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver4, new Vector2(0, tex.Height), Projectile.scale * scale, SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D star = CEExtraAssets.StarTexture;
            Main.spriteBatch.Draw(star, Projectile.Center + Projectile.rotation.ToRotationVector2() * 84 * scale - Main.screenPosition, null, Color.White * (starAlpha), 0, star.Size() / 2f, new Vector2(2.4f, 0.6f) * Projectile.scale * 0.2f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, Projectile.Center + Projectile.rotation.ToRotationVector2() * 84 * scale - Main.screenPosition, null, Color.White * (starAlpha), 0, star.Size() / 2f, new Vector2(0.6f, 2.4f) * Projectile.scale * 0.2f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 78 * scale * Projectile.scale, targetHitbox, 24);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 80 * Projectile.scale * scale, 24, DelegateMethods.CutTiles);
        }
    }
}
