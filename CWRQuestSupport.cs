using CalamityEntropy.Common;
using CalamityEntropy.Content.AzafureMiners;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.Cards;
using CalamityEntropy.Content.Items.Accessories.EvilCards;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Armor.Marivinium;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.Items.PrefixItem;
using CalamityEntropy.Content.NPCs;
using CalamityEntropy.Core.Weapons;
using InnoVault.TileProcessors;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy
{
    /// <summary>
    /// 向 CalamityOverhaul 的任务书图谱注入本模组节点。纯弱引用，只走 Mod.Call 字符串命令，
    /// 没装 CWR 或 CWR 版本不带这套接口时整段静默跳过。
    /// <para>四条来自 CWR 源码的硬约束，改这个文件前先读一遍：</para>
    /// <list type="number">
    /// <item>父节点缺席等于永久锁死：CWR 的 CheckUnlock 把 parent == null 当作未完成，
    /// 而它的灾厄系 Boss 节点全是条件加载。所以只准挂本文件里列出的原版保底节点。</item>
    /// <item>未解锁的节点不推进度：读瞬时状态的节点必须尽早解锁，不要再加第二父节点。</item>
    /// <item>坐标相对首个父节点，其余父节点只参与解锁；多父节点要求全部完成。</item>
    /// <item>谓词每帧跑且抛三次异常就被 CWR 停用：扫背包一律走 <see cref="Scan"/> 的节流缓存。</item>
    /// </list>
    /// </summary>
    internal static class CWRQuestSupport
    {
        private const string CWRName = "CalamityOverhaul";
        private const string CmdSupports = "QuestLogs.SupportsExternal";
        private const string CmdRegister = "QuestLogs.RegisterNode";

        //CWR 要求外模节点 ID 带模组名前缀，否则会和它自有的 178 个类名节点抢位
        private const string IDPrefix = "CalamityEntropy.";

        //原版保底父节点：这些是 CWR 里无条件加载的节点，装不装灾厄都在
        private const string PFirst = "FirstQuest";
        private const string PEyeOfCthulhu = "EyeofCthulhuQuest";
        private const string PSkeletron = "SkeletronQuest";
        private const string PWallOfFlesh = "WallofFleshQuest";
        private const string PPlantera = "PlanteraQuest";
        private const string PMoonLord = "MoonLordQuest";

        //讨伐线统一的竖向落差。x 方向 +75 是为了让连线避开 CWR 挂在同一 x 上的事件带与猎杀带节点中心
        private const float LaneDropY = 750f;
        private const float LaneShiftX = 75f;

        private static Mod cwr;

        private static int[] zodiacTypes;
        private static int[] oracleCardTypes;
        private static int[] mariviniumTypes;
        private static int[] finalArmTypes;

        internal static void Register() {
            if (!ModLoader.TryGetMod(CWRName, out cwr)) {
                return;
            }

            bool supported = false;
            try {
                supported = cwr.Call(CmdSupports) is true;
            } catch {
                supported = false;
            }
            if (!supported) {
                //装了 CWR 但版本不带这套对外接口
                cwr = null;
                return;
            }

            try {
                CacheItemTypes();
                RegisterBossLane();
                RegisterChronicle();
                RegisterEndgame();
            } catch (Exception ex) {
                CalamityEntropy.Instance.Logger.Info($"CalamityEntropy: CWR quest registration aborted: {ex.Message}");
            } finally {
                //注册只用这一次，不要整局捏着对方的 Mod 引用
                cwr = null;
            }
        }

        /// <summary>热重载时清掉缓存，避免下一局谓词踩到已卸掉的字典</summary>
        internal static void Unload() {
            cwr = null;
            zodiacTypes = null;
            oracleCardTypes = null;
            mariviniumTypes = null;
            finalArmTypes = null;
            minerFound = false;
            minerScanned = false;
        }

        private static void CacheItemTypes() {
            zodiacTypes = new int[]
            {
                ModContent.ItemType<BookMarkAries>(),
                ModContent.ItemType<BookMarkTaurus>(),
                ModContent.ItemType<BookMarkGemini>(),
                ModContent.ItemType<BookMarkCancer>(),
                ModContent.ItemType<BookMarkLeo>(),
                ModContent.ItemType<BookMarkVirgo>(),
                ModContent.ItemType<BookMarkLibra>(),
                ModContent.ItemType<BookMarkScorpio>(),
                ModContent.ItemType<BookMarkSagittarius>(),
                ModContent.ItemType<BookMarkCapricorn>(),
                ModContent.ItemType<BookMarkAquarius>(),
                ModContent.ItemType<BookMarkPisces>()
            };
            oracleCardTypes = new int[]
            {
                ModContent.ItemType<AuraCard>(),
                ModContent.ItemType<BrillianceCard>(),
                ModContent.ItemType<EntityCard>(),
                ModContent.ItemType<InspirationCard>(),
                ModContent.ItemType<MetropolisCard>(),
                ModContent.ItemType<RadianceCard>(),
                ModContent.ItemType<TemperanceCard>(),
                ModContent.ItemType<WisdomCard>(),
                ModContent.ItemType<EnduranceCard>()
            };
            mariviniumTypes = new int[]
            {
                ModContent.ItemType<MariviniumHelmet>(),
                ModContent.ItemType<MariviniumBodyArmor>(),
                ModContent.ItemType<MariviniumLeggings>()
            };
            finalArmTypes = new int[]
            {
                ModContent.ItemType<Content.Items.Weapons.Fractal.FinalFractal>(),
                ModContent.ItemType<Content.Items.Weapons.Swirlblades.Apeirokyklos>(),
                ModContent.ItemType<Content.Items.Weapons.VoidRelics>()
            };
        }

        #region 讨伐线

        /// <summary>
        /// 六个自有 Boss 铺在原版主脊下方同一行，各自向上连到对应的原版 Boss 节点，
        /// 彼此再横向连成本模组的讨伐链。全组计入完典，奖励只给纪念章与阶段消耗品
        /// </summary>
        private static void RegisterBossLane() {
            new Node("AcropolisMachine")
                .Parents(PEyeOfCthulhu)
                .At(LaneShiftX, LaneDropY)
                .Kind("Main", "Normal")
                .Icon("CalamityEntropy/Content/NPCs/Acropolis/AcropolisMachine_Head_Boss")
                .Chapter(60)
                .Completionist()
                .Done(_ => EDownedBosses.downedAcropolis)
                .Reward(ModContent.ItemType<AcropolisTrophy>())
                .Reward(ItemID.GoldCoin, 5)
                .Reward(ItemID.HealingPotion, 10)
                .Push();

            new Node("Apsychos")
                .Parents(PSkeletron, Full("AcropolisMachine"))
                .At(LaneShiftX, LaneDropY)
                .Kind("Main", "Normal")
                .Icon("CalamityEntropy/Content/NPCs/Apsychos/Apsychos_Head_Boss")
                .Completionist()
                .Done(_ => EDownedBosses.downedApsychos)
                .Reward(ModContent.ItemType<ApsychosTrophy>())
                .Reward(ItemID.GoldCoin, 8)
                .Reward(ItemID.HealingPotion, 15)
                .Push();

            new Node("Luminaris")
                .Parents(PWallOfFlesh, Full("Apsychos"))
                .At(LaneShiftX, LaneDropY)
                .Kind("Main", "Hard")
                .Icon("CalamityEntropy/Content/NPCs/LuminarisMoth/Luminaris_Head_Boss")
                .Completionist()
                //progression-map 的阶梯是「任一机械后」，比 CWR 那个要求三王全倒的节点松
                .Unlock(_ => NPC.downedMechBossAny)
                .Done(_ => EDownedBosses.downedLuminaris)
                .Reward(ModContent.ItemType<LuminarisTrophy>())
                .Reward(ItemID.GoldCoin, 12)
                .Reward(ItemID.GreaterHealingPotion, 15)
                .Push();

            new Node("TheProphet")
                .Parents(PPlantera, Full("Luminaris"))
                .At(LaneShiftX, LaneDropY)
                .Kind("Main", "Hard")
                .Icon("CalamityEntropy/Content/NPCs/Prophet/TheProphet_Head_Boss")
                .Completionist()
                .Done(_ => EDownedBosses.downedProphet)
                .Reward(ModContent.ItemType<ProphetTrophy>())
                .Reward(ItemID.GoldCoin, 15)
                .Reward(ItemID.SuperHealingPotion, 10)
                .Push();

            new Node("NihilityTwin")
                .Parents(PMoonLord, Full("TheProphet"))
                .At(LaneShiftX, LaneDropY)
                .Kind("Main", "Expert")
                .Icon("CalamityEntropy/Content/NPCs/NihilityTwin/NihilityActeriophage_Head_Boss")
                .Completionist()
                .Done(_ => EDownedBosses.downedNihilityTwin)
                .Reward(ModContent.ItemType<NihilityTwinTrophy>())
                .Reward(ItemID.PlatinumCoin, 1)
                .Reward(ModContent.ItemType<VoidHealingPotion>(), 15)
                .Push();

            new Node("Cruiser")
                .Parents(PMoonLord, Full("NihilityTwin"))
                .At(LaneShiftX + 150f, LaneDropY)
                .Kind("Main", "Master")
                .Icon("CalamityEntropy/Content/NPCs/Cruiser/CruiserHead_Head_Boss")
                .Completionist()
                .Done(_ => EDownedBosses.downedCruiser)
                .Reward(ModContent.ItemType<CruiserTrophy>())
                .Reward(ItemID.PlatinumCoin, 2)
                .Reward(ModContent.ItemType<VoidHealingPotion>(), 20)
                .Push();
        }

        #endregion

        #region 熵语秘录

        /// <summary>
        /// 秘录枢纽自己就是本模组的入门任务，下挂书页、卡组、器物三条支线。
        /// 器物支线里蓄势与抽奖是瞬时判定，只挂枢纽，不许再加原版第二父节点
        /// </summary>
        private static void RegisterChronicle() {
            new Node("Chronicle")
                .Parents(PFirst)
                .At(-150f, 1200f)
                .Kind("Side", "Easy")
                .IconItem(ModContent.ItemType<AncientScriptures>())
                .Chapter(61)
                .Done(p => Scan.Of(p).HasBook)
                .Reward(ModContent.ItemType<MagicBookmarkHolder>())
                .Reward(ItemID.GoldCoin, 3)
                .Push();

            //书页支线，链式推进
            new Node("BookmarkFirst")
                .Parents(Full("Chronicle"))
                .At(150f, 0f)
                .Kind("Side", "Easy")
                .IconItem(ModContent.ItemType<BookMarkAries>())
                .Done(p => LoadedBookmarkCount(p) > 0)
                .Reward(ItemID.GoldCoin, 5)
                .Reward(ItemID.ManaRegenerationPotion, 15)
                .Push();

            new Node("BookmarkLoadout")
                .Parents(Full("BookmarkFirst"))
                .At(150f, 0f)
                .Kind("Side", "Normal")
                .IconItem(ModContent.ItemType<ExquisiteBookmarkHolder>())
                //定死 3 枚而不是读满槽数：槽数随书与饰品浮动，而 progressMax 注册后就固定了
                .Progress(p => LoadedBookmarkCount(p) / 3f, 3)
                .Reward(ModContent.ItemType<ExquisiteBookmarkHolder>())
                .Reward(ItemID.GoldCoin, 8)
                .Push();

            new Node("Zodiac")
                .Parents(Full("BookmarkLoadout"))
                .At(150f, 0f)
                .Kind("Achievement", "Expert")
                .IconItem(ModContent.ItemType<BookMarkCapricorn>())
                .Progress(p => Scan.Of(p).Zodiac / 12f, 12)
                .Reward(ModContent.ItemType<HolyBookmarkHolder>())
                .Reward(ItemID.PlatinumCoin, 1)
                .Push();

            new Node("CodexShelf")
                .Parents(Full("BookmarkLoadout"))
                .At(300f, 0f)
                .Kind("Side", "Hard")
                .IconItem(ModContent.ItemType<AncientScriptures>())
                .Progress(p => Scan.Of(p).BookKinds / 5f, 5)
                .Reward(ModContent.ItemType<ForeseeOrb>())
                .Push();

            //卡组支线。神谕卡组的配方要吃掉全部九张卡，所以收集在前、成组在后
            new Node("OracleComplete")
                .Parents(Full("Chronicle"))
                .At(150f, 150f)
                .Kind("Achievement", "Expert")
                .IconItem(ModContent.ItemType<AuraCard>())
                //持有卡组即视为集齐：配方会把九张卡一并消耗，不然抢在扫描间隔里合成的人会永久卡住
                .Progress(p => p.Entropy().oracleDeckInInv ? 1f : Scan.Of(p).OracleCards / 9f, 9)
                .Reward(ModContent.ItemType<ThreadOfFate>())
                .Reward(ItemID.PlatinumCoin, 1)
                .Push();

            new Node("OracleDeck")
                .Parents(Full("OracleComplete"))
                .At(150f, 0f)
                .Kind("Side", "Normal")
                .IconItem(ModContent.ItemType<Content.Items.Accessories.Cards.OracleDeck>())
                .Done(p => p.Entropy().oracleDeckInInv)
                .Reward(ModContent.ItemType<BrillianceCard>())
                .Reward(ItemID.GoldCoin, 10)
                .Push();

            new Node("ShadowDecks")
                .Parents(Full("OracleDeck"))
                .At(150f, 0f)
                .Kind("Side", "Hard")
                .IconItem(ModContent.ItemType<TaintedDeck>())
                .Progress(p => DeckCount(p) / 2f, 2)
                .Reward(ModContent.ItemType<GreedCard>())
                .Reward(ModContent.ItemType<BitternessCard>())
                .Push();

            //器物支线，四个节点都直接挂枢纽
            new Node("ArmorPrefix")
                .Parents(Full("Chronicle"))
                .At(150f, 300f)
                .Kind("Side", "Normal")
                .IconItem(ModContent.ItemType<RuneStoneHard>())
                //整套前缀系统可以在配置里关掉，关掉时配方都不注册，任务就该保持未解锁
                .Unlock(_ => ServerConfig.Instance?.EnableArmorPrefix == true)
                .Done(p => Scan.Of(p).ArmorPrefixed)
                .Reward(ModContent.ItemType<RuneStoneHard>())
                .Reward(ItemID.GoldCoin, 10)
                .Push();

            new Node("ChargeReady")
                .Parents(Full("Chronicle"))
                .At(300f, 300f)
                .Kind("Side", "Easy")
                .IconItem(ModContent.ItemType<Content.Items.Weapons.VoidRelics>())
                //瞬时判定：只在手持就绪武器的那一刻为真，所以难度压到最低、不加任何额外门槛
                .Done(p => p?.HeldItem != null && CEChargeWeapon.IsReady(p.HeldItem))
                .Reward(ItemID.GoldCoin, 5)
                .Reward(ItemID.GreaterHealingPotion, 10)
                .Push();

            new Node("AzafureMiner")
                .Parents(Full("Chronicle"))
                .At(450f, 300f)
                .Kind("Side", "Hard")
                .IconItem(ModContent.ItemType<AzafureMiner>())
                .Done(_ => MinerPlaced())
                .Reward(ModContent.ItemType<AzafureDriverCore>())
                .Push();

            new Node("Lottery")
                .Parents(Full("Chronicle"))
                .At(600f, 300f)
                .Kind("Side", "Normal")
                .IconItem(ModContent.ItemType<LotteryBox>())
                .Done(_ => NPC.AnyNPCs(ModContent.NPCType<LotteryMachine>()))
                .Reward(ModContent.ItemType<LotteryBox>(), 3)
                .Push();
        }

        #endregion

        #region 终末与成就

        /// <summary>挂在讨伐线尾部的收集与成就节点，含全套里唯一的隐藏节点</summary>
        private static void RegisterEndgame() {
            new Node("SoulEssence")
                .Parents(Full("NihilityTwin"))
                .At(0f, 150f)
                .Kind("Side", "Expert")
                .IconItem(ModContent.ItemType<WraithSoulEssence>())
                .Progress(p => Scan.Of(p).SoulEssence / 30f, 30)
                .Reward(ModContent.ItemType<NihilityShell>())
                .Push();

            new Node("WyrmGuest")
                .Parents(Full("Cruiser"))
                .At(150f, 0f)
                .Kind("Side", "Hard")
                .IconItem(ModContent.ItemType<CruiserPlush>())
                .Done(_ => NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmNPC>()))
                .Reward(ModContent.ItemType<CruiserPlush>())
                .Reward(ItemID.PlatinumCoin, 1)
                .Push();

            new Node("MariviniumSet")
                .Parents(Full("Cruiser"))
                .At(0f, 150f)
                .Kind("Side", "Master")
                .IconItem(ModContent.ItemType<MariviniumHelmet>())
                .Progress(p => Scan.Of(p).MariviniumPieces / 3f, 3)
                .Reward(ModContent.ItemType<WyrmToothNecklace>())
                .Reward(ItemID.PlatinumCoin, 2)
                .Push();

            new Node("VoidAltar")
                .Parents(Full("Cruiser"))
                .At(150f, 150f)
                .Kind("Side", "Expert")
                .IconItem(ModContent.ItemType<AbyssalAltar>())
                .Done(p => Scan.Of(p).HasAltar)
                .Reward(ModContent.ItemType<WyrmTooth>(), 20)
                .Push();

            new Node("FinalArms")
                .Parents(Full("Cruiser"))
                .At(0f, 300f)
                .Kind("Achievement", "Master")
                .IconItem(ModContent.ItemType<Content.Items.Weapons.Fractal.FinalFractal>())
                .Progress(p => Scan.Of(p).FinalArms / 3f, 3)
                .Reward(ModContent.ItemType<Godhead>())
                .Reward(ItemID.PlatinumCoin, 5)
                .Push();

            new Node("EntropyModeRun")
                //挂虚无双子而不是巡游者：隐藏节点要等全部父节点完成才现身，
                //挂巡游者就只能在击杀之后才冒出来并立刻自我完成，玩家根本没机会把它当目标看
                .Parents(Full("NihilityTwin"))
                .At(300f, 300f)
                .Kind("Achievement", "Master")
                .IconItem(ModContent.ItemType<EntropyModeToggle>())
                .Hidden()
                //只在开了熵变的世界里现身
                .Unlock(_ => EDownedBosses.EntropyMode)
                .Done(_ => EDownedBosses.EntropyMode && EDownedBosses.downedCruiser)
                .Reward(ModContent.ItemType<CruiserRelic>())
                .Reward(ItemID.PlatinumCoin, 5)
                .Push();
        }

        #endregion

        #region 判定

        private static string Full(string key) => IDPrefix + key;

        /// <summary>
        /// 不走 <see cref="BookMarkLoader.IsABookMark"/>：那边会直接 ContainsKey，
        /// 热重载时 CustomBMByID 已被置空，谓词抛三次就会被 CWR 停用
        /// </summary>
        private static bool IsBookmarkSafe(Item item) {
            if (item.ModItem is BookMark) {
                return true;
            }
            Dictionary<int, BookMarkLoader.BookMarkTag> map = BookMarkLoader.CustomBMByID;
            return map != null && map.ContainsKey(item.type);
        }

        /// <summary>熵语之书里当前装着几枚书签。槽位表很短，不必进节流缓存</summary>
        private static int LoadedBookmarkCount(Player player) {
            List<Item> slots = player?.Entropy()?.EBookStackItems;
            if (slots == null) {
                return 0;
            }
            int count = 0;
            for (int i = 0; i < slots.Count; i++) {
                Item item = slots[i];
                if (item != null && !item.IsAir && IsBookmarkSafe(item)) {
                    count++;
                }
            }
            return count;
        }

        /// <summary>堕化与灵魂两套卡组各算一份</summary>
        private static int DeckCount(Player player) {
            EModPlayer mp = player?.Entropy();
            if (mp == null) {
                return 0;
            }
            return (mp.taintedDeckInInv ? 1 : 0) + (mp.soulDeckInInv ? 1 : 0);
        }

        private const uint MinerScanInterval = 60u;
        private static uint minerScanFrame;
        private static bool minerScanned;
        private static bool minerFound;

        /// <summary>世界里是否已经放下一台采矿机。TP 列表运行时会被增删，按 InnoVault 的要求倒序遍历</summary>
        private static bool MinerPlaced() {
            if (!NeedRescan(ref minerScanFrame, ref minerScanned, MinerScanInterval)) {
                return minerFound;
            }
            minerFound = false;

            List<TileProcessor> inWorld = TileProcessorLoader.TP_InWorld;
            if (inWorld == null) {
                return false;
            }
            for (int i = inWorld.Count - 1; i >= 0; i--) {
                TileProcessor tp = i < inWorld.Count ? inWorld[i] : null;
                if (tp != null && tp.Active && tp is AzMinerTP) {
                    minerFound = true;
                    break;
                }
            }
            return minerFound;
        }

        /// <summary>
        /// 节流闸：按游戏帧计，而不是按调用次数。同一帧里有多少个节点问过都只算一次，
        /// 否则节点一多，计数式的冷却会在几帧内就被抽干，等于没节流。
        /// GameUpdateCount 在换世界时归零，此时无符号相减会溢出成极大值，正好落在「重扫」这一侧
        /// </summary>
        private static bool NeedRescan(ref uint lastFrame, ref bool primed, uint interval) {
            uint now = Main.GameUpdateCount;
            if (primed && now - lastFrame < interval) {
                return false;
            }
            lastFrame = now;
            primed = true;
            return true;
        }

        /// <summary>
        /// 背包类判定的共享快照。CWR 每帧对每个已解锁未完成的节点跑一次谓词，
        /// 这里把所有扫背包的活压成每 30 帧一次，多个节点同帧调用只会真扫一次
        /// </summary>
        private sealed class Scan
        {
            private const uint Interval = 30u;
            private static readonly Scan cache = new Scan();
            private static uint lastFrame;
            private static bool primed;

            internal int Zodiac;
            internal int OracleCards;
            internal int BookKinds;
            internal int MariviniumPieces;
            internal int FinalArms;
            internal int SoulEssence;
            internal bool HasBook;
            internal bool HasAltar;
            internal bool ArmorPrefixed;

            internal static Scan Of(Player player) {
                if (player == null || !player.active || zodiacTypes == null
                    || !NeedRescan(ref lastFrame, ref primed, Interval)) {
                    return cache;
                }
                cache.Refresh(player);
                return cache;
            }

            private void Refresh(Player player) {
                Zodiac = 0;
                OracleCards = 0;
                BookKinds = 0;
                MariviniumPieces = 0;
                FinalArms = 0;
                SoulEssence = 0;
                HasBook = false;
                HasAltar = false;
                ArmorPrefixed = false;

                int altarType = ModContent.ItemType<AbyssalAltar>();
                int essenceType = ModContent.ItemType<WraithSoulEssence>();
                bool[] zodiacSeen = new bool[zodiacTypes.Length];
                bool[] cardSeen = new bool[oracleCardTypes.Length];
                bool[] mariviniumSeen = new bool[mariviniumTypes.Length];
                bool[] finalArmSeen = new bool[finalArmTypes.Length];
                var bookKinds = new HashSet<int>();

                //穿在身上的套装不在背包里，装进书里的书签也不该丢掉收集进度，所以三处都扫
                Sweep(player.inventory, zodiacSeen, cardSeen, mariviniumSeen, finalArmSeen, bookKinds, altarType, essenceType);
                Sweep(player.armor, zodiacSeen, cardSeen, mariviniumSeen, finalArmSeen, bookKinds, altarType, essenceType);
                Sweep(player.Entropy()?.EBookStackItems, zodiacSeen, cardSeen, mariviniumSeen, finalArmSeen, bookKinds, altarType, essenceType);

                Zodiac = CountTrue(zodiacSeen);
                OracleCards = CountTrue(cardSeen);
                MariviniumPieces = CountTrue(mariviniumSeen);
                FinalArms = CountTrue(finalArmSeen);
                BookKinds = bookKinds.Count;
                HasBook = BookKinds > 0;
            }

            private void Sweep(IList<Item> items, bool[] zodiacSeen, bool[] cardSeen, bool[] mariviniumSeen,
                bool[] finalArmSeen, HashSet<int> bookKinds, int altarType, int essenceType) {
                if (items == null) {
                    return;
                }
                for (int i = 0; i < items.Count; i++) {
                    Item item = items[i];
                    if (item == null || item.IsAir) {
                        continue;
                    }

                    if (item.ModItem is EntropyBook) {
                        bookKinds.Add(item.type);
                    }
                    if (item.type == altarType) {
                        HasAltar = true;
                    }
                    if (item.type == essenceType) {
                        SoulEssence += item.stack;
                    }
                    if (!ArmorPrefixed && item.Entropy().armorPrefix != null) {
                        ArmorPrefixed = true;
                    }

                    Mark(zodiacTypes, zodiacSeen, item.type);
                    Mark(oracleCardTypes, cardSeen, item.type);
                    Mark(mariviniumTypes, mariviniumSeen, item.type);
                    Mark(finalArmTypes, finalArmSeen, item.type);
                }
            }

            private static void Mark(int[] types, bool[] seen, int itemType) {
                for (int i = 0; i < types.Length; i++) {
                    if (types[i] == itemType) {
                        seen[i] = true;
                        return;
                    }
                }
            }

            private static int CountTrue(bool[] seen) {
                int count = 0;
                for (int i = 0; i < seen.Length; i++) {
                    if (seen[i]) {
                        count++;
                    }
                }
                return count;
            }
        }

        #endregion

        #region 参数表

        /// <summary>
        /// 一条注册的参数表构造器。文案四件套按节点键自动绑到
        /// <c>Mods.CalamityEntropy.CWRQuest.&lt;键&gt;.{Title,Summary,Objective,Detail}</c>，
        /// 传给 CWR 的是 LocalizedText，它不会把本模组的文案写进自己的 hjson
        /// </summary>
        private sealed class Node
        {
            private readonly string key;
            private readonly Dictionary<string, object> args = new Dictionary<string, object>();
            private readonly List<Dictionary<string, object>> rewards = new List<Dictionary<string, object>>();

            internal Node(string nodeKey) {
                key = nodeKey;
                args["id"] = Full(nodeKey);
                args["title"] = Text("Title");
                args["summary"] = Text("Summary");
                args["objective"] = Text("Objective");
                args["detailed"] = Text("Detail");
            }

            private LocalizedText Text(string field)
                => CalamityEntropy.Instance.GetLocalization($"CWRQuest.{key}.{field}");

            /// <summary>首个父节点决定坐标原点，其余只参与解锁。解锁要求全部父节点完成</summary>
            internal Node Parents(params string[] ids) {
                args["parents"] = ids;
                return this;
            }

            internal Node At(float x, float y) {
                args["x"] = x;
                args["y"] = y;
                return this;
            }

            internal Node Kind(string type, string difficulty) {
                args["type"] = type;
                args["difficulty"] = difficulty;
                return this;
            }

            /// <summary>
            /// 图标走贴图路径而不是 iconNpc：CWR 对 NPC 图标按 npcFrameCount 竖切首帧，
            /// 会切坏本模组的 Boss 图集，而每个 Boss 都有现成的单帧 _Head_Boss 小图
            /// </summary>
            internal Node Icon(string path) {
                args["icon"] = path;
                return this;
            }

            internal Node IconItem(int itemType) {
                args["iconItem"] = itemType;
                return this;
            }

            /// <summary>单步目标，读持久状态</summary>
            internal Node Done(Func<Player, bool> complete) {
                args["complete"] = complete;
                return this;
            }

            /// <summary>分步目标，谓词返回 0 到 1，界面按 n / max 显示</summary>
            internal Node Progress(Func<Player, float> complete, int max) {
                args["complete"] = (Func<Player, float>)(player => MathHelper.Clamp(complete(player), 0f, 1f));
                args["progressMax"] = max;
                return this;
            }

            internal Node Unlock(Func<Player, bool> unlock) {
                args["unlock"] = unlock;
                return this;
            }

            internal Node Hidden() {
                args["hiddenUntilUnlocked"] = true;
                return this;
            }

            internal Node Completionist() {
                args["countsTowardCompletionist"] = true;
                return this;
            }

            internal Node Chapter(int order) {
                args["chapterHub"] = true;
                args["chapterOrder"] = order;
                return this;
            }

            /// <summary>奖励顺序跨会话必须固定：CWR 的领取标记按下标存，调换顺序会串位</summary>
            internal Node Reward(int itemType, int stack = 1) {
                rewards.Add(new Dictionary<string, object> {
                    ["item"] = itemType,
                    ["stack"] = stack
                });
                return this;
            }

            internal void Push() {
                if (rewards.Count > 0) {
                    args["rewards"] = rewards;
                }
                try {
                    if (cwr?.Call(CmdRegister, args) is not true) {
                        CalamityEntropy.Instance.Logger.Info($"CalamityEntropy: CWR rejected quest node `{args["id"]}`");
                    }
                } catch (Exception ex) {
                    //单节点失败不能把后面的节点一起带走
                    CalamityEntropy.Instance.Logger.Info($"CalamityEntropy: CWR quest node `{args["id"]}` threw: {ex.Message}");
                }
            }
        }

        #endregion
    }
}
