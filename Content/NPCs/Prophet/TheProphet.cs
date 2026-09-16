using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.NPCs.Prophet.States;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.AI;
using CalamityEntropy.Core.CalamityRef;
using InnoVault;
using InnoVault.PRT;
using InnoVault.StateMachines;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    /// <summary>
    /// 先知:地牢 Boss,InnoVault 状态机宿主。
    /// <para>
    /// 招式一文件一个放在 States/,数值全在 <see cref="ProphetDirector"/>,选招在 <see cref="ProphetRotation"/>,
    /// 绘制在 TheProphet.Draw.cs。原代码的十二个 <c>AIStyle</c> 与状态号逐个对齐,
    /// 倒计时 <c>AIChangeDelay</c> 原样保留成 <see cref="ProphetStateContext.Countdown"/>,节拍照原样对它判定。
    /// </para>
    /// <para>
    /// 联机:状态号 <c>ai[3]</c>、阶段 <c>ai[2]</c>、冲刺窗 <c>ai[1]</c>(原版自带同步)。
    /// 选招与瞬移掷骰只在权威端,结果随 <c>SendExtraAI</c> 与位置速度原子过线;
    /// 各端都跑完整的运动数学,客户端带容差收养计时与倒计时。
    /// </para>
    /// <para>
    /// <b>天顶世界另有一套 AI</b>:整条 <see cref="AI"/> 在开头就委派给 <see cref="OlderCruiserAIGNPC"/> 并 return,
    /// 状态机在那个世界里根本不初始化。那条路径属于并行的巡洋舰任务,本次一个字没动
    /// </para>
    /// </summary>
    [AutoloadBossHead]
    public partial class TheProphet : ModNPC
    {
        #region 字段
        private NpcStateMachine<ProphetStateContext> stateMachine;
        /// <summary>状态上下文:过线事实 + 每帧重算事实</summary>
        public ProphetStateContext Context { get; private set; }
        /// <summary>联机运动:客户端位置纠偏 + 状态计时收养</summary>
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;
        /// <summary>本端上一帧显示的位置。收到瞬移流水号推进时拿它当「出发点」补放火花</summary>
        private Vector2 lastSeenCenter;

        /// <summary>绘制朝向:向真实朝向平滑收敛。纯表现,由已过线的 <c>NPC.rotation</c> 推导</summary>
        public float rl = 0;
        /// <summary>鳍摆动相位。纯表现</summary>
        public float finRotCounter = 0;
        /// <summary>二阶段换曲的单发闸(仅本端)</summary>
        public bool music2 = false;
        /// <summary>
        /// 原 <c>NoEnrange</c>:地牢里重置成 500,其余每帧自减。
        /// <b>全仓库没有任何地方读它</b>,是彻底的残留量,照搬保留
        /// </summary>
        public int NoEnrange = ProphetDirector.NoEnrageStart;
        /// <summary>冲刺尾焰粒子,由 1 号招创建、宿主每帧续点。纯表现</summary>
        public PRT_ProminenceTrail trail = null;

        /// <summary>天顶世界的第二套 AI(旧巡洋舰彩蛋)。属于并行任务,只调用不改动</summary>
        public OlderCruiserAIGNPC zenithAI = new OlderCruiserAIGNPC();

        /// <summary>尾迹质点</summary>
        public class TailPoint
        {
            public int timeLeft = ProphetDirector.TailPointLife;
            public Vector2 position;
            public Vector2 velocity;
            public TailPoint(Vector2 pos, Vector2 vel)
            {
                position = pos;
                velocity = vel;
            }
            public void update()
            {
                position += velocity;
                velocity *= ProphetDirector.TailPointDrag;
                timeLeft--;
            }
        }
        public List<TailPoint> tail = new();

        /// <summary>当前招式。原 <c>AIStyle</c> 字段的等价读法,序号一一对应</summary>
        public ProphetStateIndex CurrentStateIndex => (ProphetStateIndex)(int)NPC.ai[3];
        /// <summary>原 <c>AIStyle</c> 的对外读法</summary>
        public int AIStyle => (int)NPC.ai[3];
        /// <summary>原 <c>phase</c> 字段:映射 <c>ai[2]</c>。天顶世界不建上下文,恒为 1(与原代码一致)</summary>
        public int phase => Context?.Phase ?? 1;
        /// <summary>原 <c>spawnAnm</c> 字段:出生演出倒计时,绘制层要读</summary>
        public int spawnAnm
        {
            get => Context?.SpawnAnim ?? ProphetDirector.SpawnAnimFrames;
            set
            {
                if (Context != null)
                {
                    Context.SpawnAnim = value;
                }
            }
        }

        /// <summary>原灾厄全局 DR 字段的本地等效。AI 每帧按当前招式重算</summary>
        public float DamageReduction = ProphetDirector.BaseDamageReduction;
        #endregion

        #region 定义
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = "CalamityEntropy/Assets/BCL/Prophet",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -66
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //冲刺稳态与瞬移都远超 10 px/f,原版 netOffset 平滑只会让它在联机里锯齿;关掉后纠偏器接管
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheDungeon,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.ProphetBestiary")
            });
        }

        public override void SetDefaults()
        {
            NPC.boss = true;
            //状态机把状态号写进 ai[3],原版 AI 不许占这个槽。模组 NPC 的默认值本来就是 -1,这里写明
            NPC.aiStyle = -1;
            NPC.width = 80;
            NPC.height = 80;
            NPC.damage = 68;
            DamageReduction = ProphetDirector.BaseDamageReduction;
            NPC.lifeMax = 48000;
            //装灾厄读死亡/复仇,缺席仍走大师/专家兜底
            if (CECal.IsDeathMode)
            {
                NPC.damage += 4;
            }
            else if (CECal.IsRevengeance)
            {
                NPC.damage += 2;
            }
            var snd = CEUtils.GetSound("prophet_hurt", maxIns: 1);
            var snd2 = CEUtils.GetSound("prophet_death");
            NPC.HitSound = snd;
            NPC.DeathSound = snd2;
            NPC.value = 2000f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.25f;
            NPC.dontCountMe = true;
            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/SpectralForesight");
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            if (Main.zenithWorld)
            {
                return zenithAI.aitype != 3f;
            }
            //只有两支冲锋招带接触伤害
            return CurrentStateIndex == ProphetStateIndex.Dash || CurrentStateIndex == ProphetStateIndex.AltRuneCharge;
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1f - DamageReduction;
            if (CurrentStateIndex == ProphetStateIndex.GrandLaser)
            {
                modifiers.FinalDamage *= 0.5f;
            }
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsOverPlayers.Add(index);
        }
        #endregion

        #region 状态机装配
        private void EnsureContext()
        {
            Context ??= new ProphetStateContext
            {
                Npc = NPC,
                Owner = this,
            };
            Context.Npc = NPC;
            Context.Owner = this;
        }

        private void InitializeStateMachine()
        {
            EnsureContext();
            if (NPC.ai[2] < 1f)
            {
                NPC.ai[2] = 1f;
            }
            stateMachine = new NpcStateMachine<ProphetStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            //中途加入的客户端从 ai[3] 重建当前招,而不是默认回第一招
            IVaultState<ProphetStateContext> initial = null;
            if (VaultUtils.isClient)
            {
                initial = VaultStateRegistry<ProphetStateContext>.Create((int)NPC.ai[3]);
            }
            //原代码 AIStyle 初值 0、AIChangeDelay 初值 0,所以首帧就会立刻选招并落到 0 号槽
            stateMachine.SetInitialState(initial ?? new ProphetRuneVolleyState());
        }
        #endregion

        public override void AI()
        {
            rl = CEUtils.RotateTowardsAngle(rl, NPC.rotation, ProphetDirector.DrawRotateRate, false);
            UpdateFins();
            if (!Main.dedServ)
            {
                if (phase == 2 && !music2)
                {
                    music2 = true;
                    Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Prophet2");
                }
            }

            //天顶世界:整条 AI 交给旧巡洋舰彩蛋,状态机不启动也不推进(与原代码逐字一致)
            if (Main.zenithWorld)
            {
                zenithAI.PreAI(NPC);
                UpdateTails();
                return;
            }

            EnsureContext();
            if (stateMachine == null)
            {
                InitializeStateMachine();
            }

            bool client = VaultUtils.isClient;
            if (client)
            {
                netMotion.BeginFrame(NPC);
                CEBossHost.AdoptTimingAtFrameStart(netMotion, stateMachine);
            }

            if (Context.DrRamp > 0)
            {
                Context.DrRamp -= ProphetDirector.DrRampDecayPerFrame;
            }
            // 原灾厄该条减伤上升提示位随灾厄退场,DR 改本地字段结算
            DamageReduction = ProphetDirector.DamageReductionFor(CurrentStateIndex, Context.DrRamp);

            if (Context.SpawnAnim > 0)
            {
                NPC.dontTakeDamage = true;
                NPC.rotation = ProphetDirector.SpawnAnimRotation;
            }
            //原代码写的是 == 0。倒计时会一路穿过 0 往负数走,所以在原时间轴上两者等价;
            //改成 <= 0 是因为这个计数现在会被客户端从包里收养,可能一步跨过 0 而永远解不开免伤
            if (Context.SpawnAnim <= 0)
            {
                NPC.dontTakeDamage = false;
            }
            Context.SpawnAnim--;

            if (!NPC.HasValidTarget)
            {
                NPC.TargetClosest();
            }

            if (Context.SpawnAnim <= 0)
            {
                if (!NPC.HasValidTarget)
                {
                    //脱战:上浮离场。原代码在各端都置 active = false,这里收归权威端
                    //(原代码紧跟着就写 netUpdate,本意就是服务端驱动)
                    Context.Target = null;
                    Context.TargetValid = false;
                    NPC.localAI[0]++;
                    NPC.velocity.Y -= ProphetDirector.NoTargetRiseAccel;
                    NPC.velocity *= ProphetDirector.NoTargetDrag;
                    NPC.rotation = NPC.velocity.ToRotation();
                    if (NPC.localAI[0] > ProphetDirector.NoTargetDespawnFrames && !client)
                    {
                        NPC.active = false;
                        NPC.netUpdate = true;
                    }
                }
                else
                {
                    NPC.localAI[0] = 0;
                    targetPlayer = NPC.target.ToPlayer();
                    RunAttackFrame(targetPlayer);
                }
                UpdateTails();
            }

            lastSeenCenter = NPC.Center;
            if (client)
            {
                netMotion.EndFrame(NPC);
            }
            else
            {
                CEBossHost.Heartbeat(NPC);
            }
        }

        /// <summary>
        /// 原 <c>AttackPlayer</c> 的骨架。顺序逐条对齐:
        /// 地牢重置残留量 → 自减 → 阶段判定 → 重算难度系数 → 倒计时耗尽就选招(同帧跑新招)→
        /// 状态体 → 续尾焰点 → 倒计时自减 → 服务端解节流
        /// </summary>
        private void RunAttackFrame(Player target)
        {
            if (target.ZoneDungeon)
            {
                NoEnrange = ProphetDirector.NoEnrageDungeon;
            }
            NoEnrange--;

            // 原灾厄该条狂怒提示位(CurrentlyEnraged)随灾厄退场,狂怒数值逻辑本就在下方自持
            if (NPC.life < NPC.lifeMax / ProphetDirector.Phase2LifeDivisor && Context.Phase < 2)
            {
                Context.Phase = 2;
                if (!VaultUtils.isClient)
                {
                    NPC.netUpdate = true;
                }
            }

            Context.Target = target;
            Context.TargetValid = true;
            Context.Difficult = ProphetDirector.Difficult(NPC);

            //选招只在权威端。原代码在客户端也会自己掷骰选招,于是两端的出招序列直接分叉
            if (Context.Countdown <= 0 && !VaultUtils.isClient)
            {
                IVaultState<ProphetStateContext> next = ProphetRotation.Pick(Context);
                if (next != null)
                {
                    stateMachine.ChangeState(next);
                }
            }

            Context.BeginFrameDefaults();
            stateMachine.Update();

            trail?.AddPoint(NPC.Center + NPC.rotation.ToRotationVector2()
                * (NPC.velocity.Length() + ProphetDirector.TrailPointForward));
            Context.Countdown--;
            //原代码每帧清节流位。保留:本 Boss 的瞬移密度需要决策点当帧就发出去
            if (Main.netMode == NetmodeID.Server)
            {
                NPC.netSpam = 0;
            }
        }

        #region 表现:鳍与尾迹
        public void UpdateFins()
        {
            finRotCounter += NPC.velocity.Length() * ProphetDirector.FinPhaseSpeedFactor + ProphetDirector.FinPhaseBase;
            if (finRotCounter > 1)
            {
                finRotCounter--;
            }
            //原代码自增但全仓库无人读,残留量,照搬
            NPC.localAI[1]++;
        }

        public void UpdateTails()
        {
            foreach (TailPoint p in tail)
            {
                p.update();
            }
            if (tail.Count > 0)
            {
                if (tail[0].timeLeft <= 0)
                {
                    tail.RemoveAt(0);
                }
            }
            //侧摆相位吃 Main.GameUpdateCount,两端不同步。这条尾迹只进绘制,不参与任何判定
            tail.Add(new TailPoint(NPC.Center - rl.ToRotationVector2() * ProphetDirector.TailSpawnBack,
                (rl.ToRotationVector2() * ProphetDirector.TailSpawnSpeed)
                + rl.ToRotationVector2().RotatedBy(MathHelper.PiOver2)
                * (float)(Math.Sin(Main.GameUpdateCount * ProphetDirector.TailSwayFreq) * ProphetDirector.TailSwayAmp)));
        }
        #endregion

        #region 瞬移
        /// <summary>
        /// 原 <c>TeleportTo</c>:清速度、进出各两枚火花、落位、清尾迹。
        /// <b>只由权威端调用</b>(入口在 <c>ProphetStateBase.Teleport</c>),落点随包过线
        /// </summary>
        public void TeleportTo(Vector2 pos)
        {
            NPC.velocity *= 0;
            Color impactColor = Main.rand.NextBool(3) ? Color.SkyBlue : Color.White;

            TeleportSparkles(NPC.Center, impactColor);
            NPC.Center = pos;
            TeleportSparkles(NPC.Center, impactColor);

            tail.Clear();
            //位置被直接改写,丢掉旧预测,免得下一包被纠偏器当成失步
            netMotion.ForgetPrediction();
            lastSeenCenter = NPC.Center;
        }

        /// <summary>客户端补演:包里的位置已经是落点,这里只补两端火花、清尾迹、复位预测</summary>
        private void ReplayTeleport(Vector2 pos)
        {
            Color impactColor = Main.rand.NextBool(3) ? Color.SkyBlue : Color.White;
            TeleportSparkles(lastSeenCenter, impactColor);
            TeleportSparkles(pos, impactColor);
            tail.Clear();
            netMotion.ForgetPrediction();
            lastSeenCenter = pos;
        }

        //TeleportTo 进出各 2 枚 SparkleCal,落点清空 tail 的 GP 点
        private static void TeleportSparkles(Vector2 at, Color impactColor)
        {
            float impactParticleScale = ProphetDirector.TeleportSparkleScale;
            PRTLoader.NewParticle<PRT_SparkleCal>(at, Vector2.Zero, Color.White, impactParticleScale * 1.2f)
                .Configure(Color.SkyBlue, 12, 0, 4.5f);
            PRTLoader.NewParticle<PRT_SparkleCal>(at, Vector2.Zero, impactColor, impactParticleScale)
                .Configure(Color.SkyBlue, 10, 0, 3f);
        }
        #endregion

        #region 同步
        /// <summary>
        /// 定长块,顺序固定在这一处。先计时、再持久累加量(朝向)、再状态标量、最后瞬移事实。
        /// <para>
        /// 末尾那个天顶分支是原代码就有的:<c>Main.zenithWorld</c> 是整局恒定的世界级开关,
        /// 两端同真同假,所以它不会让读写流错位。除此之外没有任何运行时条件决定写不写字段
        /// </para>
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is ProphetStateBase state)
            {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            //持久累加量:SyncNPC case 23 不带 rotation,而 1 / 11 号招会把朝向一帧压进速度
            writer.Write(NPC.rotation);

            writer.Write(Context.Countdown);
            writer.Write(Context.SpawnAnim);
            writer.Write(Context.DrRamp);
            writer.Write(Context.AttackIndex);

            writer.Write(Context.TeleportSeq);
            writer.WriteVector2(Context.TeleportPos);

            if (Main.zenithWorld)
            {
                zenithAI.SendExtraAI(NPC, writer);
            }
        }

        /// <summary>客户端收包:position/velocity/ai 已是服务端值,先据计时差纠偏,再读事实</summary>
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is ProphetStateBase state)
            {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            //天顶路径自己每帧算朝向,不在本轮范围内,读出来但不落盘(定长块仍然对齐)
            float syncedRotation = reader.ReadSingle();
            if (!Main.zenithWorld)
            {
                NPC.rotation = syncedRotation;
            }

            //倒计时与出生演出都是「== N」型节拍的判据,硬对齐会跳过或重放拍子,一律带 ±2 容差
            Context.Countdown = CEBossNetMotion.AdoptTimer(Context.Countdown, reader.ReadInt32());
            Context.SpawnAnim = CEBossNetMotion.AdoptTimer(Context.SpawnAnim, reader.ReadInt32());
            Context.DrRamp = reader.ReadSingle();
            Context.AttackIndex = reader.ReadInt32();

            int seq = reader.ReadInt32();
            Vector2 pos = reader.ReadVector2();
            if (seq != Context.TeleportSeq)
            {
                Context.TeleportSeq = seq;
                Context.TeleportPos = pos;
                if (VaultUtils.isClient)
                {
                    ReplayTeleport(pos);
                }
            }

            if (Main.zenithWorld)
            {
                zenithAI.ReceiveExtraAI(NPC, reader);
            }

            //快速移动实体不吃原版平滑,收包后位置即最终位置
            NPC.netOffset = Vector2.Zero;
        }
        #endregion

        #region 掉落
        public override void BossLoot(ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ProphetBag>()));

            // 治疗药水按人 5-15 瓶,隐藏图鉴条目(承接原灾厄 PerPlayer 语义)
            npcLoot.Add(new DropPerPlayerOnThePlayer(ItemID.GreaterHealingPotion, 1, 5, 15, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            {
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<RuneSong>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<UrnOfSouls>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<SpiritBanner>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<RuneMachineGun>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<ProphecyFlyingKnife>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<ForeseeOrb>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<RuneWing>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<ForeseeWhip>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<BookMarkForesee>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CursedThread>(), 1));
            }
            npcLoot.Add(normalOnly);
            // 遗物:原灾厄复仇→大师条件对齐原版大师掉落惯例(difficulty-map)
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<ProphetRelic>()));

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ProphetTrophy>(), 10));

            // 首杀传说:承接原灾厄按人实例掉落语义
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<ProphetLore>(), 1, 1, 1, new LoreFirstKill()));
        }

        // 恒真但隐藏图鉴条目的条件:对应原 hideLootReport 语义
        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }

        // 首杀传说条件:对应 downed 旗标未置位时每名玩家各掉一份
        private class LoreFirstKill : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedProphet;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }

        public override void OnKill()
        {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedProphet, -1);
        }
        #endregion
    }
}
