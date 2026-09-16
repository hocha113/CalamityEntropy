using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using CalamityEntropy.Content.NPCs.LuminarisMoth.States;
using CalamityEntropy.Core.AI;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Utilities;
using InnoVault;
using InnoVault.StateMachines;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth
{
    /// <summary>
    /// 月华之蛾:夜间 Boss,InnoVault 状态机宿主。
    /// 状态只写运动与声明,宿主按固定顺序落地:生成/从属 → 冻结门 → 逐帧杂项 → 目标校验 → 状态机 → 尾巴骨架 → 尾迹采样。
    /// <para>
    /// 联机:状态号走 <c>ai[3]</c>、阶段走 <c>ai[2]</c>,选招与骰点只在权威端;
    /// 各端跑同一套运动数学;出招倒计时、朝向、两个位置锚点与三个标量随 <c>SendExtraAI</c> 过线,
    /// 客户端带容差收养。弹幕只在权威端生成。
    /// </para>
    /// <para>
    /// 数值在 <see cref="LuminarisDirector"/>,选招在 <see cref="LuminarisRotation"/>,绘制在 Luminaris.Draw.cs
    /// </para>
    /// </summary>
    [AutoloadBossHead]
    public partial class Luminaris : ModNPC
    {
        #region 字段
        private NpcStateMachine<LuminarisStateContext> stateMachine;
        public LuminarisStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;

        /// <summary>动画帧计时(原 <c>frameCounter</c>)。纯绘制,不过线</summary>
        public int frameCounter = 0;

        /// <summary>生成后的冻结倒数(原 <c>SD</c>),自减到 0 之前 AI 直接返回</summary>
        public int SD = LuminarisDirector.SpawnFreezeFrames;

        /// <summary>只在生成那一帧为真(原 <c>SpawnFlag</c>),用来触发天顶世界的分身生成</summary>
        public bool SpawnFlag = true;

        /// <summary>脱战倒计时(原 <c>deactiveCount</c>),接战时重置成 150,归零即消失</summary>
        public int deactiveCount = LuminarisDirector.DeactiveFramesInitial;

        /// <summary>两条尾巴的绳模拟。纯绘制,各端本地推进</summary>
        public Rope tail1 = null;
        public Rope tail2 = null;
        #endregion

        #region 定义
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = "CalamityEntropy/Assets/BCL/LuminarisBossCheckList",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -4
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //Dashing 稳态 40 px/f,且多数招式每帧硬写 Center;原版 netOffset 平滑会把这些位移当成快照误差
            //一点点放,拖出来回锯齿。关掉后由 CEBossNetMotion 接管纠偏
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                // 召唤条件仅剩夜晚,图鉴不再挂发光蘑菇群系标签(biome-map)
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.LuminarisBestiary")
            });
        }

        public override void SetDefaults() {
            NPC.boss = true;
            //状态机把状态号写在 ai[3],必须让原版 AI 彻底不碰 ai 槽
            NPC.aiStyle = -1;
            NPC.width = 96;
            NPC.height = 96;
            NPC.damage = 68;
            NPC.defense = 10;
            NPC.lifeMax = 28000;
            NPC.HitSound = SoundID.NPCHit32;
            NPC.DeathSound = SoundID.NPCDeath22;
            NPC.value = 1600f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 4;
            if (!Main.dedServ) {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/LuminarisBoss");
            }
            if (Main.zenithWorld) {
                NPC.scale *= 0.5f;
            }
            // 原灾厄星辉瘟疫群系归属删除,召唤条件改发光蘑菇群系夜晚(biome-map,IllusionaryDew 侧)
        }

        // 原灾厄全局 DR=0.1 的本地等效(BossRush 加成随事件裁撤);公有字段供血条等外部读取
        public float DamageReduction = 0.1f;
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 1f - DamageReduction;
        }
        #endregion

        #region 状态机装配
        private void EnsureContext() {
            Context ??= new LuminarisStateContext {
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
            stateMachine = new NpcStateMachine<LuminarisStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            IVaultState<LuminarisStateContext> initial = null;
            if (VaultUtils.isClient) {
                initial = VaultStateRegistry<LuminarisStateContext>.Create((int)NPC.ai[3]);
            }
            //原代码 `ai` 字段的初值就是 RoundShooting,且第一手不经选招:
            //倒计时的初值(非天顶 0、天顶分身 210~270)直接被这一招读走
            stateMachine.SetInitialState(initial ?? new LuminarisRoundShootingState());
        }

        /// <summary>
        /// 天顶世界生成分身时对<b>另一只</b> Luminaris 的倒计时写入。
        /// 原代码是 <c>((Luminaris)n.ToNPC().ModNPC).AIChangeCounter = Main.rand.Next(210, 270);</c>,
        /// 保留这条跨实例写入;调用点只在权威端,被写的那一只靠自己的 ExtraAI 把新值带给客户端。
        /// <para>
        /// 传进来的是<b>原字段语义</b>的值(自减前),而上下文存的是「招式体读到的值」,所以要减 1,
        /// 见 <see cref="LuminarisStateContext.Countdown"/> 的相位换算说明
        /// </para>
        /// </summary>
        public void SetSpawnCountdown(int value) {
            EnsureContext();
            Context.Countdown = value - 1;
        }
        #endregion

        #region 生成与从属
        /// <summary>
        /// 天顶世界的分身生成。只有本体(<c>realLife &lt; 0</c>)会生成,分身自己不再递归。
        /// 生成与骰点都在权威端,分身的倒计时被骰成 210~270 用来错开彼此的第一手
        /// </summary>
        private void SpawnZenithSwarm() {
            if (!SpawnFlag) {
                return;
            }
            if (Main.zenithWorld && Main.netMode != NetmodeID.MultiplayerClient) {
                if (NPC.realLife < 0) {
                    for (int i = 0; i < LuminarisDirector.ZenithCloneCount; i++) {
                        int n = NPC.NewNPC(NPC.GetSource_FromAI(),
                            (int)NPC.Center.X + Main.rand.Next(-LuminarisDirector.ZenithCloneSpreadX, LuminarisDirector.ZenithCloneSpreadX),
                            (int)NPC.Center.Y - LuminarisDirector.ZenithCloneOffsetY, NPC.type);
                        n.ToNPC().realLife = NPC.whoAmI;
                        n.ToNPC().netUpdate = true;
                        if (n.ToNPC().ModNPC is Luminaris clone) {
                            clone.SetSpawnCountdown(Main.rand.Next(LuminarisDirector.ZenithCloneCountdownMin, LuminarisDirector.ZenithCloneCountdownMax));
                        }
                    }
                    SetSpawnCountdown(Main.rand.Next(LuminarisDirector.ZenithCloneCountdownMin, LuminarisDirector.ZenithCloneCountdownMax));
                }
            }
        }

        /// <summary>
        /// realLife 从属:分身的血量与存活跟着本体。
        /// 两端都跑——它读的是已同步的 <c>本体.life</c> / <c>本体.active</c>,是确定性镜像;
        /// 收进权威端反而会让客户端上的分身血条一直停在旧值
        /// </summary>
        private void FollowRealLife() {
            if (NPC.realLife >= 0) {
                if (!NPC.realLife.ToNPC().active) {
                    NPC.active = false;
                }
                else {
                    NPC.life = NPC.realLife.ToNPC().life;
                    NPC.boss = false;
                }
            }
        }

        public override bool CheckDead() {
            if (NPC.realLife >= 0 && NPC.realLife.ToNPC().active) {
                NPC.life = NPC.realLife.ToNPC().life;
                return false;
            }
            return true;
        }
        #endregion

        public override void AI() {
            EnsureContext();
            if (stateMachine == null) {
                InitializeStateMachine();
            }

            //原 AI() 开头:分身生成与 realLife 从属都在冻结门之前,所以冻结期间血量镜像照样走
            SpawnZenithSwarm();
            FollowRealLife();
            SpawnFlag = false;
            if (SD-- > 0) {
                //冻结期不进纠偏器:没有本地预测,第一个快照会直接认服务端位置
                return;
            }

            bool client = VaultUtils.isClient;
            if (client) {
                netMotion.BeginFrame(NPC);
                CEBossHost.AdoptTimingAtFrameStart(netMotion, stateMachine);
            }

            //原 AI() 中段的逐帧杂项,顺序照搬
            if (Context.MegaTrail > 0) {
                Context.MegaTrail -= LuminarisDirector.MegaTrailDecay;
            }
            if (Context.OldPos == Vector2.Zero) {
                Context.OldPos = NPC.Center;
            }
            frameCounter++;
            if (Context.AfterImageTime > 0) {
                Context.AfterImageTime--;
            }
            EnsureTails();
            //整数除法 lifeMax / 2,单向不回退。原代码每帧无条件重写 phase,这里只在真的翻档时写一次并发包
            if (NPC.life <= NPC.lifeMax / LuminarisDirector.Phase2LifeDivisor && Context.Phase != 2) {
                Context.Phase = 2;
                if (!client) {
                    //决策点:转阶段
                    NPC.netUpdate = true;
                }
            }

            FindTarget();
            UpdateContextFacts();

            Context.BeginFrameDefaults();
            //脱战也要走 Update:客户端靠这里的 NetSync 收换态包。
            //状态体本身见 RequiresTarget,没目标不跑——于是出招倒计时在脱战期间冻结,对齐原代码
            //(原代码把自减写在 AttackPlayer 里,而 AttackPlayer 只在有目标时才调)
            stateMachine.Update();
            if (Context.TargetValid) {
                deactiveCount = LuminarisDirector.DeactiveFrames;
            }
            else {
                //脱战期间把基类计时按回 0(两端都做,所以收养口径不变)。
                //本 Boss 的节拍一律读出招倒计时,Timer 只是收养与超时通道,归零不影响任何一拍;
                //不归零的话反复丢失目标会让 Counter 一直涨,最后撞上那条新加的超时安全网
                if (stateMachine.CurrentState is CEBossStateBase<LuminarisStateContext> timed) {
                    timed.ResetTiming();
                }
                UpdateDisengageMotion();
                deactiveCount--;
                if (deactiveCount <= 0 && !client) {
                    //脱战倒计时是纯本地量,客户端自己置 active=false 会造出幽灵;收归权威端
                    NPC.active = false;
                    NPC.netUpdate = true;
                }
            }

            UpdateTails();
            Context.OldPos = NPC.Center;
            PushTrail();

            if (client) {
                netMotion.EndFrame(NPC);
            }
            else {
                CEBossHost.Heartbeat(NPC);
            }
        }

        /// <summary>原代码只在失去目标时才重新索敌,且用的是默认的 <c>faceTarget: true</c>(会改 direction)</summary>
        private void FindTarget() {
            if (!NPC.HasValidTarget) {
                NPC.TargetClosest();
            }
            targetPlayer = NPC.HasValidTarget ? Main.player[NPC.target] : null;
        }

        private void UpdateContextFacts() {
            Context.Npc = NPC;
            Context.Owner = this;
            Context.Target = targetPlayer;
            //原代码的接战条件只有 HasValidTarget,没有距离门
            Context.TargetValid = NPC.HasValidTarget;
            Context.Enrange = LuminarisDirector.Enrange();
        }

        /// <summary>脱战运动:阻尼 0.998 后每帧再向上 0.3,朝向掰正。各端都跑</summary>
        private void UpdateDisengageMotion() {
            NPC.velocity *= LuminarisDirector.DisengageDrag;
            NPC.velocity.Y -= LuminarisDirector.DisengageRise;
            NPC.rotation = 0;
        }

        #region 尾巴与尾迹
        private void EnsureTails() {
            if (tail1 == null || tail2 == null) {
                tail1 = new Rope(NPC.Center, LuminarisDirector.TailSegCount, LuminarisDirector.TailSegLength,
                    LuminarisDirector.TailInitGravity, LuminarisDirector.TailDamping, LuminarisDirector.TailAccuracy);
                tail2 = new Rope(NPC.Center, LuminarisDirector.TailSegCount, LuminarisDirector.TailSegLength,
                    LuminarisDirector.TailInitGravity, LuminarisDirector.TailDamping, LuminarisDirector.TailAccuracy);
            }
        }

        /// <summary>
        /// 尾巴一帧走 5 个子步:绳根沿「上一帧位置 → 本帧位置」插值推进,每步 <c>Update()</c> 一次。
        /// 构造用的重力是 0.14,逐帧改写成 0.12,原代码就是两个值
        /// </summary>
        private void UpdateTails() {
            for (float i = 0; i <= 1; i += LuminarisDirector.TailSampleStep) {
                Vector2 sample = NPC.velocity + Vector2.Lerp(Context.OldPos, NPC.Center, i);
                tail1.Start = sample + new Vector2(-LuminarisDirector.TailAnchorSide, LuminarisDirector.TailAnchorBack).RotatedBy(NPC.rotation) * NPC.scale;
                tail2.Start = sample + new Vector2(LuminarisDirector.TailAnchorSide, LuminarisDirector.TailAnchorBack).RotatedBy(NPC.rotation) * NPC.scale;
                tail1.gravity = LuminarisDirector.TailFrameGravity;
                tail2.gravity = LuminarisDirector.TailFrameGravity;
                tail1.Update();
                tail2.Update();
            }
        }

        /// <summary>
        /// 尾迹采样:每帧加一个点,上限 <c>24 + (int)MegaTrail × 16</c>。
        /// 裁剪循环跑三次是为了 MegaTrail 掉下来、上限缩短时能快点收敛
        /// </summary>
        private void PushTrail() {
            Context.Trail.Add(NPC.Center);
            int odMax = LuminarisDirector.TrailBaseLength + (int)Context.MegaTrail * LuminarisDirector.TrailPerMegaTrail;
            for (int i = 0; i < LuminarisDirector.TrailTrimPerFrame; i++) {
                if (Context.Trail.Count > odMax) {
                    Context.Trail.RemoveAt(0);
                }
            }
        }
        #endregion

        /// <summary>
        /// 接触伤害:定点绕转整段没有,高空砸落只在大尾迹亮着(也就是砸落段)时有。
        /// 两个判据分别是已过线的状态号与由「状态号 + 倒计时」确定性推导的 <c>MegaTrail</c>,所以各端一致
        /// </summary>
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            LuminarisStateIndex state = CurrentStateIndex;
            if (state == LuminarisStateIndex.RoundShooting) {
                return false;
            }
            if (state == LuminarisStateIndex.SmashDown && (Context?.MegaTrail ?? 0f) <= 0) {
                return false;
            }
            return true;
        }

        /// <summary>当前状态索引。状态机还没装好时回落到同步槽 <c>ai[3]</c></summary>
        private LuminarisStateIndex CurrentStateIndex
            => stateMachine?.CurrentState is LuminarisStateBase state ? state.StateIndex : (LuminarisStateIndex)(int)NPC.ai[3];

        #region 同步
        /// <summary>
        /// 定长块,顺序固定在这一处,两端肉眼可对照。
        /// 计时 → 持久累加量(朝向、出招倒计时)→ 状态标量(序号、两个锚点、三个标量)。
        /// 字节数是编译期常量(3×4 + 4 + 4 + 4 + 8 + 8 + 12 = 52),不许加运行时条件决定写不写某个字段。
        /// <para>
        /// <c>NPC.rotation</c> 必须在这里:Dashing 每帧把朝向烙进速度,而原版 <c>SyncNPC</c> 不带 rotation。
        /// 出招倒计时是本 Boss 的主时钟,全部节拍都读它,一旦分叉两端会走到完全不同的招式段落
        /// </para>
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer) {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is LuminarisStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);
            writer.Write(NPC.rotation);
            writer.Write(Context.Countdown);
            writer.Write(Context.AttackIndex);
            writer.WriteVector2(Context.Vec1);
            writer.WriteVector2(Context.Vec2);
            writer.Write(Context.Num1);
            writer.Write(Context.Num2);
            writer.Write(Context.Num3);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is LuminarisStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            NPC.rotation = reader.ReadSingle();
            //倒计时按帧计数收养:只差一两帧是网络抖动的常态,硬对齐会让 `倒计时 == N` 型一次性拍被跳过或重放
            Context.Countdown = CEBossNetMotion.AdoptTimer(Context.Countdown, reader.ReadInt32());
            Context.AttackIndex = reader.ReadInt32();
            Context.Vec1 = reader.ReadVector2();
            Context.Vec2 = reader.ReadVector2();
            Context.Num1 = AdoptScalar(Context.Num1, reader.ReadSingle());
            Context.Num2 = AdoptScalar(Context.Num2, reader.ReadSingle());
            Context.Num3 = AdoptScalar(Context.Num3, reader.ReadSingle());
        }

        /// <summary>标量当帧计数用:容差内不动,对齐 <see cref="CEBossNetMotion.AdoptTimer"/> 的口径</summary>
        private static float AdoptScalar(float local, float synced) {
            return System.Math.Abs(synced - local) > CEBossNetMotion.TimerTolerance ? synced : local;
        }
        #endregion

        #region 掉落
        public override void OnKill() {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedLuminaris, -1);
        }

        public override void BossLoot(ref int potionType) {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<LuminarisBag>()));
            if (!CERef.Has) {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkAstral>(), 3));
            }

            // 治疗药水按人 5-15 瓶,隐藏图鉴条目(承接原灾厄 PerPlayer 语义)
            npcLoot.Add(new DropPerPlayerOnThePlayer(ItemID.GreaterHealingPotion, 1, 5, 15, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            {
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<StarlitPiercer>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<Luminar>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<StarSootInjector>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<PhantomLightWing>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<BottledStarlightCocoon>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<LunarPlank>(), 5, 1, 1, 3));
                // 掉落自有化:灾厄星耀煤灰→星辉鳞尘,数量照搬(material-map §一)
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<StarlitScaleDust>(), 1, 42, 64));
            }
            npcLoot.Add(normalOnly);
            // 遗物:原灾厄复仇/大师条件对齐原版大师掉落惯例(difficulty-map)
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<LuminarisRelic>()));

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<LuminarisTrophy>(), 10));

            // 首杀传记:承接原灾厄按人实例掉落语义
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<LuminarisLore>(), 1, 1, 1, new LoreFirstKill()));
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
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedLuminaris;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
        #endregion
    }
}
