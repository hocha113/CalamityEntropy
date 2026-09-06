using CalamityEntropy.Common;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Cooldowns;
using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ShadeCloak : ModItem
    {
        // 自带暗影冲刺,冲刺期间无敌,固定5秒冷却。冷却就绪或冲刺进行中才占用其他冲刺。
        public static int CooldownTicks = 5 * 60;
        public const int DashDuration = 24;
        public const float DashSpeed = 18f;

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
            // 冷却就绪或冲刺进行中才占用:StartDash 当帧就会挂冷却,只看 HasCooldown 会让无敌窗里的排他掉下去
            bool occupyDash = player.GetModPlayer<SCDashMP>().DashTimer > 0
                || !player.HasCooldown(ShadeCloakDashCD.ID);
            player.Entropy().shadeDashExclusive = occupyDash;
            if (occupyDash)
                player.dashType = 0;
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
    /// <summary>暗影披风自管水平冲刺:输入与盾冲刺共用,冷却走 CECooldown,无敌只在冲刺窗口内。</summary>
    public class SCDashMP : ModPlayer
    {
        public int DashTimer;
        public int DashDir = 1;

        /// <summary>拖尾采样位移。冲刺中用 DashDir,不读可能已被冲掉的 velocity。</summary>
        public Vector2 TrailSampleOffset => DashTimer > 0
            ? new Vector2(DashDir * ShadeCloak.DashSpeed * 2f, 0f)
            : Player.velocity * 2f;

        public override void PostUpdateEquips()
        {
            // UpdateAccessory 里写的 dashType=0 会被后槽原版冲刺饰品盖掉,这里再清一次才赶得上随后的 DashMovement
            if (Player.Entropy().shadeDashExclusive)
                Player.dashType = 0;
        }

        /// <summary>接触伤害结算前启动冲刺并给无敌。放 PreUpdateMovement 会晚一拍,贴身对撞。</summary>
        public static void PrepareForNpcCollision(Player player)
        {
            var self = player.GetModPlayer<SCDashMP>();
            if (player.whoAmI != Main.myPlayer)
                return;

            if (self.DashTimer <= 0)
                self.TryStartDash();
            if (self.DashTimer > 0)
                self.ApplyPassThroughImmune();
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (DashTimer > 0)
                return false;
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

        public override void PreUpdateMovement()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            var mp = Player.Entropy();
            bool equipped = mp.hasAcc(ShadeCloak.ID);
            bool visual = equipped || mp.hasAccVisual(ShadeCloak.ID);

            if (DashTimer > 0)
                UpdateDash(mp, equipped, visual);
        }

        private void TryStartDash()
        {
            var mp = Player.Entropy();
            if (!mp.hasAcc(ShadeCloak.ID) || Player.mount.Active || Player.CCed)
                return;
            if (Player.HasCooldown(ShadeCloakDashCD.ID))
                return;
            if (!Player.GetModPlayer<CEShieldDashPlayer>().TryGetHorizontalDashDirection(out int direction))
                return;

            StartDash(mp, direction, true);
        }

        private void ApplyPassThroughImmune()
        {
            Player.immune = true;
            Player.immuneNoBlink = true;
            if (Player.immuneTime < 8)
                Player.immuneTime = 8;
            var mp = Player.Entropy();
            if (mp.immune < 8)
                mp.immune = 8;
        }

        private void StartDash(EModPlayer mp, int direction, bool visual)
        {
            DashDir = direction;
            DashTimer = ShadeCloak.DashDuration;
            Player.velocity.X = direction * ShadeCloak.DashSpeed;
            Player.ChangeDir(direction);
            Player.timeSinceLastDashStarted = 0;
            Player.RemoveAllGrapplingHooks();
            Player.AddCooldown(ShadeCloakDashCD.ID, ShadeCloak.CooldownTicks);
            ApplyPassThroughImmune();

            if (!visual)
                return;

            CEUtils.PlaySound("Dash2", 1, Player.Center);
            Vector2 dashAxis = new Vector2(direction * ShadeCloak.DashSpeed, 0f);
            var beam = PRTLoader.NewParticle<PRT_DashBeam>(Player.Center, dashAxis, new Color(0, 0, 0, 210), 1f)
                .Configure(1, true, PRTDrawModeEnum.NonPremultiplied);
            beam.maxLength = 30;
            // 立刻沿冲刺轴铺三点,否则首帧 odp 不够、ToRotation(0) 会把拖尾画成朝右
            beam.AddPoint(Player.Center - dashAxis * 2f);
            beam.AddPoint(Player.Center);
            beam.AddPoint(Player.Center + dashAxis * 2f);
            mp.avTrail = beam;
            for (int i = 0; i < 12; i++)
            {
                var orb = PRTLoader.NewParticle<PRT_ShadeCloakOrb>(Vector2.Zero, CEUtils.randomPointInCircle(4), Color.Black, 1)
                    .Configure(1, true, PRTDrawModeEnum.NonPremultiplied, -1, ShadeCloak.CooldownTicks);
                orb.PlayerIndex = Player.whoAmI;
            }
        }

        private void UpdateDash(EModPlayer mp, bool equipped, bool visual)
        {
            Player.dashDelay = -1;
            Player.velocity.X = DashDir * ShadeCloak.DashSpeed;
            Player.ChangeDir(DashDir);

            if (equipped)
            {
                Player.RemoveAllGrapplingHooks();
                if (mp.immune < 4)
                    mp.immune = 4;
            }

            Vector2 dashDir = new Vector2(DashDir, 0f);
            if (visual)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pVel = -dashDir.RotatedByRandom(0.12f) * 40f;
                    PRTLoader.NewParticle<PRT_ShadeDashParticle>(Player.Center + dashDir * (ShadeCloak.DashSpeed * 6f)
                        + CEUtils.randomPointInCircle(26), pVel, Color.White, 1)
                        .Configure(1, true, PRTDrawModeEnum.NonPremultiplied, pVel.ToRotation(), 16);
                }
            }

            DashTimer--;
            if (DashTimer <= 0 && mp.avTrail != null)
            {
                mp.avTrail.Lifetime = mp.avTrail.Time + 30;
            }
        }
    }
}
