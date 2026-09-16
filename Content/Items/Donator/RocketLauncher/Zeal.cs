using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo;
using CalamityEntropy.Content.Items.Weapons.GrassSword;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher
{
    public class Zeal : ModItem
    {
        public static int MaxStick => 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxStick);
        public static int ExplodeRadius => 60;
        public override void SetDefaults() {
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<CharredMissileProj>(), BaseMissileProj.AmmoType, singleShotTime: 18, shotVelocity: 40f, hasAutoReuse: true);
            Item.width = 90;
            Item.height = 42;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 85;
            Item.knockBack = 4f;
            Item.UseSound = CEUtils.GetSound("ProminenceShoot", 1.6f, 2, 0.5f);
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Red;
            Item.ArmorPenetration = 40;
        }

        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 76f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = -HoldoutOffset().Value;
            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (-0.2f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
        public override Vector2? HoldoutOffset() {
            return new Vector2(-27f, -4f);
        }
        public override bool RangedPrefix() {
            return true;
        }
        #endregion

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<Pardon>()
                .AddIngredient<OsseousRemains>(20)
                .AddIngredient(ItemID.LunarBar, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }

        public static void struggleProjKilled(Projectile proj) {

        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            position += (new Vector2(54, -16) * new Vector2(1, player.direction)).RotatedBy(velocity.ToRotation());
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, MaxStick, ExplodeRadius);
            int ztype = ModContent.ProjectileType<ZealShoot>();
            Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.3f) * 0.7f, ztype, damage / 2, knockback / 2, player.whoAmI);
            Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.3f) * 0.7f, ztype, damage / 2, knockback / 2, player.whoAmI);

            return false;
        }
    }
    public class ZealShoot : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 280;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return Projectile.position.getRectCentered(20 * Projectile.scale, 20 * Projectile.scale).Intersects(targetHitbox);
        }
        public override void AI() {
            if (Projectile.localAI[2]++ == 0) {
                Projectile.scale *= 2;
            }
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.position, 2000);
            if (target != null && drawcount > 8) {
                Projectile.velocity *= 0.94f;
                Vector2 v = target.Center - Projectile.position;
                v.Normalize();

                Projectile.velocity += v * 4;
            }
            drawcount++;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        float drawcount = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            float sparkCount = 16;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4, 12);
                int sparkLifetime2 = 12;
                float sparkScale2 = 0.34f;
                sparkScale2 *= (1 + Bramblecleave.GetLevel() * 0.05f);
                Color sparkColor2 = Color.Lerp(Color.Violet, Color.White, Main.rand.NextFloat());

                //PRT_AltSpark CalamityPorts,Configure(false,lifetime)仿Calamity spark/line
                PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
            }
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public Color ColorFunction(float completionRatio, Vector2 vertex) {
            return Color.Lerp(Color.White, Color.Violet, MathHelper.Clamp(completionRatio * 0.8f, 0f, 1f)) * base.Projectile.Opacity;
        }

        public float WidthFunction(float completionRatio, Vector2 vertex) {
            float num = 8f;
            float num2 = ((!(completionRatio < 0.1f)) ? MathHelper.Lerp(num, 0f, Utils.GetLerpValue(0.1f, 1f, completionRatio, clamped: true)) : ((float)Math.Sin(completionRatio / 0.1f * (MathF.PI / 2f)) * num + 0.1f));
            return num2 * base.Projectile.Opacity * Projectile.scale
            ;
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.StreakGoopAsset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(base.Projectile.oldPos, new CEPrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
    }
}
