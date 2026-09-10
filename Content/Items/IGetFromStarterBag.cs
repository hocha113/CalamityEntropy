using System.Collections.Generic;
using CalamityEntropy.Common;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public interface IGetFromStarterBag
    {
        public bool OwnAble(Player player, ref int count);
    }
    public class StartBagGItem : GlobalItem
    {
        public static bool NameContains(Player player, string str)
        {
            return player.name.ToLower().Contains(str);
        }
        public static List<int> items;
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            bool calBag = CERef.Has && CEID.Item_StarterBag > 0 && item.type == CEID.Item_StarterBag;
            // 装灾厄后旧档熵之馈赠仍开出接口物品,避免空壳;发放仍由 OnEnterWorld 按 A9 停发
            bool ownBag = item.ModItem is EntropyStarterBag;
            if (!calBag && !ownBag)
            {
                return;
            }
            foreach (int id in items)
            {
                Item loot = ContentSamples.ItemsByType[id];
                if (loot.ModItem is IGetFromStarterBag gfsb)
                {
                    int ItemCount = 1;
                    gfsb.OwnAble(Main.LocalPlayer, ref ItemCount);
                    itemLoot.Add(ItemDropRule.ByCondition(new OwnableCondition(gfsb), id, 1, ItemCount, ItemCount));
                }
            }
            if (calBag)
            {
                AddConvenienceMods(itemLoot);
            }
        }

        internal static void AddConvenienceMods(ItemLoot itemLoot)
        {
            ExtraItemsEnabledCondition extrasOn = new ExtraItemsEnabledCondition();
            if (ModLoader.TryGetMod("MagicStorage", out Mod magicStorage))
            {
                if (magicStorage.TryFind<ModItem>("CraftingAccess", out ModItem craftingAccess))
                {
                    itemLoot.Add(ItemDropRule.ByCondition(extrasOn, craftingAccess.Type));
                }
                if (magicStorage.TryFind<ModItem>("StorageHeart", out ModItem storageHeart))
                {
                    itemLoot.Add(ItemDropRule.ByCondition(extrasOn, storageHeart.Type));
                }
                if (magicStorage.TryFind<ModItem>("StorageUnit", out ModItem storageUnit))
                {
                    itemLoot.Add(ItemDropRule.ByCondition(extrasOn, storageUnit.Type, 1, 10, 10));
                }
            }
            if (ModLoader.TryGetMod("ImproveGame", out Mod improveGame))
            {
                string[] names = new string[] { "MagickWand", "SpaceWand", "CreateWand", "PotionBag", "BannerChest" };
                for (int i = 0; i < names.Length; i++)
                {
                    if (improveGame.TryFind<ModItem>(names[i], out ModItem tool))
                    {
                        itemLoot.Add(ItemDropRule.ByCondition(extrasOn, tool.Type));
                    }
                }
            }
        }
        // 把 OwnAble 判定包装为原生掉落条件（按掉落时的实际玩家判定）
        private class OwnableCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            private readonly IGetFromStarterBag gfsb;
            public OwnableCondition(IGetFromStarterBag gfsb)
            {
                this.gfsb = gfsb;
            }
            public bool CanDrop(DropAttemptInfo info)
            {
                if (!ServerConfig.Instance.ExtraItemsInStarterBag)
                {
                    return false;
                }
                int count = 1;
                return gfsb.OwnAble(info.player, ref count);
            }
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }
        // 开包当下读配置,关掉「新手礼包额外物品」则灾厄包与自有包都不塞额外件
        private class ExtraItemsEnabledCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info)
            {
                return ServerConfig.Instance.ExtraItemsInStarterBag;
            }
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }
    }
}
