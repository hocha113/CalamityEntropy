using CalamityEntropy.Common;
using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.NPCs.VoidDestroyer.States;
using CalamityEntropy.Core.AI;
using InnoVault;
using InnoVault.StateMachines;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 虚空驱逐舰主控:月后 T2 Boss,InnoVault 状态机宿主。
    /// 状态只写声明(<see cref="VDStateContext"/>),宿主按固定顺序落地:目标校验 → 全局转移(权威端)→
    /// 清声明 → 状态机 → 运动落地/闪现 → 判定窗 → 限制圈 → 视觉推导。
    /// 联机契约:转移只在权威端(状态号走 ai[3],阶段走 ai[2]),各端本地跑同一状态机做表现;
    /// 状态计时与服务端掷骰事实随 SendExtraAI 原子过线,客户端带容差收养(<see cref="CEBossNetMotion"/>);
    /// 弹幕只在权威端生成,粒子/音效/震屏/滤镜全走 !dedServ。
    /// 数值全部在 <see cref="VDDirector"/>,轮换与防复读在 <see cref="VDRotation"/>,绘制在 VoidDestroyer.Draw.cs
    /// </summary>
    [AutoloadBossHead]
    public partial class VoidDestroyer : ModNPC
    {
        #region 资源与字段
        //贴图由 VaultLoaden 反射赋值,= null 只为压掉 CS0649
        [VaultLoaden("CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyerP2")]
        private static Asset<Texture2D> p2Tex = null;
        [VaultLoaden("CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyerTransform")]
        private static Asset<Texture2D> transformTex = null;
        [VaultLoaden("CalamityEntropy/Content/NPCs/VoidDestroyer/EnergyWing")]
        private static Asset<Texture2D> wingTex = null;

        /// <summary>对外配色入口(弹幕沿用)</summary>
        public static Color VoidPurple => VDVfx.VoidPurple;

        private NpcStateMachine<VDStateContext> stateMachine;
        /// <summary>状态上下文:声明总线 + 事实。弹幕只读它的表现通道</summary>
        public VDStateContext Context { get; private set; }
        private Player targetPlayer;
        /// <summary>联机运动:客户端位置纠偏 + 状态计时收养</summary>
        private readonly CEBossNetMotion netMotion = new();

        #region 本地视觉字段(由同步状态逐帧推导,不过线)
        public float Alpha = 1f;
        public float DrawScale = 1f;
        /// <summary>假 Z 深度平滑值 0..1</summary>
        public float FakeZ;
        public float WingAlpha;
        public float WingExpand;
        public float WingRotation;
        public float ShieldAlpha;
        public float CoreGlow;
        public Color CoreColor = VDVfx.VoidPurple;
        #endregion

        public VDStateIndex CurrentStateIndex => (VDStateIndex)(int)NPC.ai[3];
        public Player Target => Main.player[NPC.target];
        public bool Dying => Context != null && Context.Dying;
        public int Phase => Context?.Phase ?? Math.Max(1, (int)NPC.ai[2]);
        public Vector2 AnchorPos => Context?.AnchorPos ?? NPC.Center;
        /// <summary>核心世界坐标(弹幕出手点)</summary>
        public Vector2 CorePos => NPC.Center + CoreOffset.RotatedBy(NPC.rotation) * DrawScale;
        public bool InCinematic => CurrentStateIndex is VDStateIndex.Entrance or VDStateIndex.Transform
            or VDStateIndex.ShieldUp or VDStateIndex.Death or VDStateIndex.Despawn;
        #endregion

        #region 定义
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
            //冲刺 40px/f,原版 netOffset 平滑只会让它在联机里抽搐;关掉后 netOffset 每帧被原版清零,
            //本类借它做纯绘制层的抖动偏移(CEBossNetMotion.DrawShake)
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
            //残影用原版 oldPos 缓存
            NPCID.Sets.TrailCacheLength[Type] = 10;
            NPCID.Sets.TrailingMode[Type] = 3;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.55f,
                PortraitScale = 0.7f,
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.VoidDestroyerBestiary")
            });
        }

        public override void SetDefaults()
        {
            NPC.boss = true;
            NPC.aiStyle = -1;
            NPC.width = 140;
            NPC.height = 84;
            NPC.damage = VDDirector.BaseDamage;
            NPC.defense = VDDirector.BaseDefense;
            NPC.lifeMax = VDDirector.BaseLife;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.dontCountMe = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = Item.buyPrice(gold: 15);
            NPC.Entropy().VoidTouchDR = VDDirector.VoidTouchDR;
            if (!Main.dedServ)
            {
                //暂无专属曲目,先挂巡游者主题占位
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/CruiserBoss");
            }
            SpawnModBiomes = new int[] { ModContent.GetInstance<VoidDummyBoime>().Type };
        }

        public override void OnSpawn(IEntitySource source)
        {
            EnsureContext();
            //召唤物走 SpawnOnPlayer,把本体挪到召唤者头顶,出场传送门就开在这里
            if (source is EntitySource_BossSpawn bossSpawn && bossSpawn.Target is Player summoner)
            {
                NPC.target = summoner.whoAmI;
                Context.AnchorPos = summoner.Center + new Vector2(0, -420);
            }
            else
            {
                NPC.TargetClosest(false);
                Context.AnchorPos = NPC.Center;
            }
            NPC.Center = Context.AnchorPos;
            NPC.velocity = Vector2.Zero;
            NPC.ai[3] = (int)VDStateIndex.Entrance;
            NPC.ai[2] = 1;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            NPC.netUpdate = true;
        }
        #endregion

        #region 承伤
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1f - (Context?.DamageReduction ?? VDDirector.DRPhase12);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            //天顶剑与七彩水晶召唤杖只造成一半伤害
            if (projectile.type == ProjectileID.FinalFractal || projectile.type == ProjectileID.RainbowCrystal || projectile.type == ProjectileID.RainbowCrystalExplosion)
            {
                modifiers.SourceDamage *= 0.5f;
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return ContactDamageActive();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddDebuffFixed(ModContent.BuffType<VoidFire>(), 180);
        }

        /// <summary>策划表的大师显示值 → 当前难度下的弹幕 damage 字段(命中固定 ×2,故再除 2)</summary>
        public int ProjDamage(int masterShown)
        {
            return Math.Max(1, (int)Math.Round(NPC.defDamage * (masterShown / (float)VDDirector.MasterContactDamage) / 2f));
        }

        /// <summary>接触伤害窗:演出/闪现/落地宽限/半透明/退入背景时一律关,其余由状态声明</summary>
        public bool ContactDamageActive()
        {
            if (Context == null || Context.Dying || InCinematic || Context.BlinkTimer > 0 || Context.NoContactTimer > 0)
            {
                return false;
            }
            if (Alpha < 0.6f || FakeZ > 0.3f)
            {
                return false;
            }
            return Context.ContactWindow;
        }
        #endregion

        #region 掉落
        public override void BossLoot(ref int potionType)
        {
            potionType = ModContent.ItemType<VoidHealingPotion>();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<VoidDestroyerBag>()));
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<VoidHealingPotion>(), 1, 5, 15, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DimBearing>(), 1, 15, 25));
            npcLoot.Add(normalOnly);

            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<VoidDestroyerRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoidDestroyerTrophy>(), 10));
        }

        // 恒真但隐藏图鉴条目的条件(与巡游者一致)
        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }

        public override void OnKill()
        {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedVoidDestroyer, -1);
        }
        #endregion

        #region 死亡与消失
        /// <summary>锁血:死亡演出没放完不许真死,一击超杀也拦回演出;客户端计时可能落后几帧,留容差免得收到击杀包时把自己救活</summary>
        public override bool CheckDead()
        {
            EnsureContext();
            if (Context.DeathPerformanceFinished)
            {
                return true;
            }
            if (Context.Dying && stateMachine?.CurrentState is VDDeathState death
                && death.Counter >= VDDirector.DeathDuration - VDDirector.DeathKillTolerance)
            {
                return true;
            }
            if (!Context.Dying)
            {
                Context.Dying = true;
                Context.BlinkTimer = 0;
                if (!VaultUtils.isClient && stateMachine != null && stateMachine.CurrentState is not VDDeathState)
                {
                    stateMachine.ChangeState(new VDDeathState());
                }
                NPC.netUpdate = true;
            }
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            NPC.active = true;
            return false;
        }

        public override bool CheckActive() => false;
        #endregion

        #region 状态机装配
        private void EnsureContext()
        {
            Context ??= new VDStateContext
            {
                Npc = NPC,
                Owner = this,
            };
        }

        private void InitializeStateMachine()
        {
            EnsureContext();
            stateMachine = new NpcStateMachine<VDStateContext>(Context);

            //换态包带的是新态的计时:客户端在框架换态(新实例 OnEnter 刚清零)之后立刻收养
            stateMachine.OnStateChanged += (_, next, _) =>
            {
                if (VaultUtils.isClient && next is VDStateBase entered
                    && netMotion.TryTakeTiming(entered.StateId, out int timer, out int counter))
                {
                    entered.AdoptNetTiming(timer, counter);
                }
            };

            //中途加入的客户端从 ai[3] 恢复状态,回退入场
            IVaultState<VDStateContext> initial = null;
            if (VaultUtils.isClient)
            {
                initial = VaultStateRegistry<VDStateContext>.Create((int)NPC.ai[3]);
            }
            stateMachine.SetInitialState(initial ?? new VDEntranceState());
        }
        #endregion

        #region 主 AI
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
                //自接位置纠偏 + 同态收包的计时收养(本帧拍点从权威端的计时起算)
                netMotion.BeginFrame(NPC);
                if (stateMachine.CurrentState is VDStateBase adopting
                    && netMotion.TryTakeTiming(adopting.StateId, out int timer, out int counter))
                {
                    adopting.AdoptNetTiming(timer, counter);
                }
            }

            NPC.damage = 0;
            FindTarget();
            UpdateContextFacts();
            EvaluateGlobalTransitions();

            if (Context.NoContactTimer > 0)
            {
                Context.NoContactTimer--;
            }
            if (Context.AttackCooldown > 0)
            {
                Context.AttackCooldown--;
            }

            Context.BeginFrameDefaults();
            stateMachine.Update();

            if (Context.BlinkTimer > 0)
            {
                UpdateBlink();
            }
            else
            {
                ApplyDeclaredMovement();
            }
            ApplyTilt();

            NPC.damage = ContactDamageActive() ? NPC.defDamage : 0;
            NPC.dontTakeDamage = Context.Dying || InCinematic && CurrentStateIndex != VDStateIndex.ShieldUp && CurrentStateIndex != VDStateIndex.Despawn;

            UpdateArena();
            UpdateVisualState();
            FocusCamera();

            if (client)
            {
                netMotion.EndFrame(NPC);
            }
            else if (Main.GameUpdateCount % CEBossNetMotion.HeartbeatFrames == 0)
            {
                //决策点(换态/闪现/出手锁向)各自 netUpdate,这里只留慢频兜底心跳
                NPC.netUpdate = true;
            }
        }

        private void FindTarget()
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !Main.player[NPC.target].active || Main.player[NPC.target].dead)
            {
                NPC.TargetClosest(false);
            }
            targetPlayer = Main.player[NPC.target];
        }

        private void UpdateContextFacts()
        {
            Context.Npc = NPC;
            Context.Owner = this;
            Context.Target = targetPlayer;
            Context.TargetValid = NPC.HasValidTarget && targetPlayer != null && targetPlayer.active && !targetPlayer.dead
                && NPC.Distance(targetPlayer.Center) <= VDDirector.MaxFindDistance;
        }

        /// <summary>全局转移,仅权威端:死亡/脱战/转阶段;演出与连接段中不打断</summary>
        private void EvaluateGlobalTransitions()
        {
            if (VaultUtils.isClient || stateMachine?.CurrentState == null)
            {
                return;
            }
            if (Context.Dying)
            {
                if (stateMachine.CurrentState is not VDDeathState)
                {
                    stateMachine.ChangeState(new VDDeathState());
                }
                return;
            }
            IVaultState<VDStateContext> current = stateMachine.CurrentState;
            if (current is VDEntranceState or VDTransformState or VDShieldUpState or VDDespawnState or VDDeathState)
            {
                return;
            }

            if (!Context.TargetValid)
            {
                stateMachine.ChangeState(new VDDespawnState());
                return;
            }

            //75%:变形展翼
            if (Context.Phase == 1 && NPC.life <= NPC.lifeMax * VDDirector.Phase2LifeRatio)
            {
                stateMachine.ChangeState(new VDTransformState());
                return;
            }
            //30%:护盾展开连接段
            if (Context.Phase == 2 && NPC.life <= NPC.lifeMax * VDDirector.Phase3LifeRatio)
            {
                stateMachine.ChangeState(new VDShieldUpState());
            }
        }
        #endregion

        #region 运动落地
        /// <summary>把状态声明的运动模式落到速度上;未声明一律指数刹停</summary>
        private void ApplyDeclaredMovement()
        {
            switch (Context.Mode)
            {
                case VDMoveMode.HoverTo:
                    HoverTo(Context.MoveTarget, Context.MoveSpeed, Context.Accel, Context.SlowRadius);
                    break;
                case VDMoveMode.HoldRelative:
                    if (Context.TargetValid)
                    {
                        HoldRelative(Context.HoldOffset, Context.Stiffness, Context.Accel, Context.MoveSpeed);
                    }
                    else
                    {
                        NPC.velocity *= 0.9f;
                    }
                    break;
                case VDMoveMode.Direct:
                    break;
                default:
                    NPC.velocity *= 0.9f;
                    if (NPC.velocity.LengthSquared() < 0.01f)
                    {
                        NPC.velocity = Vector2.Zero;
                    }
                    break;
            }
        }

        /// <summary>朝目标点平滑飞行:速度上限 maxSpeed,进入 slowRadius 后按距离比例减速</summary>
        private void HoverTo(Vector2 dest, float maxSpeed, float accel, float slowRadius)
        {
            Vector2 diff = dest - NPC.Center;
            float dist = diff.Length();
            Vector2 desired = Vector2.Zero;
            if (dist > 1f)
            {
                desired = diff / dist * Math.Min(maxSpeed, dist / slowRadius * maxSpeed);
            }
            NPC.velocity = Vector2.Lerp(NPC.velocity, desired, accel);
        }

        /// <summary>与目标保持相对静止:前馈目标速度,再按偏差回位</summary>
        private void HoldRelative(Vector2 offset, float stiffness, float lerp, float maxSpeed)
        {
            Vector2 desiredPos = targetPlayer.Center + offset;
            Vector2 want = targetPlayer.velocity + (desiredPos - NPC.Center) * stiffness;
            if (want.Length() > maxSpeed)
            {
                want = want.SafeNormalize(Vector2.Zero) * maxSpeed;
            }
            NPC.velocity = Vector2.Lerp(NPC.velocity, want, lerp);
        }

        private void ApplyTilt()
        {
            float target = float.IsNaN(Context.TiltOverride)
                ? MathHelper.Clamp(NPC.velocity.X * 0.012f, -0.25f, 0.25f)
                : Context.TiltOverride;
            NPC.rotation = MathHelper.Lerp(NPC.rotation, target, 0.1f);
        }
        #endregion

        #region 切技闪现
        /// <summary>开始闪现:锚点即落点,前半段淡出,过半换位,后半段淡入;落地后一段时间没有接触伤害</summary>
        public void StartBlink(Vector2 destination)
        {
            Context.AnchorPos = destination;
            Context.BlinkTimer = VDDirector.BlinkDuration;
            Context.NoContactTimer = VDDirector.PostTeleportGrace + VDDirector.BlinkDuration;
            NPC.velocity = Vector2.Zero;
            if (!Main.dedServ)
            {
                VDVfx.Sound("vbdisapear", 1f, NPC.Center, 3);
                VDVfx.BlinkBurst(NPC.Center);
            }
            if (!VaultUtils.isClient)
            {
                NPC.netUpdate = true;
            }
        }

        private void UpdateBlink()
        {
            Context.BlinkTimer--;
            NPC.velocity = Vector2.Zero;
            int half = VDDirector.BlinkDuration / 2;
            if (Context.BlinkTimer <= half)
            {
                //后半段幂等地钉在锚点上,收包晚一帧也不会漏掉换位
                if (Context.BlinkTimer == half && !Main.dedServ)
                {
                    VDVfx.Sound("vbapear", 1f, Context.AnchorPos, 3);
                    VDVfx.BlinkBurst(Context.AnchorPos);
                }
                if (NPC.Center != Context.AnchorPos)
                {
                    NPC.Center = Context.AnchorPos;
                    netMotion.ForgetPrediction();
                }
                Alpha = 1f - Context.BlinkTimer / (float)half;
            }
            else
            {
                Alpha = (Context.BlinkTimer - half) / (float)half;
            }
            DrawScale = 0.7f + 0.3f * Alpha;
        }
        #endregion

        #region 限制圈
        /// <summary>半径 200 格,圆心随本体。只处理本地玩家:玩家速度归其自身客户端所有,服务端不碰</summary>
        private void UpdateArena()
        {
            if (Main.dedServ || !Context.ArenaActive || Context.Dying)
            {
                return;
            }
            Player player = Main.LocalPlayer;
            if (!player.active || player.dead)
            {
                return;
            }
            Vector2 toCenter = NPC.Center - player.Center;
            float dist = toCenter.Length();
            if (dist <= VDDirector.ArenaRadius)
            {
                return;
            }
            Vector2 dir = toCenter / dist;
            float excess = dist - VDDirector.ArenaRadius;
            float pull = MathHelper.Clamp(VDDirector.ArenaPullBase + excess / 400f * VDDirector.ArenaPullPer400, VDDirector.ArenaPullBase, VDDirector.ArenaPullMax);
            player.velocity += dir * pull;
            float along = Vector2.Dot(player.velocity, dir);
            if (along > VDDirector.ArenaInwardSpeedCap)
            {
                player.velocity -= dir * (along - VDDirector.ArenaInwardSpeedCap);
            }
            player.AddBuff(ModContent.BuffType<VoidTouch>(), 5);
        }
        #endregion

        #region 视觉推导(全端同算,不过线)
        private void UpdateVisualState()
        {
            if (Context.BlinkTimer <= 0)
            {
                if (!float.IsNaN(Context.AlphaDeclared))
                {
                    Alpha = Context.AlphaDeclared;
                    DrawScale = Context.DrawScaleDeclared;
                }
                else
                {
                    Alpha = MathHelper.Lerp(Alpha, 1f, 0.15f);
                    DrawScale = MathHelper.Lerp(DrawScale, 1f, 0.15f);
                }
            }
            FakeZ = MathHelper.Lerp(FakeZ, Context.FakeZ, 0.2f);
            if (FakeZ < 0.005f)
            {
                FakeZ = 0f;
            }
            WingAlpha = MathHelper.Lerp(WingAlpha, Context.WingsVisible ? 1f : 0f, 0.05f);
            WingExpand = Math.Max(WingExpand * 0.94f, Context.WingPulse);
            WingRotation += 0.018f + WingExpand * 0.02f;
            ShieldAlpha = MathHelper.Lerp(ShieldAlpha, Context.ShieldVisible ? 1f : 0f, 0.04f);
            CoreColor = Color.Lerp(CoreColor, Context.CoreColorTarget, 0.06f);
            CoreGlow = Context.CoreGlow;

            if (Main.dedServ)
            {
                return;
            }

            //抖动只走绘制层:原版把 NPC 画在 position + netOffset,NoMultiplayerSmoothing 让它每帧被清零
            if (Context.ShakeStrength > 0.02f)
            {
                CEBossNetMotion.DrawShake(NPC, new Vector2(
                    MathF.Sin(Main.GlobalTimeWrappedHourly * 61f + NPC.whoAmI),
                    MathF.Cos(Main.GlobalTimeWrappedHourly * 47f + NPC.whoAmI * 1.7f)) * (5f * Context.ShakeStrength));
            }

            //二阶段底部火焰喷吐的粒子层(锥形光在 Draw 里)
            if (WingAlpha > 0.5f && Alpha > 0.5f && Main.GameUpdateCount % 2 == 0)
            {
                Vector2 pos = NPC.Center + new Vector2(Main.rand.NextFloat(-28f, 28f), 34f) * DrawScale;
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(2.5f, 5.5f)) + NPC.velocity * 0.3f;
                VDVfx.VoidPuff(pos, vel, Main.rand.NextFloat(0.9f, 1.5f), 0.65f);
                if (Main.rand.NextBool(3))
                {
                    VDVfx.SparkBurst(pos, new Color(230, 120, 255), 1, 2f, 4f, 16, 0.4f, 0.7f);
                }
            }
        }

        /// <summary>出场演出的相机聚焦:状态声明焦点与力度,宿主只在本地玩家够近时写入 EModPlayer</summary>
        private void FocusCamera()
        {
            if (Main.dedServ || float.IsNaN(Context.CameraFocus.X) || Context.CameraShift <= 0f)
            {
                return;
            }
            Player lp = Main.LocalPlayer;
            if (!lp.active || lp.dead || lp.Distance(Context.CameraFocus) > 2400f)
            {
                return;
            }
            var ep = lp.Entropy();
            ep.screenShift = Context.CameraShift;
            ep.screenPos = Context.CameraFocus;
        }
        #endregion

        #region 同步
        /// <summary>权威端:当前状态计时与全部裁决/掷骰事实随位置速度原子过线</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            EnsureContext();
            int stateId = (int)NPC.ai[3];
            int timer = 0;
            int counter = 0;
            if (stateMachine?.CurrentState is VDStateBase state)
            {
                stateId = state.StateId;
                timer = state.Timer;
                counter = state.Counter;
            }
            CEBossNetMotion.WriteTiming(writer, stateId, timer, counter);

            writer.WriteVector2(Context.AnchorPos);
            writer.Write((byte)Context.CornerIndex);
            writer.Write((sbyte)Context.SideDir);
            writer.Write(Context.RandCount);
            for (int i = 0; i < Context.RolledAngles.Length; i++)
            {
                writer.Write(Context.RolledAngles[i]);
            }
            for (int i = 0; i < Context.RolledPoints.Length; i++)
            {
                writer.WriteVector2(Context.RolledPoints[i]);
            }
            writer.Write(Context.BlinkTimer);
            writer.Write(Context.NoContactTimer);
            writer.Write(Context.AttackIndex);
            writer.Write(Context.QueuedChainState);
            writer.Write(Context.ForcedNextState);
            for (int i = 0; i < Context.RecentHistory.Length; i++)
            {
                writer.Write((sbyte)Context.RecentHistory[i]);
            }
            writer.Write((byte)Context.LastFamily);
            writer.Write(Context.DamageReduction);
            writer.Write(Context.Dying);
            writer.Write(Context.DeathPerformanceFinished);
            writer.Write(Context.ImpactFrameUsed);
            writer.Write(NPC.dontTakeDamage);
        }

        /// <summary>客户端收包:position/velocity/ai 已是服务端值,据计时差纠偏,计时留给收养,再读事实</summary>
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            EnsureContext();
            int localStateId = -1;
            int localTimer = 0;
            if (stateMachine?.CurrentState is VDStateBase state)
            {
                localStateId = state.StateId;
                localTimer = state.Timer;
            }
            netMotion.ReceiveTiming(reader, NPC, localStateId, localTimer);

            Context.AnchorPos = reader.ReadVector2();
            Context.CornerIndex = reader.ReadByte();
            Context.SideDir = reader.ReadSByte();
            Context.RandCount = reader.ReadInt32();
            for (int i = 0; i < Context.RolledAngles.Length; i++)
            {
                Context.RolledAngles[i] = reader.ReadSingle();
            }
            for (int i = 0; i < Context.RolledPoints.Length; i++)
            {
                Context.RolledPoints[i] = reader.ReadVector2();
            }
            int packetBlink = reader.ReadInt32();
            Context.NoContactTimer = reader.ReadInt32();
            Context.AttackIndex = reader.ReadInt32();
            Context.QueuedChainState = reader.ReadInt32();
            Context.ForcedNextState = reader.ReadInt32();
            for (int i = 0; i < Context.RecentHistory.Length; i++)
            {
                Context.RecentHistory[i] = reader.ReadSByte();
            }
            Context.LastFamily = (VDAttackFamily)reader.ReadByte();
            Context.DamageReduction = reader.ReadSingle();
            Context.Dying = reader.ReadBoolean();
            Context.DeathPerformanceFinished = reader.ReadBoolean();
            Context.ImpactFrameUsed = reader.ReadBoolean();
            NPC.dontTakeDamage = reader.ReadBoolean();

            //闪现计时带容差收养(硬对齐会让 BlinkTimer == half 的换位拍被跳过或重放)
            int prev = Context.BlinkTimer;
            Context.BlinkTimer = CEBossNetMotion.AdoptTimer(Context.BlinkTimer, packetBlink);
            //闪现是服务端发起的,客户端在这里补放旧位置的消失演出(此时包里的位置还是旧位置)
            if (prev <= 0 && Context.BlinkTimer > VDDirector.BlinkDuration / 2 && !Main.dedServ)
            {
                VDVfx.Sound("vbdisapear", 1f, NPC.Center, 3);
                VDVfx.BlinkBurst(NPC.Center);
            }
            //快速移动实体不吃原版平滑,收包后位置即最终位置
            NPC.netOffset = Vector2.Zero;
        }
        #endregion
    }
}
