using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.MusicBoxes;
using CalamityEntropy.Content.Items.Tools;
using CalamityEntropy.Content.NPCs.Acropolis.Core;
using CalamityEntropy.Content.NPCs.Acropolis.States;
using CalamityEntropy.Content.Particles;
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

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 卫城机器:地狱层的自我晋升型 Boss,InnoVault 状态机宿主。
    /// <para>
    /// 原代码没有互斥状态,是三个并行的倒计时/布尔开关(<c>CannonUpAtk</c> / <c>JumpAndShoot</c> /
    /// <c>Jumping</c>)。2026-09-17 迁移时把每个开关代表的<b>招式</b>抽成互斥状态,
    /// 其余「不属于任何一招」的东西留在宿主里当背景行为每帧跑:
    /// 跨招冷却 <c>TeslaCD</c>、地面走位与悬停、腿部步态、鱼叉装填与发射、鱼叉拽拉、朝向翻转、重力阻尼。
    /// 数值一律照搬,只有「几招不能再叠在一起」这一条是被授权的手感变动。
    /// </para>
    /// <para>
    /// 三个形态由宿主前置分叉,不进战斗状态机:
    /// 血量 ≥ 98% 的<b>未晋升形态</b>(走普通重力,<see cref="Dummy"/> 腾空姿态)、
    /// 脱战漂移、以及 <see cref="Defeated"/> 死亡演出(直接 return,和原代码一样跳过所有战斗逻辑)。
    /// </para>
    /// <para>
    /// 联机:转移只在权威端(状态号 ai[3],形态 ai[2]);各端跑同一套运动数学;
    /// 计时、朝向、朝向锁存、四条腿的落点与步数种子、两条手臂的两节朝向、全部倒计时随
    /// <see cref="SendExtraAI"/> 过线。弹幕与骰点只在权威端,骰点结果必过线。
    /// 数值在 <see cref="AcropolisDirector"/>,选招在 <see cref="AcropolisRotation"/>,绘制在 AcropolisMachine.Draw.cs
    /// </para>
    /// </summary>
    [AutoloadBossHead]
    public partial class AcropolisMachine : ModNPC
    {
        #region 字段
        private NpcStateMachine<AcropolisStateContext> stateMachine;
        public AcropolisStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();
        private Player targetPlayer;

        /// <summary>四条腿。锚定型部件,不是 NPC</summary>
        public List<AcropolisLeg> legs = null;
        /// <summary>炮臂</summary>
        public AcropolisHand cannon;
        /// <summary>鱼叉臂</summary>
        public AcropolisHand harpoon;
        /// <summary>鱼叉实体索引,-1 表示还没生成</summary>
        public int _harpoon = -1;

        /// <summary>腾空中。持久量,腿组与鱼叉实体都读它,三个招式/事件都能置位</summary>
        public bool Jumping = false;
        /// <summary>落地锁存:腾空期间置位,踩实的那一帧消费掉并把下坠速度清零</summary>
        public bool JFlag = false;
        /// <summary>跳跃冷却。每帧无条件自减,允许跌成负数——追高跳靠它透支 260 帧来限频</summary>
        public int JumpCD = 0;
        /// <summary>朝向。翻转时把 <c>NPC.rotation</c> 转半圈,所以它是累加量,必须过线</summary>
        public int dir = 1;
        /// <summary>未晋升且腾空:腿贴着本体、机体按横速倾斜。每帧重算</summary>
        public bool Dummy = false;
        /// <summary>晋升闸,只放行一次</summary>
        public bool SetBoss = true;
        /// <summary>已进入死亡演出</summary>
        public bool Defeated = false;
        /// <summary>死亡演出倒计时</summary>
        public int DeathCounter = AcropolisDirector.DeathCounterInit;
        /// <summary>脱战累计帧数</summary>
        public int dcounter = 0;
        /// <summary>死亡演出的充能音</summary>
        public LoopSound chargeSnd = null;

        /// <summary>死亡演出的本地帧计数。原代码用 <c>Main.GameUpdateCount % 2</c>,那是各端各走的计数</summary>
        private int deathFrame = 0;
        /// <summary>开火计数是否已经对齐过,用来抑制中途加入时的假边沿</summary>
        private bool shotCueReady = false;
        /// <summary>本帧的落地探测结果(本体盒子压到实心块或平台),朝向结算要用</summary>
        private bool groundProbe = false;

        /// <summary>形态编号,映射 <c>ai[2]</c> 同步槽。1 = 未晋升,2 = 已晋升为 Boss</summary>
        public int phase {
            get => Context == null ? (int)NPC.ai[2] : Context.Phase;
            set {
                if (Context != null) {
                    Context.Phase = value;
                }
                else {
                    NPC.ai[2] = value;
                }
            }
        }
        #endregion

        #region 定义
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = "CalamityEntropy/Assets/BCL/AcropolisMachine",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -4
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            //跳跃 24×scale、被鱼叉拽拉 40 px/f,都远超原版平滑能消化的 2~4 px/f;
            //而且锁链是从本体的枪口画到鱼叉实体的,两端必须读同一个平滑层级(都清零)
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                // 群系迁移:原灾厄硫火之崖图鉴背景改原版地狱(biome-map)
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheUnderworld,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.Acropolis")
            });
        }

        public override void SetDefaults() {
            //状态机把状态号写在 ai[3],必须确保原版 AI 不占槽(模组 NPC 的默认值就是 -1,这里写明)
            NPC.aiStyle = -1;
            NPC.width = 142;
            NPC.height = 132;
            NPC.damage = 26;
            NPC.defense = 8;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = CEUtils.GetSound("chainsaw_break");
            NPC.value = 1600f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 12;
            NPC.lavaImmune = true;
            NPC.scale = 1f;
            if (Main.getGoodWorld) {
                NPC.scale += 0.2f;
            }
            if (Main.zenithWorld) {
                NPC.scale += 0.8f;
            }
            NPC.boss = false;
            // 灾厄元素易伤体系不移植(debuff-map:等效取基准值);原硫火之崖群系归属改原版地狱层(biome-map)
        }

        public override bool CheckActive() {
            return !NPC.boss;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            // 生成条件:原灾厄硫火之崖改地狱层自然生成,频率照搬(biome-map)
            return (spawnInfo.Player.ZoneUnderworldHeight && !NPC.AnyNPCs(Type) && EModSys.AcropolisDontSpawn <= 0)
                ? (NPC.downedMoonlord ? AcropolisDirector.SpawnChancePostMoonlord
                    : (Main.hardMode ? AcropolisDirector.SpawnChanceHardmode : AcropolisDirector.SpawnChancePreHardmode))
                : 0f;
        }

        public static bool CanStandOn(Vector2 pos) {
            return !CEUtils.isAir(pos, true);
        }

        public bool CanStandOn(int x, int y) {
            if (!CEUtils.inWorld(x, y)) return false;
            return CanStandOn(new Vector2(x, y) * 16f);
        }
        #endregion

        #region 状态机装配
        private void EnsureContext() {
            Context ??= new AcropolisStateContext {
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
            stateMachine = new NpcStateMachine<AcropolisStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            IVaultState<AcropolisStateContext> initial = null;
            if (VaultUtils.isClient) {
                initial = VaultStateRegistry<AcropolisStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new AcropolisWalkState());
        }

        /// <summary>懒创建腿组与两条手臂。名字保留,原代码在 AI 与 ReceiveExtraAI 两处都调它</summary>
        public void SegCheck() {
            if (legs == null) {
                legs = new List<AcropolisLeg>(AcropolisDirector.LegMounts.Length);
                for (int i = 0; i < AcropolisDirector.LegMounts.Length; i++) {
                    (float x, float y, float scale) = AcropolisDirector.LegMounts[i];
                    legs.Add(new AcropolisLeg(NPC, new Vector2(x, y), scale, i));
                }
                cannon = new AcropolisHand(NPC, new Vector2(AcropolisDirector.CannonMountX, AcropolisDirector.CannonMountY),
                    AcropolisDirector.CannonSeg1Length, MathHelper.PiOver2, MathHelper.PiOver2);
                harpoon = new AcropolisHand(NPC, new Vector2(AcropolisDirector.HarpoonMountX, AcropolisDirector.HarpoonMountY),
                    AcropolisDirector.HarpoonSeg1Length, MathHelper.PiOver2, MathHelper.PiOver2);
            }
        }
        #endregion

        #region 鱼叉实体
        /// <summary>鱼叉实体。索引无效时返回 null(原代码直接 <c>Main.npc[-1]</c>,那是会崩的)</summary>
        public NPC HarpoonEntity => _harpoon >= 0 && _harpoon < Main.maxNPCs ? Main.npc[_harpoon] : null;

        /// <summary>鱼叉是否在发射架上。走位、追高、装填冷却都读它</summary>
        public bool HarpoonOnLauncher {
            get {
                NPC hp = HarpoonEntity;
                return hp != null && hp.ModNPC is Harpoon h && h.OnLauncher;
            }
        }

        /// <summary>鱼叉在发射架上时的枪口位置。鱼叉实体与锁链绘制都读它</summary>
        public Vector2 HarpoonPos => harpoon.seg1end
            + harpoon.Seg2Rot.ToRotationVector2() * AcropolisDirector.HarpoonMuzzleReach * NPC.scale
            + new Vector2(0, AcropolisDirector.HarpoonMuzzleSide * dir).RotatedBy(harpoon.Seg2Rot) * NPC.scale;

        private void EnsureHarpoonEntity() {
            if (_harpoon != -1 || VaultUtils.isClient) {
                return;
            }
            _harpoon = NPC.NewNPC(NPC.GetSource_FromAI(), 0, 0, ModContent.NPCType<Harpoon>(), 0, NPC.whoAmI);
            NPC spawned = HarpoonEntity;
            if (spawned != null) {
                spawned.Center = HarpoonPos;
                spawned.netSpam = 9;
                spawned.netUpdate = true;
            }
            //部件索引是决策,必须立刻过线
            NPC.netUpdate = true;
            NPC.netSpam = 0;
        }

        /// <summary>鱼叉扎墙后每帧调用:把本体拽过去。由鱼叉实体在各端同步驱动</summary>
        public void RequestHarpoonPull() {
            EnsureContext();
            Context.PullTimer = AcropolisDirector.PullTimerRefill;
            Jumping = true;
            JumpCD = AcropolisDirector.PullJumpCD;
        }

        /// <summary>鱼叉松钩:本体落回地面</summary>
        public void ReleaseHarpoonPull() {
            JumpCD = AcropolisDirector.PullJumpCD;
            Jumping = false;
        }
        #endregion

        #region 主循环
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

            //原 AI() 开头的固定顺序:腿组读的是上一帧的 Jumping 与速度,不能挪到状态机之后
            NPC.chaseable = NPC.boss;
            JumpCD--;
            SegCheck();
            cannon.Update();
            harpoon.Update();
            EnsureHarpoonEntity();
            UpdateLegs();

            if (NPC.life < 2) {
                Defeated = true;
            }
            if (Defeated) {
                RunDeathSequence();
                if (client) {
                    netMotion.EndFrame(NPC);
                }
                return;
            }

            if (!NPC.HasValidTarget) {
                NPC.TargetClosest();
            }
            FindTarget();
            UpdateContextFacts();
            EvaluateGlobalTransitions();

            //脱战与未晋升形态也要走 Update:客户端靠这里的 NetSync 收到权威端的换态。
            //状态体本身由 RequiresTarget 把关,没接战就不跑
            Context.BeginFrameDefaults();
            stateMachine.Update();

            if (Context.Engaged) {
                SettleCombatFrame();
                dcounter = 0;
            }
            else {
                if (stateMachine.CurrentState is AcropolisStateBase idle) {
                    idle.ResetTiming();
                }
                if (NPC.boss) {
                    RunDisengage();
                }
                else {
                    RunNonBossForm();
                }
            }

            ApplyBodyPhysics();
            UpdateDummyFlag();
            ApplyRotation();

            if (client) {
                netMotion.EndFrame(NPC);
            }
            else {
                CEBossHost.Heartbeat(NPC);
            }
        }

        private void FindTarget() {
            targetPlayer = NPC.HasValidTarget ? Main.player[NPC.target] : null;
        }

        /// <summary>每帧事实重算。跨招冷却也在这里扣:它是背景冷却,出招期间照常流逝</summary>
        private void UpdateContextFacts() {
            Context.Npc = NPC;
            Context.Owner = this;
            Context.Target = targetPlayer;
            Context.Enrange = AcropolisDirector.Enrange(NPC);
            Context.TargetDistance = targetPlayer == null ? 0f : CEUtils.getDistance(targetPlayer.Center, NPC.Center);
            Context.HarpoonOnLauncher = HarpoonOnLauncher;

            int onTile = 0;
            for (int i = 0; i < legs.Count; i++) {
                if (legs[i].OnTile) {
                    onTile++;
                }
            }
            Context.LegsOnTile = onTile;
            Context.Grounded = onTile >= AcropolisDirector.LegsOnTileForGround || CEUtils.CheckSolidTile(NPC.getRect());

            bool promoted = (float)NPC.life / NPC.lifeMax < AcropolisDirector.BossPromoteLifeRatio;
            Context.Engaged = promoted && targetPlayer != null
                && Context.TargetDistance < AcropolisDirector.DisengageDistance;
            Context.TargetValid = Context.Engaged;

            if (Context.Engaged) {
                //原代码在 AttackPlayer 中段扣它,再立刻判到点;这里提前到状态机之前,判定仍在同一帧
                Context.TeslaCD -= Context.Enrange;
            }
        }

        /// <summary>晋升 / 脱战 / 形态开关。原代码写在 AI() 的血量分叉里</summary>
        private void EvaluateGlobalTransitions() {
            if ((float)NPC.life / NPC.lifeMax < AcropolisDirector.BossPromoteLifeRatio) {
                if (SetBoss) {
                    SetBoss = false;
                    if (!Main.dedServ) {
                        Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/HellBlazenRobotics");
                    }
                    NPC.boss = true;
                    Context.Phase = 2;
                }
                NPC.noTileCollide = true;
            }
            else {
                NPC.boss = false;
                NPC.noTileCollide = false;
                Context.Phase = 1;
            }
        }
        #endregion

        #region 背景行为:腿 / 炮口 / 鱼叉 / 走位 / 朝向 / 拽拉
        private void UpdateLegs() {
            for (int i = 0; i < legs.Count; i++) {
                if (!legs[i].Update()) {
                    continue;
                }
                //一条腿迈步就压住同侧其它腿,避免同侧一起抬脚
                for (int j = 0; j < legs.Count; j++) {
                    if (Math.Sign(legs[j].offset.X) == Math.Sign(legs[i].offset.X)
                        && legs[j].NoMoveTime < AcropolisDirector.LegStepCooldown) {
                        legs[j].NoMoveTime = AcropolisDirector.LegStepCooldown;
                    }
                }
            }
        }

        /// <summary>原 <c>AttackPlayer</c> 里不属于任何一招的那些行为,顺序照搬</summary>
        private void SettleCombatFrame() {
            Player player = Context.Target;
            float enrange = Context.Enrange;

            //炮口:状态没声明就走常态瞄准(玩家身位抬 14,超过 500 再按平方补抛物线落差)
            if (Context.CannonAim == null) {
                float d = Context.TargetDistance;
                float drop = d > AcropolisDirector.IdleAimDropDistance
                    ? -(((d - AcropolisDirector.IdleAimDropDistance) * AcropolisDirector.IdleAimDropFactor)
                        * ((d - AcropolisDirector.IdleAimDropDistance) * AcropolisDirector.IdleAimDropFactor))
                    : 0f;
                cannon.PointAPos(player.Center + new Vector2(0, AcropolisDirector.IdleAimRise) + new Vector2(0, drop));
            }

            //落地即清跳射计数
            if (!Jumping) {
                Context.JumpAndShoot = -1;
            }

            //鱼叉臂:蓄力到 0.8 之前追瞄玩家,发射出去之后改为跟着鱼叉实体
            NPC harpoonEntity = HarpoonEntity;
            if (Context.HarpoonCharge <= AcropolisDirector.HarpoonAimChargeCap && Context.HarpoonOnLauncher) {
                harpoon.PointAPos(player.Center);
            }
            else if (!Context.HarpoonOnLauncher && harpoonEntity != null) {
                harpoon.PointAPos(harpoonEntity.Center);
            }

            ConsumeShotCue();
            UpdateHarpoonCycle(enrange, harpoonEntity);

            if (!Jumping) {
                UpdateGroundedMovement(player, enrange);
            }
            else {
                UpdateAirborneMovement();
            }

            UpdateFacing();

            //鱼叉拽拉:只要鱼叉还卡着,它每帧把计时刷成 2,本体就被按 40 px/f 拽过去
            if (Context.PullTimer-- > 0 && harpoonEntity != null) {
                NPC.velocity = (harpoonEntity.Center - NPC.Center).normalize() * AcropolisDirector.PullSpeed;
            }
        }

        /// <summary>单发电球的本地表现。骰点只在权威端,各端靠过线的开火计数补上反冲与音效</summary>
        private void ConsumeShotCue() {
            if (Context.LocalShotCue == Context.ShotCue) {
                return;
            }
            Context.LocalShotCue = Context.ShotCue;
            cannon.Seg1RotV = AcropolisDirector.SingleShotRecoil * dir;
            CEUtils.PlaySound("ofshoot", 1, cannon.TopPos);
        }

        /// <summary>鱼叉装填与发射。与任何招式并行,原代码就是这样</summary>
        private void UpdateHarpoonCycle(float enrange, NPC harpoonEntity) {
            if (Context.HarpoonOnLauncher) {
                Context.HarpoonCD -= enrange;
            }
            if (Context.HarpoonCD > 0f) {
                return;
            }
            Context.HarpoonCharge += AcropolisDirector.HarpoonChargeRate * enrange;
            if (Context.HarpoonCharge < 1f) {
                return;
            }
            Context.HarpoonCharge = 0f;
            Context.HarpoonCD = AcropolisDirector.HarpoonCDAfterLaunch;
            if (harpoonEntity == null || harpoonEntity.ModNPC is not Harpoon hp) {
                return;
            }
            //发射是确定性的(蓄力与冷却都过线),各端同帧执行;权威端再补一个决策点同步
            hp.Back = AcropolisDirector.HarpoonBackFrames;
            hp.OnLauncher = false;
            harpoonEntity.velocity = harpoon.Seg2Rot.ToRotationVector2() * AcropolisDirector.HarpoonLaunchSpeed * NPC.scale;
            harpoon.Seg1RotV = AcropolisDirector.HarpoonRecoil * dir;
            CEUtils.PlaySound("chainsawHit", 1, NPC.Center);
            if (!VaultUtils.isClient) {
                NPC.netUpdate = true;
                harpoonEntity.netUpdate = true;
            }
        }

        /// <summary>地面推进:落地锁存、悬停高度控制、横向接近。追高跳的触发已移进行走态</summary>
        private void UpdateGroundedMovement(Player player, float enrange) {
            bool flag = false;
            if (Context.LegsOnTile >= AcropolisDirector.LegsOnTileForGround) {
                flag = true;
                if (JFlag) {
                    JFlag = false;
                    if (NPC.velocity.Y > 0) {
                        NPC.velocity.Y = 0;
                    }
                }
            }
            if (!(flag || CEUtils.CheckSolidTile(NPC.getRect()))) {
                NPC.velocity.Y += AcropolisDirector.FreeFallAccel;
                if (NPC.velocity.Y > AcropolisDirector.FreeFallMaxSpeed) {
                    NPC.velocity.Y = AcropolisDirector.FreeFallMaxSpeed;
                }
                return;
            }

            float yof = AcropolisDirector.HoverYOffset * NPC.scale;
            float hoverRise = -AcropolisDirector.HoverYOffset;
            //已经压到玩家下方:限速并额外阻尼,免得一路砸下去
            if (NPC.Center.Y - yof + hoverRise * NPC.scale * NPC.scale > player.Center.Y) {
                if (NPC.velocity.Y > AcropolisDirector.HoverFallClamp * NPC.scale) {
                    NPC.velocity.Y = AcropolisDirector.HoverFallClamp * NPC.scale;
                }
                if (NPC.velocity.Y > 0) {
                    NPC.velocity.Y *= AcropolisDirector.HoverFallDamp;
                }
            }

            float v = AcropolisDirector.HoverThrustNear;
            if (Math.Abs(NPC.Center.Y + yof - player.Center.Y) > AcropolisDirector.HoverFarDistance * NPC.scale) {
                v = AcropolisDirector.HoverThrustFar;
            }
            if (Math.Abs(NPC.Center.Y + yof - player.Center.Y) < AcropolisDirector.HoverDeadZone * NPC.scale) {
                v = 0;
                NPC.velocity.Y *= AcropolisDirector.HoverDeadZoneDamp;
            }
            v *= NPC.scale;

            if (Context.HarpoonOnLauncher && Math.Abs(yof + player.Center.Y - NPC.Center.Y) > AcropolisDirector.HoverGate * NPC.scale) {
                if (player.Center.Y + yof > NPC.Center.Y) {
                    NPC.velocity.Y += AcropolisDirector.HoverDownAccel * enrange * v;
                }
                else {
                    bool f = true;
                    bool f2 = false;
                    for (int i = 0; i < legs.Count; i++) {
                        AcropolisLeg l = legs[i];
                        if (l.OnTile && l.StandPoint.Y > NPC.Center.Y + AcropolisDirector.LegLowThreshold * NPC.scale) {
                            f = false;
                        }
                        if (l.OnTile && l.StandPoint.Y > NPC.Center.Y + AcropolisDirector.LegVeryLowThreshold * NPC.scale) {
                            f2 = true;
                        }
                    }
                    if (f || CEUtils.CheckSolidTile(NPC.getRect())) {
                        NPC.velocity.Y += AcropolisDirector.HoverUpAccel * enrange * v;
                    }
                    else if (f2) {
                        NPC.velocity.Y += AcropolisDirector.HoverPushDownAccel * enrange * v;
                    }
                }
            }

            if (Context.HarpoonOnLauncher
                && CEUtils.getDistance(NPC.Center, player.Center) > AcropolisDirector.WalkKeepDistance * NPC.scale) {
                NPC.velocity.X += Math.Sign(player.Center.X - NPC.Center.X) * AcropolisDirector.WalkAccel * enrange * NPC.scale;
            }
        }

        /// <summary>
        /// 落地判定。起跳后 50 帧内不许收(跳射计数闸),被鱼叉拽着时也不许收。
        /// 原代码在这里还算了一遍着地腿数,但算完没人用,已删
        /// </summary>
        private void UpdateAirborneMovement() {
            if (Context.JumpAndShoot <= AcropolisDirector.JumpEndCounterGate && Context.PullTimer <= 0) {
                if (JumpCD < AcropolisDirector.JumpEndJumpCD
                    || (NPC.velocity.Y > 0 && CEUtils.CheckSolidTileOrPlatform(GroundProbeRect()))
                        && NPC.velocity.Y > AcropolisDirector.JumpEndFallSpeed) {
                    Jumping = false;
                    NPC.velocity *= 0;
                }
            }
            JFlag = true;
        }

        private Rectangle GroundProbeRect()
            => new Rectangle((int)NPC.position.X, (int)NPC.position.Y, NPC.width,
                (int)(NPC.height * AcropolisDirector.GroundProbeHeightScale));

        private void UpdateFacing() {
            if (NPC.velocity.X > 0) {
                if (dir == -1) {
                    NPC.rotation += MathHelper.Pi;
                }
                dir = 1;
            }
            if (NPC.velocity.X < 0) {
                if (dir == 1) {
                    NPC.rotation += MathHelper.Pi;
                }
                dir = -1;
            }
        }
        #endregion

        #region 形态分叉
        /// <summary>脱战漂移:向右加速,贴到实心块就上浮,超时直接消失</summary>
        private void RunDisengage() {
            dcounter++;
            NPC.velocity.X += AcropolisDirector.DriftAccelX;
            if (CEUtils.CheckSolidTile(NPC.getRect())) {
                NPC.velocity.Y += AcropolisDirector.DriftUp;
            }
            else {
                NPC.velocity.Y += AcropolisDirector.DriftDown;
            }
            if (dcounter > AcropolisDirector.DespawnFrames && !VaultUtils.isClient) {
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        /// <summary>未晋升形态:普通重力 + 贴地清零。不跑战斗状态机</summary>
        private void RunNonBossForm() {
            NPC.velocity.Y += AcropolisDirector.DummyGravity;
            if (CEUtils.CheckSolidTile(NPC.getRect())) {
                NPC.velocity.Y = 0;
            }
        }

        /// <summary>腾空走重力,落地走整体阻尼。三个形态共用</summary>
        private void ApplyBodyPhysics() {
            if (Jumping) {
                NPC.velocity.Y += AcropolisDirector.JumpGravity * NPC.scale;
            }
            else {
                NPC.velocity *= AcropolisDirector.GroundDrag;
            }
        }

        /// <summary>未晋升形态的腾空姿态。<see cref="groundProbe"/> 供朝向结算复用</summary>
        private void UpdateDummyFlag() {
            Dummy = false;
            groundProbe = CEUtils.CheckSolidTileOrPlatform(GroundProbeRect());
            if (NPC.boss) {
                return;
            }
            if (groundProbe) {
                NPC.velocity.X *= AcropolisDirector.DummyGroundDragX;
            }
            else {
                Dummy = true;
                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation,
                    NPC.velocity.X * AcropolisDirector.DummyTiltFactor, AcropolisDirector.DummyTiltRate, false);
            }
        }

        /// <summary>朝向:腾空按横速倾斜,落地按左右腿落点的连线找地形倾角</summary>
        private void ApplyRotation() {
            if (Jumping) {
                NPC.rotation = (Math.Abs(NPC.velocity.X * AcropolisDirector.AirRotationFactor).ToRotationVector2()
                    * new Vector2(dir, 1)).ToRotation();
                return;
            }
            if (!(NPC.boss || groundProbe)) {
                return;
            }

            Vector2 lr = Vector2.Zero;
            Vector2 rr = Vector2.Zero;
            int lc = 0;
            int rc = 0;
            int ontile = 0;
            for (int i = 0; i < legs.Count; i++) {
                AcropolisLeg leg = legs[i];
                if (!leg.OnTile) {
                    continue;
                }
                ontile++;
                if (leg.offset.X < 0) {
                    lr += leg.StandPoint;
                    lc++;
                }
                if (leg.offset.X > 0) {
                    rr += leg.StandPoint;
                    rc++;
                }
            }

            if (ontile > 2) {
                if (lc > 0 && rc > 0) {
                    float r = ((rr / rc) - (lr / lc)).ToRotation();
                    float maxr = MathHelper.ToRadians(AcropolisDirector.MaxTerrainTiltDegrees);
                    if (r > maxr) {
                        r = maxr;
                    }
                    if (r < -maxr) {
                        r = -maxr;
                    }
                    if (dir < 0) {
                        r += MathHelper.Pi;
                    }
                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, r, AcropolisDirector.TerrainRotateRate, false);
                }
                else if (lc > rc) {
                    NPC.rotation += AcropolisDirector.SingleSideSpinRate;
                }
                else if (lc < rc) {
                    NPC.rotation -= AcropolisDirector.SingleSideSpinRate;
                }
                else {
                    float r = dir == 1 ? 0 : MathHelper.Pi;
                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, r, AcropolisDirector.FacingSnapRate, false);
                }
            }
            else {
                float r = dir == 1 ? 0 : MathHelper.Pi;
                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, r, AcropolisDirector.FacingFallbackRate, false);
            }
        }
        #endregion

        #region 死亡演出
        public override bool CheckDead() {
            if (DeathCounter <= 0) {
                return true;
            }

            Defeated = true;
            NPC.dontTakeDamage = true;
            NPC.active = true;
            NPC.netUpdate = true;
            NPC.damage = 0;
            NPC.boss = true;
            NPC.life = 1;
            if (Main.dedServ) {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }

            return false;
        }

        /// <summary>
        /// 死亡演出:充能音升调、每两帧一次震屏与充能粒子,倒计时跑完自爆。
        /// 宿主前置分叉,直接 return,不进状态机——原代码也是这么跳过全部战斗逻辑的
        /// </summary>
        private void RunDeathSequence() {
            NPC.netUpdate = true;
            if (NPC.netSpam >= 10) {
                NPC.netSpam = 9;
            }
            deathFrame++;
            int d = 1;
            if (CECal.IsDeathMode && deathFrame % 2 == 0) {
                d++;
            }
            if (Main.zenithWorld) {
                d = 1;
            }
            DeathCounter -= d;
            NPC.velocity *= 0;
            Jumping = false;
            Context.JumpAndShoot = -1;

            if (!Main.dedServ) {
                if (chargeSnd == null) {
                    chargeSnd = new LoopSound(CalamityEntropy.ofCharge);
                    chargeSnd.instance.Pitch = 0;
                    chargeSnd.instance.Volume = 0;
                    chargeSnd.play();
                    chargeSnd.timeleft = 2;
                }
                chargeSnd.setVolume_Dist(NPC.Center, 400, 1800, 1);
                chargeSnd.instance.Pitch = (1 - (DeathCounter / (float)AcropolisDirector.DeathCounterInit)) * AcropolisDirector.DeathPitchScale;
                chargeSnd.timeleft = 2;

                if (deathFrame % 2 == 0) {
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero,
                        Utils.Remap(Main.LocalPlayer.Center.Distance(NPC.Center),
                            AcropolisDirector.DeathShakeFarDistance, AcropolisDirector.DeathShakeNearDistance, 0, AcropolisDirector.DeathShakeMaxPower)));
                    //DeathCounter充电每2tick ShockParticle,NonPremultiplied是旧ShockParticle默认桶
                    PRTLoader.NewParticle<PRT_ShockParticle>(NPC.Center, Vector2.Zero, Color.White, AcropolisDirector.DeathParticleScale * NPC.scale)
                        .Configure(1, true, PRTDrawModeEnum.NonPremultiplied, CEUtils.randomRot());
                }
            }

            if (DeathCounter < 0) {
                if (chargeSnd != null) {
                    chargeSnd.timeleft = 0;
                }
                if (!VaultUtils.isClient) {
                    NPC.dontTakeDamage = false;
                    NPC.StrikeInstantKill();
                    NPC.netUpdate = true;
                }
            }
            if (Main.netMode == NetmodeID.Server) {
                //死亡演出期间逐帧强推:240 帧的定时演出,各端必须同拍
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }
        #endregion

        #region 伤害与交互
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            return NPC.boss;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile) {
            return Defeated ? false : null;
        }

        public override bool? CanBeHitByItem(Player player, Item item) {
            return Defeated ? false : null;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
            target.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 8;
            target.AddBuff(ModContent.BuffType<MechanicalTrauma>(), 180);
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
        }

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox) {
            return false;
        }

        /// <summary>生成敌对弹幕:伤害 <c>NPC.damage / 6.2</c>,击退 4,owner 传 -1。客户端不生成</summary>
        public void Shoot<T>(Vector2 pos, Vector2 velocity, float damageMult = 1, float ai0 = 0, float ai1 = 0, float ai2 = 0) where T : ModProjectile {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                int baseDamage = (int)(NPC.damage / AcropolisDirector.ProjDamageDivisor);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, velocity, ModContent.ProjectileType<T>(),
                    (int)(baseDamage * damageMult), AcropolisDirector.ProjKnockback, -1, ai0, ai1, ai2);
            }
        }
        #endregion

        #region 同步
        /// <summary>
        /// 定长块,顺序固定在这一处。先计时,再持久累加量(朝向、朝向锁存、腿的落点与步数种子、
        /// 手臂两节朝向),再状态标量,最后部件索引。
        /// 字节数是编译期常量:不许加运行时条件决定写不写某个字段
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer) {
            EnsureContext();
            SegCheck();

            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is AcropolisStateBase state) {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            //持久累加量:原版 SyncNPC case 23 不带 rotation,原 ExtraAI 也漏了它与 dir
            writer.Write(NPC.rotation);
            writer.Write((sbyte)dir);

            cannon.NetSend(writer);
            harpoon.NetSend(writer);
            for (int i = 0; i < AcropolisDirector.LegMounts.Length; i++) {
                legs[i].NetSend(writer);
            }

            writer.Write(Context.TeslaCD);
            writer.Write(Context.TeslaUpCD);
            writer.Write(Context.HarpoonCD);
            writer.Write(Context.HarpoonCharge);
            writer.Write(Context.JumpAndShoot);
            writer.Write(Context.PullTimer);
            writer.Write(Context.ShotCue);
            writer.Write(JumpCD);
            writer.Write(Jumping);
            writer.Write(JFlag);

            //SetBoss 故意不过线:它是「本端有没有放过晋升演出」的本地闸,
            //过线会让中途加入的客户端拿到 false,从此再也不切 Boss 音乐
            writer.Write(NPC.boss);
            writer.Write(Defeated);
            writer.Write(DeathCounter);

            writer.Write(_harpoon);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureContext();
            SegCheck();

            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is AcropolisStateBase state) {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            NPC.rotation = reader.ReadSingle();
            dir = reader.ReadSByte();

            cannon.NetReceive(reader);
            harpoon.NetReceive(reader);
            for (int i = 0; i < AcropolisDirector.LegMounts.Length; i++) {
                legs[i].NetReceive(reader);
            }

            Context.TeslaCD = reader.ReadSingle();
            Context.TeslaUpCD = reader.ReadSingle();
            Context.HarpoonCD = reader.ReadSingle();
            Context.HarpoonCharge = reader.ReadSingle();
            Context.JumpAndShoot = reader.ReadInt32();
            Context.PullTimer = reader.ReadInt32();
            Context.ShotCue = reader.ReadByte();
            JumpCD = reader.ReadInt32();
            Jumping = reader.ReadBoolean();
            JFlag = reader.ReadBoolean();

            NPC.boss = reader.ReadBoolean();
            Defeated = reader.ReadBoolean();
            DeathCounter = reader.ReadInt32();

            _harpoon = reader.ReadInt32();

            //中途加入:先对齐开火计数,免得补放一声不属于自己的炮响
            if (!shotCueReady) {
                shotCueReady = true;
                Context.LocalShotCue = Context.ShotCue;
            }
        }
        #endregion

        #region 掉落
        public override void OnKill() {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedAcropolis, -1);
            int dmg = AcropolisDirector.DeathBlastDamage;
            if (Main.expertMode) {
                dmg *= 2;
            }
            if (Main.masterMode || CECal.IsDeathMode) {
                dmg *= 2;
            }
            dmg = (int)(dmg * NPC.scale);
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                CEUtils.SpawnExplotionHostile(NPC.GetSource_FromAI(), NPC.Center, dmg, AcropolisDirector.DeathBlastRadius * NPC.scale, true);
            }
        }

        public override void BossLoot(ref int potionType) {
            potionType = ItemID.HealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HellIndustrialComponents>(), 1, 24, 30));
            // 掉落自有化:灾厄可疑镀层→阿扎弗镀层,数量照搬(material-map §一)
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AzafurePlating>(), 1, 18, 36));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<MottledSpear>(), 5, 1, 1, 2));
            // 遗物:原灾厄复仇/大师条件对齐原版大师掉落惯例(difficulty-map)
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<AcropolisRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AcropolisTrophy>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AzafurePhonograph>(), 8));
            // 首杀传记:承接原灾厄按人实例掉落语义
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<LoreAcropolis>(), 1, 1, 1, new LoreFirstKill()));
        }

        // 首杀传记条件:对应 downed 旗标未置位时每名玩家各掉一份
        private class LoreFirstKill : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedAcropolis;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
        #endregion
    }
}
