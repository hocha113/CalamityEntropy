using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.Items.SunMoonGods;
using CalamityEntropy.Content.Items.Weapons.BossLoot.Moonrise;
using CalamityEntropy.Content.Items.Weapons.BossLoot.StarryNight;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States;
using CalamityEntropy.Content.Skies;
using CalamityEntropy.Core.AI;
using InnoVault;
using InnoVault.StateMachines;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage
{
    /// <summary>
    /// 星月贤者,光阴符文夜里召出的先知强化体,InnoVault 状态机宿主
    /// 状态号写 ai[3],阶段写 ai[2];状态只写声明,宿主统一落地运动、朝向、判定窗和表现
    /// 转移、弹幕、锁向只在权威端,计时和事实随 SendExtraAI 过线;NPC.rotation 是头朝向,骨架根跟着它
    /// </summary>
    [AutoloadBossHead]
    public partial class StarmoonSage : ModNPC
    {
        private NpcStateMachine<SageStateContext> stateMachine;
        public SageStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;
        private int musicNormal;
        private int musicLate;

        #region 本地视觉字段(由声明逐帧推导,不过线)
        public float Alpha = 1f;
        /// <summary>星遁淡出淡入的透明度,乘在 Alpha 上</summary>
        private float blinkAlpha = 1f;
        public float CrestGlow;
        public float WingSpread;
        public float BodyGlow;
        public float CastGlow;
        public float RingGlow;
        /// <summary>翼扇动相位,随速度加快</summary>
        private float wingPhase;
        #endregion

        public SageStateIndex CurrentStateIndex => (SageStateIndex)(int)NPC.ai[3];
        public int Phase => Context?.Phase ?? Math.Max(1, (int)NPC.ai[2]);

        #region 定义
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //冲刺 46px/f,原版 netOffset 平滑会叠成锯齿
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                CustomTexturePath = "CalamityEntropy/Assets/BCL/StarmoonSage",
                Scale = 0.4f,
                PortraitScale = 0.5f,
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.StarmoonSageBestiary")
            });
        }

        public override void SetDefaults() {
            NPC.boss = true;
            NPC.aiStyle = -1;
            NPC.width = 110;
            NPC.height = 110;
            NPC.damage = SageDirector.BaseDamage;
            NPC.defense = SageDirector.BaseDefense;
            NPC.lifeMax = SageDirector.BaseLife;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.npcSlots = 15f;
            NPC.HitSound = CEUtils.GetSound("prophet_hurt", 1.15f, 1);
            NPC.DeathSound = null;
            NPC.value = Item.buyPrice(gold: 20);
            if (!Main.dedServ) {
                musicNormal = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/SpectralForesight");
                musicLate = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Prophet2");
                Music = musicNormal;
            }
        }

        public override void OnSpawn(IEntitySource source) {
            EnsureContext();
            //召唤物走 SpawnOnPlayer,把本体挪到召唤者头顶
            if (source is EntitySource_BossSpawn bossSpawn && bossSpawn.Target is Player summoner) {
                NPC.target = summoner.whoAmI;
                NPC.Center = summoner.Center + new Vector2(0f, -SageDirector.EntranceSpawnHeight);
            }
            else {
                NPC.TargetClosest(false);
            }
            NPC.velocity = Vector2.Zero;
            NPC.rotation = MathHelper.PiOver2;
            NPC.ai[3] = (int)SageStateIndex.Entrance;
            NPC.ai[2] = 1;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        public override bool CheckActive() => false;

        /// <summary>星遁期间地图上不显示头像</summary>
        public override void BossHeadSlot(ref int index) {
            if (Alpha * blinkAlpha < 0.3f) {
                index = -1;
            }
        }
        #endregion

        #region 承伤与判定
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 1f - (Phase >= 3 ? SageDirector.DRPhase3 : SageDirector.DRPhase12);
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return ContactDamageActive();
        }

        /// <summary>接触伤害只在状态声明的窗里开,演出、星遁和落地宽限期间一律关</summary>
        public bool ContactDamageActive() {
            if (Context == null || Context.Dying || Context.Invulnerable || Context.BlinkTimer > 0 || Context.NoContactTimer > 0) {
                return false;
            }
            return Context.ContactWindow;
        }

        /// <summary>
        /// 普通档基值转成弹幕 damage 字段,难度倍率由原版命中结算乘
        /// FTW 世界本体接触伤害多乘 (倍率 + 1) / 倍率,弹幕补同一比例
        /// </summary>
        public int ProjDamage(int normalBase) {
            float mult = 1f;
            if (Main.getGoodWorld && !Main.GameModeInfo.IsJourneyMode) {
                float m = Main.GameModeInfo.EnemyDamageMultiplier;
                mult = (m + 1f) / m;
            }
            return Math.Max(1, (int)Math.Round(normalBase * mult));
        }
        #endregion

        #region 掉落
        public override void BossLoot(ref int potionType) {
            potionType = ModContent.ItemType<VoidHealingPotion>();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            //每人一份,联机掉在各自脚下,单人掉在本体位置
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<WraithSoulEssence>(), 1, 35, 45, null));
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<PowerOfStarmoon>(), 1, 1, 1, null));

            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<StarmoonSageBag>()));
            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<Moonrise>(), 3, 1, 1, 2));
            normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<StarryNight>(), 3, 1, 1, 2));
            npcLoot.Add(normalOnly);
        }

        public override void OnKill() {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedStarmoonSage, -1);
        }
        #endregion

        #region 死亡
        /// <summary>死亡演出没放完时锁血,客户端计时可能落后几帧,留容差免得收到击杀包时把自己救活</summary>
        public override bool CheckDead() {
            EnsureContext();
            if (Context.DeathFinished) {
                return true;
            }
            if (Context.Dying && stateMachine?.CurrentState is SageDeathState death
                && death.Counter >= SageDirector.DeathDuration - SageDirector.DeathKillTolerance) {
                return true;
            }
            if (!Context.Dying) {
                Context.Dying = true;
                Context.BlinkTimer = 0;
                if (!VaultUtils.isClient && stateMachine != null && stateMachine.CurrentState is not SageDeathState) {
                    stateMachine.ChangeState(new SageDeathState());
                }
                NPC.netUpdate = true;
            }
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            NPC.active = true;
            return false;
        }
        #endregion

        #region 状态机装配
        private void EnsureContext() {
            Context ??= new SageStateContext();
            Context.Npc = NPC;
            Context.Owner = this;
        }

        private void InitializeStateMachine() {
            EnsureContext();
            if (NPC.ai[2] < 1f) {
                NPC.ai[2] = 1f;
            }
            stateMachine = new NpcStateMachine<SageStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            //中途加入的客户端从 ai[3] 恢复当前状态
            IVaultState<SageStateContext> initial = null;
            if (VaultUtils.isClient) {
                initial = VaultStateRegistry<SageStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new SageEntranceState());
        }
        #endregion

        #region 主 AI
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

            FindTarget();
            Context.Target = targetPlayer;
            Context.TargetValid = NPC.HasValidTarget && targetPlayer != null && targetPlayer.active && !targetPlayer.dead
                && NPC.Distance(targetPlayer.Center) <= SageDirector.MaxFindDistance;
            EvaluateGlobalTransitions();

            if (Context.NoContactTimer > 0) {
                Context.NoContactTimer--;
            }
            Context.BeginFrameDefaults();
            stateMachine.Update();

            if (Context.BlinkTimer > 0) {
                UpdateBlink();
            }
            else {
                blinkAlpha = MathHelper.Lerp(blinkAlpha, 1f, 0.3f);
                ApplyDeclaredMovement();
            }
            ApplyFacing();

            NPC.damage = ContactDamageActive() ? NPC.defDamage : 0;
            NPC.dontTakeDamage = Context.Dying || Context.Invulnerable;
            UpdateVisualState();

            if (client) {
                netMotion.EndFrame(NPC);
            }
            else {
                CEBossHost.Heartbeat(NPC);
            }
        }

        private void FindTarget() {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !Main.player[NPC.target].active || Main.player[NPC.target].dead) {
                NPC.TargetClosest(false);
            }
            targetPlayer = Main.player[NPC.target];
        }

        /// <summary>全局转移只在权威端:死亡、脱战、转阶段,演出态不打断</summary>
        private void EvaluateGlobalTransitions() {
            if (VaultUtils.isClient || stateMachine?.CurrentState == null) {
                return;
            }
            IVaultState<SageStateContext> current = stateMachine.CurrentState;
            if (Context.Dying) {
                if (current is not SageDeathState) {
                    stateMachine.ChangeState(new SageDeathState());
                }
                return;
            }
            if (current is SageEntranceState or SagePhaseShiftState or SageDespawnState or SageDeathState) {
                return;
            }
            if (!Context.TargetValid) {
                stateMachine.ChangeState(new SageDespawnState());
                return;
            }
            bool toPhase2 = Context.Phase == 1 && NPC.life <= NPC.lifeMax * SageDirector.Phase2LifeRatio;
            bool toPhase3 = Context.Phase == 2 && NPC.life <= NPC.lifeMax * SageDirector.Phase3LifeRatio;
            if (toPhase2 || toPhase3) {
                stateMachine.ChangeState(new SagePhaseShiftState());
            }
        }
        #endregion

        #region 运动落地
        /// <summary>把状态声明的运动模式落到速度上,未声明一律指数刹停</summary>
        private void ApplyDeclaredMovement() {
            switch (Context.Mode) {
                case SageMoveMode.HoverTo: {
                    Vector2 diff = Context.MoveTarget - NPC.Center;
                    float dist = diff.Length();
                    Vector2 desired = Vector2.Zero;
                    if (dist > 1f) {
                        desired = diff / dist * Math.Min(Context.MoveSpeed, dist / Context.SlowRadius * Context.MoveSpeed);
                    }
                    NPC.velocity = Vector2.Lerp(NPC.velocity, desired, Context.Accel);
                    break;
                }
                case SageMoveMode.HoldRelative: {
                    if (!Context.TargetValid) {
                        NPC.velocity *= 0.9f;
                        break;
                    }
                    Vector2 want = targetPlayer.velocity + (targetPlayer.Center + Context.HoldOffset - NPC.Center) * Context.Stiffness;
                    if (want.Length() > Context.MoveSpeed) {
                        want = want.SafeNormalize(Vector2.Zero) * Context.MoveSpeed;
                    }
                    NPC.velocity = Vector2.Lerp(NPC.velocity, want, Context.Accel);
                    break;
                }
                case SageMoveMode.Direct:
                    break;
                default:
                    NPC.velocity *= 0.9f;
                    if (NPC.velocity.LengthSquared() < 0.01f) {
                        NPC.velocity = Vector2.Zero;
                    }
                    break;
            }
        }

        private void ApplyFacing() {
            float desired = NPC.rotation;
            switch (Context.Face) {
                case SageFaceMode.Target:
                    if (Context.TargetValid) {
                        desired = (targetPlayer.Center - NPC.Center).ToRotation();
                    }
                    break;
                case SageFaceMode.Velocity:
                    if (NPC.velocity.LengthSquared() > 4f) {
                        desired = NPC.velocity.ToRotation();
                    }
                    break;
                case SageFaceMode.Angle:
                    desired = Context.FaceAngle;
                    break;
            }
            NPC.rotation = NPC.rotation.AngleLerp(desired, SageDirector.TurnRate);
        }
        #endregion

        #region 星遁
        /// <summary>
        /// 锚点就是落点,从第一帧起落点上就有星印标记;前半段淡出,过半换位,后半段淡入
        /// 落点不贴脸,落地后一段时间没有接触伤害
        /// </summary>
        public void StartBlink(Vector2 destination) {
            if (Context.TargetValid) {
                Vector2 away = destination - targetPlayer.Center;
                if (away.Length() < SageDirector.BlinkMinPlayerDistance) {
                    destination = targetPlayer.Center + away.SafeNormalize(-Vector2.UnitY) * SageDirector.BlinkMinPlayerDistance;
                }
            }
            Context.AnchorPos = destination;
            Context.BlinkTimer = SageDirector.BlinkDuration;
            Context.NoContactTimer = SageDirector.PostBlinkGrace + SageDirector.BlinkDuration;
            NPC.velocity = Vector2.Zero;
            BlinkCue(NPC.Center);
            if (!VaultUtils.isClient) {
                NPC.netUpdate = true;
            }
        }

        private void UpdateBlink() {
            Context.BlinkTimer--;
            NPC.velocity = Vector2.Zero;
            int half = SageDirector.BlinkDuration / 2;
            if (Context.BlinkTimer <= half) {
                //后半段幂等地钉在锚点上,收包晚一帧也不会漏掉换位
                if (NPC.Center != Context.AnchorPos) {
                    NPC.Center = Context.AnchorPos;
                    netMotion.ForgetPrediction();
                    if (Context.TargetValid) {
                        NPC.rotation = (targetPlayer.Center - NPC.Center).ToRotation();
                    }
                    SnapRig();
                    BlinkCue(NPC.Center);
                }
                blinkAlpha = 1f - Context.BlinkTimer / (float)half;
            }
            else {
                blinkAlpha = (Context.BlinkTimer - half) / (float)half;
            }
        }

        private static void BlinkCue(Vector2 at) {
            SageVfx.Burst(at, 1.4f);
            SageVfx.Stardust(at, 16, 2f, 7f);
            SageVfx.Cue("teleport", at, 1.1f, 0.7f, 2);
        }
        #endregion

        #region 视觉推导(各端同算,不过线)
        private void UpdateVisualState() {
            Alpha = float.IsNaN(Context.AlphaDeclared) ? MathHelper.Lerp(Alpha, 1f, 0.2f) : Context.AlphaDeclared;
            CrestGlow = MathHelper.Lerp(CrestGlow, Context.CrestGlow, 0.12f);
            WingSpread = MathHelper.Lerp(WingSpread, Context.WingSpread, 0.14f);
            BodyGlow = MathHelper.Lerp(BodyGlow, Context.BodyGlow, 0.2f);
            CastGlow = MathHelper.Lerp(CastGlow, Context.CastGlow, 0.25f);
            RingGlow = MathHelper.Lerp(RingGlow, Context.RingGlow, 0.2f);
            wingPhase += 0.08f + NPC.velocity.Length() * 0.004f;

            if (Main.dedServ) {
                return;
            }
            UpdateRig();
            float shown = Alpha * blinkAlpha;
            Music = Context.Phase >= 3 ? musicLate : musicNormal;
            GodSkyDrive.Moon.Report(Context.SkyIntensity, Context.Phase, NPC.Center);
            Lighting.AddLight(NPC.Center, 0.55f * shown, 0.7f * shown, 1.3f * shown);

            //翼尖洒落的星尘,二阶段起更密
            if (shown > 0.5f && Main.rand.NextBool(Context.Phase >= 2 ? 2 : 4)) {
                Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(150f, 150f);
                Dust d = Dust.NewDustPerfect(pos, Main.rand.NextBool(3) ? DustID.PurpleTorch : DustID.MagicMirror,
                    NPC.velocity * 0.2f + Main.rand.NextVector2Circular(0.6f, 0.6f), 100, default, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }
        }
        #endregion

        #region 同步
        /// <summary>包是定长的,不按条件省略字段</summary>
        public override void SendExtraAI(BinaryWriter writer) {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is SageStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            writer.Write(Context.AttackIndex);
            writer.Write(Context.PendingState);
            writer.Write((sbyte)Context.SideDir);
            writer.WriteVector2(Context.AnchorPos);
            writer.Write(Context.LockedAngle);
            writer.Write(Context.BaseAngle);
            writer.Write(Context.BlinkTimer);
            writer.Write(Context.NoContactTimer);
            writer.Write(Context.Dying);
            writer.Write(Context.DeathFinished);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is SageStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            Context.AttackIndex = reader.ReadInt32();
            Context.PendingState = reader.ReadInt32();
            Context.SideDir = reader.ReadSByte();
            Context.AnchorPos = reader.ReadVector2();
            Context.LockedAngle = reader.ReadSingle();
            Context.BaseAngle = reader.ReadSingle();
            int packetBlink = reader.ReadInt32();
            Context.NoContactTimer = reader.ReadInt32();
            Context.Dying = reader.ReadBoolean();
            Context.DeathFinished = reader.ReadBoolean();

            //星遁计时带容差收养,硬对齐会让换位拍被跳过或重放
            int previous = Context.BlinkTimer;
            Context.BlinkTimer = CEBossNetMotion.AdoptTimer(Context.BlinkTimer, packetBlink);
            if (previous <= 0 && Context.BlinkTimer > SageDirector.BlinkDuration / 2) {
                BlinkCue(NPC.Center);
            }
            NPC.netOffset = Vector2.Zero;
        }
        #endregion
    }
}