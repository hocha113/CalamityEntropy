using CalamityEntropy.Core.Cooldowns;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Weapons
{
    /// <summary>
    /// 「蓄势」大招原语静态门面(替代灾厄潜伏攻击,rogue-weapons.md §一)。
    /// 武器侧对照:原「潜伏就绪查询」→ IsReady(Item);
    /// 原 CEUtils.CostStealthForPlr(player) → TryConsume(player, Item);
    /// 原「给弹幕打 stealthStrike 标志」→ Empower(p)。
    /// 完整接入模板见 Doc/decouple/charge-api.md。
    /// </summary>
    public static class CEChargeWeapon
    {
        /// <summary>取该武器的充能计量器(每物品存储,随物品存档与同步)。HUD 可读 meter.Ratio。</summary>
        public static CEChargeMeter GetMeter(Item item) {
            if (item?.ModItem is not ICEChargeWeapon chargeWeapon)
                return null;
            return item.GetChargeMeter(chargeWeapon.ChargeProfile.Max);
        }

        /// <summary>大招是否就绪(只查询不消耗)。CanUseItem 门控与 HUD 用。</summary>
        public static bool IsReady(Item item) {
            CEChargeMeter meter = GetMeter(item);
            return meter != null && meter.Ready;
        }

        /// <summary>
        /// 尝试消耗就绪状态。就绪时清空充能并返回 true(本次攻击为大招),否则返回 false。
        /// 在武器 Shoot 开头调用;多弹幕大招整组只调用一次。
        /// 调用成功会打开当帧强化窗口:本帧内该玩家由本武器发射的弹幕自动获得蓄势强化标志,
        /// 因此走原版发射路径(Shoot 返回 true)的武器无需手动 Empower。
        /// </summary>
        public static bool TryConsume(Player player, Item item) {
            CEChargeMeter meter = GetMeter(item);
            if (meter == null || !meter.Consume())
                return false;
            player.GetModPlayer<CEChargePlayer>().OpenEmpowerWindow();
            return true;
        }

        /// <summary>把指定弹幕标记为蓄势强化弹并同步。对照原 stealthStrike = true 写法。</summary>
        public static void Empower(int projIndex) {
            if (projIndex >= 0 && projIndex < Main.maxProjectiles)
                Main.projectile[projIndex].SetEmpowered();
        }

        /// <summary>同上,直接传弹幕实例。</summary>
        public static void Empower(Projectile projectile) => projectile.SetEmpowered();

        /// <summary>
        /// 命中计数模式的充能入口,+1 次命中(自动应用充能速度加成)。
        /// 框架经 CEEmpowerGlobalProjectile 自动调用;极端场景(伤害不经弹幕父链,
        /// 如自定义生成源、直接改生命值的判定)可手动调用记功。非命中计数武器调用无效果。
        /// </summary>
        public static void CreditHit(Player player, Item item) {
            if (item?.ModItem is not ICEChargeWeapon chargeWeapon)
                return;
            if (chargeWeapon.ChargeProfile.Trigger != CEChargeTrigger.HitCount)
                return;
            Gain(player, item, chargeWeapon.ChargeProfile, 1f);
        }

        /// <summary>统一充能推进:应用玩家充能速度加成,恰好充满的那一帧播放就绪反馈。</summary>
        internal static void Gain(Player player, Item item, in CEChargeProfile profile, float amount) {
            CEChargeMeter meter = item.GetChargeMeter(profile.Max);
            float rate = player.GetModPlayer<CEChargePlayer>().ChargeRateMult;
            if (meter.Gain(amount * rate))
                PlayReadyFeedback(player);
        }

        /// <summary>简易就绪反馈:提示音 + 头顶文字,仅本地玩家可见。</summary>
        public static void PlayReadyFeedback(Player player) {
            if (Main.dedServ || player.whoAmI != Main.myPlayer)
                return;
            CEChargeMeter.PlayReadyCue(player);
            string text = CalamityEntropy.Instance.GetLocalization("ChargeReady", () => "蓄势就绪").Value;
            CombatText.NewText(player.getRect(), new Color(255, 224, 120), text);
        }
    }

    /// <summary>
    /// 蓄势系统的玩家侧状态:大招充能速度加成与释放当帧的强化窗口。
    /// </summary>
    public class CEChargePlayer : ModPlayer
    {
        /// <summary>
        /// 大招充能速度乘数,默认 1,每帧重置。
        /// 饰品在 UpdateAccessory 里做乘/加(对应 rogue-weapons.md §三「大招充能速度 +X%」)。
        /// </summary>
        public float ChargeRateMult = 1f;

        /// <summary>强化窗口所在帧。TryConsume 打开,同帧由本人物品使用生成的蓄势武器弹幕自动打标。</summary>
        private uint empowerWindowFrame = uint.MaxValue;

        public override void ResetEffects() {
            ChargeRateMult = 1f;
        }

        internal void OpenEmpowerWindow() => empowerWindowFrame = Main.GameUpdateCount;

        internal bool EmpowerWindowActive => empowerWindowFrame == Main.GameUpdateCount;
    }
}
