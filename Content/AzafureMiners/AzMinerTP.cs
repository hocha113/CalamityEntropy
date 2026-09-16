using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using InnoVault.TileProcessors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.AzafureMiners
{
    public class AzMinerTP : TileProcessor
    {
        public override int TargetTileID => ModContent.TileType<AzafureMinerTile>();
        public List<Item> filters;
        public List<Item> items;
        public static int FiltersCount = 6;
        public static int ItemsCount = 35;
        public bool IsWork;
        public Vector2 OffsetPos;
        private Vector2 currentOffset = Vector2.Zero;
        private Vector2 targetOffset = Vector2.Zero;
        private Vector2 targetOffset2 = Vector2.Zero;
        public static readonly Dictionary<int, bool> ItemIsOre = [];
        public static bool init = true;

        public bool SmartCursorHover = false;
        public static void SetUpList() {
            try {
                List<int> oreTileIDs = [];
                for (int i = 0; i < TileLoader.TileCount; i++) {
                    if (!TileID.Sets.Ore[i]) {
                        continue;
                    }
                    oreTileIDs.Add(i);
                }

                for (int i = 0; i < ItemLoader.ItemCount; i++) {
                    Item item = new Item(i);
                    if (item.type == ItemID.None) {
                        continue;
                    }

                    ItemIsOre.Add(i, oreTileIDs.Contains(item.createTile) || EGlobalItem.GemItemIDToTileIDMap.ContainsKey(i));
                }
            } catch (Exception ex) {
                CalamityEntropy.Instance.Logger.Error($"AzMinerTP.SetStaticDefaults: An Error Has Occurred {ex.Message}");
            }
        }
        public override void SetProperty() {
            IsWork = true;
            filters = [];
            items = [];
            for (int i = 0; i < FiltersCount; i++) {
                filters.Add(new Item());
            }
            for (int i = 0; i < ItemsCount; i++) {
                items.Add(new Item());
            }
        }

        public override void LoadData(TagCompound tag) {
            filters = [];
            items = [];
            if (tag.ContainsKey("items")) {
                TagCompound isave = tag.Get<TagCompound>("items");
                for (int i = 0; i < FiltersCount; i++) {
                    filters.Add(ItemIO.Load(isave.Get<TagCompound>("f" + i.ToString())));
                }
                for (int i = 0; i < ItemsCount; i++) {
                    items.Add(ItemIO.Load(isave.Get<TagCompound>("i" + i.ToString())));
                }
            }
            else {
                for (int i = 0; i < FiltersCount; i++) {
                    filters.Add(new Item());
                }
                for (int i = 0; i < ItemsCount; i++) {
                    items.Add(new Item());
                }
            }
        }

        public override void SaveData(TagCompound tag) {
            TagCompound itemSaves = [];
            int c = 0;
            foreach (Item item in filters) {
                itemSaves.Add("f" + c.ToString(), ItemIO.Save(item));
                c++;
            }
            c = 0;
            foreach (Item item in items) {
                itemSaves.Add("i" + c.ToString(), ItemIO.Save(item));
                c++;
            }
            tag.Add("items", itemSaves);
        }

        public override void OnKill() {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            foreach (var item in filters) {
                if (!item.IsAir) {
                    VaultUtils.SpwanItem(this.FromObjectGetParent(), HitBox, item.Clone());
                }
            }
            foreach (var item in items) {
                if (!item.IsAir) {
                    VaultUtils.SpwanItem(this.FromObjectGetParent(), HitBox, item.Clone());
                }
            }
        }

        public override void Update() {
            if (init) {
                init = false;
                SetUpList();
            }
            if (IsWork) {
                //随机目标抖动点，范围 ±1.5
                targetOffset = targetOffset2 + new Vector2(
                    Main.rand.NextFloat(-8f, 8f),
                    Main.rand.NextFloat(-4f, 10f)
                );

                //让 currentOffset 缓慢向 targetOffset 逼近，产生平滑抖动效果
                currentOffset = Vector2.Lerp(currentOffset, targetOffset, 0.1f);
            }
            else {
                targetOffset2 = Vector2.Zero;
                //非工作时平滑回到0
                currentOffset = Vector2.Lerp(currentOffset, Vector2.Zero, 0.1f);
            }

            targetOffset2 *= 0.6f;
            OffsetPos = new Vector2((int)currentOffset.X, (int)currentOffset.Y);

            List<int> types = [];
            IsWork = false;
            foreach (Item item in filters) {
                if (item.type == ItemID.None) {
                    continue;
                }
                types.Add(item.type);
                IsWork = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }

            bool didMine = false;

            for (int i = 0; i < 3; i++) {
                Point16 p = new Point16(Main.rand.Next(Main.maxTilesX), Main.rand.Next(Main.maxTilesY));

                Tile t = Main.tile[p.X, p.Y];
                if (!t.HasTile) {
                    continue;
                }

                int itemtype = t.GetTileDrop(p.X, p.Y);

                //if (!ItemIsOre.ContainsKey(itemtype))
                //{
                //   continue;//这个判定目前是不需要的，因为types里面的肯定都是矿物物品
                //}
                if (!types.Contains(itemtype)) {
                    continue;
                }

                for (int x = -14; x < 15; x++) {
                    for (int y = -14; y < 15; y++) {
                        if (!CEUtils.inWorld(p.X + x, p.Y + y)) {
                            continue;
                        }

                        if (!ItemIsOre.ContainsKey(Main.tile[p.X + x, p.Y + y].GetTileDrop(p.X + x, p.Y + y))) {
                            continue;
                        }

                        if (!MineOre(p.X + x, p.Y + y)) {
                            continue;
                        }
                        didMine = true;
                    }
                }
            }

            if (!didMine) {
                return;
            }

            SendData();
        }

        public bool MineOre(int x, int y) {
            Tile tile = Main.tile[x, y];
            int itemType = tile.GetTileDrop(x, y);
            if (itemType <= 0) {
                return false;
            }

            if (EGlobalItem.GemTileIDToItemIDMap.TryGetValue(tile.TileType, out int dropID)) {
                itemType = dropID;
            }

            //先检查矿石是否在过滤器里
            bool matchesFilter = filters.Any(f => f.type == itemType);
            if (!matchesFilter) {
                return false;
            }

            //先尝试堆叠已有物品
            foreach (var slot in items) {
                if (slot.type != itemType || slot.stack >= slot.maxStack) {
                    continue;
                }
                slot.stack++;
                PerformMineEffects(x, y);
                return true;
            }

            //再尝试放到空格
            foreach (var slot in items) {
                if (!slot.IsAir) {
                    continue;
                }
                slot.SetDefaults(itemType);
                PerformMineEffects(x, y);
                return true;
            }

            return false;
        }

        ///<summary>
        ///执行采矿后通用的视觉/音效/网络同步
        ///</summary>
        private void PerformMineEffects(int x, int y) {
            Main.tile[x, y].ClearTile();
            if (Main.dedServ) {
                NetMessage.SendTileSquare(-1, x, y);
            }


            if (!Main.rand.NextBool(12)) {
                return;
            }
            //粒子效果
            PRTLoader.NewParticle<PRT_EMediumSmoke>(
                CenterInWorld + new Vector2(Main.rand.NextFloat(-24, 24), 0),
                new Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-2, -6)),
                Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()),
                Main.rand.NextFloat(0.8f, 1.4f))
                .Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());
            //淫叫
            SoundEngine.PlaySound(SoundID.Tink with { PitchRange = (-0.1f, 2f) }, CenterInWorld);

            if (targetOffset2.Length() > 1f) {
                return;
            }
            //蹦跶一下
            targetOffset2 += new Vector2(0, -116);
        }
        public override void SendData(ModPacket data) {
            data.Write(IsWork);
            for (int i = 0; i < FiltersCount; i++) {
                Item item = filters[i];
                data.Write(item.type);
                data.Write7BitEncodedInt(item.netID);
                data.Write7BitEncodedInt(item.stack);
            }
            for (int i = 0; i < ItemsCount; i++) {
                Item item = items[i];
                data.Write(item.type);
                data.Write7BitEncodedInt(item.netID);
                data.Write7BitEncodedInt(item.stack);
            }
        }

        public override void ReceiveData(BinaryReader reader, int whoAmI) {
            IsWork = reader.ReadBoolean();
            for (int i = 0; i < FiltersCount; i++) {
                Item item = new Item(reader.ReadInt32());
                item.netDefaults(reader.Read7BitEncodedInt());
                item.stack = reader.Read7BitEncodedInt();
                filters[i] = item;
            }
            for (int i = 0; i < ItemsCount; i++) {
                Item item = new Item(reader.ReadInt32());
                item.netDefaults(reader.Read7BitEncodedInt());
                item.stack = reader.Read7BitEncodedInt();
                items[i] = item;
            }
        }
    }
}
