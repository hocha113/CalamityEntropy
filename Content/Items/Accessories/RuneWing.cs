using CalamityEntropy.Common;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Items.Armor;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Dash;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Wings)]
    public class RuneWing : CEBaseWings, ISpecialDrawingWing
    {
        public static float HorSpeed = 7.5f;
        public static float AccMul = 1.2f;
        public static int wTime = 170;
        /// <summary>灵魂冲刺持续帧数。</summary>
        public const int DashDuration = 30;
        /// <summary>灵魂冲刺总位移(像素),40 格。</summary>
        public const float DashDistance = 40 * 16f;
        /// <summary>灵魂冲刺冷却(帧),30 秒。</summary>
        public const int DashCooldownTicks = 30 * 60;
        public override float BonusAscentWhileFalling => 1f;
        public override float BonusAscentWhileRising => 0.12f;
        public override float RisingSpeedThreshold => 1f;
        public override float MaxAscentSpeed => 2.8f;
        public override float BaseAscent => 0.13f;
        public int AnimationTick => 4;
        public int FallingFrame => 0;
        public int MaxFrame => 5;
        public int SlowFallingFrame => 5;
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(wTime, HorSpeed, AccMul, false, 20, 2.8f);
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 22;
            Item.height = 20;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;

        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[CD]", (DashCooldownTicks / 60).ToString());
            // 脱离灾厄:灾厄 IntegrateHotkey 扩展改自有键名提示
            tooltips.Replace("[KEY]", CEKeybinds.RuneDashHotKey.TooltipKeyHint());
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().addEquip("RuneWing", !hideVisual);
            player.GetModPlayer<CEDashPlayer>().Offer(CEDashRegistry.Get<SoarRuneDash>());
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().addEquipVisual("RuneWing");
        }

    }

    /// <summary>
    /// 翱翔符文的灵魂冲刺:按键朝鼠标方向冲 30 帧,全程无敌、穿平台,冷却 30 秒。
    /// 可以打断进行中的常规冲刺;不接受暗影强化。
    /// </summary>
    public class SoarRuneDash : CEDashEffect
    {
        public override string ID => "SoarRuneDash";
        public override bool UsesDoubleTap => false;
        public override ModKeybind Hotkey => CEKeybinds.RuneDashHotKey;
        public override bool CanInterrupt => true;
        public override int Duration => RuneWing.DashDuration;
        public override float Distance => RuneWing.DashDistance;
        public override int Cooldown => 20;
        public override float Curve => 1.5f;
        public override float GravityMult => 0f;
        public override bool Invincible => true;
        public override bool IgnorePlatforms => true;
        public override bool CanBeEnhanced => false;

        public override float EndSpeed(Player player, Vector2 direction) => 4f;

        public override Vector2? HotkeyDirection(Player player)
        {
            Vector2 toMouse = Main.MouseWorld - player.Center;
            return toMouse == Vector2.Zero ? new Vector2(player.direction, 0f) : toMouse;
        }

        public override bool CanStart(Player player) => !player.HasCooldown(RuneDashCD.ID);

        public override void OnStart(Player player, CEDashState state)
        {
            if (!state.Remote)
                player.AddCooldown(RuneDashCD.ID, RuneWing.DashCooldownTicks);
            CEUtils.PlaySound("RuneDash", 1, player.Center);
            var trail = PRTLoader.NewParticle<PRT_ProminenceTrail>(player.Center, Vector2.Zero, Color.White, 5f)
                .Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
            trail.color1 = Color.DeepSkyBlue;
            trail.color2 = Color.White;
            trail.maxLength = 120;
            state.EffectData = trail;
        }

        public override void OnVisuals(Player player, CEDashState state)
        {
            if (state.EffectData is not PRT_ProminenceTrail trail || !trail.active)
            {
                trail = PRTLoader.NewParticle<PRT_ProminenceTrail>(player.Center, Vector2.Zero, Color.White, 5f)
                    .Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
                trail.color1 = Color.DeepSkyBlue;
                trail.color2 = Color.White;
                trail.maxLength = 120;
                state.EffectData = trail;
            }
            // 拖尾以本帧位移插 10 个点,冲刺速度太快只加一个点会画成折线
            Vector2 step = player.velocity * 0.1f;
            Vector2 from = player.Center - player.velocity;
            for (int f = 1; f <= 10; f++)
                trail.AddPoint(from + step * f);
            trail.Lifetime = trail.Time + 13;

            for (int i = 0; i < 3; i++)
            {
                PRTLoader.NewParticle<PRT_RuneParticle>(player.Center + CEUtils.randomVec(26), CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-0.6f, 0.6f), Color.White, 1)
                    .Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
            }
        }
    }
}
