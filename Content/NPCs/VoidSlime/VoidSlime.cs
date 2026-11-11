using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.NPCs.VoidSlime.States;
using CalamityEntropy.Core.AI;
using InnoVault;
using InnoVault.StateMachines;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidSlime
{
    /// <summary>
    /// 虚空史莱姆,星冕召出的深空史莱姆,InnoVault 状态机宿主,四个形态共用一个 NPC
    /// 状态号写 ai[3],形态写 ai[2];状态只写声明,宿主统一落地运动、判定、引力场和表现
    /// 转移、弹幕、锁向只在权威端,计时和事实随 SendExtraAI 过线;血量上限随形态切换,各端按形态号自己算
    /// </summary>
    [AutoloadBossHead]
    public partial class VoidSlime : ModNPC
    {
        private const string LocalizationRoot = "Mods.CalamityEntropy.NPCs.VoidSlime.";

        private NpcStateMachine<VSlimeStateContext> stateMachine;
        public VSlimeStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;

        /// <summary>虚空形态经难度和人数缩放后的血量上限,首次用到时从 SetDefaults 的结果里记下</summary>
        private int fullLifeMax;
        /// <summary>虚空形态缩放后的接触伤害</summary>
        private int fullDamage;
        private int musicVoid;
        private static int headPale = -1;
        private static int headSingularity = -1;
        private static int headDeathStar = -1;

        public VSlimeStateIndex CurrentStateIndex => (VSlimeStateIndex)(int)NPC.ai[3];
        public int Phase => Context?.Phase ?? Math.Max(1, (int)NPC.ai[2]);
        /// <summary>当前状态的 Timer,绘制按它算环绕物的相位,和权威端生成弹幕用的是同一个钟</summary>
        public int StateTimer => stateMachine?.CurrentState is VSlimeStateBase state ? state.Timer : 0;
        public bool RowHeld => Context != null && Context.RowHeld;
        public float HazardRadius => Context?.HazardRadius ?? 0f;
        public float StarRadius => Context?.StarRadius ?? 0f;

        #region 定义
        public override void Load() {
            string path = "CalamityEntropy/Content/NPCs/VoidSlime/VoidSlime_Head_";
            headPale = Mod.AddBossHeadTexture(path + "Pale");
            headSingularity = Mod.AddBossHeadTexture(path + "Singularity");
            headDeathStar = Mod.AddBossHeadTexture(path + "DeathStar");
        }

        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //跃迁和下砸都在 30px/f 以上,原版 netOffset 平滑会叠成锯齿
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
            NPCID.Sets.TrailCacheLength[Type] = 8;
            NPCID.Sets.TrailingMode[Type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<VoidFire>()] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                CustomTexturePath = "CalamityEntropy/Assets/BCL/VoidSlime",
                Scale = 0.6f,
                PortraitScale = 0.8f,
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.VoidSlimeBestiary")
            });
        }

        public override void SetDefaults() {
            NPC.boss = true;
            NPC.aiStyle = -1;
            NPC.width = (int)VSlimeDirector.BodySize.X;
            NPC.height = (int)VSlimeDirector.BodySize.Y;
            NPC.damage = VSlimeDirector.VoidContact;
            NPC.defense = VSlimeDirector.VoidDefense;
            NPC.lifeMax = VSlimeDirector.VoidLife;
            NPC.knockBackResist = 0f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.netAlways = true;
            NPC.npcSlots = 15f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = null;
            //钱币走掉落表里固定的 10 铂金,本体不再按身价掉钱
            NPC.value = 0f;
            if (!Main.dedServ) {
                //专属曲还没入库时先借巡游者的曲子,文件放进 Assets/Sounds/Music/VoidSlimeBoss 就自动切过去
                musicVoid = MusicLoader.MusicExists(Mod, "Assets/Sounds/Music/VoidSlimeBoss")
                    ? MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/VoidSlimeBoss")
                    : MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/CruiserBoss");
                Music = MusicID.Boss1;
            }
        }

        public override void OnSpawn(IEntitySource source) {
            EnsureContext();
            //召唤物走 SpawnOnPlayer,把本体挪到召唤者斜上方的高空
            if (source is EntitySource_BossSpawn bossSpawn && bossSpawn.Target is Player summoner) {
                NPC.target = summoner.whoAmI;
                int side = Main.rand.NextBool() ? 1 : -1;
                NPC.Center = summoner.Center + new Vector2(side * 260f, -VSlimeDirector.EntranceSpawnHeight);
            }
            else {
                NPC.TargetClosest(false);
            }
            NPC.velocity = Vector2.Zero;
            NPC.ai[3] = (int)VSlimeStateIndex.Entrance;
            NPC.ai[2] = VSlimeForm.Pale;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        public override bool CheckActive() => false;

        /// <summary>苍白、奇点、死兆星三个形态各有自己的名字,图鉴样本的 ai[2] 是 0,保持本名</summary>
        public override void ModifyTypeName(ref string typeName) {
            //终幕换回本名,击败公告报的是虚空史莱姆
            if (Context != null && Context.Dying) {
                return;
            }
            switch ((int)NPC.ai[2]) {
                case VSlimeForm.Pale:
                    typeName = Language.GetTextValue(LocalizationRoot + "FormNames.Pale");
                    break;
                case VSlimeForm.Singularity:
                    typeName = Language.GetTextValue(LocalizationRoot + "FormNames.Singularity");
                    break;
                case VSlimeForm.DeathStar:
                    typeName = Language.GetTextValue(LocalizationRoot + "FormNames.DeathStar");
                    break;
            }
        }

        public override void BossHeadSlot(ref int index) {
            switch ((int)NPC.ai[2]) {
                case VSlimeForm.Pale:
                    index = headPale;
                    break;
                case VSlimeForm.Singularity:
                    index = headSingularity;
                    break;
                case VSlimeForm.DeathStar:
                    index = headDeathStar;
                    break;
            }
        }
        #endregion

        #region 形态数值
        /// <summary>SetDefaults 的缩放结果只能在改动血量上限之前记</summary>
        private void CaptureScaledStats() {
            if (fullLifeMax > 0) {
                return;
            }
            fullLifeMax = Math.Max(1, NPC.lifeMax);
            fullDamage = Math.Max(1, NPC.defDamage > 0 ? NPC.defDamage : NPC.damage);
        }

        /// <summary>
        /// lifeMax 不过线,各端按形态号自己切;客户端收到满血标记时 life 取的是本地 lifeMax,切上限时把满血跟着带过去
        /// </summary>
        private void ApplyFormStats() {
            CaptureScaledStats();
            bool pale = Phase <= VSlimeForm.Pale;
            int wantMax = pale
                ? Math.Max(1, (int)Math.Round(fullLifeMax * (VSlimeDirector.PaleLife / (double)VSlimeDirector.VoidLife)))
                : fullLifeMax;
            if (NPC.lifeMax != wantMax) {
                bool wasFull = NPC.life >= NPC.lifeMax;
                NPC.lifeMax = wantMax;
                if (wasFull || NPC.life > wantMax) {
                    NPC.life = wantMax;
                }
            }
            int defense = pale ? VSlimeDirector.PaleDefense : VSlimeDirector.VoidDefense;
            NPC.defense = defense;
            NPC.defDefense = defense;
        }

        /// <summary>破壳拍在权威端调,换成虚空形态的血量上限并回满</summary>
        public void RefillLife() {
            ApplyFormStats();
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
        }

        /// <summary>破壳拍在权威端调,以本体为心展开引力场</summary>
        public void OpenArena() {
            Context.ArenaActive = true;
            Context.ArenaCenter = NPC.Center;
            Context.ArenaRadius = VSlimeDirector.ArenaOpenRadius;
        }
        #endregion

        #region 承伤与判定
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            int form = Phase;
            if (form >= VSlimeForm.Void) {
                modifiers.FinalDamage *= 1f - VSlimeDirector.VoidDR;
            }
            if (form >= VSlimeForm.DeathStar) {
                modifiers.FinalDamage *= VSlimeDirector.DeathStarDamageTaken;
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return ContactDamageActive();
        }

        /// <summary>演出、闪现、落地宽限和状态声明 NoContact 期间一律没有接触伤害</summary>
        public bool ContactDamageActive() {
            if (Context == null || Context.Dying || Context.Invulnerable || Context.NoContact || Context.BlinkTimer > 0 || Context.NoContactTimer > 0) {
                return false;
            }
            return Alpha * blinkAlpha > 0.5f;
        }

        /// <summary>
        /// 接触判定的形状和倍率:状态声明了圆形判定就按圆算(黑洞、恒星、对撞的双黑洞),否则用略收窄的本体框
        /// NPC.damage 恒为虚空形态的接触伤害,其余档位按基值比例乘倍率
        /// </summary>
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox) {
            if (Context == null) {
                return true;
            }
            int baseDamage = Context.ContactBase > 0
                ? Context.ContactBase
                : Phase <= VSlimeForm.Pale ? VSlimeDirector.PaleContact : VSlimeDirector.VoidContact;
            damageMultiplier *= baseDamage / (float)VSlimeDirector.VoidContact;

            if (Context.HazardRadius > 0f) {
                bool hit = CircleHits(NPC.Center, Context.HazardRadius, victimHitbox)
                    || (Context.Hazard2Center != Vector2.Zero && CircleHits(Context.Hazard2Center, Context.HazardRadius, victimHitbox));
                npcHitbox = hit ? victimHitbox : Rectangle.Empty;
            }
            else {
                //史莱姆贴图四角是圆的,判定框左右各收 14,上沿收 12
                npcHitbox = new Rectangle(npcHitbox.X + 14, npcHitbox.Y + 12, Math.Max(2, npcHitbox.Width - 28), Math.Max(2, npcHitbox.Height - 12));
            }
            return true;
        }

        private static bool CircleHits(Vector2 center, float radius, Rectangle target) {
            Vector2 nearest = new Vector2(
                MathHelper.Clamp(center.X, target.Left, target.Right),
                MathHelper.Clamp(center.Y, target.Top, target.Bottom));
            return Vector2.DistanceSquared(center, nearest) <= radius * radius;
        }

        /// <summary>红巨星的爆发冲击不伤图腾里的玩家,判定点取玩家中心</summary>
        public bool InsideTotem(Vector2 point) {
            if (Context == null || Context.TotemStrength < 0.5f) {
                return false;
            }
            for (int i = 0; i < VSlimeDirector.TotemCount; i++) {
                Vector2 center = VSlimeDirector.TotemCenter(Context.ArenaCenter, Context.TotemAngle, i);
                if (Vector2.DistanceSquared(center, point) <= VSlimeDirector.TotemRadius * VSlimeDirector.TotemRadius) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 普通档基值转成弹幕 damage 字段,难度倍率由原版命中结算乘
        /// FTW 世界本体接触伤害多乘 (倍率 + 1) / 倍率,弹幕补同一比例
        /// </summary>
        public static int ProjDamage(int normalBase) {
            float mult = 1f;
            if (Main.getGoodWorld && !Main.GameModeInfo.IsJourneyMode) {
                float m = Main.GameModeInfo.EnemyDamageMultiplier;
                mult = (m + 1f) / m;
            }
            return Math.Max(1, (int)Math.Round(normalBase * mult));
        }
        #endregion

        #region 死亡
        /// <summary>
        /// 终幕没放完一律锁在 1 血:苍白形态靠它触发破壳,死兆星靠它等四段演完
        /// 客户端计时可能落后几帧,留容差免得收到击杀包时把自己救活
        /// </summary>
        public override bool CheckDead() {
            EnsureContext();
            if (Context.DeathFinished) {
                return true;
            }
            if (Context.Dying && stateMachine?.CurrentState is VSlimeDeathState death
                && death.Counter >= VSlimeDirector.DeathDuration - VSlimeDirector.DeathKillTolerance) {
                return true;
            }
            NPC.life = 1;
            NPC.active = true;
            if (!VaultUtils.isClient) {
                NPC.netUpdate = true;
            }
            return false;
        }

        public override void BossLoot(ref int potionType) {
            potionType = ModContent.ItemType<VoidHealingPotion>();
        }

        public override void OnKill() {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedVoidSlime, -1);
        }
        #endregion

        #region 状态机装配
        private void EnsureContext() {
            Context ??= new VSlimeStateContext();
            Context.Npc = NPC;
            Context.Owner = this;
        }

        private void InitializeStateMachine() {
            EnsureContext();
            if (NPC.ai[2] < 1f) {
                NPC.ai[2] = VSlimeForm.Pale;
            }
            stateMachine = new NpcStateMachine<VSlimeStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            //中途加入的客户端从 ai[3] 恢复当前状态
            IVaultState<VSlimeStateContext> initial = null;
            if (VaultUtils.isClient) {
                initial = VaultStateRegistry<VSlimeStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new VSlimeEntranceState());
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

            ApplyFormStats();
            FindTarget();
            Context.Target = targetPlayer;
            Context.TargetValid = NPC.HasValidTarget && targetPlayer != null && targetPlayer.active && !targetPlayer.dead
                && NPC.Distance(targetPlayer.Center) <= VSlimeDirector.MaxFindDistance;
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
            ApplyPhysicsMode();
            UpdateHitbox();
            UpdateArena();

            NPC.damage = ContactDamageActive() ? fullDamage : 0;
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

        /// <summary>全局转移只在权威端:终幕、脱战、破壳、转形态,演出态不打断</summary>
        private void EvaluateGlobalTransitions() {
            if (VaultUtils.isClient || stateMachine?.CurrentState == null) {
                return;
            }
            IVaultState<VSlimeStateContext> current = stateMachine.CurrentState;
            if (Context.Dying) {
                if (current is not VSlimeDeathState) {
                    stateMachine.ChangeState(new VSlimeDeathState());
                }
                return;
            }
            if (current is VSlimeEntranceState or VSlimePaleBurstState or VSlimeSingularityShiftState or VSlimeDeathStarShiftState
                or VSlimeDespawnState or VSlimeDeathState) {
                return;
            }
            if (!Context.TargetValid) {
                stateMachine.ChangeState(new VSlimeDespawnState());
                return;
            }
            switch (Context.Phase) {
                case VSlimeForm.Pale:
                    if (NPC.life <= 1) {
                        stateMachine.ChangeState(new VSlimePaleBurstState());
                    }
                    break;
                case VSlimeForm.Void:
                    if (NPC.life <= NPC.lifeMax * VSlimeDirector.SingularityLifeRatio) {
                        stateMachine.ChangeState(new VSlimeSingularityShiftState());
                    }
                    break;
                case VSlimeForm.Singularity:
                    if (NPC.life <= NPC.lifeMax * VSlimeDirector.DeathStarLifeRatio) {
                        stateMachine.ChangeState(new VSlimeDeathStarShiftState());
                    }
                    break;
            }
        }
        #endregion

        #region 运动落地
        /// <summary>把状态声明的运动模式落到速度上,未声明一律指数刹停</summary>
        private void ApplyDeclaredMovement() {
            switch (Context.Mode) {
                case VSlimeMoveMode.HoverTo: {
                    Vector2 diff = Context.MoveTarget - NPC.Center;
                    float dist = diff.Length();
                    Vector2 desired = Vector2.Zero;
                    if (dist > 1f) {
                        desired = diff / dist * Math.Min(Context.MoveSpeed, dist / Context.SlowRadius * Context.MoveSpeed);
                    }
                    NPC.velocity = Vector2.Lerp(NPC.velocity, desired, Context.Accel);
                    break;
                }
                case VSlimeMoveMode.HoldRelative: {
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
                case VSlimeMoveMode.Direct:
                    break;
                default:
                    NPC.velocity *= 0.9f;
                    if (NPC.velocity.LengthSquared() < 0.01f) {
                        NPC.velocity = Vector2.Zero;
                    }
                    break;
            }
        }

        /// <summary>苍白形态声明落地物理的那一帧才受重力和物块碰撞,其余时候憬空穿墙</summary>
        private void ApplyPhysicsMode() {
            bool ground = Context.GroundPhysics && Context.BlinkTimer <= 0;
            NPC.noGravity = !ground;
            NPC.noTileCollide = !ground;
            if (ground) {
                NPC.GravityMultiplier *= VSlimeDirector.PaleGravityMult;
                NPC.MaxFallSpeedMultiplier *= VSlimeDirector.PaleMaxFallMult;
            }
        }

        /// <summary>受击判定框朝状态声明的边长逐帧靠近,中心不动</summary>
        private void UpdateHitbox() {
            Vector2 target = Context.HitboxTarget == Vector2.Zero ? VSlimeDirector.BodySize : Context.HitboxTarget;
            int width = (int)MoveToward(NPC.width, target.X, VSlimeDirector.HitboxStep);
            int height = (int)MoveToward(NPC.height, target.Y, VSlimeDirector.HitboxStep);
            if (width == NPC.width && height == NPC.height) {
                return;
            }
            Vector2 center = NPC.Center;
            NPC.width = width;
            NPC.height = height;
            NPC.Center = center;
        }

        private static float MoveToward(float current, float target, float maxStep) {
            float diff = target - current;
            return Math.Abs(diff) <= maxStep ? target : current + Math.Sign(diff) * maxStep;
        }
        #endregion

        #region 闪现
        /// <summary>锚点就是落点,前半段淡出,过半换位,后半段淡入,落地后一段时间没有接触伤害</summary>
        public void StartBlink(Vector2 destination) {
            Context.AnchorPos = destination;
            Context.BlinkTimer = VSlimeDirector.BlinkDuration;
            Context.NoContactTimer = VSlimeDirector.PostBlinkGrace + VSlimeDirector.BlinkDuration;
            NPC.velocity = Vector2.Zero;
            BlinkCue(NPC.Center);
            if (!VaultUtils.isClient) {
                NPC.netUpdate = true;
            }
        }

        private void UpdateBlink() {
            Context.BlinkTimer--;
            NPC.velocity = Vector2.Zero;
            int half = VSlimeDirector.BlinkDuration / 2;
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

        /// <summary>本体不可见时的直接换位,只在权威端调,位置随包过线</summary>
        public void Teleport(Vector2 center) {
            CEBossHost.PlaceEntity(NPC, center, netMotion);
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        private static void BlinkCue(Vector2 at) {
            VSlimeVfx.VoidBurst(at, 1.3f);
            VSlimeVfx.Stardust(at, 14, 2f, 7f);
            VSlimeVfx.Sound(SoundID.Item8 with { Volume = 0.8f, Pitch = -0.2f, MaxInstances = 2 }, at);
        }
        #endregion

        #region 同步
        /// <summary>包是定长的,不按条件省略字段</summary>
        public override void SendExtraAI(BinaryWriter writer) {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is VSlimeStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            writer.Write(Context.AttackIndex);
            writer.Write(Context.PendingState);
            writer.Write((sbyte)Context.SideDir);
            writer.WriteVector2(Context.AnchorPos);
            writer.WriteVector2(Context.LeapFrom);
            writer.WriteVector2(Context.LockedPoint);
            writer.WriteVector2(Context.LockedPoint2);
            writer.Write(Context.LockedAngle);
            writer.Write(Context.RollSeed);
            writer.Write(Context.Combo);
            writer.Write(Context.HeldRadius);
            writer.Write(Context.BlinkTimer);
            writer.Write(Context.NoContactTimer);
            writer.Write(Context.ArenaActive);
            writer.WriteVector2(Context.ArenaCenter);
            writer.Write(Context.ArenaRadius);
            writer.Write(Context.Dying);
            writer.Write(Context.DeathFinished);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is VSlimeStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            Context.AttackIndex = reader.ReadInt32();
            Context.PendingState = reader.ReadInt32();
            Context.SideDir = reader.ReadSByte();
            Context.AnchorPos = reader.ReadVector2();
            Context.LeapFrom = reader.ReadVector2();
            Context.LockedPoint = reader.ReadVector2();
            Context.LockedPoint2 = reader.ReadVector2();
            Context.LockedAngle = reader.ReadSingle();
            Context.RollSeed = reader.ReadInt32();
            Context.Combo = reader.ReadInt32();
            Context.HeldRadius = reader.ReadSingle();
            int packetBlink = reader.ReadInt32();
            Context.NoContactTimer = reader.ReadInt32();
            Context.ArenaActive = reader.ReadBoolean();
            Context.ArenaCenter = reader.ReadVector2();
            Context.ArenaRadius = reader.ReadSingle();
            Context.Dying = reader.ReadBoolean();
            Context.DeathFinished = reader.ReadBoolean();

            //闪现计时带容差收养,硬对齐会让换位拍被跳过或重放
            int previous = Context.BlinkTimer;
            Context.BlinkTimer = CEBossNetMotion.AdoptTimer(Context.BlinkTimer, packetBlink);
            if (previous <= 0 && Context.BlinkTimer > VSlimeDirector.BlinkDuration / 2) {
                BlinkCue(NPC.Center);
            }
            NPC.netOffset = Vector2.Zero;
            //形态号和生命值已经随这个包写入,血量上限当场跟上
            ApplyFormStats();
        }
        #endregion
    }
}
