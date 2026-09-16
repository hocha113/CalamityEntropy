using InnoVault.Storages;
using InnoVault.TileProcessors;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace CalamityEntropy.Content.AzafureMiners
{
    /// <summary>
    /// 挖矿机存储提供者工厂
    /// </summary>
    internal class AzMinerStorageProviderFactory : IStorageProviderFactory
    {
        public string Identifier => "CE.AzMiner";
        public int Priority => 5;
        public bool IsAvailable => true;

        public IEnumerable<IStorageProvider> FindStorageProviders(Point16 position, int range, Item item) {
            var provider = AzMinerStorageProvider.FindNearPosition(position, range, item);
            if (provider != null) {
                yield return provider;
            }
        }

        public IStorageProvider GetStorageProviders(Point16 position, Item item) {
            return AzMinerStorageProvider.GetAtPosition(position, item);
        }
    }

    /// <summary>
    /// 挖矿机的存储提供者实现
    /// </summary>
    internal class AzMinerStorageProvider : IStorageProvider
    {
        private readonly AzMinerTP _azMinerTP;
        private readonly Point16 _position;

        public string Identifier => "CE.AzMiner";
        public Point16 Position => _position;
        public Vector2 WorldCenter => _azMinerTP?.CenterInWorld ?? _position.ToWorldCoordinates();
        public Rectangle HitBox => _azMinerTP?.HitBox ?? new Rectangle(_position.X * 16, _position.Y * 16, 64, 48);

        public bool IsValid {
            get {
                if (_azMinerTP == null) {
                    return false;
                }
                return TileProcessorLoader.AutoPositionGetTP(_position, out AzMinerTP tp) && tp == _azMinerTP;
            }
        }

        public bool HasSpace {
            get {
                if (!IsValid) {
                    return false;
                }
                //检查是否有空槽位
                foreach (var item in _azMinerTP.items) {
                    if (item == null || item.IsAir) {
                        return true;
                    }
                }
                //检查是否有可堆叠空间
                foreach (var item in _azMinerTP.items) {
                    if (item != null && !item.IsAir && item.stack < item.maxStack) {
                        return true;
                    }
                }
                return false;
            }
        }

        public AzMinerStorageProvider(AzMinerTP azMinerTP) {
            _azMinerTP = azMinerTP;
            _position = azMinerTP?.Position ?? Point16.NegativeOne;
        }

        /// <summary>
        /// 从位置查找AzMinerTP并创建存储提供者
        /// </summary>
        public static AzMinerStorageProvider FromPosition(Point16 position) {
            if (!TileProcessorLoader.AutoPositionGetTP(position, out AzMinerTP tp)) {
                return null;
            }
            return new AzMinerStorageProvider(tp);
        }

        /// <summary>
        /// 查找范围内最近的AzMinerTP存储提供者
        /// </summary>
        public static AzMinerStorageProvider FindNearPosition(Point16 position, int range, Item item) {
            //使用TileProcessorLoader提供的高效范围搜索方法
            var tp = TileProcessorLoader.FindModuleRangeSearch<AzMinerTP>(position, range);
            if (tp == null) {
                return null;
            }
            var provider = new AzMinerStorageProvider(tp);
            if (item != null && !item.IsAir && !provider.CanAcceptItem(item)) {
                return null;
            }
            return provider;
        }

        /// <summary>
        /// 获取指定位置的AzMinerTP存储提供者
        /// </summary>
        public static AzMinerStorageProvider GetAtPosition(Point16 position, Item item) {
            if (!TileProcessorLoader.AutoPositionGetTP(position, out AzMinerTP tp)) {
                return null;
            }
            var provider = new AzMinerStorageProvider(tp);
            if (item != null && !item.IsAir && !provider.CanAcceptItem(item)) {
                return null;
            }
            return provider;
        }

        public bool CanAcceptItem(Item item) {
            if (!IsValid || item == null || item.IsAir) {
                return false;
            }
            return HasSpace;
        }

        public bool DepositItem(Item item) {
            if (!CanAcceptItem(item)) {
                return false;
            }

            //尝试堆叠到现有物品
            foreach (var stored in _azMinerTP.items) {
                if (stored.type == item.type && stored.stack < stored.maxStack) {
                    int addAmount = Math.Min(item.stack, stored.maxStack - stored.stack);
                    stored.stack += addAmount;
                    item.stack -= addAmount;
                    if (item.stack <= 0) {
                        item.TurnToAir();
                        _azMinerTP.SendData();
                        return true;
                    }
                }
            }

            //添加新物品到空槽位
            for (int i = 0; i < _azMinerTP.items.Count; i++) {
                if (_azMinerTP.items[i] == null || _azMinerTP.items[i].IsAir) {
                    _azMinerTP.items[i] = item.Clone();
                    item.TurnToAir();
                    _azMinerTP.SendData();
                    return true;
                }
            }

            return false;
        }

        public Item WithdrawItem(int itemType, int count) {
            if (!IsValid || count <= 0) {
                return new Item();
            }

            int remaining = count;
            Item result = new Item(itemType, 0);

            for (int i = _azMinerTP.items.Count - 1; i >= 0 && remaining > 0; i--) {
                Item slotItem = _azMinerTP.items[i];
                if (slotItem == null || slotItem.IsAir || slotItem.type != itemType) {
                    continue;
                }

                int take = Math.Min(remaining, slotItem.stack);
                slotItem.stack -= take;
                result.stack += take;
                remaining -= take;

                if (slotItem.stack <= 0) {
                    _azMinerTP.items[i] = new Item();
                }
            }

            if (result.stack > 0) {
                result.type = itemType;
                _azMinerTP.SendData();
            }

            return result;
        }

        public IEnumerable<Item> GetStoredItems() {
            if (!IsValid) {
                yield break;
            }
            foreach (var item in _azMinerTP.items) {
                if (item != null && !item.IsAir) {
                    yield return item;
                }
            }
        }

        public long GetItemCount(int itemType) {
            if (!IsValid) {
                return 0;
            }
            long count = 0;
            foreach (var item in _azMinerTP.items) {
                if (item != null && !item.IsAir && item.type == itemType) {
                    count += item.stack;
                }
            }
            return count;
        }
    }
}
