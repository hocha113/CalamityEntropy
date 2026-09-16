using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class DarkBlade : ModItem, IDevItem
    {
        public string DevName => "Knight";
        public override void SetStaticDefaults() {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        public override void SetDefaults() {
            Item.damage = Main.zenithWorld ? 999 : 74;
            Item.DamageType = DamageClass.Melee;
            Item.width = 108;
            Item.height = 108;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.crit = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 7;
            Item.value = Item.buyPrice(0, 2);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<DarkBladeHeld>();
            Item.shootSpeed = 12f;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0, 1, 1);
            return false;
        }
        public override bool MeleePrefix() {
            return true;
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.SoulofNight, 8)
                .AddIngredient(ItemID.DarkLance)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class DarkBladeHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/DarkBlade";
        List<float> odr = new List<float>();
        List<float> ods = new List<float>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

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
            Projectile.timeLeft = 1000;
            Projectile.MaxUpdates = 2;
        }
        public float rotRP = Main.rand.NextFloat(-0.2f, 0.2f);
        public float counter = 0;
        public float scale = 1;
        public float alpha = 1;
        public bool init = true;
        public bool shoot = true;
        public bool RightHold = true;
        public bool LeftClicked = false;
        public bool Spin = false;
        public float rt = Main.rand.NextFloat(-1, 1);
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {

            float sparkCount = 28;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = (target.Center - Projectile.Center).normalize().RotatedByRandom(0.2f) * Main.rand.NextFloat(6, 26);
                int sparkLifetime2 = 26;
                float sparkScale2 = 0.8f;
                Color sparkColor2 = Color.Lerp(Color.Black, Color.Red, Main.rand.NextBool() ? Main.rand.NextFloat(0, 0.16f) : Main.rand.NextFloat(0.84f, 1f));
                //DarkBladeParticle旧AlphaBlend,Additive那套会过曝
                //这类走AlphaBlend桶,Additive会把烟/圈打亮
                PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
            }
        }
        public float rScale = 1;
        public float slashP = Main.rand.NextFloat(0.5f, 1);
        public float rp = 0;
        public override void AI() {
            if (Projectile.timeLeft <= 3) {
                return;
            }
            Projectile.timeLeft++;
            Player owner = Projectile.GetOwner();
            if (owner.dead) {
                Projectile.Kill();
                return;
            }
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates * Projectile.ai[1];
            float progress = (counter / MaxUpdateTimes);
            counter++;
            if (init) {
                CEUtils.PlaySound("darkbladespawn", 1, Projectile.Center);
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                init = false;
                if (Main.myPlayer == Projectile.owner) {
                    if (Main.zenithWorld) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), owner.Center, new Vector2(owner.direction * -2, 0), ModContent.ProjectileType<DarkBladeShoot>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner, 0);
                    }
                    else {
                        for (float i = -1; i <= 1; i += 1) {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), owner.Center, new Vector2(owner.direction * -2, 0).RotatedBy(i * 0.6f), ModContent.ProjectileType<DarkBladeShoot>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner, i);
                        }
                    }
                }
            }
            Projectile.Center = Projectile.GetOwner().GetDrawCenter() - Projectile.velocity.normalize() * 10;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((owner.Entropy().MouseWorld - Projectile.Center).ToRotation());


            if (Projectile.velocity.X > 0) {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            /*
            if(counter == MaxUpdateTimes)
            {
                Vector2 sparkVelocity2 = Projectile.velocity * 1.12f;
                int sparkLifetime2 = 24;
                float sparkScale2 = 2f;
                Color sparkColor2 = Color.Black;
                PRTLoader.NewParticle<PRT_AltSpark>(Projectile.Center + Projectile.velocity.normalize() * 80, sparkVelocity2, sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));

                sparkVelocity2 = Projectile.velocity * 1.4f;
                sparkColor2 = Color.Red;
                sparkScale2 = 0.8f;
                PRTLoader.NewParticle<PRT_AltSpark>(Projectile.Center + Projectile.velocity.normalize() * 30, sparkVelocity2, sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));

            }*/
            if (counter > MaxUpdateTimes) {
                if (progress >= 1f) {
                    owner.itemTime = 1;
                    owner.itemAnimation = 1;
                    Projectile.timeLeft = 3;
                    return;
                }
                if (progress > 2.4f) {
                    NoDraw = true;
                }
            }

        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public bool NoDraw = false;
        public override bool PreDraw(ref Color lightColor) {
            if (NoDraw) {
                return false;
            }
            Texture2D tex = Projectile.GetTexture();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);

            int dir = 1;
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;
            rot += rp;
            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale * rScale, effect);


            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 130 * Projectile.scale * scale * rScale, targetHitbox, 64);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 130 * Projectile.scale * scale * rScale, 54, DelegateMethods.CutTiles);
        }
    }
    public class DarkBladeShoot : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/DarkBlade";
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 80 * 26;
            Projectile.MaxUpdates = 26;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 1;
        public bool init = true;
        public bool shoot = true;
        public Vector2 offsetToPlr = Vector2.Zero;
        public Vector2 offsetVel = Vector2.Zero;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Main.zenithWorld && target.life <= 0) {
                CEUtils.PlaySound("bksnd", 1, Projectile.Center, 1);
            }
            else {
                CEUtils.PlaySound(Main.zenithWorld ? "bksnd1" : "spearImpact", 1, Projectile.Center, 1);
            }
            float sparkCount = 12;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = Projectile.velocity.normalize().RotatedByRandom(0.2f) * Main.rand.NextFloat(6, 26);
                int sparkLifetime2 = 26;
                float sparkScale2 = 0.8f;
                Color sparkColor2 = Color.Lerp(Color.Black, Color.Red, Main.rand.NextBool() ? Main.rand.NextFloat(0, 0.16f) : Main.rand.NextFloat(0.84f, 1f));
                PRTLoader.NewParticle<PRT_AltSpark>(Projectile.Center, sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
            }
            Projectile.damage = (int)(Projectile.damage * 0.9f);
        }
        public override void AI() {
            if (init) {
                init = false;
                if (Projectile.ai[0] == 0) {
                    Projectile.scale *= 1.25f;
                    Projectile.damage = (int)(Projectile.damage * 1.25f);
                }
                offsetVel = Projectile.velocity;
                Projectile.rotation = (-Projectile.velocity).ToRotation();
            }
            Player player = Projectile.GetOwner();
            counter++;
            if (counter < 22 * Projectile.MaxUpdates) {
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (player.Entropy().MouseWorld - Projectile.Center).ToRotation(), 0.004f, false);
                offsetVel *= 0.987f;
                offsetToPlr += offsetVel;
                Projectile.Center = player.Center + offsetToPlr;
                if (counter % (Projectile.MaxUpdates) == 0) {
                    PRTLoader.NewParticle<PRT_DarkBladeParticle>(Projectile.Center, Vector2.Zero, Color.White, Projectile.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, Projectile.rotation + MathHelper.PiOver4, 12);
                }
            }
            else {
                if (shoot) {

                    shoot = false;
                    Projectile.velocity = (player.Entropy().MouseWorld - Projectile.Center).normalize() * 4;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    if (Projectile.ai[0] == 0) {
                        //CalamityEntropy.cutScreenVel = 2.6f;
                        //CalamityEntropy.cutScreenCenter = Projectile.Center;
                        //CalamityEntropy.cutScreenRot = Projectile.velocity.ToRotation();
                    }
                }
                if (CEUtils.getDistance(Projectile.Center, Main.LocalPlayer.Center) < 2000) {
                    if (counter % Projectile.MaxUpdates == 0) {
                        PRTLoader.NewParticle<PRT_DarkBladeParticle>(Projectile.Center, Vector2.Zero, new Color(255, 40, 40), Projectile.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, Projectile.rotation + MathHelper.PiOver4, 20);
                        PRTLoader.NewParticle<PRT_DarkBladeParticle>(Projectile.Center + Projectile.velocity * Projectile.MaxUpdates / 2, Vector2.Zero, new Color(255, 40, 40), Projectile.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, Projectile.rotation + MathHelper.PiOver4, 20);
                    }
                    if (Main.rand.NextBool(8)) {
                        int sparkLifetime2 = 42;
                        var sparkVelocity2 = Projectile.velocity * 1.4f;
                        var sparkColor2 = Color.Red;
                        var sparkScale2 = Main.rand.NextFloat(0.1f, 0.6f);
                        PRTLoader.NewParticle<PRT_AltSpark>(Projectile.Center - Projectile.velocity.normalize() * 18, sparkVelocity2, sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                    }
                }
            }

        }
        public override bool? CanHitNPC(NPC target) {
            if (counter < 22 * Projectile.MaxUpdates)
                return false;
            return null;
        }
        public bool NoDraw = false;
        public override bool PreDraw(ref Color lightColor) {
            if (NoDraw) {
                return false;
            }
            Texture2D tex = Projectile.GetTexture();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);

            int dir = 1;
            Vector2 origin = tex.Size() / 2f;
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale, effect);


            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 256;
            target.Entropy().Decrease20DR = 80;
        }
    }
}
