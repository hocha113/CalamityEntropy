using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using CalamityEntropy.Content.NPCs.SpiritFountain.States;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.AI;
using InnoVault;
using InnoVault.PRT;
using InnoVault.StateMachines;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SpiritFountain
{
    /// <summary>
    /// 一根魂柱。<c>offset</c> / <c>rotation</c> / <c>alpha</c> / <c>Num</c> 参与判定(魂环的位置、
    /// 魂乱判定线),随 <c>SendExtraAI</c> 过线;<c>trailDrawOffset</c> 与 <c>scale</c> 只喂绘制
    /// </summary>
    public class FountainColumn
    {
        public Vector2 offset = Vector2.Zero;
        public float rotation = -MathHelper.PiOver2;
        public float trailDrawOffset = 0;
        public float alpha = 1;
        public float scale = 1;
        public float Num = 0;
        public int id = 0;
        public FountainColumn(float a) {
            alpha = a;
        }
        public Vector2 GetPointAtMe(float poffset) {
            return offset + rotation.ToRotationVector2() * poffset;
        }
    }

    /// <summary>
    /// 冥魂泉:InnoVault 状态机宿主。
    /// <para>
    /// 本体<b>全程不动</b>——整条 AI 一次都没写过 <c>velocity</c>,位置只在出场首帧落一次。
    /// 它的「运动」全在两根魂柱的偏移与倾角上,而魂柱又直接决定 <see cref="SpiritRing"/> 的位置,
    /// 所以柱子的四个量是这只 Boss 真正的运动状态,必须过线。
    /// </para>
    /// <para>
    /// 宿主按固定顺序落地:序幕(阶段/难度/魂乱判定/计数/粒子)→ 血量触发的转阶段插入 →
    /// 状态机同帧续跑链 → 尾声(摇摆清零/脱战/眼睛插值)。
    /// 数值在 <see cref="SpiritFountainDirector"/>,链序在 <see cref="SpiritFountainRotation"/>,
    /// 绘制在 SpiritFountain.Draw.cs。
    /// </para>
    /// <para>
    /// 联机:转移只在权威端(状态号 ai[3],阶段 ai[2]);各端跑同一套柱子数学;
    /// 柱子四量、摇摆相位/幅度、回旋进度、全局计数与聚魂倒计时随 SendExtraAI 过线,客户端带容差收养计时。
    /// 弹幕与魂环只在权威端生成,骰点只在权威端骰、结果过线。
    /// </para>
    /// </summary>
    [AutoloadBossHead]
    public partial class SpiritFountain : ModNPC
    {
        #region 字段
        private NpcStateMachine<SpiritFountainStateContext> stateMachine;
        public SpiritFountainStateContext Context { get; private set; }
        /// <summary>只用它的计时通道:本体不动,位置预测纠偏对它没有意义,所以从不调 BeginFrame / EndFrame</summary>
        private readonly CEBossNetMotion netMotion = new();

        public FountainColumn column1 = new FountainColumn(0) { id = 0 };
        public FountainColumn column2 = new FountainColumn(0) { id = 1 };

        /// <summary>魂环数量,按难度与世界种子在 <c>SetDefaults</c> 里算一次</summary>
        public int SpiritCount = SpiritFountainDirector.SpiritCountBase;

        /// <summary>
        /// 清场倒计时。原代码每帧自减却<b>从来没有任何地方把它置正</b>,所以它恒为非正、
        /// <c>&gt; 0</c> 的分支(魂环重置、弹幕自杀)在当前版本里到不了。照搬保留,
        /// 它是 <see cref="SpiritRing"/> 与 <c>SpiritBullet</c> 的对外可达字段
        /// </summary>
        public int ClearMyProjs = 0;
        #endregion

        #region 对外可达面(魂环与弹幕从这里读)
        /// <summary>当前状态号。原 <c>AIStyle ai</c> 字段现在由 ai[3] 直接推导,所以客户端不用额外过线就能读到</summary>
        public SpiritFountainStateIndex ai => (SpiritFountainStateIndex)(int)NPC.ai[3];

        /// <summary>
        /// 当前状态体本帧读到的计时,等价于原 <c>aiTimer</c>。
        /// 读的是 <see cref="SpiritFountainStateBase.BodyTimer"/> 而不是 <c>Timer</c>:
        /// 基类的自增排在状态体之后,魂环要的是自增前的值
        /// </summary>
        public int aiTimer => (stateMachine?.CurrentState as SpiritFountainStateBase)?.BodyTimer ?? 0;

        /// <summary>全局帧计数,永不归零。原 <c>Counter</c></summary>
        public float Counter => Context?.GlobalCounter ?? 0f;

        /// <summary>回旋段的进度量。原 <c>num1</c>,魂环拿自己的 Index 跟它比大小</summary>
        public float num1 => Context?.Num1 ?? 0f;

        /// <summary>阶段号 1~7,映射 ai[2]。原 <c>phase</c></summary>
        public int phase => Context?.Phase ?? 1;

        /// <summary>难度系数。原 <c>enrage</c></summary>
        public float enrage => Context?.Enrage ?? 1f;

        /// <summary>转阶段演出期间魂环整体免伤。原 <c>DontTakeDmg</c></summary>
        public bool DontTakeDmg => Context?.DontTakeDmg ?? false;

        /// <summary>
        /// 生成敌对弹幕。伤害是 <c>NPC.damage / 6</c> 的<b>整数除法</b>再乘倍率,击退 3,owner 传 -1。
        /// 守卫留在这一处,与原代码同一位置,魂环也从这里出手
        /// </summary>
        public void Shoot(int type, Vector2 pos, Vector2 velo, float damageMult = 1, float ai0 = 0, float ai1 = 0, float ai2 = 0) {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, velo, type,
                    (int)(NPC.damage / SpiritFountainDirector.ProjDamageDivisor * damageMult),
                    SpiritFountainDirector.ProjKnockback, -1, ai0, ai1, ai2);
            }
        }
        #endregion

        #region 定义
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            // 图鉴隐藏:原灾厄隐藏扩展的原版等价写法
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            //本体不动,原版平滑本来就没东西可摊;关掉是为了让本体和魂环读同一个(清零的)平滑层级
            NPCID.Sets.NoMultiplayerSmoothingByType[NPC.type] = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
        }

        public override void SetDefaults() {
            NPC.boss = true;
            //状态机把状态号写在 ai[3],原版 AI 必须让位
            NPC.aiStyle = -1;
            NPC.width = SpiritFountainDirector.BodySize;
            NPC.height = SpiritFountainDirector.BodySize;
            NPC.damage = SpiritFountainDirector.BaseDamage;
            if (Main.expertMode) {
                NPC.damage += SpiritFountainDirector.DamageExpertBonus;
            }
            if (Main.masterMode) {
                NPC.damage += SpiritFountainDirector.DamageMasterBonus;
            }
            SpiritCount = SpiritFountainDirector.SpiritCount();
            NPC.buffImmune[ModContent.BuffType<SoulDisorder>()] = true;
            NPC.defense = 0;
            NPC.lifeMax = SpiritFountainDirector.LifeMax;
            NPC.HitSound = SoundID.NPCHit11;
            NPC.DeathSound = SoundID.NPCDeath11;
            NPC.value = SpiritFountainDirector.Value;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = SpiritFountainDirector.VoidTouchDR;
            NPC.dontCountMe = true;
            NPC.scale = 1f;
            NPC.timeLeft *= SpiritFountainDirector.TimeLeftMul;
            if (Main.masterMode) {
                NPC.scale = SpiritFountainDirector.MasterScale;
            }
            NPC.netAlways = true;
            NPC.Entropy().damageMul = SpiritFountainDirector.DamageMul;
            if (!Main.dedServ) {
                Music = MusicID.OtherworldlyTowers;
            }
        }
        #endregion

        #region 掉落
        public override void BossLoot(ref int potionType) {
            potionType = ModContent.ItemType<VoidHealingPotion>();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            // 月后虚空治疗药水,数量沿用原欧米茄档 8-23;隐藏图鉴条目
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<VoidHealingPotion>(), 1, 8, 23, new HiddenDropCondition()));
        }

        // 恒真但隐藏图鉴条目的条件:对应原 hideLootReport 语义
        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }

        public override void OnKill() {
            //NPC.SetEventFlagCleared(ref EDownedBosses.downedCruiser, -1);
        }
        #endregion

        #region 判定开关
        public override bool CheckActive() => false;

        public override bool CheckDead() => true;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override bool CanHitNPC(NPC target) => false;

        public override bool? CanBeHitByItem(Player player, Item item) => null;

        public override bool? CanBeHitByProjectile(Projectile projectile) => null;

        public override bool CanBeHitByNPC(NPC attacker) => false;

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox) => false;
        #endregion

        #region 状态机装配
        private void EnsureContext() {
            Context ??= new SpiritFountainStateContext {
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
            stateMachine = new NpcStateMachine<SpiritFountainStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            IVaultState<SpiritFountainStateContext> initial = null;
            if (VaultUtils.isClient) {
                //中途加入的客户端从 ai[3] 重建当前状态,不要默认回出场演出
                initial = VaultStateRegistry<SpiritFountainStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new SpiritFountainSpawnAnimationState());
        }
        #endregion

        #region 魂环生成
        /// <summary>
        /// 放出一整圈魂环。<paramref name="columnId"/> 决定它们挂在哪根柱子上(写进魂环的 ai[2])。
        /// 只在权威端生成,并逐个补 SyncNPC——原代码就是这样写的
        /// </summary>
        public void SpawnRingSet(int columnId) {
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }
            for (int i = 0; i < SpiritCount; i++) {
                int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y,
                    ModContent.NPCType<SpiritRing>(), 0, NPC.whoAmI, float.Lerp(-1, 1, i / (float)(SpiritCount - 1)), columnId);
                if (Main.netMode == NetmodeID.Server) {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, idx);
                    Main.npc[idx].netUpdate = true;
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
            //本体不做速度积分,所以不进 CEBossNetMotion 的预测纠偏器,只清原版平滑:
            //本体与魂环这样才读同一个(清零的)平滑层级,贴图不会错位
            CEBossHost.RunAnchoredPartFrame(NPC);
            if (client) {
                CEBossHost.AdoptTimingAtFrameStart(netMotion, stateMachine);
            }

            RunPrologue();
            EvaluatePhaseTransition();
            RunStateChain();
            RunEpilogue();

            if (!client) {
                CEBossHost.Heartbeat(NPC);
            }
        }

        /// <summary>原 AI() 开头那一段全局账,顺序逐行对齐</summary>
        private void RunPrologue() {
            //阶段每帧按血量比例重算,是确定性推导量:血量本身走原版同步,各端同值
            Context.Phase = SpiritFountainDirector.PhaseFor(NPC);
            ClearMyProjs--;
            Context.Enrage = SpiritFountainDirector.Enrage();
            ApplyColumnSoulDisorder();
            NPC.dontTakeDamage = true;
            Context.GlobalCounter++;
            //原代码的 aiTimer++ 在这个位置,现在由状态基类在状态体之后自增,时序等价
            column1.trailDrawOffset += Context.FountainSpeed;
            column2.trailDrawOffset += Context.FountainSpeed;
            Context.BeginFrameDefaults();
            NPC.TargetClosest();
            UpdateContextFacts();
            SpawnColumnParticles();
        }

        private void UpdateContextFacts() {
            Context.Npc = NPC;
            Context.Owner = this;
            Player target = NPC.HasValidTarget ? Main.player[NPC.target] : null;
            Context.Target = target;
            Context.TargetValid = target != null && target.active && !target.dead;
        }

        /// <summary>柱子是一条贯穿全屏的判定线,站上去就持续挂魂乱。原代码只在非专用服务端判定</summary>
        private void ApplyColumnSoulDisorder() {
            if (Main.netMode == NetmodeID.Server) {
                return;
            }
            int width = (int)(SpiritFountainDirector.ColumnBuffLineWidth * NPC.scale);
            foreach (Player plr in Main.ActivePlayers) {
                if (column1.alpha > SpiritFountainDirector.ColumnBuffAlphaGate
                    && CEUtils.LineThroughRect(NPC.Center + column1.GetPointAtMe(-SpiritFountainDirector.ColumnBuffLineHalfLength),
                        NPC.Center + column1.GetPointAtMe(SpiritFountainDirector.ColumnBuffLineHalfLength), plr.getRect(), width)) {
                    plr.AddBuff(ModContent.BuffType<SoulDisorder>(), SpiritFountainDirector.ColumnBuffDuration);
                }
                if (column2.alpha > SpiritFountainDirector.ColumnBuffAlphaGate
                    && CEUtils.LineThroughRect(NPC.Center + column2.GetPointAtMe(-SpiritFountainDirector.ColumnBuffLineHalfLength),
                        NPC.Center + column2.GetPointAtMe(SpiritFountainDirector.ColumnBuffLineHalfLength), plr.getRect(), width)) {
                    plr.AddBuff(ModContent.BuffType<SoulDisorder>(), SpiritFountainDirector.ColumnBuffDuration);
                }
            }
        }

        /// <summary>双柱烟雾。90× 双柱密度很高,3200px 屏心裁剪 + 50% 随机省一半 spawn</summary>
        private void SpawnColumnParticles() {
            if (Main.dedServ) {
                return;
            }
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight).Half();
            for (int i = 0; i < SpiritFountainDirector.ColumnSmokeLoop; i++) {
                if (column1.alpha > 0) {
                    SpawnOneColumnParticle(column1, SpiritFountainDirector.ColumnSmokeAcross1, screenCenter);
                }
                if (column2.alpha > 0) {
                    SpawnOneColumnParticle(column2, SpiritFountainDirector.ColumnSmokeAcross2, screenCenter);
                }
            }
        }

        private void SpawnOneColumnParticle(FountainColumn column, float across, Vector2 screenCenter) {
            Vector2 pos = NPC.Center + column.offset
                + column.rotation.ToRotationVector2() * Main.rand.NextFloat(-SpiritFountainDirector.ColumnSmokeAlong, SpiritFountainDirector.ColumnSmokeAlong)
                + column.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-across, across) * column.scale;
            if (Vector2.Distance(pos, screenCenter) >= SpiritFountainDirector.ColumnSmokeCullRadius) {
                return;
            }
            if (Main.rand.NextBool()) {
                PRT_Smoke smoke = PRTLoader.NewParticle<PRT_Smoke>(pos, Vector2.Zero,
                    Color.AliceBlue * column.alpha * SpiritFountainDirector.ColumnSmokeColorMul,
                    Main.rand.NextFloat(SpiritFountainDirector.ColumnSmokeScaleMin, SpiritFountainDirector.ColumnSmokeScaleMax));
                smoke.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), SpiritFountainDirector.ColumnSmokeLife);
            }
            else {
                PRTLoader.NewParticle<PRT_GlowLightParticle>(pos, CEUtils.randomPointInCircle(SpiritFountainDirector.ColumnGlowVelRadius),
                    Color.AliceBlue * column.alpha * SpiritFountainDirector.ColumnGlowColorMul,
                    Main.rand.NextFloat(SpiritFountainDirector.ColumnGlowScaleMin, SpiritFountainDirector.ColumnGlowScaleMax))
                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), SpiritFountainDirector.ColumnGlowLife);
            }
        }

        /// <summary>
        /// 血量驱动的一次性插入件:阶段号超过 3(血量跌破 66%)就无条件切进转阶段演出,不等当前招打完。
        /// <para>
        /// 它在原 AI() 里的位置是出场演出块之后、横扫块之前,所以切出去的转阶段演出会在<b>同一帧</b>接着跑,
        /// 由后面的 <see cref="RunStateChain"/> 自然完成。
        /// </para>
        /// <para>一次性闸与柱子归位各端都做(只是记账与已过线量的本地推导),换态与魂环生成只在权威端。</para>
        /// </summary>
        private void EvaluatePhaseTransition() {
            if (Context.Phase <= SpiritFountainDirector.PhaseTransThreshold || !Context.SpawnSpirits2) {
                return;
            }
            Context.SpawnSpirits2 = false;
            Context.CenterRing = (int)Math.Ceiling(SpiritCount / 2f);
            column2.rotation = 0;
            if (VaultUtils.isClient) {
                return;
            }
            SpawnRingSet(1);
            //决策点:转阶段。ChangeState 本身会经 AiSlotNetSync 置 netUpdate,这里写出来是为了让决策显式可读
            stateMachine.ChangeState(new SpiritFountainPhaseTranse1State());
            NPC.netUpdate = true;
        }

        /// <summary>
        /// 同帧续跑链。
        /// <para>
        /// 原 AI() 的七个状态块是自上而下的顺序 <c>if</c>(不是 <c>else if</c>),所以在第 k 块里换到
        /// <b>更靠后</b>的状态时,新块会在同一帧接着跑、且读到的 <c>aiTimer</c> 是 0;
        /// 换回<b>更靠前</b>的状态(落环喷泉 → 横扫)则要等下一帧,而那一帧开头的统一自增
        /// 会让它从 1 起跑。两种情形都要还原,否则整段节拍会整体偏一帧。
        /// </para>
        /// <para>
        /// 客户端不走这条链:它的换态来自 ai[3],<see cref="VaultStateMachine{TContext}.Update"/>
        /// 在被动换态之后本来就会立刻跑一次新状态体,再续跑一次就成了一帧双跑。
        /// </para>
        /// </summary>
        private void RunStateChain() {
            if (VaultUtils.isClient) {
                stateMachine.Update();
                return;
            }
            for (int step = 0; step <= SpiritFountainDirector.MaxChainStepsPerFrame; step++) {
                IVaultState<SpiritFountainStateContext> before = stateMachine.CurrentState;
                int beforeOrder = SpiritFountainRotation.ChainOrder(before);
                stateMachine.Update();
                IVaultState<SpiritFountainStateContext> after = stateMachine.CurrentState;
                if (ReferenceEquals(before, after)) {
                    return;
                }
                if (SpiritFountainRotation.ChainOrder(after) > beforeOrder) {
                    continue;
                }
                if (after is SpiritFountainStateBase deferred) {
                    deferred.AdoptDeferredEntry();
                }
                return;
            }
        }

        /// <summary>
        /// 原 AI() 末尾那一段:挂在横扫块上的摇摆清零、脱战判定、眼睛插值。
        /// 聚魂期间原代码在这之前就 <c>return</c> 了,所以这三样都要一起跳过
        /// </summary>
        private void RunEpilogue() {
            if (Context.StareAtLocalPlayer) {
                Context.StarePoint = Main.LocalPlayer.Center;
            }
            if (Context.HaltFrame) {
                return;
            }
            if (!Context.KeepMovingSway) {
                Context.MCounter = 0;
                Context.MAmp = 0;
            }

            NPC.localAI[2] = NPC.HasValidTarget ? 0 : NPC.localAI[2] + 1;
            if (NPC.localAI[2] > SpiritFountainDirector.DeactiveFrames
                || (NPC.HasValidTarget && !NPC.target.ToPlayer().Center
                        .getRectCentered(SpiritFountainDirector.DespawnPlayerBox, SpiritFountainDirector.DespawnPlayerBox)
                        .Intersects(NPC.Center.getRectCentered(SpiritFountainDirector.DespawnBoxTiles * 16, SpiritFountainDirector.DespawnBoxTiles * 16)))) {
                //消失是世界写入:只在权威端做并立刻发包。原代码各端各自 active = false,
                //客户端那一份会把 Boss 从自己屏幕上抹掉却没人告诉服务端
                if (!VaultUtils.isClient) {
                    NPC.active = false;
                    NPC.netUpdate = true;
                }
            }
            Context.EyeAlpha = float.Lerp(Context.EyeAlpha, Context.EyeAlphaTarget, SpiritFountainDirector.EyeAlphaLerp);
        }

        #region 同步
        /// <summary>
        /// 定长块,顺序固定在这一处。先计时,再持久累加量(两根柱子的四个量 + 摇摆相位/幅度),
        /// 最后状态标量。字节数是编译期常量:不许加运行时条件决定写不写某个字段。
        /// <para>
        /// 原版 <c>SyncNPC</c> 不带 <c>NPC.rotation</c>,不过这只 Boss 的本体 rotation 从头到尾没被写过,
        /// 真正要过线的是<b>柱子的</b> rotation
        /// </para>
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer) {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is SpiritFountainStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            writer.Write(column1.offset.X);
            writer.Write(column1.offset.Y);
            writer.Write(column1.rotation);
            writer.Write(column1.alpha);
            writer.Write(column1.Num);
            writer.Write(column2.offset.X);
            writer.Write(column2.offset.Y);
            writer.Write(column2.rotation);
            writer.Write(column2.alpha);
            writer.Write(column2.Num);
            writer.Write(Context.MCounter);
            writer.Write(Context.MAmp);

            writer.Write(Context.GlobalCounter);
            writer.Write(Context.Num1);
            writer.Write(Context.GatheringAnimation);
            writer.Write(Context.SpawnSpirits);
            writer.Write(Context.SpawnSpirits2);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is SpiritFountainStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            column1.offset.X = reader.ReadSingle();
            column1.offset.Y = reader.ReadSingle();
            column1.rotation = reader.ReadSingle();
            column1.alpha = reader.ReadSingle();
            column1.Num = reader.ReadSingle();
            column2.offset.X = reader.ReadSingle();
            column2.offset.Y = reader.ReadSingle();
            column2.rotation = reader.ReadSingle();
            column2.alpha = reader.ReadSingle();
            column2.Num = reader.ReadSingle();
            Context.MCounter = reader.ReadSingle();
            Context.MAmp = reader.ReadSingle();

            Context.GlobalCounter = AdoptScalar(Context.GlobalCounter, reader.ReadSingle());
            //回旋进度不走容差:它整段只涨到 1.66,±2 的帧计数口径等于永远不纠正,
            //而魂环脱柱与否直接读它,必须认权威端
            Context.Num1 = reader.ReadSingle();
            Context.GatheringAnimation = (int)AdoptScalar(Context.GatheringAnimation, reader.ReadInt32());
            Context.SpawnSpirits = reader.ReadBoolean();
            Context.SpawnSpirits2 = reader.ReadBoolean();
        }

        /// <summary>标量当帧计数用:容差内不动,对齐 AdoptTimer 的口径</summary>
        private static float AdoptScalar(float local, float synced) {
            return Math.Abs(synced - local) > CEBossNetMotion.TimerTolerance ? synced : local;
        }
        #endregion
    }
}
