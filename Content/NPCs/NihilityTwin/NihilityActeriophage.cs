using CalamityEntropy.Common;
using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.NPCs.NihilityTwin.States;
using CalamityEntropy.Core.AI;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Utilities;
using InnoVault;
using InnoVault.StateMachines;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    /// <summary>
    /// 虚无双子(本体:虚无噬菌体),InnoVault 状态机宿主。
    /// <para>
    /// 「双子」是本体 + <see cref="ChaoticCell"/> 混沌细胞这一对:细胞是共享血量的部件
    /// (<c>realLife</c> 指回本体),自己没有攻击逻辑,运动全由本体逐帧写速度。
    /// 天顶世界里还会额外复制一只同类型本体,同样挂 <c>realLife</c> 并<b>共用同一颗细胞</b>。
    /// </para>
    /// <para>
    /// 状态只写运动与出手,宿主按固定顺序落地:客户端纠偏 → 部件就位 → 目标校验 →
    /// 全局转移 → 清声明 → 状态机 → 声明结算 → 尾处理 → 客户端记预测。
    /// 联机:转移只在权威端(状态号 ai[3],阶段 ai[2]);各端跑同一套运动数学;
    /// 计时与朝向、自旋、环射基准角等累加量随 SendExtraAI 过线。
    /// 数值在 <see cref="NihilityDirector"/>,选招在 <see cref="NihilityRotation"/>,绘制在 .Draw.cs
    /// </para>
    /// </summary>
    [AutoloadBossHead]
    public partial class NihilityActeriophage : ModNPC
    {
        #region 字段
        private NpcStateMachine<NihilityStateContext> stateMachine;
        public NihilityStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;

        /// <summary>混沌细胞实体。可能为 null(尚未生成 / 刚死 / 客户端还没收到索引)</summary>
        public NPC cell = null;
        /// <summary>细胞的 <c>whoAmI</c>。NPC 槽位由服务端裁决,各端一致,可直接当跨端身份用</summary>
        public int cellIndex = -1;
        private bool spawnCell = true;

        /// <summary>出场演出倒计时。原代码没同步它,本轮随 ExtraAI 过线</summary>
        public int spawnAnm = NihilityDirector.SpawnAnimFrames;
        /// <summary>出场屏震包络(纯本地)</summary>
        private float shake = 0f;
        /// <summary>出场动画持续屏震的复用实例(仅客户端)</summary>
        private ScreenShaker.ScreenShake spawnShake = null;

        /// <summary>脱战倒计时。纯本地:真正的下线只在权威端执行</summary>
        private int escapeCounter = 0;

        /// <summary>连接两端的绳索。纯绘制</summary>
        public Rope rope = null;
        /// <summary>绳索显示插值。二阶段每帧减 1(即立刻收起),只有对撞合体那一手把它顶回 1</summary>
        public float ropeLerp = 1;
        #endregion

        #region 定义
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                Scale = 0.65f,
                PortraitScale = 0.7f,
                CustomTexturePath = "CalamityEntropy/Assets/Extra/NABes",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -14
            };
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Burning] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<VoidVirus>()] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //冲刺与突进稳态远超 10 px/f,原版 netOffset 会让本体与细胞、绳索分家;关掉后纠偏器接管
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.NihilityTwinBestiary")
            });
        }

        public override void SetDefaults() {
            NPC.boss = true;
            //ai[3] 归状态机占用,必须关掉原版 AI 分支
            NPC.aiStyle = -1;
            NPC.width = 140;
            NPC.height = 140;
            NPC.damage = 106;
            if (Main.expertMode) {
                NPC.damage += 2;
            }
            if (Main.masterMode) {
                NPC.damage += 2;
            }
            NPC.defense = 75;
            NPC.lifeMax = 360000;
            //装灾厄读死亡/复仇,缺席仍走大师/专家兜底
            if (CECal.IsDeathMode) {
                NPC.damage += 5;
            }
            else if (CECal.IsRevengeance) {
                NPC.damage += 4;
            }
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCHit4;
            NPC.value = Item.buyPrice(1, 2, 60, 0);
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.5f;
            NPC.dontCountMe = true;
            NPC.netAlways = true;
            SpawnModBiomes = new int[] { ModContent.GetInstance<VoidDummyBoime>().Type };
        }

        // 原灾厄全局 DR=0.15 的本地等效;公有字段供血条等外部读取
        public float DamageReduction = 0.15f;
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 1f - DamageReduction;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 360);
        }

        public override bool CheckActive() {
            return false;
        }

        public override void BossHeadRotation(ref float rotation) {
            rotation = NPC.rotation + MathHelper.PiOver2;
        }

        public override void OnSpawn(IEntitySource source) {
        }
        #endregion

        #region 状态机装配
        private void EnsureContext() {
            Context ??= new NihilityStateContext {
                Npc = NPC,
                Owner = this,
            };
            Context.Npc = NPC;
            Context.Owner = this;
        }

        private void InitializeStateMachine() {
            EnsureContext();
            if (NPC.ai[2] < 1f) {
                NPC.ai[2] = 1f;
            }
            stateMachine = new NpcStateMachine<NihilityStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            IVaultState<NihilityStateContext> initial = null;
            if (VaultUtils.isClient) {
                initial = VaultStateRegistry<NihilityStateContext>.Create((int)NPC.ai[3]);
            }
            //原代码的 aitype 初值是 3(一阶段悬停爆发),不是整备;照搬
            stateMachine.SetInitialState(initial ?? new NihilityP1HoverBurstState());
        }
        #endregion

        #region 细胞实体
        /// <summary>
        /// 生成 / 找回混沌细胞。生成只在权威端;客户端靠 <see cref="cellIndex"/> 认领。
        /// 原代码不校验槽位里的东西还是不是细胞,这里补一次类型与存活校验:
        /// 槽位被回收后继续往里写速度会砸到陌生 NPC 身上
        /// </summary>
        private void EnsureCell() {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                if (spawnCell) {
                    spawnCell = false;
                    if (NPC.realLife < 0) {
                        SpawnCellAndZenithClone();
                    }
                }
                //原代码用 Main.GameUpdateCount % 5 轮询,这一支只在权威端跑,不构成跨端分叉
                if (Main.GameUpdateCount % NihilityDirector.CellRespawnPollFrames == 0
                    && (cell == null || !cell.active) && NPC.realLife < 0) {
                    SpawnCellAndZenithClone(cloneToo: false);
                }
            }

            if (cell == null && cellIndex >= 0) {
                cell = cellIndex.ToNPC();
            }
            if (cell != null && (!cell.active || cell.ModNPC is not ChaoticCell)) {
                cell = null;
            }
        }

        private void SpawnCellAndZenithClone(bool cloneToo = true) {
            int n = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<ChaoticCell>());
            n.ToNPC().realLife = NPC.whoAmI;
            n.ToNPC().netUpdate = true;
            cell = n.ToNPC();
            cellIndex = cell.whoAmI;
            NPC.netUpdate = true;
            NPC.netSpam = NihilityDirector.NetSpamClampTo;

            if (cloneToo && Main.zenithWorld) {
                int n2 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<NihilityActeriophage>());
                n2.ToNPC().realLife = NPC.whoAmI;
                n2.ToNPC().netUpdate = true;
                n2.ToNPC().position += CEUtils.randomPointInCircle(NihilityDirector.ZenithCloneScatter);
                if (n2.ToNPC().ModNPC is NihilityActeriophage na) {
                    na.cell = cell;
                    na.cellIndex = cellIndex;
                }
            }
        }
        #endregion

        public override void AI() {
            EnsureContext();
            if (stateMachine == null) {
                InitializeStateMachine();
            }

            bool client = VaultUtils.isClient;
            if (client) {
                netMotion.BeginFrame(NPC);
                CEBossHost.AdoptTimingAtFrameStart(netMotion, stateMachine);
            }

            // 天顶分身与本体同类型,必须每帧清掉 boss 标记,否则每只各顶一根血条
            // 联机不单独同步该位:realLife 是原版字段,中途加入的客户端进 AI 后也会落到 false
            if (NPC.realLife >= 0) {
                NPC.boss = false;
            }

            EnsureCell();

            if (spawnAnm > 0) {
                UpdateSpawnAnimation();
                if (client) {
                    netMotion.EndFrame(NPC);
                }
                return;
            }

            if (cell == null) {
                //细胞缺席时整棵状态树都没有第二个支点,原代码会在这里空引用;直接跳过这一帧
                if (client) {
                    netMotion.EndFrame(NPC);
                }
                return;
            }

            Context.FrameCounter++;
            if (rope == null) {
                rope = new Rope(NPC.Center, cell.Center, NihilityDirector.RopeSegments, 0, new Vector2(0, 0f),
                    NihilityDirector.RopeStiffness, NihilityDirector.RopeIterations, false);
            }
            if (!Main.dedServ) {
                Main.LocalPlayer.Entropy().NihSky = NihilityDirector.NihSkyRefresh;
            }
            NPC.localAI[0]++;

            FindTarget();
            UpdateContextFacts();

            if (Context.TargetValid) {
                escapeCounter = 0;
                EvaluateGlobalTransitions();
                if (Context.Phase >= 2 && ropeLerp > 0) {
                    ropeLerp -= 1f;
                }
            }
            Context.BeginFrameDefaults();
            //脱战也要走 Update:客户端靠这里的 NetSync 收权威端换态。状态体本身见 RequiresTarget,没目标不跑
            stateMachine.Update();
            if (Context.TargetValid) {
                SettleDeclarations();
            }
            else {
                UpdateEscapeMotion();
            }

            cell.life = NPC.life;
            cell.target = NPC.target;
            NPC.velocity *= NihilityDirector.GlobalDrag;
            UpdateRope();

            if (client) {
                netMotion.EndFrame(NPC);
            }
            else {
                CEBossHost.Heartbeat(NPC);
            }
        }

        /// <summary>出场演出:两端焊在一起不动,屏震包络逐帧涨。状态机不推进</summary>
        private void UpdateSpawnAnimation() {
            spawnAnm--;
            shake += NihilityDirector.SpawnShakeRise;
            if (cell != null) {
                cell.Center = NPC.Center;
                cell.velocity *= 0;
                NPC.velocity *= 0;
                // 原灾厄全局屏震(逐帧置强度)改自有 ScreenShaker:复用同一震动实例并逐帧刷新振幅
                if (!Main.dedServ) {
                    if (spawnShake == null || !spawnShake.active) {
                        spawnShake = new ScreenShaker.ScreenShake(Vector2.Zero, 0);
                        ScreenShaker.AddShake(spawnShake);
                    }
                    spawnShake.amplitude = NihilityDirector.SpawnShakeAmplitude * shake;
                }
            }
        }

        private void FindTarget() {
            if (!NPC.HasValidTarget) {
                NPC.TargetClosest(false);
            }
            targetPlayer = NPC.HasValidTarget ? NPC.target.ToPlayer() : null;
        }

        private void UpdateContextFacts() {
            Context.Npc = NPC;
            Context.Owner = this;
            Context.Target = targetPlayer;
            //原代码的接战判据只有 HasValidTarget,没有距离上限;照搬
            Context.TargetValid = NPC.HasValidTarget;
            Context.TargetDistance = Context.TargetValid ? NPC.Distance(targetPlayer.Center) : 0f;
        }

        /// <summary>
        /// 转阶段。原代码把它写在一阶段攻击段的最前面,所以转阶段那一帧当前招直接被掐断。
        /// 阶段本身是同步血量的纯函数,各端自行落位(<c>ai[2]</c> 随后也会被快照覆盖成同值);
        /// 无敌帧必须各端各写(受击判定跑在各自机器上);只有换态收归权威端
        /// </summary>
        private void EvaluateGlobalTransitions() {
            if (Context.Phase != 1 || NPC.life >= NPC.lifeMax / NihilityDirector.Phase2LifeDivisor) {
                return;
            }
            foreach (Player plr in Main.ActivePlayers) {
                plr.Entropy().immune = NihilityDirector.Phase2GraceFrames;
            }
            Context.Phase = 2;
            if (!VaultUtils.isClient) {
                Context.Num1 = 0;
                stateMachine.ChangeState(new NihilityRegroupState());
            }
        }

        /// <summary>
        /// 原代码挂在「aitype == 1」上的那两句 <c>else { rotSpeed = 0; }</c>。
        /// 一阶段那句带 <c>aitype != 4</c> 豁免,二阶段那句没有,合起来就是
        /// 「只有一阶段 1/4 号与二阶段 1 号保留自旋,其余每帧清零」。脱战时这两句都不执行,所以自旋量会冻住
        /// </summary>
        private void SettleDeclarations() {
            if (!Context.KeepRotSpeed) {
                Context.RotSpeed = 0f;
            }
        }

        private void UpdateEscapeMotion() {
            if (cell != null) {
                cell.velocity += (NPC.Center - cell.Center) * NihilityDirector.EscapeCellPull;
            }
            NPC.velocity.Y -= NihilityDirector.EscapeRiseAccel;
            escapeCounter++;
            if (escapeCounter > NihilityDirector.EscapeDespawnFrames && !VaultUtils.isClient) {
                //原代码在各端都直接置 active = false,客户端会自己把 Boss 抹掉;下线收归权威端
                NPC.active = false;
                NPC.netUpdate = true;
            }
            NPC.velocity *= NihilityDirector.EscapeDrag;
            NPC.rotation = NPC.velocity.ToRotation();
        }

        private void UpdateRope() {
            if (ropeLerp <= 0 || rope == null || cell == null) {
                return;
            }
            Vector2 rend = Vector2.Lerp(buttom, cell.Center, ropeLerp);
            rope.segmentLength = CEUtils.getDistance(buttom, rend) / NihilityDirector.RopeSegmentDivisor;
            rope.Start = buttom;
            rope.End = rend;
            rope.Update();
        }

        #region 状态可用的小件
        /// <summary>额外推一次绳索求解(自旋狙击每帧多推两次)。纯绘制</summary>
        public void TickRope() {
            rope?.Update();
        }

        /// <summary>
        /// 直写细胞位置(蓄力焊接、对撞对齐这类瞬移)。顺手丢掉细胞纠偏器的旧预测,
        /// 免得下一包把「直写造成的位移」当成失步
        /// </summary>
        public void PlaceCell(Vector2 center) {
            if (cell == null) {
                return;
            }
            cell.Center = center;
            if (cell.ModNPC is ChaoticCell cc) {
                cc.ForgetPrediction();
            }
        }

        /// <summary>直写本体位置(对撞对齐)。同样要丢掉旧预测</summary>
        public void TeleportBody(Vector2 center) {
            NPC.Center = center;
            netMotion.ForgetPrediction();
        }
        #endregion

        #region 同步
        /// <summary>
        /// 定长块,顺序固定在这一处:计时 → 持久累加量 → 锁存标量 → 部件索引。
        /// 字节数是编译期常量(3 int + 6 float + 3 int = 48 B):不许加运行时条件决定写不写某个字段
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer) {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is NihilityStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            writer.Write(NPC.rotation);
            writer.Write(Context.RotSpeed);
            writer.Write(Context.Num2);

            writer.Write(Context.Num3);
            writer.Write(Context.Nz.X);
            writer.Write(Context.Nz.Y);
            writer.Write(Context.Num1);
            writer.Write(spawnAnm);

            writer.Write(cellIndex);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is NihilityStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            NPC.rotation = reader.ReadSingle();
            Context.RotSpeed = reader.ReadSingle();
            //Num2 走容差是因为客户端「唯一会读它的地方」是两手冲刺的子计时(20 倒数到 -30、每帧 -1、
            //为正即推进窗),那是范围 50 的正经帧计数;一阶段 6 号招拿同一个槽存环射基准角,
            //但那个角的全部读取点都在 IsServer 里,容差吃不到客户端要用的东西。
            //哪天有状态让客户端读这个槽里的角度,就必须把角度拆成独立字段直取——
            //±2 rad 是 114°,带着容差等于永远不纠正(CEBossNetAdopt 守则第 5 条:一个槽只准一种语义)
            Context.Num2 = CEBossNetAdopt.AdoptFrameCounter(Context.Num2, reader.ReadSingle());

            Context.Num3 = reader.ReadSingle();
            float nzX = reader.ReadSingle();
            float nzY = reader.ReadSingle();
            Context.Nz = new Vector2(nzX, nzY);
            //Num1 是帧计数:能量球、激光、对撞都靠 `Num1 == N` 的等值判定起拍,硬对齐会跳过或重放
            Context.Num1 = CEBossNetAdopt.AdoptFrameCounter(Context.Num1, reader.ReadInt32());
            spawnAnm = reader.ReadInt32();

            cellIndex = reader.ReadInt32();
            if (cellIndex >= 0) {
                NPC candidate = cellIndex.ToNPC();
                cell = candidate != null && candidate.active && candidate.ModNPC is ChaoticCell ? candidate : null;
            }
        }

        #endregion

        #region 掉落
        public override void OnKill() {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedNihilityTwin, -1);
            if (cell != null) {
                cell.StrikeInstantKill();
            }
        }

        public override void BossLoot(ref int potionType) {
            potionType = ModContent.ItemType<VoidHealingPotion>();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<NihilityTwinBag>()));

            // 深渊亡魂移除后,幽渊魂髓与深渊书签改由本 Boss 承接(数量与概率照搬旧掉落表);
            // 与旧主人一样不挂 NotExpert,专家模式下也照常掉,不进宝袋
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WraithSoulEssence>(), 1, 15, 25));
            if (!CERef.Has) {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkAbyss>(), 2));
            }

            // 月后虚空治疗药水,数量沿用原至尊档 5-15;按人掉落并隐藏图鉴条目
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<VoidHealingPotion>(), 1, 5, 15, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            {
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<NihilityShell>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<Voidseeker>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<EventideSniper>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<StarlessNight>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<NihilityBacteriophageWand>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<VoidPathology>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<NihilityFragments>(), 1, 18, 24));
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ChaoticPiece>(), 1, 18, 24));
            }
            npcLoot.Add(normalOnly);
            // 遗物:原灾厄复仇/大师条件对齐原版大师掉落惯例(difficulty-map)
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<NihilityTwinRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NihilityTwinTrophy>(), 10));

            // 首杀传记:承接原灾厄按人实例掉落语义
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<NihilityTwinLore>(), 1, 1, 1, new LoreFirstKill()));
        }

        // 恒真但隐藏图鉴条目的条件:对应原 hideLootReport 语义
        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }

        // 首杀传记条件:对应 downed 旗标未置位时每名玩家各掉一份
        private class LoreFirstKill : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedNihilityTwin;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
        #endregion
    }
}
