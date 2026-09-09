using CalamityEntropy.Content.ArmorPrefixes;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.Cards;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Donator.RocketLauncher;
using CalamityEntropy.Content.Items.PrefixItem;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.CalamityRef;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class ExtraLoot : ModSystem
    {
        public override void PostWorldGen()
        {
            List<int> ancientPrefixItem = new List<int>();
            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                var ins = ItemLoader.GetItem(i);
                if (ins != null && ins is AncientPrefixItem)
                {
                    ancientPrefixItem.Add(i);
                }
            }
            if (ArmorPrefix.Enabled)
            {
                for (int i = 0; i < 3; i++)
                {
                    int bc = 0;
                    while (bc++ < 4096)
                    {
                        int px = Main.rand.Next(Main.tile.Width);
                        int py = Main.rand.Next(Main.tile.Height);
                        if (Main.tile[px, py].HasTile && Main.tileBrick[Main.tile[px, py].TileType])
                        {
                            Main.tile[px, py].ResetToType((ushort)ModContent.TileType<TheHeatDeath>());
                            break;
                        }
                    }
                }
            }
            int itemsPlaced = 0;
            for (int chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
            {
                Chest chest = Main.chest[chestIndex];
                if (chest == null)
                {
                    continue;
                }
                Tile chestTile = Main.tile[chest.x, chest.y];

                if (ArmorPrefix.Enabled)
                {
                    if (WorldGen.genRand.NextBool(20))
                    {
                        for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                        {
                            if (chest.item[inventoryIndex].type == ItemID.None)
                            {
                                chest.item[inventoryIndex].SetDefaults(ancientPrefixItem[WorldGen.genRand.Next(ancientPrefixItem.Count)]);
                                itemsPlaced++;
                                break;
                            }
                        }
                    }
                }
                if (chestTile.TileType == TileID.Containers)
                {
                    if (chestTile.TileFrameX == 1 * 36)
                    {
                        if (WorldGen.genRand.NextBool(10))
                        {
                            for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                            {
                                if (chest.item[inventoryIndex].type == ItemID.None)
                                {
                                    chest.item[inventoryIndex].SetDefaults(ModContent.ItemType<AuraCard>());
                                    itemsPlaced++;
                                    break;
                                }
                            }
                        }
                    }
                    if (chestTile.TileFrameX == 13 * 36)
                    {
                        if (WorldGen.genRand.NextBool(2))
                        {
                            for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                            {
                                if (chest.item[inventoryIndex].type == ItemID.None)
                                {
                                    chest.item[inventoryIndex].SetDefaults(ModContent.ItemType<IndigoCard>());
                                    itemsPlaced++;
                                    break;
                                }
                            }
                        }
                    }
                    // 原灾厄热泉宝箱注入的 WispLantern，按表外裁定改挂原版水中宝箱 1/4（bookmark-rehang §六）
                    if (chestTile.TileFrameX == 17 * 36)
                    {
                        if (WorldGen.genRand.NextBool(4))
                        {
                            for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                            {
                                if (chest.item[inventoryIndex].type == ItemID.None)
                                {
                                    chest.item[inventoryIndex].SetDefaults(ModContent.ItemType<WispLantern>());
                                    itemsPlaced++;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (chestTile.TileType == TileID.Containers2)
                {
                    if (chestTile.TileFrameX == 2 * 36 || chestTile.TileFrameX == 3 * 36)
                    {
                        for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                        {

                            if (chest.item[inventoryIndex].type == ItemID.None)
                            {

                                chest.item[inventoryIndex].SetDefaults(ModContent.ItemType<OsseousRemains>());
                                chest.item[inventoryIndex].stack = WorldGen.genRand.Next(46, 64);
                                itemsPlaced++;
                                break;
                            }
                        }
                    }
                    if (chestTile.TileFrameX == 10 * 36)
                    {
                        if (WorldGen.genRand.NextBool(3))
                        {
                            for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                            {

                                if (chest.item[inventoryIndex].type == ItemID.None)
                                {

                                    chest.item[inventoryIndex].SetDefaults(ModContent.ItemType<InspirationCard>());
                                    itemsPlaced++;
                                    break;
                                }
                            }
                        }
                    }
                }
                //装灾厄时补回深渊宝箱三段覆写;水中宝箱 WispLantern 1/4 保留(无灾厄唯一来源)
                if (CERef.Has && CEID.Tile_AbyssTreasureChest > 0 && chestTile.TileType == CEID.Tile_AbyssTreasureChest)
                {
                    if (WorldGen.genRand.NextBool(3))
                    {
                        for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                        {
                            if (chest.item[inventoryIndex].type == ItemID.None)
                            {
                                int type = ModContent.ItemType<WispLantern>();
                                if (WorldGen.genRand.NextBool(4))
                                {
                                    type = ModContent.ItemType<AbyssLantern>();
                                }
                                if (WorldGen.genRand.NextBool(2))
                                {
                                    type = ModContent.ItemType<EnduranceCard>();
                                }
                                chest.item[inventoryIndex].SetDefaults(type);
                                itemsPlaced++;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
