using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    /// <summary>
    /// 熵之馈赠：替代灾厄新手包注入通道的自有礼包。
    /// 开包内容由 StartBagGItem.ModifyItemLoot 按 IGetFromStarterBag 接口物品统一注入；
    /// MagicStorage/ImproveGame 的开局便利物品由本类 ModifyItemLoot 条件注入；
    /// 首次进入世界的发放与一次性旗标由 EModPlayer.OnEnterWorld 侧落地，受 ServerConfig.ExtraItemsInStarterBag 控制。
    /// </summary>
    public class EntropyStarterBag : ModItem
    {
        // 暂用彩票箱贴图占位，正式贴图画好后换回同名资源
        public override string Texture => "CalamityEntropy/Content/Items/LotteryBox";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.consumable = true;
            Item.rare = ItemRarityID.White;
        }

        public override bool CanRightClick() => true;

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            if (CERef.Has)
            {
                return;
            }
            StartBagGItem.AddConvenienceMods(itemLoot);
        }
    }
}
