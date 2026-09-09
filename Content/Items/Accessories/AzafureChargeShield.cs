using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Dash;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Shield)]
    public class AzafureChargeShield : ModItem, IAzafureEnhancable
    {
        public const int DashDamage = 50;
        public const float DashKnockback = 6f;
        public const int DashImmuneFrames = 12;
        public const int DashDuration = 20;
        public const float DashDistance = 16 * 16f;
        public const int DashCooldown = 30;
        public float charge = 0;
        public float maxCharge = 3.6f;

        /// <summary>一次冲刺的充能开销;阿扎弗强化时减半。</summary>
        public static float DashCost(Player player) => player.AzafureEnhance() ? 0.5f : 1f;

        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 54;
            Item.value = Item.buyPrice(gold: 5);
            Item.defense = 4;
            Item.accessory = true;
            Item.rare = ModContent.RarityType<AzafureOrange>();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (charge < maxCharge)
            {
                charge += 1f / 300f;
            }
            player.Entropy().AzafureChargeShieldItem = Item;
            player.GetModPlayer<CEDashPlayer>().Offer(CEDashRegistry.Get<AzafureShieldDash>());
        }
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (charge < maxCharge)
            {
                CEUtils.DrawChargeBar(scale * 1.2f, position + new Vector2(0, 18) * scale, ((float)charge / maxCharge), (charge < 1) ? Color.DarkOrange : Color.Orange);
            }
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DubiousPlating, CEID.Item_AerialiteBar))
            {
                CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient(CEID.Item_DubiousPlating, 10)
                .AddIngredient(CEID.Item_AerialiteBar, 5)
                .AddIngredient(ItemID.HellstoneBar, 5)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient<AzafurePlating>(10)
                .AddIngredient(ItemID.HallowedBar, 5)
                .AddIngredient(ItemID.HellstoneBar, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    /// <summary>
    /// 阿扎弗两件盾冲刺的共同部分:火花拖尾、撞击火花、蒸汽与屏震。子类只给数值与充能来源。
    /// </summary>
    public abstract class AzafureDashBase : CEDashEffect
    {
        public override bool HitsEnemies => true;
        public override int Duration => 20;
        public override int Cooldown => 30;
        /// <summary>长冲刺:曲线放缓一点,让速度撑满大半程再收尾,而不是一闪就滑行。</summary>
        public override float Curve => 1.6f;

        protected abstract int HitDamage { get; }
        protected abstract float HitKnockback { get; }
        protected abstract int HitImmuneFrames { get; }

        /// <summary>取充能来源;为空表示饰品不在身上。</summary>
        protected abstract bool TryGetCharge(Player player, out float charge);
        protected abstract void ConsumeCharge(Player player, float cost);

        public override bool CanStart(Player player)
            => TryGetCharge(player, out float charge) && charge >= AzafureChargeShield.DashCost(player);

        public override void OnStart(Player player, CEDashState state)
        {
            if (!state.Remote)
                ConsumeCharge(player, AzafureChargeShield.DashCost(player));
            CEUtils.PlaySound("Dash2", Main.rand.NextFloat(0.9f, 1.1f), player.Center, 6, 0.55f);

            Vector2 back = -state.Direction;
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = back.RotatedByRandom(0.5f) * Main.rand.NextFloat(4f, 10f);
                PRTLoader.NewParticle<PRT_LineCal>(player.Center + CEUtils.randomPointInCircle(12), vel,
                    Color.Lerp(Color.OrangeRed, Color.LightGoldenrodYellow, Main.rand.NextFloat()), Main.rand.NextFloat(0.8f, 1.3f)).Configure(false, Main.rand.Next(16, 24));
            }
        }

        public override void OnVisuals(Player player, CEDashState state)
        {
            // 火花强度随冲刺进度收束,收尾时只剩少量余火
            float intensity = 1f - state.Progress;
            Vector2 axis = state.Direction;
            Vector2 side = axis.RotatedBy(MathHelper.PiOver2);
            Vector2 back = -axis * Math.Max(4f, state.CurrentSpeed) * 0.4f;
            int dir = state.HorizontalSign(player);

            if (intensity > 0.2f)
            {
                Color sparkColor = Color.Lerp(Color.OrangeRed, Color.Firebrick, Main.rand.NextFloat());
                float sparkScale = Main.rand.NextFloat(1f, 1.4f) * (0.6f + 0.4f * intensity);
                int sparkLifetime = Main.rand.Next(18, 28);
                PRTLoader.NewParticle<PRT_LineCal>(player.Center + side * 20f, back.RotatedBy(-0.2f * dir), sparkColor, sparkScale).Configure(false, sparkLifetime);
                PRTLoader.NewParticle<PRT_LineCal>(player.Center - side * 20f, back.RotatedBy(0.2f * dir), sparkColor, sparkScale).Configure(false, sparkLifetime);
                PRTLoader.NewParticle<PRT_LineCal>(player.Center + side * 12f, back.RotatedBy(0.15f * dir), Color.LightGoldenrodYellow, sparkScale * 0.8f).Configure(false, 12);
                PRTLoader.NewParticle<PRT_LineCal>(player.Center - side * 12f, back.RotatedBy(-0.15f * dir), Color.LightGoldenrodYellow, sparkScale * 0.8f).Configure(false, 12);
            }

            int dustCount = 1 + (int)(2 * intensity);
            for (int i = 0; i < dustCount; i++)
            {
                float f = axis.ToRotation() + state.Timer / 5f;
                float radius = 15f + (float)Math.Cos(state.Timer / 3f) * 12f;
                Dust dust = Dust.NewDustPerfect(player.Center - axis * 24f + f.ToRotationVector2().RotatedBy(i / 5f * MathHelper.TwoPi) * radius, Main.rand.NextBool(5) ? DustID.Torch : DustID.FlameBurst);
                dust.alpha = 220;
                dust.noGravity = true;
                dust.velocity = player.velocity * 0.8f;
                dust.scale = Main.rand.NextFloat(1.7f, 2f);
                dust.shader = GameShaders.Armor.GetSecondaryShader(player.cShield, player);
                Dust spark = Dust.NewDustPerfect(player.Center + side * Main.rand.NextFloat(-15f, 15f) + axis * Main.rand.NextFloat(-6f, 6f), Main.rand.NextBool(6) ? DustID.SparksMech : DustID.MinecartSpark, back.RotatedByRandom(MathHelper.ToRadians(30f)) * Main.rand.NextFloat(0.2f, 1f), 0, default, Main.rand.NextFloat(1.7f, 1.9f));
                spark.alpha = 170;
                spark.noGravity = true;
                spark.shader = GameShaders.Armor.GetSecondaryShader(player.cShield, player);
            }
        }

        public override void OnHit(Player player, NPC npc, CEDashState state, ref CEDashHit hit)
        {
            var mp = player.Entropy();
            if (mp.AzChargeShieldSteamTime <= 0)
                mp.AzChargeShieldSteamTime = 32;
            if (state.HitCount == 1)
                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 6));

            Vector2 axis = state.Direction;
            Vector2 side = axis.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 16; i++)
            {
                Vector2 top = npc.Center + side * Main.rand.NextFloat(-12f, 12f);
                Vector2 sparkVelocity = -axis.RotatedByRandom(0.4f) * Main.rand.NextFloat(3f, 10f);
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity, Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat()), Main.rand.NextFloat(0.6f, 1.4f)).Configure(false, Main.rand.Next(24, 28));
            }
            for (int i = 0; i < 16; i++)
            {
                Vector2 sparkVelocity = -axis.RotatedByRandom(0.6f) * Main.rand.NextFloat(5f, 14f);
                PRTLoader.NewParticle<PRT_AltSpark>(npc.Center, sparkVelocity, Color.Lerp(Color.Red, Color.Firebrick, Main.rand.NextFloat()), Main.rand.NextFloat(1f, 1.8f)).Configure(false, Main.rand.Next(24, 28));
            }
            CEUtils.PlaySound("ExoHit" + Main.rand.Next(1, 5), Main.rand.NextFloat(0.8f, 1.2f), npc.Center);

            hit.Damage = HitDamage;
            hit.Knockback = HitKnockback;
            hit.PlayerImmuneFrames = HitImmuneFrames;
            hit.DamageClass = DamageClass.Generic;
        }
    }

    public class AzafureShieldDash : AzafureDashBase
    {
        public override string ID => "AzafureShieldDash";
        public override int Priority => 10;
        public override float Distance => AzafureChargeShield.DashDistance;
        public override int Duration => AzafureChargeShield.DashDuration;
        public override int Cooldown => AzafureChargeShield.DashCooldown;
        protected override int HitDamage => AzafureChargeShield.DashDamage;
        protected override float HitKnockback => AzafureChargeShield.DashKnockback;
        protected override int HitImmuneFrames => AzafureChargeShield.DashImmuneFrames;

        protected override bool TryGetCharge(Player player, out float charge)
        {
            charge = 0f;
            if (player.Entropy().AzafureChargeShieldItem?.ModItem is not AzafureChargeShield shield)
                return false;
            charge = shield.charge;
            return true;
        }

        protected override void ConsumeCharge(Player player, float cost)
        {
            if (player.Entropy().AzafureChargeShieldItem?.ModItem is AzafureChargeShield shield)
                shield.charge = Math.Max(0f, shield.charge - cost);
        }
    }
}
