using System.Collections.Generic;
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
            bool ownBag = !CERef.Has && item.ModItem is EntropyStarterBag;
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
            if (ModLoader.TryGetMod("MagicStorage", out Mod magicStorage))
            {
                if (magicStorage.TryFind<ModItem>("CraftingAccess", out ModItem craftingAccess))
                {
                    itemLoot.Add(ItemDropRule.Common(craftingAccess.Type));
                }
                if (magicStorage.TryFind<ModItem>("StorageHeart", out ModItem storageHeart))
                {
                    itemLoot.Add(ItemDropRule.Common(storageHeart.Type));
                }
                if (magicStorage.TryFind<ModItem>("StorageUnit", out ModItem storageUnit))
                {
                    itemLoot.Add(ItemDropRule.Common(storageUnit.Type, 1, 10, 10));
                }
            }
            if (ModLoader.TryGetMod("ImproveGame", out Mod improveGame))
            {
                string[] names = new string[] { "MagickWand", "SpaceWand", "CreateWand", "PotionBag", "BannerChest" };
                for (int i = 0; i < names.Length; i++)
                {
                    if (improveGame.TryFind<ModItem>(names[i], out ModItem tool))
                    {
                        itemLoot.Add(ItemDropRule.Common(tool.Type));
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
                int count = 1;
                return gfsb.OwnAble(info.player, ref count);
            }
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }
    }
}
