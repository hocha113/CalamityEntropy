using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Fractal
{
    public class BrokenHilt : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 18;
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;
            Item.height = 60;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 4.6f;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BrokenHiltHeld>();
            Item.shootSpeed = 12f;
            Item.scale *= 1.2f;
        }
        public int atkType = 1;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType == 0 ? -1 : atkType);
            atkType *= -1;
            return false;
        }

        public override bool MeleePrefix() {
            return true;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.WoodenSword)
                .AddIngredient(ItemID.RichMahoganySword)
                .AddIngredient(ItemID.BorealWoodSword)
                .AddIngredient(ItemID.PalmWoodSword)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
    public class BrokenHiltHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/BrokenHilt";
        List<float> odr = new List<float>();
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
            Projectile.timeLeft = 100000;
            Projectile.extraUpdates = 3;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public override void AI() {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            counter++;
            if (init) {
                CEUtils.PlaySound("HiltAttack", 1 + Projectile.ai[0] * 0.12f, Projectile.Center, volume: 0.6f * CEUtils.WeapSound);
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                init = false;
            }
            odr.Add(Projectile.rotation);
            Projectile.timeLeft = 3;
            float RotF = 4f;
            alpha = 1;
            scale = 1f;
            Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + RotF * CEUtils.GetRepeatedCosFromZeroToOne(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
            Projectile.Center = Projectile.GetOwner().MountedCenter;


            if (odr.Count > 60) {
                odr.RemoveAt(0);
            }
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
            if (counter > MaxUpdateTimes) {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            int dir = (int)(Projectile.ai[0]) * (Projectile.velocity.X > 0 ? -1 : 1);
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale, effect);

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (70 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (70 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, 54, DelegateMethods.CutTiles);
        }
    }

}
