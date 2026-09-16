namespace CalamityEntropy.Core.Weapons
{
    /// <summary>
    /// 「蓄势」大招触发器类型,对应 rogue-weapons.md §一的三种触发器。
    /// </summary>
    public enum CEChargeTrigger
    {
        /// <summary>充能条:武器持有期间每帧蓄能,满后下次攻击自动作为大招释放。</summary>
        ChargeBar,

        /// <summary>命中计数:该武器普通弹幕每真实命中一次 +1,达到上限后下次攻击为大招。</summary>
        HitCount,

        /// <summary>周期就绪:随时间自动回复(在背包即可),满后下次攻击为大招,释放后重新进入冷却。</summary>
        Periodic
    }

    /// <summary>
    /// 单件武器的蓄势参数。武器在 <see cref="ICEChargeWeapon.ChargeProfile"/> 里声明一次,
    /// 其余推进/就绪反馈/释放乘数全部由框架自动处理。
    /// </summary>
    public readonly struct CEChargeProfile
    {
        public readonly CEChargeTrigger Trigger;

        /// <summary>充满所需量。充能条/周期就绪为帧数(秒 × 60),命中计数为命中次数。</summary>
        public readonly float Max;

        /// <summary>大招释放时的伤害乘数,承接原灾厄 StealthDamageMultiplier。</summary>
        public readonly float DamageMult;

        /// <summary>大招释放时的弹速乘数,承接原灾厄 StealthVelocityMultiplier。</summary>
        public readonly float VelocityMult;

        /// <summary>大招释放时的击退乘数,承接原灾厄 StealthKnockbackMultiplier。</summary>
        public readonly float KnockbackMult;

        private CEChargeProfile(CEChargeTrigger trigger, float max, float damageMult, float velocityMult, float knockbackMult) {
            Trigger = trigger;
            Max = max;
            DamageMult = damageMult;
            VelocityMult = velocityMult;
            KnockbackMult = knockbackMult;
        }

        /// <summary>充能条触发器:持有武器 seconds 秒充满。</summary>
        public static CEChargeProfile ChargeBar(float seconds, float damageMult = 1f, float velocityMult = 1f, float knockbackMult = 1f)
            => new(CEChargeTrigger.ChargeBar, seconds * 60f, damageMult, velocityMult, knockbackMult);

        /// <summary>命中计数触发器:普通弹幕命中 hits 次就绪。</summary>
        public static CEChargeProfile HitCount(int hits, float damageMult = 1f, float velocityMult = 1f, float knockbackMult = 1f)
            => new(CEChargeTrigger.HitCount, hits, damageMult, velocityMult, knockbackMult);

        /// <summary>周期就绪触发器:释放后 seconds 秒自动重新就绪。</summary>
        public static CEChargeProfile Periodic(float seconds, float damageMult = 1f, float velocityMult = 1f, float knockbackMult = 1f)
            => new(CEChargeTrigger.Periodic, seconds * 60f, damageMult, velocityMult, knockbackMult);
    }

    /// <summary>
    /// 蓄势武器接口。武器 ModItem 实现本接口后:
    /// 充能推进(HoldItem/UpdateInventory)、命中计数、就绪音效与文字提示、
    /// 就绪时的伤害/弹速/击退乘数(ModifyShootStats)、头顶充能条与物品栏角标全部自动生效;
    /// 武器自身只需在 Shoot 里调 <see cref="CEChargeWeapon.TryConsume"/> 决定大招释放。
    /// 注意:每物品充能仅适用于 maxStack = 1 的武器,可堆叠物品勿用。
    /// </summary>
    public interface ICEChargeWeapon
    {
        CEChargeProfile ChargeProfile { get; }
    }
}
