using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Dash;
using InnoVault.PRT;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Wings)]
    public class VoidElytra : CEBaseWings
    {
        public static float HorSpeed = 12;
        public static float AccMul = 3;
        public static int wTime = 340;
        /// <summary>四向冲刺持续帧数。</summary>
        public const int DashDuration = 18;
        /// <summary>四向冲刺总位移(像素),14 格。</summary>
        public const float DashDistance = 14 * 16f;
        public const int DashCooldown = 40;
        /// <summary>一次冲刺消耗的飞行时间。</summary>
        public const int DashWingCost = 20;
        /// <summary>飞行时间不足时改由虚空侵蚀扣血的时长。</summary>
        public const int DashVoidTouchTicks = 40;
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(wTime, HorSpeed, AccMul, false, 20, 3f);
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 22;
            Item.height = 20;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = CECal.RarityTurquoise(ModContent.RarityType<NihilityBlue>());
            Item.accessory = true;

        }
        public override float BonusAscentWhileFalling => 1f;
        public override float BonusAscentWhileRising => 0.2f;
        public override float RisingSpeedThreshold => 1f;
        public override float MaxAscentSpeed => 3f;
        public override float BaseAscent => 0.135f;
        public override bool WingUpdate(Player player, bool inUse)
        {
            if (inUse && !player.GetModPlayer<CEDashPlayer>().IsDashing)
            {
                for (int i = 0; i < 10; i++)
                {
                    //翼飞时拖PRT_Void,Opacity=0.2旧VoidElytra原值
                    var p = PRTLoader.NewParticle<PRT_Void>((player.Center - new Vector2(14f, 0f) * (float)player.direction) - player.velocity * ((float)i * 0.1f) + new Vector2(-8f, 10f) * new Vector2((float)player.direction, 1f) * ((float)i * 0.1f), new Vector2(-8f, 10f) * new Vector2((float)player.direction, 1f), Color.White, 1f);
                    p.Opacity = 0.2f;
                }

            }

            return base.WingUpdate(player, inUse);
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CEDashPlayer>().Offer(CEDashRegistry.Get<VoidElytraDash>());
            if (player.wingTime < 2 && !(player.mount.Active))
            {
                player.wingTime = 2;
                player.AddBuff(ModContent.BuffType<VoidTouch>(), 5);
            }
        }

    }

    /// <summary>
    /// 虚空推进鞘翅的四向冲刺:双击任意方向或按冲刺键,消耗飞行时间;
    /// 飞行时间不够时照样能冲,改由虚空侵蚀扣血。冲刺期间无重力。
    /// </summary>
    public class VoidElytraDash : CEDashEffect
    {
        public override string ID => "VoidElytraDash";
        public override int Priority => 1;
        public override bool Omnidirectional => true;
        public override int Duration => VoidElytra.DashDuration;
        public override float Distance => VoidElytra.DashDistance;
        public override int Cooldown => VoidElytra.DashCooldown;
        public override float GravityMult => 0f;

        public override void OnStart(Player player, CEDashState state)
        {
            if (!state.Remote)
            {
                if (player.wingTime >= VoidElytra.DashWingCost)
                {
                    player.wingTime -= VoidElytra.DashWingCost;
                }
                else
                {
                    player.wingTime = 0;
                    player.AddBuff(ModContent.BuffType<VoidTouch>(), VoidElytra.DashVoidTouchTicks);
                }
            }
            Vector2 burst = state.Direction * state.CurrentSpeed;
            for (int i = 0; i < 32; i++)
            {
                //PRT_Void dash残影,Opacity/vd spawn后赋,旧VoidParticles原值
                var p = PRTLoader.NewParticle<PRT_Void>(player.Center, burst + CEUtils.randomPointInCircle(6), Color.White, 1f);
                p.Opacity = 0.36f;
                p.vd = 0.9f;
            }
        }

        public override void OnVisuals(Player player, CEDashState state)
        {
            var p = PRTLoader.NewParticle<PRT_Void>(player.Center, Vector2.Zero, Color.White, 1f);
            p.Opacity = 0.34f;
            p.vd = 0.9f;
            p = PRTLoader.NewParticle<PRT_Void>(player.Center - player.velocity / 2, Vector2.Zero, Color.White, 1f);
            p.Opacity = 0.34f;
            p.vd = 0.9f;
        }
    }
}
