using CalamityEntropy.Content.Particles;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Particles;
using CalamityMod.Systems.Collections;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ShadowMantle : ModItem
    {
        public override void SetStaticDefaults()
        {
            CalamityItemSets.HasAccessoryKeybind[Type] = true;
        }
        public static float BaseDamage = 25;
        public static int CooldownTicks = 30 * 60;
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.defense = 4;
            Item.height = 48;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var k = Main.LocalPlayer.Calamity().FindAccessory<ShadowMantle>().GetDynamicModHotkey();
            tooltips.IntegrateHotkey(k);
        }

        public static string ID = "ShadowMantle";

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().addEquip(ID, !hideVisual);
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().addEquipVisual(ID);
        }
        public override void AddRecipes()
        {
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
    public class ShadowMantleSlash : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC);
            Projectile.timeLeft = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.penetrate = -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("SwiftSlice", 1, target.Center);
            EParticle.NewParticle(new MultiSlash() { xadd = 1f, lx = 1f, endColor = Color.Blue }, target.Center, Vector2.Zero, Color.LightBlue, 1, 1, true, BlendState.Additive, 0);
        }
        public bool MovePlayer = true;
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if (MovePlayer)
            {
                Vector2 odp = player.Center;
                player.Center = Vector2.Lerp(Projectile.Center + Projectile.velocity, Projectile.Center, Projectile.timeLeft / 10f);
                if (CEUtils.IsPlayerStuck(player))
                {
                    MovePlayer = false;
                    player.Center = odp;
                }
            }
            if (Projectile.timeLeft == 10)
            {
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
                for (float i = 0; i < 1; i += 0.02f)
                {
                    var spark = new AltSparkParticle(top + rd * rdp, sparkVelocity2 * (0.1f + i) - rd * rdc, false, (int)(sparkLifetime2), sparkScale2 * (0.4f + (1 - i)), sparkColor2);
                    GeneralParticleHandler.SpawnParticle(spark);
                    spark = new AltSparkParticle(top2 + rd * rdp, -sparkVelocity2 * (0.1f + i) - rd * rdc, false, (int)(sparkLifetime2), sparkScale2 * (0.4f + (1 - i)), sparkColor2);
                    GeneralParticleHandler.SpawnParticle(spark);

                    spark = new AltSparkParticle(top - rd * rdp, sparkVelocity2 * (0.1f + i) + rd * rdc, false, (int)(sparkLifetime2), sparkScale2 * (0.4f + (1 - i)), sparkColor2);
                    GeneralParticleHandler.SpawnParticle(spark);
                    spark = new AltSparkParticle(top2 - rd * rdp, -sparkVelocity2 * (0.1f + i) + rd * rdc, false, (int)(sparkLifetime2), sparkScale2 * (0.4f + (1 - i)), sparkColor2);
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                sparkScale2 = 1;
                sparkColor2 = Color.Lerp(Color.Aqua, new Color(200, 200, 255), Main.rand.NextFloat(0, 1));
                for (float i = 0; i < 1; i += 0.02f)
                {
                    var spark2 = new LineParticle(top, sparkVelocity2 * (0.1f + i), false, (int)(sparkLifetime2), sparkScale2 * (0.4f + (1 - i)), sparkColor2);
                    GeneralParticleHandler.SpawnParticle(spark2);
                    spark2 = new LineParticle(top2, -sparkVelocity2 * (0.1f + i), false, (int)(sparkLifetime2), sparkScale2 * (0.4f + (1 - i)), sparkColor2);
                    GeneralParticleHandler.SpawnParticle(spark2);
                }
            }
        }

        public override bool ShouldUpdatePosition() { return false; }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity, targetHitbox, 32);
        }
    }
}
