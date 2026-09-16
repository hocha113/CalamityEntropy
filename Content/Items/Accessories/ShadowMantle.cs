using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Armor;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityEntropy.Content.Items.Accessories
{
    public class ShadowMantle : ModItem
    {
        public static float BaseDamage = 25;
        public static int CooldownTicks = 30 * 60;
        // 突进后增伤窗口:1.5 秒内 +10% 伤害(rogue-weapons.md §三)
        public const int DashBoostTime = 90;
        public const float DashBoostDamage = 0.10f;
        public override void SetDefaults() {
            Item.width = 42;
            Item.defense = 4;
            Item.height = 48;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            // 脱离灾厄:灾厄动态饰品键位并入自有 AccessoryAbilityHotKey(player-api.md §2)
            tooltips.Replace("[KEY]", EModPlayer.AccessoryAbilityHotKey.TooltipKeyHint());
        }

        public static string ID = "ShadowMantle";

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().addEquip(ID, !hideVisual);
        }
        public override void UpdateVanity(Player player) {
            player.Entropy().addEquipVisual(ID);
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.ShadowScale, 6)
                .AddIngredient(ItemID.Silk, 12)
                .AddTile(TileID.Loom)
                .Register();

            CreateRecipe().AddIngredient(ItemID.TissueSample, 6)
                .AddIngredient(ItemID.Silk, 12)
                .AddTile(TileID.Loom)
                .Register();
        }
    }
    /// <summary>暗影披风增伤窗口:影遁突进后 1.5 秒内 +10% 伤害。</summary>
    public class ShadowMantlePlayer : ModPlayer
    {
        public int dashBoostTime;

        public override void PostUpdateEquips() {
            if (dashBoostTime > 0) {
                dashBoostTime--;
                Player.GetDamage(DamageClass.Generic) += ShadowMantle.DashBoostDamage;
            }
        }
    }
    public class ShadowMantleSlash : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Generic);
            Projectile.timeLeft = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.penetrate = -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("SwiftSlice", 1, target.Center);
            //MultiSlash寿命-1跟着斩击,字段赋值在Configure前
            var p = PRTLoader.NewParticle<PRT_MultiSlash>(target.Center, Vector2.Zero, Color.LightBlue, 1);
            p.xadd = 1f;
            p.lx = 1f;
            p.endColor = Color.Blue;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
        }
        public bool MovePlayer = true;
        public override void AI() {
            Player player = Projectile.GetOwner();
            if (MovePlayer) {
                Vector2 odp = player.Center;
                player.Center = Vector2.Lerp(Projectile.Center + Projectile.velocity, Projectile.Center, Projectile.timeLeft / 10f);
                if (CEUtils.IsPlayerStuck(player)) {
                    MovePlayer = false;
                    player.Center = odp;
                }
            }
            if (Projectile.timeLeft == 10) {
                // 突进落地开启增伤窗口
                player.GetModPlayer<ShadowMantlePlayer>().dashBoostTime = ShadowMantle.DashBoostTime;
                CEUtils.PlaySound("ShadowDash", 1, Projectile.Center);
                player.Entropy().screenShift = 1;
                player.Entropy().screenPos = player.Center;
                Vector2 top = Projectile.Center;
                Vector2 top2 = Projectile.Center + Projectile.velocity;
                Vector2 sparkVelocity2 = Projectile.velocity * 0.08f;
                Vector2 rd = Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2);
                int sparkLifetime2 = 24;
                float sparkScale2 = 1;
                float rdp = 8;
                float rdc = 0.8f;
                Color sparkColor2 = Color.Lerp(Color.DarkBlue, Color.Purple, Main.rand.NextFloat(0, 1));
                for (float i = 0; i < 1; i += 0.02f) {
                    PRTLoader.NewParticle<PRT_AltSpark>(top + rd * rdp, sparkVelocity2 * (0.1f + i) - rd * rdc, sparkColor2, sparkScale2 * (0.4f + (1 - i))).Configure(false, (int)(sparkLifetime2));
                    PRTLoader.NewParticle<PRT_AltSpark>(top2 + rd * rdp, -sparkVelocity2 * (0.1f + i) - rd * rdc, sparkColor2, sparkScale2 * (0.4f + (1 - i))).Configure(false, (int)(sparkLifetime2));

                    PRTLoader.NewParticle<PRT_AltSpark>(top - rd * rdp, sparkVelocity2 * (0.1f + i) + rd * rdc, sparkColor2, sparkScale2 * (0.4f + (1 - i))).Configure(false, (int)(sparkLifetime2));
                    PRTLoader.NewParticle<PRT_AltSpark>(top2 - rd * rdp, -sparkVelocity2 * (0.1f + i) + rd * rdc, sparkColor2, sparkScale2 * (0.4f + (1 - i))).Configure(false, (int)(sparkLifetime2));
                }

                sparkScale2 = 1;
                sparkColor2 = Color.Lerp(Color.Aqua, new Color(200, 200, 255), Main.rand.NextFloat(0, 1));
                for (float i = 0; i < 1; i += 0.02f) {
                    PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2 * (0.1f + i), sparkColor2, sparkScale2 * (0.4f + (1 - i))).Configure(false, (int)(sparkLifetime2));
                    PRTLoader.NewParticle<PRT_LineCal>(top2, -sparkVelocity2 * (0.1f + i), sparkColor2, sparkScale2 * (0.4f + (1 - i))).Configure(false, (int)(sparkLifetime2));
                }
            }
        }

        public override bool ShouldUpdatePosition() { return false; }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity, targetHitbox, 32);
        }
    }
}
