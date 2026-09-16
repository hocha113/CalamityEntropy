using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.Apsychos.Core;
using CalamityEntropy.Content.NPCs.Apsychos.States;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.AI;
using InnoVault;
using InnoVault.PRT;
using InnoVault.StateMachines;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    /// <summary>
    /// 焦渴:炼狱 Boss,InnoVault 状态机宿主。
    /// 状态只写声明,宿主按固定顺序落地:目标校验 → 全局转移 → 清声明 → 状态机 → 声明结算 → 尾巴骨架。
    /// 联机:转移只在权威端(状态号 ai[3],阶段 ai[2]);各端跑同一套运动数学;
    /// 计时与朝向等累加量随 SendExtraAI 过线,客户端带容差收养。弹幕只在权威端生成。
    /// 数值在 <see cref="ApsychosDirector"/>,轮换在 <see cref="ApsychosRotation"/>,绘制在 Apsychos.Draw.cs
    /// </summary>
    [AutoloadBossHead]
    public partial class Apsychos : ModNPC
    {
        #region 字段
        private NpcStateMachine<ApsychosStateContext> stateMachine;
        public ApsychosStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;
        private bool spawnFlag = true;
        private int deactiveCount = ApsychosDirector.DeactiveFrames;
        /// <summary>脱战中:不推进状态机,计时保持 0,重新接战时从接近起跑</summary>
        private bool disengaging;

        public int TailNPCIndex = -1;
        public NPC tail;
        public List<TailSeg> segs;

        /// <summary>虚拟骨节。盔甲尾(SmolderingHelmet)用 <c>using static</c> 取这个类型,不能挪走</summary>
        public class TailSeg
        {
            public Vector2 Center = Vector2.Zero;
            public float rotation = 0;
        }
        #endregion

        #region 定义
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                Scale = 0.44f,
                PortraitScale = 0.3f,
                CustomTexturePath = "CalamityEntropy/Assets/BCL/Apsychos",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -90
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //Dash 稳态远超 10 px/f,原版 netOffset 会让头和尾巴分家;关掉后纠偏器接管
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheUnderworld,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.ApsychosBestiary")
            });
        }

        public override void SetDefaults() {
            NPC.boss = true;
            NPC.aiStyle = -1;
            NPC.width = 156;
            NPC.height = 156;
            NPC.damage = 54;
            NPC.defense = 10;
            NPC.lifeMax = 10000;
            NPC.HitSound = null;
            NPC.DeathSound = SoundID.NPCDeath25;
            NPC.value = 1000f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 4;
            if (!Main.dedServ) {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Apsychos");
            }
            if (Main.getGoodWorld) {
                NPC.scale = 1.25f;
            }
            if (Main.zenithWorld) {
                NPC.scale = 0.7f;
            }
        }
        #endregion

        #region 状态机装配
        private void EnsureContext() {
            Context ??= new ApsychosStateContext {
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
            stateMachine = new NpcStateMachine<ApsychosStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            IVaultState<ApsychosStateContext> initial = null;
            if (VaultUtils.isClient) {
                initial = VaultStateRegistry<ApsychosStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new ApsychosMoveToTargetState());
        }
        #endregion

        #region 尾巴实体
        private void EnsureTail() {
            if (spawnFlag) {
                spawnFlag = false;
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    int index = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<ApsychosTail>(), 0, NPC.whoAmI);
                    TailNPCIndex = index;
                    tail = index.ToNPC();
                    NPC.netUpdate = true;
                    NPC.netSpam = 0;
                }
            }
            if (TailNPCIndex >= 0) {
                tail = TailNPCIndex.ToNPC();
            }
            if (tail == null) {
                spawnFlag = true;
                return;
            }
            if (!tail.active) {
                spawnFlag = true;
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

            EnsureTail();
            if (tail == null) {
                if (client) {
                    netMotion.EndFrame(NPC);
                }
                return;
            }

            FindTarget();
            UpdateContextFacts();
            EvaluateGlobalTransitions();
            Context.BeginFrameDefaults();
            //脱战也要走 Update:客户端靠这里的 NetSync 收到权威端切回接近。状态体本身见 RequiresTarget,没目标不跑
            stateMachine.Update();
            if (disengaging) {
                if (stateMachine.CurrentState is CEBossStateBase<ApsychosStateContext> state) {
                    state.ResetTiming();
                }
                UpdateDisengageMotion();
            }

            SettleDeclarations();
            UpdateTail();

            if (client) {
                netMotion.EndFrame(NPC);
            }
            else {
                CEBossHost.Heartbeat(NPC);
            }
        }

        private void FindTarget() {
            NPC.TargetClosest(false);
            targetPlayer = NPC.HasValidTarget ? Main.player[NPC.target] : null;
        }

        private void UpdateContextFacts() {
            Context.Npc = NPC;
            Context.Owner = this;
            Context.Target = targetPlayer;
            Context.Enrange = ApsychosDirector.Enrange();
            bool inRange = targetPlayer != null && targetPlayer.active && !targetPlayer.dead
                && NPC.Distance(targetPlayer.Center) < ApsychosDirector.DisengageDistance;
            Context.TargetValid = NPC.HasValidTarget && inRange;
            Context.TargetDistance = Context.TargetValid ? NPC.Distance(targetPlayer.Center) : 0f;
        }

        /// <summary>脱战只在这里处理。转阶段不打断当前招,等收招时由轮换表接走(对齐原 SetAIStyle)</summary>
        private void EvaluateGlobalTransitions() {
            if (Context.TargetValid) {
                deactiveCount = ApsychosDirector.DeactiveFrames;
                if (disengaging) {
                    disengaging = false;
                    if (stateMachine.CurrentState is CEBossStateBase<ApsychosStateContext> timed) {
                        timed.ResetTiming();
                    }
                }
                return;
            }

            if (!disengaging) {
                disengaging = true;
                if (!VaultUtils.isClient && stateMachine.CurrentState is not ApsychosMoveToTargetState) {
                    stateMachine.ChangeState(new ApsychosMoveToTargetState());
                }
            }
            if (stateMachine.CurrentState is CEBossStateBase<ApsychosStateContext> state) {
                state.ResetTiming();
            }
            deactiveCount--;
            if (deactiveCount <= 0 && !VaultUtils.isClient) {
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        private void UpdateDisengageMotion() {
            Context.Outline *= ApsychosDirector.DisengageGlowDecay;
            Context.TailLight *= ApsychosDirector.DisengageGlowDecay;
            Context.HighLight *= ApsychosDirector.DisengageGlowDecay;
            tail.velocity *= ApsychosDirector.DisengageGlowDecay;
            NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, MathHelper.PiOver2, ApsychosDirector.DisengageRotateRate, false);
            NPC.velocity = NPC.rotation.ToRotationVector2() * ApsychosDirector.DisengageSpeed;
            Context.TailStyle = ApsychosTailStyle.Follow;
        }

        /// <summary>原 AttackPlayer 末尾三个 Flag 的结算:状态没关掉衰减开关就把对应量拉回去</summary>
        private void SettleDeclarations() {
            if (disengaging) {
                return;
            }
            if (Context.DecayTailSpeed && tail != null) {
                tail.velocity *= ApsychosDirector.TailSpeedDecay;
            }
            if (Context.DecayOutline) {
                Context.Outline *= ApsychosDirector.OutlineDecay;
            }
            if (Context.DecayTailLight) {
                Context.TailLight *= ApsychosDirector.TailLightDecay;
            }
            if (Context.DecayHighLight) {
                Context.HighLight *= ApsychosDirector.HighLightDecay;
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => true;

        public override void HitEffect(NPC.HitInfo hit) {
            if (!Main.dedServ) {
                CEUtils.PlaySound("ApsychosHit", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
            }
            if (NPC.life <= 0 && !Main.dedServ) {
                float scale = 360 / 40f;
                PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.Red * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_CustomPulse>(NPC.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(NPC.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
                PRTLoader.NewParticle<PRT_CustomPulse>(NPC.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
                if (tail != null && segs != null) {
                    Gore.NewGore(NPC.GetSource_Death(), tail.Center, CEUtils.randomPointInCircle(6), Mod.Find<ModGore>("ApsychosGore1").Type);
                    foreach (var seg in segs) {
                        Gore.NewGore(NPC.GetSource_Death(), seg.Center, CEUtils.randomPointInCircle(6), Mod.Find<ModGore>("ApsychosGore2").Type);
                    }
                }
            }
        }

        #region 同步
        /// <summary>
        /// 定长块,顺序固定。累加量(朝向)写在计时之后,不改 CEBossNetMotion 的线格式。
        /// 字节数是编译期常量:不许加运行时条件决定写不写某个字段
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer) {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is ApsychosStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);
            writer.Write(NPC.rotation);
            writer.Write(tail != null ? tail.rotation : 0f);
            writer.Write(Context.Num1);
            writer.Write(Context.Num2);
            writer.Write(Context.Num3);
            writer.Write(Context.AttackIndex);
            writer.Write(Context.TailDashReps);
            writer.Write(TailNPCIndex);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is ApsychosStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            NPC.rotation = reader.ReadSingle();
            float tailRot = reader.ReadSingle();
            Context.Num1 = AdoptScalar(Context.Num1, reader.ReadSingle());
            Context.Num2 = AdoptScalar(Context.Num2, reader.ReadSingle());
            Context.Num3 = AdoptScalar(Context.Num3, reader.ReadSingle());
            Context.AttackIndex = reader.ReadInt32();
            Context.TailDashReps = reader.ReadInt32();
            TailNPCIndex = reader.ReadInt32();
            if (TailNPCIndex >= 0) {
                tail = TailNPCIndex.ToNPC();
                if (tail != null && tail.active) {
                    tail.rotation = tailRot;
                }
            }

#if DEBUG
            if (stateMachine?.CurrentState is ApsychosStateBase adopted
                && System.Math.Abs(localTimer - adopted.Timer) > CEBossNetMotion.TimerTolerance)
            {
                Mod.Logger.Debug($"Apsychos net |v|={NPC.velocity.Length():0.0} frameDelta~{localTimer - adopted.Timer} state={adopted.StateIndex}");
            }
#endif
        }

        /// <summary>标量当帧计数用:容差内不动,对齐 AdoptTimer 的口径</summary>
        private static float AdoptScalar(float local, float synced) {
            return System.Math.Abs(synced - local) > CEBossNetMotion.TimerTolerance ? synced : local;
        }
        #endregion

        #region 掉落
        public override void OnKill() {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedApsychos, -1);
        }

        public override void BossLoot(ref int potionType) {
            potionType = ItemID.HealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ApsychosBag>()));
            npcLoot.Add(new DropPerPlayerOnThePlayer(ItemID.HealingPotion, 1, 5, 15, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            {
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TectonicShard>(), 1, 24, 28));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<GreatSwordofEmbers>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<ScorchingChakram>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<AshesBow>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<EmberBolt>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(ItemDropRule.Common(ItemID.Hellstone, 1, 32, 40));
            }
            npcLoot.Add(normalOnly);
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<ApsychosRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ApsychosTrophy>(), 10));
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<LoreApsychos>(), 1, 1, 1, new LoreFirstKill()));
        }

        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }

        private class LoreFirstKill : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedApsychos;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
        #endregion
    }
}
