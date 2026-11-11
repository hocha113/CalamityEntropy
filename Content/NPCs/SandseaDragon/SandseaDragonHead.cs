using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.NPCs.SandseaDragon.States;
using CalamityEntropy.Core.AI;
using InnoVault;
using InnoVault.StateMachines;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SandseaDragon
{
    /// <summary>
    /// 沙海冥龙的头部,InnoVault 状态机宿主,三节身体和尾部只跟随脊柱,不带 AI
    /// 状态号写 ai[3],阶段写 ai[2];转移、弹幕、锁向只在权威端,计时和事实随 SendExtraAI 过线
    /// 脊柱由各端按头部轨迹本地重建,不过线
    /// </summary>
    [AutoloadBossHead]
    public partial class SandseaDragonHead : ModNPC
    {
        private NpcStateMachine<SandseaStateContext> stateMachine;
        public SandseaStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;
        /// <summary>体节只由权威端生成一次</summary>
        private bool segmentsSpawned;

        public float DamageReduction = SandseaDirector.DamageReduction;

        #region 本地视觉字段(由声明逐帧推导,不过线)
        public float Alpha = 1f;
        /// <summary>背鳍辉光 0..1</summary>
        public float Glow;
        public float JawOpen;
        public float Barrier;
        private float jawIdlePhase;
        private bool wasBuried;
        private bool buriedKnown;
        #endregion

        public SandseaStateIndex CurrentStateIndex => (SandseaStateIndex)(int)NPC.ai[3];
        public int Phase => Context?.Phase ?? Math.Max(1, (int)NPC.ai[2]);
        /// <summary>当前状态的帧计时,过线,弹幕拿它当共用时钟</summary>
        public int StateTimer => stateMachine?.CurrentState is SandseaStateBase state ? state.Timer : 0;

        #region 定义
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //冲刺 21 像素/帧以上,原版 netOffset 平滑会叠成锯齿,头身尾三个类型必须一致
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                CustomTexturePath = "CalamityEntropy/Assets/BCL/SandseaDragon",
                Scale = 0.28f,
                PortraitScale = 0.42f,
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundDesert,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.SandseaDragonBestiary")
            });
        }

        public override void SetDefaults() {
            NPC.boss = true;
            NPC.aiStyle = -1;
            NPC.width = 110;
            NPC.height = 110;
            NPC.damage = SandseaDirector.BaseDamage;
            NPC.defense = SandseaDirector.DefenseHead;
            NPC.lifeMax = SandseaDirector.BaseLife;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.netAlways = true;
            NPC.npcSlots = 12f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = null;
            NPC.value = Item.buyPrice(gold: 15);
            if (!Main.dedServ) {
                Music = MusicID.DukeFishron;
            }
        }

        public override void OnSpawn(IEntitySource source) {
            EnsureContext();
            Player summoner;
            if (source is EntitySource_BossSpawn bossSpawn && bossSpawn.Target is Player player) {
                summoner = player;
                NPC.target = player.whoAmI;
            }
            else {
                NPC.TargetClosest(false);
                summoner = Main.player[NPC.target];
            }

            //竞技场以召唤者为中线,沙面取召唤者脚下
            float floor = SandseaTerrain.Surface(summoner.Center.X, summoner.Center.Y);
            Context.ArenaCenterX = summoner.Center.X;
            Context.ArenaFloorY = floor;
            Context.ArenaHalfWidth = SandseaDirector.ArenaHalfWidth;
            Context.SideDir = Main.rand.NextBool() ? 1 : -1;

            NPC.Center = new Vector2(summoner.Center.X + Context.SideDir * SandseaDirector.EntranceBreachDist, floor + SandseaDirector.CruiseDepth + 160f);
            NPC.velocity = Vector2.Zero;
            NPC.rotation = -MathHelper.PiOver2;
            NPC.ai[3] = (int)SandseaStateIndex.Entrance;
            NPC.ai[2] = 1;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            NPC.netUpdate = true;

            //召唤时正在刮的沙尘暴立刻结束
            SandseaWeather.Stop();
        }

        public override bool CheckActive() => false;
        #endregion

        #region 承伤与判定
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) => ApplyHitRules(ref modifiers);

        /// <summary>头身尾共用的承伤规则:减伤,以及三阶段前血量压不穿锁血线</summary>
        public void ApplyHitRules(ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 1f - DamageReduction;
            if (Phase < 3) {
                int floor = Math.Max(1, (int)(NPC.lifeMax * SandseaDirector.Phase3LifeRatio));
                modifiers.SetMaxDamage(Math.Max(1, NPC.life - floor));
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return ContactDamageActive();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
            target.AddDebuffFixed(BuffID.Venom, SandseaDirector.VenomFrames);
        }

        /// <summary>接触伤害常开,演出态和死亡期间关,体节也读它</summary>
        public bool ContactDamageActive() {
            if (Context == null || Context.Dying || Context.Invulnerable) {
                return false;
            }
            return Context.ContactActive;
        }

        public override void HitEffect(NPC.HitInfo hit) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 4; i++) {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Sand, hit.HitDirection * 2f, -1.5f);
            }
        }
        #endregion

        #region 死亡
        /// <summary>
        /// 三阶段前不许真死,血量回到锁血线,转阶段由全局转移接手
        /// 死亡演出没放完时锁血,客户端计时可能落后几帧,留容差免得收到击杀包时把自己救活
        /// </summary>
        public override bool CheckDead() {
            EnsureContext();
            if (Context.DeathFinished) {
                return true;
            }
            if (Context.Phase < 3) {
                NPC.life = Math.Max(1, (int)(NPC.lifeMax * SandseaDirector.Phase3LifeRatio));
                NPC.active = true;
                NPC.netUpdate = true;
                return false;
            }
            if (Context.Dying && stateMachine?.CurrentState is SandseaDeathState death
                && death.Counter >= SandseaDirector.DeathDuration - SandseaDirector.DeathKillTolerance) {
                return true;
            }
            if (!Context.Dying) {
                Context.Dying = true;
                if (!VaultUtils.isClient && stateMachine != null && stateMachine.CurrentState is not SandseaDeathState) {
                    stateMachine.ChangeState(new SandseaDeathState());
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
            Context ??= new SandseaStateContext();
            Context.Npc = NPC;
            Context.Owner = this;
        }

        private void InitializeStateMachine() {
            EnsureContext();
            if (NPC.ai[2] < 1f) {
                NPC.ai[2] = 1f;
            }
            stateMachine = new NpcStateMachine<SandseaStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            //中途加入的客户端从 ai[3] 恢复当前状态
            IVaultState<SandseaStateContext> initial = null;
            if (VaultUtils.isClient) {
                initial = VaultStateRegistry<SandseaStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new SandseaEntranceState());
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

            EnsureSegments();
            FindTarget();
            Context.Target = targetPlayer;
            UpdateTargetValidity();
            EvaluateGlobalTransitions();

            Context.BeginFrameDefaults();
            stateMachine.Update();
            ApplyDeclaredMovement();
            UpdateSpine();

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

        /// <summary>三节身体加一节尾,ai[0] 存头部下标,ai[1] 存节序 1..4,血量用 realLife 共用</summary>
        private void EnsureSegments() {
            if (segmentsSpawned || VaultUtils.isClient) {
                return;
            }
            segmentsSpawned = true;
            for (int i = 1; i < SandseaDirector.SegmentCount; i++) {
                int type = i == SandseaDirector.SegmentCount - 1 ? ModContent.NPCType<SandseaDragonTail>() : ModContent.NPCType<SandseaDragonBody>();
                int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, type, NPC.whoAmI, NPC.whoAmI, i);
                if (index < Main.maxNPCs) {
                    Main.npc[index].realLife = NPC.whoAmI;
                    Main.npc[index].netUpdate = true;
                }
            }
        }

        /// <summary>目标要活着、在沙漠里、离本体 150 格以内</summary>
        private bool Eligible(Player player) {
            return player.active && !player.dead && (player.ZoneDesert || player.ZoneUndergroundDesert)
                && NPC.Distance(player.Center) <= SandseaDirector.DisengageDistance;
        }

        private int ClosestEligible() {
            int best = -1;
            float bestDist = float.MaxValue;
            foreach (Player player in Main.ActivePlayers) {
                if (!Eligible(player)) {
                    continue;
                }
                float dist = NPC.DistanceSQ(player.Center);
                if (dist < bestDist) {
                    bestDist = dist;
                    best = player.whoAmI;
                }
            }
            return best;
        }

        private void FindTarget() {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers) {
                NPC.TargetClosest(false);
            }
            targetPlayer = Main.player[NPC.target];
        }

        /// <summary>
        /// 离开沙漠满 3 秒或超出 150 格即失效,先找另一个合格的玩家,找不到才脱战
        /// 客户端只看死活,脱战由权威端换态带过来
        /// </summary>
        private void UpdateTargetValidity() {
            bool alive = targetPlayer != null && targetPlayer.active && !targetPlayer.dead;
            if (VaultUtils.isClient) {
                Context.TargetValid = alive;
                return;
            }
            if (alive) {
                bool inDesert = targetPlayer.ZoneDesert || targetPlayer.ZoneUndergroundDesert;
                Context.LeaveDesertTimer = inDesert ? 0 : Context.LeaveDesertTimer + 1;
                Context.FarTimer = NPC.Distance(targetPlayer.Center) > SandseaDirector.DisengageDistance ? Context.FarTimer + 1 : 0;
                alive = Context.LeaveDesertTimer < SandseaDirector.LeaveDesertFrames && Context.FarTimer < SandseaDirector.DisengageGraceFrames;
            }
            if (!alive) {
                int other = ClosestEligible();
                if (other >= 0) {
                    NPC.target = other;
                    targetPlayer = Main.player[other];
                    Context.Target = targetPlayer;
                    Context.LeaveDesertTimer = 0;
                    Context.FarTimer = 0;
                    NPC.netUpdate = true;
                    alive = true;
                }
            }
            Context.TargetValid = alive;
        }

        /// <summary>全局转移只在权威端:死亡、脱战、转阶段,演出态不打断</summary>
        private void EvaluateGlobalTransitions() {
            if (VaultUtils.isClient || stateMachine?.CurrentState == null) {
                return;
            }
            IVaultState<SandseaStateContext> current = stateMachine.CurrentState;
            if (Context.Dying) {
                if (current is not SandseaDeathState) {
                    stateMachine.ChangeState(new SandseaDeathState());
                }
                return;
            }
            if (current is SandseaEntranceState or SandseaPhaseShiftState or SandseaFinaleShiftState or SandseaDespawnState or SandseaDeathState) {
                return;
            }
            if (!Context.TargetValid) {
                stateMachine.ChangeState(new SandseaDespawnState());
                return;
            }
            if (Context.Phase == 1 && NPC.life <= NPC.lifeMax * SandseaDirector.Phase2LifeRatio) {
                stateMachine.ChangeState(new SandseaPhaseShiftState());
                return;
            }
            if (Context.Phase == 2 && NPC.life <= NPC.lifeMax * SandseaDirector.Phase3LifeRatio) {
                stateMachine.ChangeState(new SandseaFinaleShiftState());
            }
        }
        #endregion

        #region 运动落地
        /// <summary>把状态声明的运动模式落到速度上,未声明时沙中减速、空中受重力</summary>
        private void ApplyDeclaredMovement() {
            switch (Context.Mode) {
                case SandseaMoveMode.HoldRelative: {
                    if (!Context.TargetValid) {
                        NPC.velocity *= 0.9f;
                        break;
                    }
                    Vector2 want = targetPlayer.velocity + (targetPlayer.Center + Context.HoldOffset - NPC.Center) * Context.HoldStiffness;
                    if (want.Length() > Context.HoldMaxSpeed) {
                        want = want.SafeNormalize(Vector2.Zero) * Context.HoldMaxSpeed;
                    }
                    NPC.velocity = Vector2.Lerp(NPC.velocity, want, Context.HoldLerp);
                    Context.FrameVelocity = NPC.velocity;
                    break;
                }
                case SandseaMoveMode.Direct:
                    break;
                default:
                    if (SandseaTerrain.Buried(NPC.Center)) {
                        NPC.velocity *= 0.92f;
                        if (NPC.velocity.LengthSquared() < 0.01f) {
                            NPC.velocity = Vector2.Zero;
                        }
                    }
                    else {
                        NPC.velocity.X *= 0.99f;
                        NPC.velocity.Y = Math.Min(NPC.velocity.Y + SandseaDirector.Gravity, SandseaDirector.MaxFallSpeed);
                    }
                    break;
            }
        }
        #endregion

        #region 视觉推导(各端同算,不过线)
        private void UpdateVisualState() {
            Alpha = float.IsNaN(Context.AlphaDeclared) ? MathHelper.Lerp(Alpha, 1f, 0.2f) : Context.AlphaDeclared;
            float glowBase = Context.Phase >= 3 ? SandseaDirector.GlowPhase3 : Context.Phase >= 2 ? SandseaDirector.GlowPhase2 : 0f;
            Glow = MathHelper.Lerp(Glow, Math.Max(glowBase, Context.GlowBoost), 0.1f);
            Barrier = MathHelper.Lerp(Barrier, Context.Barrier, 0.25f);

            //嘴一直在轻微开合,状态声明的张口叠在上面
            jawIdlePhase += 0.05f + NPC.velocity.Length() * 0.004f;
            float jawTarget = Math.Max(Context.JawOpen, 0.1f + 0.08f * MathF.Sin(jawIdlePhase));
            JawOpen = MathHelper.Lerp(JawOpen, jawTarget, 0.25f);

            if (!Main.dedServ) {
                UpdateLocalEffects();
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
            if (stateMachine?.CurrentState is SandseaStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            writer.Write(Context.AttackIndex);
            writer.Write(Context.PendingState);
            writer.Write((sbyte)Context.SideDir);
            writer.WriteVector2(Context.LockedPoint);
            writer.WriteVector2(Context.LockedVelocity);
            writer.Write(Context.LockedAngle);
            writer.Write(Context.ArenaCenterX);
            writer.Write(Context.ArenaFloorY);
            writer.Write(Context.ArenaHalfWidth);
            writer.Write(Context.Dying);
            writer.Write(Context.DeathFinished);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is SandseaStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            Context.AttackIndex = reader.ReadInt32();
            Context.PendingState = reader.ReadInt32();
            Context.SideDir = reader.ReadSByte();
            Context.LockedPoint = reader.ReadVector2();
            Context.LockedVelocity = reader.ReadVector2();
            Context.LockedAngle = reader.ReadSingle();
            Context.ArenaCenterX = reader.ReadSingle();
            Context.ArenaFloorY = reader.ReadSingle();
            Context.ArenaHalfWidth = reader.ReadSingle();
            Context.Dying = reader.ReadBoolean();
            Context.DeathFinished = reader.ReadBoolean();
            NPC.netOffset = Vector2.Zero;
        }
        #endregion
    }
}
