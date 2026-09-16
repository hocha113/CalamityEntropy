using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class GaleWristblades : ModItem
    {
        public static int BaseDamage = 12;
        public static float MoveSpeed = 0.04f;
        // 内置冷却 0.5 秒(rogue-weapons.md §三)
        public const int BladeCooldown = 30;
        public override void SetDefaults() {
            Item.width = 38;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
        }

        public static string ID = "GaleWristblades";

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().addEquip(ID, !hideVisual);
            // 新效果:移速加成 + 暴击放出追踪风刃(潜行体系退役)
            player.Entropy().moveSpeed += MoveSpeed;
            player.GetModPlayer<GaleWristbladesPlayer>().equipped = true;
        }
        public override void UpdateVanity(Player player) {
            player.Entropy().addEquipVisual(ID);
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.GoldBar, 8)
                .AddIngredient(ItemID.Cloud, 30)
                .AddIngredient(ItemID.Chain, 8)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe()
                .AddIngredient(ItemID.PlatinumBar, 8)
                .AddIngredient(ItemID.Cloud, 30)
                .AddIngredient(ItemID.Chain, 8)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    /// <summary>疾风腕刃触发器:暴击时放出一对追踪风刃,内置冷却 0.5 秒。</summary>
    public class GaleWristbladesPlayer : ModPlayer
    {
        public bool equipped;
        private int bladeCooldown;

        public override void ResetEffects() {
            equipped = false;
            if (bladeCooldown > 0)
                bladeCooldown--;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
            TrySpawnBlades(target, hit);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
            // 风刃自身命中不再触发,避免连锁
            if (proj.ModProjectile is GaleWindBlade)
                return;
            TrySpawnBlades(target, hit);
        }

        private void TrySpawnBlades(NPC target, NPC.HitInfo hit) {
            if (!equipped || !hit.Crit || bladeCooldown > 0)
                return;
            if (Player.whoAmI != Main.myPlayer)
                return;
            bladeCooldown = GaleWristblades.BladeCooldown;
            int damage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(GaleWristblades.BaseDamage);
            Vector2 dir = (target.Center - Player.Center).normalize();
            CEUtils.PlaySound("swing" + Main.rand.Next(1, 5), Main.rand.NextFloat(1.1f, 1.3f), Player.Center, 4, 0.5f);
            for (int i = -1; i <= 1; i += 2) {
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, dir.RotatedBy(0.4f * i) * 9f, ModContent.ProjectileType<GaleWindBlade>(), damage, 1f, Player.whoAmI);
            }
        }
    }

    /// <summary>追踪风刃:疾风腕刃暴击触发的弹幕。</summary>
    public class GaleWindBlade : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Generic);
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
        }
        public override void AI() {
            Projectile.HomingToNPCNearby(0.6f, 0.98f, 700);
            if (Projectile.velocity.Length() < 14f)
                Projectile.velocity *= 1.02f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("ofhit", Main.rand.NextFloat(1.2f, 1.4f), target.Center, 4, 0.4f);
        }
        public override bool PreDraw(ref Color lightColor) {
            // 风刃视觉:两层加算条带沿速度方向拉伸
            Texture2D streak = CEExtraAssets.Streak2;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Vector2 scale = new Vector2(Projectile.velocity.Length() * 3.2f / streak.Width, 14f / streak.Height);
            Main.spriteBatch.Draw(streak, Projectile.Center - Main.screenPosition, null, Color.LightCyan * Projectile.Opacity, Projectile.rotation, new Vector2(streak.Width, streak.Height / 2f), scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(streak, Projectile.Center - Main.screenPosition, null, Color.White * 0.6f * Projectile.Opacity, Projectile.rotation, new Vector2(streak.Width, streak.Height / 2f), scale * new Vector2(0.7f, 0.5f), SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    public class WristTornado : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Throwing);
            Projectile.timeLeft = 120;
            Projectile.width = 64;
            Projectile.height = 128;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.penetrate = -1;
        }
        public override void AI() {
            Projectile.Opacity = Projectile.timeLeft > 30 ? 1f : Projectile.timeLeft / 30f;
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D tex = Projectile.GetTexture();
            for (float i = 0; i <= 1; i += 0.01f) {
                Main.spriteBatch.Draw(tex, Projectile.Center + new Vector2(0, -44 + i * 128) - Main.screenPosition, null, Color.White * (1.01f - i) * Projectile.Opacity, Main.GlobalTimeWrappedHourly * 10 + i * 4, tex.Size() / 2f, (1.02f - i), SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
