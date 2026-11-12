using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Core.Dash;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Shield)]
    public class OdinsRefuge : ModItem
    {
        // 2026-08-31 平衡案重做:18防,免疫击退,免疫火块,
        //玩家有神圣屏障可以格挡,自己和所有队友最终伤害 -15% 而且不叠加,仇恨加 600
        // 减益免疫与原版十字章护身符同一组,不含渊洋神迹那张额外表。
        // 上述整套只在无灾厄时生效。装灾厄时配方换回 3.33 的两件成品盾,
        // 效果随之整体回到 3.33 形态,见 ApplyCalamityEraEffects。
        public const float TeamWardDR = 0.15f;

        // 以下常数全部照抄灾厄 2.2.2 原值,不是自拟:
        // AsgardianAegis.cs:22-27 的盾击与撞击爆炸,DeificAmulet.cs:18-19 的无敌帧上限与坠星伤害,
        // Utilities/PlayerUtils.cs:467 的大伤额外无敌帧,CalPlayer/CalamityPlayerHitHurt.cs:2378 的坠星数量。
        public const int ShieldSlamDamage = 1000;
        public const float ShieldSlamKnockback = 15f;
        public const int ShieldSlamIFrames = 12;
        public const int RamExplosionDamage = 300;
        public const int DashDuration = 20;
        public const float DashDistance = 16 * 20f;
        public const int DashCooldown = 30;
        /// <summary>护身符按缺失血量给的无敌帧上限,满血 0 帧、四分之一血及以下拿满。</summary>
        public const int MaxBonusIFrames = 30;
        public const int BigHitDamageThreshold = 200;
        public const int BigHitBonusIFrames = 30;
        public const int RetaliationStarDamage = 130;
        public const int RetaliationStarCount = 12;

        public override void SetDefaults() {
            Item.width = 86;
            Item.height = 86;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.accessory = true;
            // 配方与效果都随时代走,防御也一起:装灾厄时交的是 3.33 那两件成品盾,回 3.33 的 24 防
            Item.defense = CERef.Has ? 24 : 18;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            //两个时代各写一个完整方法,效果不叠加,装灾厄时配方要交出阿斯加德之庇护和神之壁垒
            //两件屠龙后的成品盾,效果如果停在 4.0 平衡案那套,合成之后反而变弱
            if (CERef.Has) {
                ApplyCalamityEraEffects(player);
                return;
            }
            //玩家开启神圣屏障,用来格挡
            player.Entropy().holyMantle = true;
            //团队最终减伤光环,在 EPHurtModifier 结算,不可叠加
            player.Entropy().odinAura = true;
            player.noKnockback = true;
            player.fireWalk = true;
            ApplyAnkhCharmImmune(player);
            player.aggro += 600;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            // 装灾厄整段换成灾厄时代键,无灾厄不碰,Items.OdinsRefuge.Tooltip 保持 4.0 原文
            if (!CERef.Has)
                return;
            string cal = Mod.GetLocalization("OdinsRefugeCal").Value;
            int insertAt = -1;
            for (int i = 0; i < tooltips.Count; i++) {
                if (tooltips[i].Name.StartsWith("Tooltip")) {
                    insertAt = i;
                    break;
                }
            }
            for (int i = tooltips.Count - 1; i >= 0; i--) {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                    tooltips.RemoveAt(i);
            }
            if (insertAt < 0)
                insertAt = tooltips.Count;
            string[] lines = cal.Replace("\r\n", "\n").Split('\n');
            int offset = 0;
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                tooltips.Insert(insertAt + offset, new TooltipLine(Mod, "TooltipCal" + offset, line));
                offset++;
            }
        }

        /// <summary>
        /// 不转调阿斯加德之庇护和神之壁垒,那两个方法写 DashID / dAmulet / rampartOfDeities
        /// DashID 会把冲刺交回灾厄,和本仓 Core/Dash 抢同一份双击
        /// </summary>
        private static void ApplyCalamityEraEffects(Player player) {
            //神圣屏障两个时代都开着,配方两侧也都要交神圣斗篷
            player.Entropy().holyMantle = true;
            // 阿斯加德之庇护:免疫击退与盾击冲刺
            player.noKnockback = true;
            player.GetModPlayer<CEDashPlayer>().Offer(CEDashRegistry.Get<OdinShieldSlamDash>());
            // 神之壁垒:十字项链无敌帧、恐慌项链、圣骑士盾团队分摊。
            // 三个都是原版字段,每端每帧从装备重算,无需同步也不进存档。
            player.longInvince = true;
            player.panic = true;
            player.hasPaladinShield = true;
            // 受击侧的四条(护身符递增无敌帧、大伤额外无敌帧、蜂蜜、坠星反击)与
            // 冰霜屏障都要在血量结算完之后才准,统一放 EModPlayer,见 odinRefugeCalEra 的读取点
            player.Entropy().odinRefugeCalEra = true;
        }

        /// <summary>与原版十字章护身符(ItemID.AnkhCharm, Player.cs type 1612)同一组减益。</summary>
        public static void ApplyAnkhCharmImmune(Player player) {
            player.buffImmune[BuffID.Weak] = true;
            player.buffImmune[BuffID.BrokenArmor] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Slow] = true;
            player.buffImmune[BuffID.Confused] = true;
            player.buffImmune[BuffID.Silenced] = true;
            player.buffImmune[BuffID.Cursed] = true;
            player.buffImmune[BuffID.Darkness] = true;
            player.buffImmune[BuffID.Stoned] = true;
        }

        public override void AddRecipes() {
            if (CECal.CalChainReady(CEID.Item_AsgardianAegis, CEID.Item_RampartofDeities)) {
                CreateRecipe().
                AddIngredient(CEID.Item_AsgardianAegis, 1).
                AddIngredient(CEID.Item_RampartofDeities, 1).
                AddIngredient(ModContent.ItemType<HolyMantle>(), 1).
                AddIngredient(ModContent.ItemType<VoidBar>(), 10).
                AddTile(ModContent.TileType<VoidWellTile>()).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient(ItemID.AnkhShield, 1).
                AddIngredient(ItemID.HeroShield, 1).
                AddIngredient(ModContent.ItemType<HolyMantle>(), 1).
                AddIngredient(ModContent.ItemType<SupernovaGel>(), 5).
                AddTile(ModContent.TileType<GalaxySolidifierTile>()).
                Register();
        }
    }

    /// <summary>无灾厄时不登记,这条冲刺不会被触发</summary>
    public class OdinShieldSlamDash : CEDashEffect
    {
        public override string ID => "OdinShieldSlam";
        /// <summary>屠龙后档位,压过阿扎弗盾冲(10)与其余低阶冲刺。</summary>
        public override int Priority => 20;
        public override bool HitsEnemies => true;
        public override int Duration => OdinsRefuge.DashDuration;
        public override float Distance => OdinsRefuge.DashDistance;
        public override int Cooldown => OdinsRefuge.DashCooldown;
        /// <summary>长冲刺:速度撑满大半程再收尾,与阿扎弗盾冲同一条曲线。</summary>
        public override float Curve => 1.6f;

        public override void OnStart(Player player, CEDashState state) {
            CEUtils.PlaySound("Dash2", Main.rand.NextFloat(0.85f, 1.05f), player.Center, 6, 0.55f);
            Vector2 back = -state.Direction;
            for (int i = 0; i < 12; i++) {
                Dust dust = Dust.NewDustPerfect(player.Center + CEUtils.randomPointInCircle(14), DustID.FrostStaff,
                    back.RotatedByRandom(0.5f) * Main.rand.NextFloat(3f, 9f), 0, default, Main.rand.NextFloat(1.1f, 1.6f));
                dust.noGravity = true;
            }
        }

        public override void OnVisuals(Player player, CEDashState state) {
            // 霜与金的双色残迹,强度随冲刺进度收束
            float intensity = 1f - state.Progress;
            Vector2 axis = state.Direction;
            Vector2 side = axis.RotatedBy(MathHelper.PiOver2);
            int dustCount = 1 + (int)(2 * intensity);
            for (int i = 0; i < dustCount; i++) {
                Dust frost = Dust.NewDustPerfect(player.Center + side * Main.rand.NextFloat(-16f, 16f) - axis * 20f,
                    DustID.FrostStaff, -player.velocity * Main.rand.NextFloat(0.1f, 0.5f), 0, default, Main.rand.NextFloat(1.2f, 1.8f));
                frost.noGravity = true;
                Dust holy = Dust.NewDustPerfect(player.Center + side * Main.rand.NextFloat(-12f, 12f),
                    DustID.GoldFlame, -player.velocity * Main.rand.NextFloat(0.05f, 0.3f), 0, default, Main.rand.NextFloat(1f, 1.5f));
                holy.noGravity = true;
                holy.alpha = 120;
            }
        }

        public override void OnHit(Player player, NPC npc, CEDashState state, ref CEDashHit hit) {
            if (state.HitCount == 1)
                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 5));
            CEUtils.PlaySound("metalhit", Main.rand.NextFloat(0.8f, 1.1f), npc.Center);

            hit.Damage = OdinsRefuge.ShieldSlamDamage;
            hit.Knockback = OdinsRefuge.ShieldSlamKnockback;
            hit.PlayerImmuneFrames = OdinsRefuge.ShieldSlamIFrames;
            hit.DamageClass = DamageClass.Melee;

            // 撞击爆炸。原灾厄给的击退是 20,自有爆炸助手不带击退参数,这一项无法等价,已接受
            int explosionDamage = (int)player.GetBestClassDamage().ApplyTo(OdinsRefuge.RamExplosionDamage);
            CEUtils.SpawnExplotionFriendly(player.GetSource_FromThis(), player, npc.Center, explosionDamage, 90f, DamageClass.Generic);
            npc.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300);
        }
    }
}
