using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.NPCs.Cruiser.States;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Skies;
using CalamityEntropy.Core.AI;
using CalamityEntropy.Core.CalamityRef;
using InnoVault;
using InnoVault.PRT;
using InnoVault.StateMachines;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    /// <summary>
    /// 巡游者:本模组的终局 Boss,蠕虫链型多部件,InnoVault 状态机宿主。
    /// <para>
    /// 宿主按固定顺序落地:客户端纠偏与计时收养 → 原版层补偿 → 死亡演出 → 登场骑瓶 →
    /// 战场半径 → 目标校验 → 全局转移(阶段/转阶段) → 清声明 → 状态机 → 朝向结算 →
    /// 嘴部/尾焰/尾鞭结算 → 心跳 → 整链骨架落地。
    /// </para>
    /// <para>
    /// 联机:状态号 <c>ai[3]</c>、阶段 <c>ai[2]</c>,<c>aiStyle = -1</c>;转移与弹幕只在权威端;
    /// 各端跑同一套运动数学;计时与持久累加量随 <c>SendExtraAI</c> 过线,客户端带容差收养。
    /// <b>头、全部体节、尾节都显式关掉原版 netOffset 平滑</b>——整链是头部集中绘制、
    /// 读的是裸坐标,任何一节留着平滑都会让接缝每包崩一次。
    /// </para>
    /// <para>数值在 <see cref="CruiserDirector"/>,轮换在 <see cref="CruiserRotation"/>,
    /// 链条落地在 CruiserChainRig.cs,绘制在 CruiserHead.Draw.cs</para>
    /// </summary>
    [AutoloadBossHead]
    public partial class CruiserHead : ModNPC
    {
        #region 状态机与联机
        private NpcStateMachine<CruiserStateContext> stateMachine;
        public CruiserStateContext Context { get; private set; }
        private readonly CEBossNetMotion netMotion = new();

        /// <summary>当前状态号。读的是已同步的 <c>ai[3]</c>,所以判定在各端一致</summary>
        public CruiserStateIndex CurrentState => (CruiserStateIndex)(int)NPC.ai[3];

        /// <summary>
        /// 阶段 1 或 2。映射到已同步的 <c>ai[2]</c>,不再是单独同步的字段。
        /// <c>EffectLoader</c> 的二阶段像素通道读它,名字与可见性都不能改
        /// </summary>
        public int phase => System.Math.Max(1, (int)NPC.ai[2]);
        #endregion

        #region 战斗状态字段
        // 原灾厄全局 DR 字段的本地等效:承伤按 (1-DR) 结算,随阶段调整并走 SendExtraAI 同步
        public float DamageReduction = CruiserDirector.DRPhase1;

        /// <summary>登场倒计时。>0 期间骑在虚空之瓶上蓄力(无敌、不绘制),归零那一帧揭幕</summary>
        public int noaitime = CruiserDirector.IntroFrames;
        /// <summary>转阶段进度 0~122。二阶段贴图、图鉴头像、体节 Phase2 判定都读它,所以必须过线</summary>
        public int phaseTrans = 0;
        /// <summary>无目标累计帧数。<b>原代码从不清零</b>,是全场累计值</summary>
        public int notargettime = 0;
        /// <summary>战场半径与其目标值,越界玩家每帧续虚空侵蚀</summary>
        public float maxDistance = CruiserDirector.ArenaRadiusStart;
        public float maxDistanceTarget = CruiserDirector.ArenaRadiusTargetStart;
        /// <summary>战场中心:本体与尾节的中点,转阶段期间改成玩家所在</summary>
        public Vector2 SpaceCenter = Vector2.Zero;
        /// <summary>尾节实体索引(原 <c>tail</c>)。原代码只写不读,过线保留以免留下各端不一致的公开字段</summary>
        public int tail = -1;

        public bool DeathAnm = false;
        public int DeathAnmCount = CruiserDirector.DeathAnmFrames;

        private int length = CruiserDirector.ChainSegments;
        private bool b_added = false;
        /// <summary>转阶段的体节增删只做一次(纯本地闩锁,判据 <c>phaseTrans &gt;= 122</c> 本身是同步量)</summary>
        private bool phase2SegmentsDone = false;
        #endregion

        #region 链条与表现字段
        /// <summary>虚拟骨节坐标。由已过线的本体坐标与朝向确定性重算(一阶滤波,自收敛),不过线</summary>
        public List<Vector2> bodies = new List<Vector2>();
        /// <summary>本帧绘制用的头部坐标。netOffset 已被清零,所以它与 <c>NPC.Center</c> 同一平滑层级</summary>
        public Vector2 vtodraw = new Vector2();

        /// <summary>鞭毛张角(原 <c>da</c>)。它同时是尾部新星的触发判据,所以要过线</summary>
        public float flagellumAngle = 50;
        /// <summary>鞭毛静息角(原 <c>ja</c>),每帧由速度推出,纯中间量</summary>
        private float flagellumRest = 50;
        /// <summary>鞭击角速度(原 <c>tail_vj</c>)与鞭击进行中闩锁(原 <c>jv</c>),都要过线</summary>
        public float whipSpeed = 0;
        public bool whipActive = false;

        public float alpha = 1;
        public float whiteLerp = 0;
        public float camLerp = 0;
        public float WarningAlpha = 0;
        /// <summary>二阶段像素通道用:只有 <c>EffectLoader</c> 代调 <see cref="PreDraw"/> 时才为真</summary>
        public bool candraw = false;
        #endregion

        #region 遗留字段(仅为保持对外形状,AI 不再读写)
        // 以下字段是从旧天顶 AI 抄过来的残留,迁移前就已经只在 SendExtraAI 里来回搬、没有任何读点。
        // 本轮把它们从同步块里摘掉,字段本身保留(全仓 grep 无外部引用,但它们是 public)
        public float ProgressDraw = 0;
        public float speedMuti = 1;
        public float speed = 18;
        public float targetSpeed = 18;
        public int slowDownTime = 0;
        public int nrc = 0;
        public int rotDist = 900;
        public Vector2 rotPos = Vector2.Zero;
        public int circleDir = 1;
        public bool flag = false;
        public int counterc = 0;
        public float jaslowdown = 0;
        /// <summary>从未被赋值,所以 <see cref="ModifyCollisionData"/> 与 <see cref="ModifyHitPlayer"/> 的两条分支是死代码。照搬</summary>
        public float aitype = 0;
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
                CustomTexturePath = "CalamityEntropy/Assets/Extra/CruiserBes",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
            //蠕虫平滑陷阱:原版只在 aiStyle >= 0 时查 NoMultiplayerSmoothingByAI,而这条虫从来没设过
            //aiStyle(默认 0),所以它一直吃着原版 netOffset 平滑;状态机要占 ai[3] 又必须把 aiStyle 改 -1,
            //于是只剩按类型豁免这一条路。整链是头部集中绘制、读裸坐标,头与体节任一节留着平滑,
            //接缝就会每包崩一次——所以头/体/尾三个类型都要显式关掉(体节与尾节在各自文件里关)
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetDefaults()
        {
            // 原灾厄 DR 体系本地化:一阶段减伤 54%,二阶段 42%(见 DamageReduction/ModifyIncomingHit)
            DamageReduction = CruiserDirector.DRPhase1;
            NPC.boss = true;
            //状态机把状态号写在 ai[3],原版 AI 层必须让位
            NPC.aiStyle = -1;
            NPC.width = CruiserDirector.Width;
            NPC.height = CruiserDirector.Height;
            NPC.damage = CruiserDirector.BaseDamage;
            if (Main.expertMode)
            {
                NPC.damage += CruiserDirector.DamageExpert;
            }
            if (Main.masterMode)
            {
                NPC.damage += CruiserDirector.DamageMaster;
            }
            NPC.defense = CruiserDirector.Defense;
            NPC.lifeMax = CruiserDirector.LifeMax;
            //装灾厄读死亡/复仇,缺席仍走大师/专家兜底
            if (CECal.IsDeathMode)
            {
                NPC.damage += CruiserDirector.DamageDeath;
                length += CruiserDirector.ChainSegmentsDeathBonus;
            }
            else if (CECal.IsRevengeance)
            {
                NPC.damage += CruiserDirector.DamageRevenge;
                length += CruiserDirector.ChainSegmentsRevengeBonus;
            }
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCHit4;
            NPC.value = CruiserDirector.Value;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = CruiserDirector.VoidTouchDR;
            NPC.dontCountMe = true;
            NPC.scale = 1f;
            if (Main.masterMode)
            {
                NPC.scale = CruiserDirector.ScaleMaster;
            }
            if (Main.getGoodWorld)
            {
                NPC.scale = CruiserDirector.ScaleGetGood;
                NPC.lifeMax += CruiserDirector.LifeMaxGetGoodBonus;
            }
            if (Main.zenithWorld)
            {
                NPC.scale = CruiserDirector.ScaleZenith;
                length = CruiserDirector.ChainSegmentsZenith;
            }
            NPC.netAlways = true;
            NPC.Entropy().damageMul = CruiserDirector.DamageMulStart;
            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/CruiserBoss");
            }
            SpawnModBiomes = new int[] { ModContent.GetInstance<VoidDummyBoime>().Type };
        }
        #endregion

        #region 状态机装配
        private void EnsureContext()
        {
            Context ??= new CruiserStateContext
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
            stateMachine = new NpcStateMachine<CruiserStateContext>(Context);
            CEBossHost.HookStateSwapAdoption(netMotion, stateMachine);

            IVaultState<CruiserStateContext> initial = null;
            if (VaultUtils.isClient)
            {
                initial = VaultStateRegistry<CruiserStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new CruiserTryToClosePlayerState());
        }
        #endregion

        #region AI
        public override void AI()
        {
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

            //原本 aiStyle = 0 时由原版 AI 层每帧代做的两件事。状态机占用 ai[3] 迫使 aiStyle 改 -1,
            //原版层随之不再执行,这里自己补上(原代码在接战分支里另有一次等价的 TargetClosest)
            int lastTarget = NPC.target;
            NPC.TargetClosest();
            NPC.spriteDirection = NPC.direction;
            if (!client && NPC.target != lastTarget)
            {
                //换目标是决策点
                NPC.netUpdate = true;
            }

            UpdateHitRecords();

            if (DeathAnm)
            {
                UpdateDeathAnimation();
                if (!client)
                {
                    CEBossHost.Heartbeat(NPC);
                }
                vtodraw = NPC.Center;
                UpdateChain();
                if (client)
                {
                    netMotion.EndFrame(NPC);
                }
                return;
            }

            NPC.Entropy().damageMul += CruiserDirector.DamageMulRamp;
            if (NPC.Entropy().damageMul > 1)
            {
                NPC.Entropy().damageMul = 1;
            }
            counterc++;
            ReportSky();

            if (noaitime > 0)
            {
                NPC.dontTakeDamage = true;
                for (int i = 0; i < bodies.Count; i++)
                {
                    bodies[i] = NPC.Center;
                }
                foreach (Projectile pj in Main.ActiveProjectiles)
                {
                    if (pj.ModProjectile is VoidBottleThrow)
                    {
                        //骑瓶期是位置直写,不是速度积分,预测器会跟它打架,丢掉预测
                        NPC.Center = pj.Center;
                        netMotion.ForgetPrediction();
                        break;
                    }
                }
            }
            else if (CurrentState == CruiserStateIndex.PhaseTransing)
            {
                NPC.dontTakeDamage = true;
            }
            noaitime--;

            if (noaitime == 0)
            {
                NPC.dontTakeDamage = false;
                //登场揭幕拍点:天幕闪电齐发
                CruiserSkyDrive.PushBurst(CruiserDirector.SkyIntroBurstBolts);
                if (!client)
                {
                    NPC.netUpdate = true;
                }
            }

            if (noaitime < 0)
            {
                EnsureChainParts();
                Main.LocalPlayer.Entropy().crSky = CruiserDirector.LegacySkyTimer;
                maxDistance += (maxDistanceTarget - maxDistance) * CruiserDirector.ArenaRadiusLerp;
                ApplyArenaDebuff();

                UpdateContextFacts();
                if (Context.TargetValid)
                {
                    UpdateMouthApproach();
                    EvaluatePhaseTransition();
                    SettleArena();
                }
                //脱战也要走 Update:客户端靠这里的 NetSync 收到权威端的换态。
                //状态体本身见 RequiresTarget,没目标不跑
                Context.BeginFrameDefaults();
                stateMachine.Update();

                if (Context.TargetValid)
                {
                    //虚空激光自管朝向,其余状态朝向跟速度走
                    if (CurrentState != CruiserStateIndex.VoidLaser)
                    {
                        NPC.rotation = NPC.velocity.ToRotation();
                    }
                }
                else
                {
                    notargettime++;
                    NPC.velocity.Y += CruiserDirector.NoTargetRise;
                    if (notargettime > CruiserDirector.DespawnNoTargetFrames && !client)
                    {
                        //实体生死收归权威端(原代码各端都写,客户端那一次会被下一个快照打回来)
                        NPC.active = false;
                        NPC.netUpdate = true;
                    }
                    NPC.rotation = NPC.velocity.ToRotation();
                }

                SettleMouth();
                UpdatePhase2Exhaust();
                UpdateFlagellum();
            }

            if (!client)
            {
                CEBossHost.Heartbeat(NPC);
            }
            vtodraw = NPC.Center;
            UpdateChain();
            if (client)
            {
                netMotion.EndFrame(NPC);
            }
        }

        /// <summary>天空强度续租(各端本地):骑瓶蓄力期渐临到 0.6,揭幕后推满;P2 转换抬躁动</summary>
        private void ReportSky()
        {
            //死亡演出分支在上方提前 return,续租自然过期,天空威压随死亡消退
            float skyDrive = noaitime > 0
                ? (1f - noaitime / CruiserDirector.SkyIntroDivisor) * CruiserDirector.SkyIntroCap
                : 1f;
            float skyAgitation = Context.Phase == 2
                ? MathHelper.Clamp(phaseTrans / CruiserDirector.SkyAgitationDivisor, 0f, 1f)
                : 0f;
            CruiserSkyDrive.Report(skyDrive, skyAgitation);
        }

        private void UpdateDeathAnimation()
        {
            WarningAlpha = 0;
            if (camLerp < 1)
            {
                camLerp += CruiserDirector.DeathCamRamp;
            }
            else
            {
                camLerp = CruiserDirector.DeathCamHold;
            }
            Main.LocalPlayer.Entropy().screenShift = camLerp;
            Main.LocalPlayer.Entropy().screenPos = NPC.Center;
            if (NPC.velocity.Length() > CruiserDirector.DeathSpeedFloor)
            {
                NPC.velocity *= CruiserDirector.DeathDrag;
            }
            NPC.rotation = NPC.velocity.ToRotation();
            DeathAnmCount--;
            if (whiteLerp < 1)
            {
                whiteLerp += CruiserDirector.DeathWhiteRamp;
            }
            //死亡演出每 6 tick 一颗爆闪,dedServ 守卫别漏,服务端孤儿 PRT 对不上
            if (DeathAnmCount % CruiserDirector.DeathBurstInterval == 0 && !Main.dedServ)
            {
                PRTLoader.NewParticle<PRT_PremultBurst>(NPC.Center, Vector2.Zero, Color.LightBlue, 3.2f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
            if (DeathAnmCount <= 0 && !VaultUtils.isClient)
            {
                NPC.StrikeInstantKill();
                NPC.netSpam = 9;
                NPC.netUpdate = true;
            }
        }

        /// <summary>生成整条链。骨节坐标各端都建(节数由已同步的世界难度决定),实体只在权威端生成</summary>
        private void EnsureChainParts()
        {
            if (b_added)
            {
                return;
            }
            b_added = true;
            for (int i = 0; i < length + 1; i++)
            {
                bodies.Add(NPC.Center - new Vector2(0, 0));
            }
            if (VaultUtils.isClient)
            {
                return;
            }
            int syg = NPC.whoAmI;
            for (int i = 0; i < length + 1; i++)
            {
                int type = i == length ? ModContent.NPCType<CruiserTail>() : ModContent.NPCType<CruiserBody>();
                int bodyIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, type);

                Main.npc[bodyIndex].ai[1] = syg;
                Main.npc[bodyIndex].ai[2] = i;
                //体节自己的 ai[3] 存头部索引,这一处保留:体节不跑状态机,槽位不冲突。
                //被摘掉的是原代码紧接着那句 NPC.ai[3] = syg(头部自己的 ai[3]),它已让位给状态号
                Main.npc[bodyIndex].ai[3] = NPC.whoAmI;
                Main.npc[bodyIndex].realLife = NPC.whoAmI;
                syg = bodyIndex;
                //NewNPC 的首包在 ai 槽赋值之前就发了,所以这里必须补一包
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, bodyIndex);
                    Main.npc[bodyIndex].netUpdate = true;
                }
            }
            tail = syg;
            //生成部件是决策点
            NPC.netUpdate = true;
        }

        private void ApplyArenaDebuff()
        {
            foreach (Player p in Main.ActivePlayers)
            {
                if (CEUtils.getDistance(SpaceCenter, p.Center) > maxDistance)
                {
                    if (!Main.dedServ)
                    {
                        p.AddBuff(ModContent.BuffType<VoidTouch>(), CruiserDirector.ArenaDebuffFrames);
                    }
                }
            }
        }

        private void UpdateContextFacts()
        {
            Context.Npc = NPC;
            Context.Owner = this;
            Context.Target = NPC.HasValidTarget ? Main.player[NPC.target] : null;
            Context.TargetValid = NPC.HasValidTarget;
            Context.TargetDistance = Context.TargetValid ? NPC.Distance(Context.Target.Center) : 0f;
        }

        /// <summary>张嘴预备(纯绘制)。六个状态被排除在外,它们自己管嘴</summary>
        private void UpdateMouthApproach()
        {
            Player target = Context.Target;
            float dist = NPC.Distance(target.Center);
            CruiserStateIndex state = CurrentState;
            bool excluded = state == CruiserStateIndex.SplittingVoidStar
                || state == CruiserStateIndex.VoidResidue
                || state == CruiserStateIndex.BiteAndDash
                || state == CruiserStateIndex.EnergyBall
                || state == CruiserStateIndex.AroundPlayerAndShootVoidStar
                || state == CruiserStateIndex.AroundSpawnVoidBomb;
            if (!Context.Biting && dist < CruiserDirector.MouthOpenFar && !excluded)
            {
                Context.MouthRot += Utils.Remap(dist, CruiserDirector.MouthOpenFar, CruiserDirector.MouthOpenNear, 0, CruiserDirector.MouthOpenRate);
                if (dist < float.Max(CruiserDirector.BiteTriggerSpeedFloor, NPC.velocity.Length()) * CruiserDirector.BiteTriggerFactor)
                {
                    Context.Biting = true;
                }
            }
        }

        /// <summary>
        /// 阶段与转阶段。原代码把这一段写在状态判定<b>之前</b>,每帧强制 <c>ai = PhaseTransing</c>,
        /// 所以被打断那一手当帧就不再执行——这里的调用顺序保持一致
        /// </summary>
        private void EvaluatePhaseTransition()
        {
            //原代码是整数除法 lifeMax / 2,每帧重算一次
            int phaseNow = NPC.life < NPC.lifeMax / CruiserDirector.Phase2LifeDivisor ? 2 : 1;
            if (Context.Phase != phaseNow)
            {
                Context.Phase = phaseNow;
                if (!VaultUtils.isClient)
                {
                    NPC.netUpdate = true;
                }
            }
            if (phaseNow != 2)
            {
                return;
            }

            if (phaseTrans < CruiserDirector.PhaseTransFrames)
            {
                if (!VaultUtils.isClient && CurrentState != CruiserStateIndex.PhaseTransing)
                {
                    stateMachine.ChangeState(new CruiserPhaseTransingState());
                }
                phaseTrans++;
                //二阶段转换拍点:一次性闪电爆发
                if (phaseTrans == 1)
                {
                    CruiserSkyDrive.PushBurst(CruiserDirector.SkyPhaseTransBurstBolts);
                }
                alpha *= CruiserDirector.PhaseTransAlphaDecay;
                Context.AttackIndex = 0;
                if (phaseTrans <= CruiserDirector.PhaseTransClearWindow)
                {
                    flagellumAngle = 0;
                    whipSpeed = 0;
                    whipActive = false;
                    foreach (Projectile p in Main.ActiveProjectiles)
                    {
                        if (p.ModProjectile is CruiserEnergyBall || p.ModProjectile is VoidResidue)
                        {
                            p.active = false;
                        }
                    }
                }
                return;
            }

            //转阶段收尾。原代码只在越线那一帧做一次,这里每帧幂等重申(判据 phaseTrans 已过线,各端同值)
            NPC.Entropy().VoidTouchDR = CruiserDirector.VoidTouchDRPhase2;
            NPC.dontTakeDamage = false;
            NPC.width = CruiserDirector.WidthPhase2;
            NPC.height = CruiserDirector.HeightPhase2;
            ApplyPhase2Segments();
            if (!VaultUtils.isClient && CurrentState == CruiserStateIndex.PhaseTransing)
            {
                //原代码在这里直写 ai = VoidSpike:不走选招口,所以既不清 ChangeCounter 也不动 AttackIndex。
                //二阶段第一手尖刺因此带着被打断那一手的残余计数起跑,见 CruiserRotation 注释
                stateMachine.ChangeState(new CruiserVoidSpikeState());
                NPC.netUpdate = true;
            }
            if (alpha < 1)
            {
                alpha += CruiserDirector.PhaseTransAlphaRise;
                if (alpha > 1)
                {
                    alpha = 1;
                }
            }
        }

        /// <summary>
        /// 转阶段的体节增删。缩尺寸是确定性的、各端都做(判据 <c>ai[2]</c> 与 <c>ai[3]</c> 都随原版快照过线);
        /// 摘掉多余体节属于实体生死,只在权威端做并显式补包。
        /// 原代码按 <c>realLife</c> 认亲,而 <c>realLife</c> 不随快照过线,客户端认不出来;
        /// 改用体节自己的 <c>ai[3]</c>(生成时写的就是同一个头部索引)
        /// </summary>
        private void ApplyPhase2Segments()
        {
            if (phase2SegmentsDone)
            {
                return;
            }
            bool authority = !VaultUtils.isClient;
            bool found = false;
            foreach (NPC n in Main.npc)
            {
                if (!n.active || (n.ModNPC is not CruiserBody && n.ModNPC is not CruiserTail) || (int)n.ai[3] != NPC.whoAmI)
                {
                    continue;
                }
                found = true;
                if (n.ai[2] <= CruiserDirector.SegmentKeepMaxIndex && n.ai[2] > CruiserDirector.SegmentShrinkMinIndex)
                {
                    n.width = CruiserDirector.SegmentSizePhase2;
                    n.height = CruiserDirector.SegmentSizePhase2;
                }
                if (authority && n.ai[2] > CruiserDirector.SegmentKeepMaxIndex)
                {
                    n.active = false;
                    n.netUpdate = true;
                    if (Main.dedServ)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n.whoAmI, 0f, 0f, 0f, 0);
                    }
                }
            }
            if (found)
            {
                phase2SegmentsDone = true;
            }
        }

        private void SettleArena()
        {
            maxDistanceTarget = CruiserDirector.ArenaRadiusEngaged;
            if (bodies.Count > 0)
            {
                SpaceCenter = (NPC.Center + bodies[bodies.Count - 1]) / 2f;
            }
            if (CurrentState == CruiserStateIndex.PhaseTransing)
            {
                SpaceCenter = Context.Target.Center;
                maxDistanceTarget = CruiserDirector.ArenaRadiusPhaseTrans;
            }
        }

        /// <summary>咬合结算(纯绘制)。写在接战分支之外,无目标时也照跑,原代码如此</summary>
        private void SettleMouth()
        {
            if (Context.Biting)
            {
                Context.MouthRot += CruiserDirector.BiteCloseRate;
                if (Context.MouthRot < CruiserDirector.MouthMin)
                {
                    Context.Biting = false;
                }
            }
            else
            {
                Context.MouthRot *= CruiserDirector.MouthDecay;
            }
            if (Context.MouthRot < CruiserDirector.MouthMin)
            {
                Context.MouthRot = CruiserDirector.MouthMin;
            }
        }

        /// <summary>二阶段尾焰与全场无限飞行。前者纯绘制,后者是原灾厄无限飞行改成每帧回满翅膀时间</summary>
        private void UpdatePhase2Exhaust()
        {
            if (phaseTrans <= CruiserDirector.PhaseTransDrawSwitch)
            {
                return;
            }
            foreach (var plr in Main.ActivePlayers)
            {
                plr.wingTime = plr.wingTimeMax;
            }
            if (Main.dedServ)
            {
                return;
            }
            var r = Main.rand;
            for (int i = 0; i < CruiserDirector.ExhaustCount; i++)
            {
                var p = PRTLoader.NewParticle<PRT_Void>(NPC.Center - NPC.rotation.ToRotationVector2() * CruiserDirector.ExhaustNozzleBack,
                    new Vector2((float)((r.NextDouble() - 0.5) * CruiserDirector.ExhaustJitterX), (float)((r.NextDouble() - 0.5) * CruiserDirector.ExhaustJitterY)), Color.White, 1f);
                p.shape = 4;
                p.Opacity = CruiserDirector.ExhaustOpacity * NPC.scale;
                p.ad = CruiserDirector.ExhaustFade;
            }
            for (int i = 0; i < CruiserDirector.ExhaustCount; i++)
            {
                var p = PRTLoader.NewParticle<PRT_Void>(NPC.Center - NPC.rotation.ToRotationVector2() * CruiserDirector.ExhaustNozzleBack - NPC.velocity * 0.5f,
                    new Vector2((float)((r.NextDouble() - 0.5) * CruiserDirector.ExhaustJitterX), (float)((r.NextDouble() - 0.5) * CruiserDirector.ExhaustJitterY)), Color.White, 1f);
                p.shape = 4;
                p.Opacity = CruiserDirector.ExhaustOpacity * NPC.scale;
                p.ad = CruiserDirector.ExhaustFade;
            }
        }
        #endregion

        #region 同步
        /// <summary>
        /// 定长块,顺序固定在这一处。先计时,再持久累加量,再状态标量,最后部件索引。
        /// 字节数是编译期常量:不许加运行时条件决定写不写某个字段。
        /// <para>
        /// 迁移前这里搬的是一堆从旧天顶 AI 抄来、AI 根本不读的残留字段(speedMuti / rotPos / circleDir …),
        /// 已全部摘掉;新增过线的是原版漏同步的几项:本体朝向、鞭毛角、鞭击角速度与闩锁、
        /// 战场半径、原 <c>localAI[2]</c> 的激光瞄准计时、轮换序号
        /// </para>
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is CruiserStateBase state)
            {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            //持久累加量:被逐帧积分出来、又反过来决定出手时机的量
            writer.Write(NPC.rotation);
            writer.Write(flagellumAngle);
            writer.Write(whipSpeed);
            writer.Write(whipActive);
            writer.Write(maxDistance);
            writer.WriteVector2(SpaceCenter);

            //状态标量
            writer.Write(Context.ChangeCounter);
            writer.Write(Context.LaserAim);
            writer.Write(Context.AttackIndex);
            writer.Write(noaitime);
            writer.Write(phaseTrans);
            writer.Write(NPC.defense);
            writer.Write(DamageReduction);
            writer.Write(NPC.dontTakeDamage);
            writer.Write(DeathAnm);
            writer.Write(DeathAnmCount);

            //部件索引
            writer.Write(tail);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is CruiserStateBase state)
            {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            NPC.rotation = reader.ReadSingle();
            flagellumAngle = reader.ReadSingle();
            whipSpeed = reader.ReadSingle();
            whipActive = reader.ReadBoolean();
            maxDistance = reader.ReadSingle();
            SpaceCenter = reader.ReadVector2();

            Context.ChangeCounter = AdoptCounter(Context.ChangeCounter, reader.ReadInt32());
            Context.LaserAim = AdoptCounter(Context.LaserAim, reader.ReadInt32());
            Context.AttackIndex = reader.ReadInt32();
            noaitime = reader.ReadInt32();
            phaseTrans = reader.ReadInt32();
            NPC.defense = reader.ReadInt32();
            DamageReduction = reader.ReadSingle();
            NPC.dontTakeDamage = reader.ReadBoolean();
            DeathAnm = reader.ReadBoolean();
            DeathAnmCount = reader.ReadInt32();

            tail = reader.ReadInt32();
        }

        /// <summary>帧计数按计时口径收养:容差内不动本地值,硬对齐会让 <c>== N</c> 型一次性拍被跳过或重放</summary>
        private static int AdoptCounter(int local, int synced)
            => CEBossNetMotion.AdoptTimer(local, synced);
        #endregion

    }
}
