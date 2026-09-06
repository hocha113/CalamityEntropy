using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    /// <summary>
    /// 自有翼类基类:承接灾厄 BaseWings 的五项垂直飞行参数与 VerticalWingSpeeds 派发。
    /// 灾厄的动态飞行属性 Tooltip 块依赖其本地化与配置,不移植。
    /// </summary>
    public abstract class CEBaseWings : ModItem
    {
        // 翅膀文案在分册 Mods.CalamityEntropy.Items.Accessories.Wings.hjson,不走主表 Items.*
        public override string LocalizationCategory => "Items.Accessories.Wings";

        /// <summary>下落时的额外上升加速度。</summary>
        public virtual float BonusAscentWhileFalling => 0.5f;

        /// <summary>低速上升时的额外上升加速度。</summary>
        public virtual float BonusAscentWhileRising => 0.1f;

        /// <summary>触发上升加速的垂直速度阈值(乘玩家跳跃速度)。</summary>
        public virtual float RisingSpeedThreshold => 0.5f;

        /// <summary>最大上升速度阈值(乘玩家跳跃速度)。</summary>
        public virtual float MaxAscentSpeed => 1.5f;

        /// <summary>基础每帧上升加速度。</summary>
        public virtual float BaseAscent => 0.1f;

        public override void SetDefaults()
        {
            Item.accessory = true;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            if (Item.wingSlot == -1)
                return;
            ascentWhenFalling = BonusAscentWhileFalling;
            ascentWhenRising = BonusAscentWhileRising;
            maxCanAscendMultiplier = RisingSpeedThreshold;
            maxAscentMultiplier = MaxAscentSpeed;
            constantAscend = BaseAscent;

            AdditionalFlightMovement(player, ref ascentWhenFalling, ref ascentWhenRising, ref maxCanAscendMultiplier, ref maxAscentMultiplier, ref constantAscend);
        }

        /// <summary>上冲/悬停等额外飞行行为的扩展点。</summary>
        public virtual void AdditionalFlightMovement(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend) { }
    }
}
