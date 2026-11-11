using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SandseaDragon.Loot
{
    public class DesertGladiatorNecklace : ModItem
    {
        #region 数值
        public const float DamageBonus = 0.08f;  //全伤害加成(策划 8%)
        public const int ArmorPenetration = 10;  //护甲穿透(策划 10 点)
        public const float SandAccelMult = 1.3f;  //站在沙地上时的加速度倍率(策划 +30%)
        public const float SandTopSpeedMph = 40f;  //站在沙地上时补到的最大移速(秒表读数,mph)
        public const float MphPerSpeed = 216000f / 42240f;  //原版秒表换算(Main.cs 的 InfoDisplay.Stopwatch),mph = 像素/帧 x 此值
        public const float SandTopSpeed = SandTopSpeedMph / MphPerSpeed;  //40 mph 折合约 7.82 像素/帧
        public const float SprintAccelShare = 0.2f;  //原版超过 maxRunSpeed 后的加速度占 runAcceleration 的比例(Player.HorizontalMovement)
        public const float WingSprintMult = 2f;  //装着翅膀时原版把上面那段加速度再加一倍
        #endregion

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 30;
            Item.accessory = true;
            Item.expert = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: 6);
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetDamage(DamageClass.Generic) += DamageBonus;
            player.GetArmorPenetration(DamageClass.Generic) += ArmorPenetration;
            //沙块窒息只经 AddBuff(Suffocation) 施加,免疫这个增益就够
            player.buffImmune[BuffID.Suffocation] = true;
            player.GetModPlayer<DesertGladiatorPlayer>().equipped = true;
        }
    }

    /// <summary>沙漠角斗士项链的沙地机动,站在沙类物块上时加速度 x1.3、冲刺速度上限补到秒表 40 mph</summary>
    public class DesertGladiatorPlayer : ModPlayer
    {
        public bool equipped;  //项链每帧置位,各端都会算到

        public override void ResetEffects() {
            equipped = false;
        }

        public override void PostUpdateRunSpeeds() {
            //runningOnSand 是原版沙丘行者靴用的落地标记,按脚下物块判沙、硬化沙、沙岩及其感染变体;坐骑速度由坐骑接管,不叠
            if (!equipped || !Player.runningOnSand || Player.mount.Active) {
                return;
            }
            Player.runAcceleration *= DesertGladiatorNecklace.SandAccelMult;
            //冲到 accRunSpeed 后原版每超限一帧扣一次 runSlowdown,秒表读到的是锯齿均值,
            //比上限低 (runSlowdown - 冲刺加速度) / 2,上限按此补回读数才稳在 40
            float sprintAccel = Player.runAcceleration * DesertGladiatorNecklace.SprintAccelShare;
            if (Player.wingsLogic > 0) {
                sprintAccel *= DesertGladiatorNecklace.WingSprintMult;
            }
            float cap = DesertGladiatorNecklace.SandTopSpeed + Math.Max(0f, Player.runSlowdown - sprintAccel) / 2f;
            if (Player.accRunSpeed < cap) {
                Player.accRunSpeed = cap;
            }
        }
    }
}
