using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Dash;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ShadeCloak : ModItem
    {
        // 自身不提供冲刺:佩戴其它冲刺饰品时,每 5 秒把下一次常规冲刺强化为暗影冲刺
        public static int CooldownTicks = 5 * 60;
        public const float SpeedMult = 1.2f;

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
            Item.expert = true;
        }
        public static string ID = "ShadeCloak";

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().addEquip(ID, !hideVisual);
            player.GetModPlayer<CEDashPlayer>().Offer(CEDashRegistry.GetEnhancer<ShadeCloakEnhancer>());
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().addEquipVisual(ID);
        }
        public override void AddRecipes()
        {
            /*CreateRecipe().AddIngredient(ItemID.SoulofNight, 8)
                .AddIngredient(ItemID.Ectoplasm, 12)
                .AddIngredient(ItemID.SoulofNight, 4)
                .AddIngredient(ItemID.Ectoplasm, 8);*/
        }
    }

    /// <summary>
    /// 暗影冲刺强化器:冷却就绪时把常规冲刺(本模组盾冲刺、鞘翅冲刺、原版冲刺)改成暗影冲刺,
    /// 全程无敌、速度与加速度 +20%。冷却走 CECooldown,视觉是黑色光束拖尾加环绕暗球。
    /// </summary>
    public class ShadeCloakEnhancer : CEDashEnhancer
    {
        public override string ID => "ShadeCloak";
        public override float SpeedMult => ShadeCloak.SpeedMult;
        public override bool Invincible => true;

        public override bool TryConsume(Player player)
        {
            if (player.HasCooldown(ShadeCloakDashCD.ID))
                return false;
            player.AddCooldown(ShadeCloakDashCD.ID, ShadeCloak.CooldownTicks);
            return true;
        }

        public override void OnStart(Player player, CEDashState state)
        {
            CEUtils.PlaySound("Dash2", 1, player.Center);
            Vector2 dashAxis = state.Direction * 18f;
            var beam = PRTLoader.NewParticle<PRT_DashBeam>(player.Center, dashAxis, new Color(0, 0, 0, 210), 1f)
                .Configure(1, true, PRTDrawModeEnum.NonPremultiplied);
            beam.maxLength = 30;
            // 先沿冲刺轴铺三点,否则首帧 odp 不够、ToRotation(0) 会把拖尾画成朝右
            beam.AddPoint(player.Center - dashAxis * 2f);
            beam.AddPoint(player.Center);
            beam.AddPoint(player.Center + dashAxis * 2f);
            state.EnhancerData = beam;
            for (int i = 0; i < 12; i++)
            {
                var orb = PRTLoader.NewParticle<PRT_ShadeCloakOrb>(Vector2.Zero, CEUtils.randomPointInCircle(4), Color.Black, 1)
                    .Configure(1, true, PRTDrawModeEnum.NonPremultiplied, -1, ShadeCloak.CooldownTicks);
                orb.PlayerIndex = player.whoAmI;
            }
        }

        public override void OnVisuals(Player player, CEDashState state)
        {
            if (state.EnhancerData is PRT_DashBeam beam && beam.active)
                beam.AddPoint(player.Center + player.velocity);

            Vector2 dashDir = state.Direction;
            for (int i = 0; i < 2; i++)
            {
                Vector2 pVel = -dashDir.RotatedByRandom(0.12f) * 40f;
                PRTLoader.NewParticle<PRT_ShadeDashParticle>(player.Center + dashDir * 100f + CEUtils.randomPointInCircle(26), pVel, Color.White, 1)
                    .Configure(1, true, PRTDrawModeEnum.NonPremultiplied, pVel.ToRotation(), 16);
            }
        }

        public override void OnEnd(Player player, CEDashState state)
        {
            if (state.EnhancerData is PRT_DashBeam beam && beam.active)
                beam.Lifetime = beam.Time + 30;
        }
    }
}
