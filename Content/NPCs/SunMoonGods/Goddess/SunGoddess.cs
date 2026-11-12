using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.Items.SunMoonGods;
using CalamityEntropy.Content.Items.Weapons.BossLoot.Dawnlight;
using CalamityEntropy.Content.Items.Weapons.BossLoot.NoontideRapier;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States;
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

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess
{
    /// <summary>
    /// 太阳女神,光阴符文白天召出的光之女皇强化体,InnoVault 状态机宿主
    /// 状态号写 ai[3],阶段写 ai[2];状态只写声明,宿主统一落地运动、判定窗和表现
    /// 转移、弹幕、锁向只在权威端,计时和事实随 SendExtraAI 过线
    /// </summary>
    [AutoloadBossHead]
    public partial class SunGoddess : ModNPC
    {
        /// <summary>日轮和光芒的圆心相对本体中心的偏移</summary>
        public static readonly Vector2 HaloOffset = new Vector2(0f, -24f);

        private NpcStateMachine<SunStateContext> stateMachine;
        public SunStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;

        #region 本地视觉字段(由声明逐帧推导,不过线)
        public float Alpha = 1f;
        /// <summary>闪现淡出淡入的透明度,乘在 Alpha 上</summary>
        private float blinkAlpha = 1f;
        public float HaloGlow;
        public float WingPose;
        public float BodyGlow;
        public float CastLeft;
        public float CastRight;
        /// <summary>翼扇动相位,随速度加快</summary>
        private float wingPhase;
        #endregion

        public SunStateIndex CurrentStateIndex => (SunStateIndex)(int)NPC.ai[3];
        public int Phase => Context?.Phase ?? Math.Max(1, (int)NPC.ai[2]);

        #region 定义
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //冲刺 50px/f,原版 netOffset 平滑会叠成锯齿
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
            NPCID.Sets.TrailCacheLength[Type] = 8;
            NPCID.Sets.TrailingMode[Type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Daybreak] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<SunblazeBurn>()] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                CustomTexturePath = "CalamityEntropy/Assets/BCL/SunGoddess",
                Scale = 0.5f,
                PortraitScale = 0.6f,
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheHallow,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.DayTime,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.SunGoddessBestiary")
            });
        }

        public override void SetDefaults() {
            NPC.boss = true;
            NPC.aiStyle = -1;
            NPC.width = 80;
            NPC.height = 120;
            NPC.damage = SunDirector.BaseDamage;
            NPC.defense = SunDirector.BaseDefense;
            NPC.lifeMax = SunDirector.BaseLife;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.npcSlots = 15f;
            NPC.HitSound = SoundID.NPCHit5;
            NPC.DeathSound = null;
            NPC.value = Item.buyPrice(gold: 20);
            if (!Main.dedServ) {
                Music = MusicID.EmpressOfLight;
            }
        }

        public override void OnSpawn(IEntitySource source) {
            EnsureContext();
            //召唤物走 SpawnOnPlayer,把本体挪到召唤者头顶
            if (source is EntitySource_BossSpawn bossSpawn && bossSpawn.Target is Player summoner) {
                NPC.target = summoner.whoAmI;
                NPC.Center = summoner.Center + new Vector2(0f, -SunDirector.EntranceSpawnHeight);
            }
            else {
                NPC.TargetClosest(false);
            }
            NPC.velocity = Vector2.Zero;
            NPC.ai[3] = (int)SunStateIndex.Entrance;
            NPC.ai[2] = 1;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        public override bool CheckActive() => false;
        #endregion

        #region 承伤与判定
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 1f - (Phase >= 3 ? SunDirector.DRPhase3 : SunDirector.DRPhase12);
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return ContactDamageActive();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
            target.AddDebuffFixed(ModContent.BuffType<SunblazeBurn>(), SunDirector.BurnFrames);
        }

        /// <summary>接触伤害只在状态声明的窗里开,演出、闪现和落地宽限期间一律关</summary>
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
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<BlazingSunlight>(), 1, 35, 45, null));
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<PowerOfSunlight>(), 1, 1, 1, null));

            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<SunGoddessBag>()));
            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<NoontideRapier>(), 3, 1, 1, 2));
            normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<Dawnlight>(), 3, 1, 1, 2));
            npcLoot.Add(normalOnly);
        }

        public override void OnKill() {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedSunGoddess, -1);
        }
        #endregion

        #region 死亡
        /// <summary>死亡演出没放完时锁血,客户端计时可能落后几帧,留容差免得收到击杀包时把自己救活</summary>
        public override bool CheckDead() {
            EnsureContext();
            if (Context.DeathFinished) {
                return true;
            }
            if (Context.Dying && stateMachine?.CurrentState is SunDeathState death
                && death.Counter >= SunDirector.DeathDuration - SunDirector.DeathKillTolerance) {
                return true;
            }
            if (!Context.Dying) {
                Context.Dying = true;
                Context.BlinkTimer = 0;
                if (!VaultUtils.isClient && stateMachine != null && stateMachine.CurrentState is not SunDeathState) {
                    stateMachine.ChangeState(new SunDeathState());
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
            Context ??= new SunStateContext();
            Context.Npc = NPC;
            Context.Owner = this;
        }

        private void InitializeStateMachine() {
            EnsureContext();
            if (NPC.ai[2] < 1f) {
                NPC.ai[2] = 1f;
            }
            stateMachine = new NpcStateMachine<SunStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            //中途加入的客户端从 ai[3] 恢复当前状态
            IVaultState<SunStateContext> initial = null;
            if (VaultUtils.isClient) {
                initial = VaultStateRegistry<SunStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new SunEntranceState());
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
                && NPC.Distance(targetPlayer.Center) <= SunDirector.MaxFindDistance;
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
            ApplyTilt();

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
            IVaultState<SunStateContext> current = stateMachine.CurrentState;
            if (Context.Dying) {
                if (current is not SunDeathState) {
                    stateMachine.ChangeState(new SunDeathState());
                }
                return;
            }
            if (current is SunEntranceState or SunPhaseShiftState or SunDespawnState or SunDeathState) {
                return;
            }
            if (!Context.TargetValid) {
                stateMachine.ChangeState(new SunDespawnState());
                return;
            }
            bool toPhase2 = Context.Phase == 1 && NPC.life <= NPC.lifeMax * SunDirector.Phase2LifeRatio;
            bool toPhase3 = Context.Phase == 2 && NPC.life <= NPC.lifeMax * SunDirector.Phase3LifeRatio;
            if (toPhase2 || toPhase3) {
                stateMachine.ChangeState(new SunPhaseShiftState());
            }
        }
        #endregion

        #region 运动落地
        /// <summary>把状态声明的运动模式落到速度上,未声明一律指数刹停</summary>
        private void ApplyDeclaredMovement() {
            switch (Context.Mode) {
                case SunMoveMode.HoverTo: {
                    Vector2 diff = Context.MoveTarget - NPC.Center;
                    float dist = diff.Length();
                    Vector2 desired = Vector2.Zero;
                    if (dist > 1f) {
                        desired = diff / dist * Math.Min(Context.MoveSpeed, dist / Context.SlowRadius * Context.MoveSpeed);
                    }
                    NPC.velocity = Vector2.Lerp(NPC.velocity, desired, Context.Accel);
                    break;
                }
                case SunMoveMode.HoldRelative: {
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
                case SunMoveMode.Direct:
                    break;
                default:
                    NPC.velocity *= 0.9f;
                    if (NPC.velocity.LengthSquared() < 0.01f) {
                        NPC.velocity = Vector2.Zero;
                    }
                    break;
            }
        }

        private void ApplyTilt() {
            float target = float.IsNaN(Context.TiltOverride)
                ? MathHelper.Clamp(NPC.velocity.X * 0.008f, -0.2f, 0.2f)
                : Context.TiltOverride;
            NPC.rotation = MathHelper.Lerp(NPC.rotation, target, 0.12f);
        }
        #endregion

        #region 化光闪现
        /// <summary>锚点就是落点,前半段淡出,过半换位,后半段淡入,落地后一段时间没有接触伤害</summary>
        public void StartBlink(Vector2 destination) {
            Context.AnchorPos = destination;
            Context.BlinkTimer = SunDirector.BlinkDuration;
            Context.NoContactTimer = SunDirector.PostBlinkGrace + SunDirector.BlinkDuration;
            NPC.velocity = Vector2.Zero;
            BlinkCue(NPC.Center);
            if (!VaultUtils.isClient) {
                NPC.netUpdate = true;
            }
        }

        private void UpdateBlink() {
            Context.BlinkTimer--;
            NPC.velocity = Vector2.Zero;
            int half = SunDirector.BlinkDuration / 2;
            if (Context.BlinkTimer <= half) {
                //后半段幂等地钉在锚点上,收包晚一帧也不会漏掉换位
                if (NPC.Center != Context.AnchorPos) {
                    NPC.Center = Context.AnchorPos;
                    netMotion.ForgetPrediction();
                    BlinkCue(NPC.Center);
                }
                blinkAlpha = 1f - Context.BlinkTimer / (float)half;
            }
            else {
                blinkAlpha = (Context.BlinkTimer - half) / (float)half;
            }
        }

        private static void BlinkCue(Vector2 at) {
            SunVfx.Burst(at, 1.4f);
            SunVfx.Embers(at, 14, 2f, 7f);
            SunVfx.Sound(SoundID.Item165 with { Volume = 0.7f, Pitch = 0.2f, MaxInstances = 2 }, at);
        }
        #endregion

        #region 视觉推导(各端同算,不过线)
        private void UpdateVisualState() {
            Alpha = float.IsNaN(Context.AlphaDeclared) ? MathHelper.Lerp(Alpha, 1f, 0.2f) : Context.AlphaDeclared;
            HaloGlow = MathHelper.Lerp(HaloGlow, Context.HaloGlow, 0.12f);
            WingPose = MathHelper.Lerp(WingPose, Context.WingPose, 0.14f);
            BodyGlow = MathHelper.Lerp(BodyGlow, Context.BodyGlow, 0.2f);
            CastLeft = MathHelper.Lerp(CastLeft, Context.CastLeft, 0.25f);
            CastRight = MathHelper.Lerp(CastRight, Context.CastRight, 0.25f);
            wingPhase += 0.07f + NPC.velocity.Length() * 0.004f;

            if (Main.dedServ) {
                return;
            }
            float shown = Alpha * blinkAlpha;
            GodSkyDrive.Sun.Report(Context.SkyIntensity, Context.Phase, NPC.Center);
            Lighting.AddLight(NPC.Center, 1.3f * shown, 0.95f * shown, 0.5f * shown);

            //翼上飘起的火星,二阶段起更密
            if (shown > 0.5f && Main.rand.NextBool(Context.Phase >= 2 ? 2 : 4)) {
                int side = Main.rand.NextBool() ? -1 : 1;
                Vector2 pos = NPC.Center + new Vector2(side * Main.rand.NextFloat(30f, 110f), Main.rand.NextFloat(-120f, -10f));
                Dust d = Dust.NewDustPerfect(pos, DustID.SolarFlare, new Vector2(side * Main.rand.NextFloat(0f, 1f), -Main.rand.NextFloat(1f, 3f)) + NPC.velocity * 0.3f,
                    100, default, Main.rand.NextFloat(0.8f, 1.3f));
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
            if (stateMachine?.CurrentState is SunStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            writer.Write(Context.AttackIndex);
            writer.Write(Context.PendingState);
            writer.Write((sbyte)Context.SideDir);
            writer.WriteVector2(Context.AnchorPos);
            writer.WriteVector2(Context.LockedPoint);
            writer.Write(Context.LockedAngle);
            writer.Write(Context.BlinkTimer);
            writer.Write(Context.NoContactTimer);
            writer.Write(Context.Dying);
            writer.Write(Context.DeathFinished);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is SunStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            Context.AttackIndex = reader.ReadInt32();
            Context.PendingState = reader.ReadInt32();
            Context.SideDir = reader.ReadSByte();
            Context.AnchorPos = reader.ReadVector2();
            Context.LockedPoint = reader.ReadVector2();
            Context.LockedAngle = reader.ReadSingle();
            int packetBlink = reader.ReadInt32();
            Context.NoContactTimer = reader.ReadInt32();
            Context.Dying = reader.ReadBoolean();
            Context.DeathFinished = reader.ReadBoolean();

            //闪现计时带容差收养,硬对齐会让换位拍被跳过或重放
            int previous = Context.BlinkTimer;
            Context.BlinkTimer = CEBossNetMotion.AdoptTimer(Context.BlinkTimer, packetBlink);
            if (previous <= 0 && Context.BlinkTimer > SunDirector.BlinkDuration / 2) {
                BlinkCue(NPC.Center);
            }
            NPC.netOffset = Vector2.Zero;
        }
        #endregion
    }
}