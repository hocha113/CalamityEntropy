using CalamityEntropy.Core.AI;
using InnoVault;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.Core
{
    /// <summary>状态索引,写入 npc.ai[3] 网络同步。0~9 演出/连接段,10~19 既有招式,20~24 第二批,25+ 纵深招</summary>
    public enum VDStateIndex
    {
        /// <summary>传送门出场</summary>
        Entrance = 0,
        /// <summary>选招 hub(连接段 + 四角闪现)</summary>
        Hub = 1,
        /// <summary>75% 变形(能量翼展开)</summary>
        Transform = 2,
        /// <summary>30% 护盾展开连接段</summary>
        ShieldUp = 3,
        /// <summary>死亡演出</summary>
        Death = 4,
        /// <summary>脱战撤离</summary>
        Despawn = 5,

        /// <summary>弧形火球:斜上逼近,三轮五发扇形包裹</summary>
        ArcFireball = 10,
        /// <summary>追踪导弹:核心激光流 + 导弹环 + 核弹</summary>
        HomingMissiles = 11,
        /// <summary>幻影冲刺:传送门后五连冲</summary>
        PhantomDash = 12,
        /// <summary>虚空火焰:压到玩家下方向下扇散再转向上加速</summary>
        VoidFlame = 13,
        /// <summary>相位激光:孢子无人机纵列 + 井字网</summary>
        PhaseLaser = 14,
        /// <summary>传送火弹:四角依次闪现扇射</summary>
        TeleportFire = 15,
        /// <summary>支援投送:地面开门投下前卫教徒</summary>
        Reinforcement = 16,
        /// <summary>红色地狱:全息红恶魔 + 红射线 + 三叉戟排</summary>
        RedHell = 17,
        /// <summary>绿色丛林:全息陆龟横冲</summary>
        GreenJungle = 18,
        /// <summary>蓝色天空:全息小白龙绕圈 + 形状弹幕</summary>
        BlueSky = 19,

        /// <summary>裂隙斩:空间缝线预告后拉开,两侧喷弹</summary>
        RiftCut = 20,
        /// <summary>轨道轰炸:退入背景标记落点,虚空光柱砸落</summary>
        OrbitalStrike = 21,
        /// <summary>虚空奇点:引力透镜牵引后塌缩环爆</summary>
        Singularity = 22,
        /// <summary>幻影舰队:四门四舰同步齐冲,真身有破绽</summary>
        PhantomFleet = 23,
        /// <summary>湮灭主炮:P3 压轴蓄力扫射</summary>
        AnnihilationCannon = 24,

        /// <summary>纵深环门:退到深处逐个推出带缺口的弹环,环从背景逼近平面</summary>
        DepthGates = 25,
        /// <summary>深空掠袭:在背景里带两艘幻影舰横越,一路朝平面射纵深贯穿弹</summary>
        DeepStrafe = 26,
    }

    /// <summary>招式家族:轮换表的防复读按家族判同类相邻</summary>
    public enum VDAttackFamily : byte
    {
        None = 0,
        /// <summary>冲刺压迫</summary>
        Dash,
        /// <summary>弹幕齐射</summary>
        Barrage,
        /// <summary>区域封锁</summary>
        Zone,
        /// <summary>引力</summary>
        Gravity,
        /// <summary>全息投影</summary>
        Hologram,
        /// <summary>召唤</summary>
        Summon,
        /// <summary>压轴</summary>
        Finale,
    }

    /// <summary>描边风格:决定外扩叠画的偏移几何与噪声侵蚀比例,按招式家族分派(<see cref="VDDirector.RimStyleFor"/>)</summary>
    public enum VDRimStyle : byte
    {
        /// <summary>往外逸散:偏移绕圈匀布,半径随强度外扩(弹幕/区域/支援/演出的默认)</summary>
        Dissipate,
        /// <summary>塌缩:半径随蓄力收紧到贴边,读成能量被吸回(奇点)</summary>
        Collapse,
        /// <summary>拖尾:偏移沿速度反向拉开,与高速残影叠成一条(幻影冲刺/舰队)</summary>
        Streak,
        /// <summary>过热:噪声几乎不侵蚀、整圈实心白热、高频闪(湮灭主炮)</summary>
        Overheat,
    }

    /// <summary>虚空驱逐舰状态接口</summary>
    public interface IVDState : IVaultState<VDStateContext>
    {
        VDStateIndex StateIndex { get; }
        void OnEnter(VDStateContext context);
        IVDState OnUpdate(VDStateContext context);
        void OnExit(VDStateContext context);
    }

    /// <summary>
    /// 状态基类:桥接 VaultState 泛型签名,集中公共小件(收招/出手/运动声明/演出糖)。
    /// 约定:状态里 Timer 是当前拍内计时(换拍归零),Counter 是状态总龄(超时兜底用),二者随快照过线
    /// </summary>
    public abstract class VDStateBase : VaultState<VDStateContext>, IVDState, ICEBossNetTiming
    {
        public override int StateId => (int)StateIndex;
        public abstract override string StateName { get; }
        public abstract VDStateIndex StateIndex { get; }

        /// <summary>本状态默认开接触伤害窗;冲刺类/演出类关掉后按拍自行声明</summary>
        public virtual bool ContactByDefault => true;
        /// <summary>
        /// 几何上必须由 hub 闪现到 <see cref="AnchorFor"/> 才能起手的招(如压到玩家脚下的虚空火焰)。
        /// 其余招在连接段里飞过去;距离锚点超过 <see cref="VDDirector.ConnectorBlinkDistance"/> 时 hub 也会闪现一次。
        /// 自带传送/开门逻辑的招返回 false
        /// </summary>
        public virtual bool NeedsRepositionBlink => false;
        /// <summary>本招起手时本体想待的位置:hub 连接段里飞向它(或闪现到它)。默认玩家斜上方的通用悬停点</summary>
        public virtual Vector2 AnchorFor(VDStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.ConnectorDefaultAnchor.X, VDDirector.ConnectorDefaultAnchor.Y);
        /// <summary>闪现进行中是否仍推进本状态(只有演出/hub 需要)</summary>
        public virtual bool RunsDuringBlink => false;
        /// <summary>状态总龄超时上限(帧),超过即强制收招。演出态返回 int.MaxValue</summary>
        public virtual int TimeoutFrames => VDDirector.AttackTimeoutFrames;
        /// <summary>
        /// 本招起手时本体所在的深度(0 平面)。hub 连接段的落定拍把 Depth 朝它爬:「它在退远」本身就是「要轰炸了」的可读预告。
        /// 招式进入后自己每帧声明 Depth,这里只给连接段一个目标
        /// </summary>
        public virtual float StartDepth(VDStateContext ctx) => 0f;

        public virtual void OnEnter(VDStateContext context) {
            Timer = 0;
            Counter = 0;
        }

        public abstract IVDState OnUpdate(VDStateContext context);

        public virtual void OnExit(VDStateContext context) {
        }

        public sealed override void OnEnter(VaultStateMachine<VDStateContext> machine, VDStateContext ctx) {
            OnEnter(ctx);
        }

        public sealed override IVaultState<VDStateContext> OnUpdate(VaultStateMachine<VDStateContext> machine, VDStateContext ctx) {
            //闪现期间状态计时暂停(落地后再接着出手),演出态例外
            if (ctx.BlinkTimer > 0 && !RunsDuringBlink) {
                return null;
            }
            if (ContactByDefault) {
                ctx.ContactWindow = true;
            }
            Counter++;
            IVDState next = OnUpdate(ctx);
            //超时兜底:状态机永远不许死在这里,靠惯性飘走
            if (next == null && Counter > TimeoutFrames) {
                ctx.Npc.velocity *= 0.6f;
                next = EndAttack(ctx);
            }
            return next;
        }

        public sealed override void OnExit(VaultStateMachine<VDStateContext> machine, VDStateContext ctx) {
            OnExit(ctx);
        }

        /// <summary>
        /// 收养权威端随快照过线的状态计时(客户端)。容差内不动本地值:
        /// 只差一两帧是网络抖动的常态,硬对齐会让 Timer == X 型一次性拍被跳过或重放
        /// </summary>
        public void AdoptNetTiming(int timer, int counter) {
            Timer = CEBossNetMotion.AdoptTimer(Timer, timer);
            Counter = counter;
        }

        /// <summary>换拍:Timer 归零,不在一个计时器上串烧</summary>
        protected void ResetTimer() {
            Timer = 0;
        }

        #region 公共小件
        protected static bool IsServer => !VaultUtils.isClient;

        /// <summary>结束攻击:有连击队列且合法直接接招(连段刻意跳过连接段),否则回 hub 走连接段三拍</summary>
        protected static IVDState EndAttack(VDStateContext ctx) {
            if (ctx.QueuedChainState >= 0 && ctx.TargetValid) {
                VDStateIndex next = (VDStateIndex)ctx.QueuedChainState;
                ctx.QueuedChainState = -1;
                if (VDRotation.IsLegal(ctx, next)) {
                    IVDState chained = VDRotation.Create(next);
                    if (chained != null) {
                        VDRotation.Commit(ctx, next);
                        return chained;
                    }
                }
            }
            ctx.QueuedChainState = -1;
            return new States.VDHubState();
        }

        /// <summary>本 Boss 存活的敌对弹幕数(不计演出弹幕):杂波阀的判据</summary>
        protected static int CountHostileProjectiles() {
            int n = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.hostile && p.ModProjectile is Projectiles.VoidDestroyer.IVoidDestroyerProjectile) {
                    n++;
                }
            }
            return n;
        }

        /// <summary>目标预测点</summary>
        protected static Vector2 PredictTarget(VDStateContext ctx, float leadFrames)
            => ctx.Target.Center + ctx.Target.velocity * leadFrames;

        /// <summary>服务端生成敌对弹幕,伤害按大师显示值折算;客户端返回 -1</summary>
        protected static int Shoot<T>(VDStateContext ctx, Vector2 pos, Vector2 vel, int masterShown, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile {
            if (!IsServer) {
                return -1;
            }
            return Projectile.NewProjectile(ctx.Npc.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<T>(), ctx.Owner.ProjDamage(masterShown), 0f, Main.myPlayer, ai0, ai1, ai2);
        }

        /// <summary>纯演出弹幕(传送门/无人机装饰/预警),伤害 0;客户端返回 -1</summary>
        protected static int SpawnVisual<T>(VDStateContext ctx, Vector2 pos, Vector2 vel, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile {
            if (!IsServer) {
                return -1;
            }
            return Projectile.NewProjectile(ctx.Npc.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<T>(), 0, 0f, Main.myPlayer, ai0, ai1, ai2);
        }

        #region 纵深小件
        /// <summary>
        /// 服务端生成深度弹幕:初始 Z / Z 速度 / Z 加速度经 <see cref="Projectiles.VoidDestroyer.VDDepthSource"/> 在 OnSpawn 就位,
        /// 生成包里的 ExtraAI 已是正确深度。伤害按大师显示值折算;客户端返回 -1
        /// </summary>
        protected static int ShootDepth<T>(VDStateContext ctx, Vector2 pos, Vector2 vel, int masterShown, float z, float zVel, float zAccel = 0f, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile {
            if (!IsServer) {
                return -1;
            }
            var source = new Projectiles.VoidDestroyer.VDDepthSource(ctx.Npc, z, zVel, zAccel);
            return Projectile.NewProjectile(source, pos, vel, ModContent.ProjectileType<T>(), ctx.Owner.ProjDamage(masterShown), 0f, Main.myPlayer, ai0, ai1, ai2);
        }

        /// <summary>纯演出的深度弹幕(远处的门、坠落舱壳等),伤害 0</summary>
        protected static int SpawnVisualDepth<T>(VDStateContext ctx, Vector2 pos, Vector2 vel, float z, float zVel, float zAccel = 0f, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) where T : ModProjectile {
            if (!IsServer) {
                return -1;
            }
            var source = new Projectiles.VoidDestroyer.VDDepthSource(ctx.Npc, z, zVel, zAccel);
            return Projectile.NewProjectile(source, pos, vel, ModContent.ProjectileType<T>(), 0, 0f, Main.myPlayer, ai0, ai1, ai2);
        }

        /// <summary>
        /// 纵深配速:从平面点 <paramref name="from"/>、深度 <paramref name="z"/> 出发,<paramref name="frames"/> 帧后恰好在
        /// <paramref name="landing"/> 处到达平面。返回平面速度与 Z 速度(无加速度)
        /// </summary>
        protected static (Vector2 vel, float zVel) AimThroughPlane(Vector2 from, float z, Vector2 landing, int frames) {
            frames = Math.Max(frames, 1);
            return ((landing - from) / frames, -z / frames);
        }

        /// <summary>深度锚点:相对目标玩家的表观偏移 + Z → 世界坐标(服务端以目标玩家中心做相机代理)</summary>
        protected static Vector2 DepthAnchor(VDStateContext ctx, Vector2 apparentOffset, float z)
            => VDDepth.WorldFromApparent(ctx.Target.Center, apparentOffset, z);

        /// <summary>声明:与目标保持相对静止,偏移按深度换算成世界偏移(表观上就是 apparentOffset)</summary>
        protected static void DeclareHoldRelativeDepth(VDStateContext ctx, Vector2 apparentOffset, float z, float stiffness = 0.12f, float lerp = 0.35f, float maxSpeed = 40f) {
            DeclareHoldRelative(ctx, VDDepth.WorldOffset(apparentOffset, z), stiffness, lerp, maxSpeed);
        }

        /// <summary>
        /// 俯冲拍(退远的招收尾都用它):深度从 <paramref name="fromDepth"/> 按立方缓入归零(慢起猛到,「朝镜头飞来」),
        /// 全程在 <paramref name="landing"/> 画落点大环,落地前 2 帧到落地后 DiveContactFrames 帧开接触窗,落地帧震屏 + 冲击环。
        /// 调用方按自己的拍内 Timer 逐帧调用;返回是否已落地(含落地后的接触窗期)
        /// </summary>
        protected static bool DeclareDive(VDStateContext ctx, float fromDepth, int timer, int frames, Vector2 landing) {
            float p = MathHelper.Clamp(timer / (float)frames, 0f, 1f);
            ctx.Depth = fromDepth * (1f - VDDepth.DiveCurve(p));
            ctx.DiveMarkerPos = landing;
            ctx.DiveMarkerProgress = timer <= frames ? p : 0f;
            ctx.CoreGlow = Math.Max(ctx.CoreGlow, 0.4f + 0.6f * p);
            if (timer >= frames - 2 && timer <= frames + VDDirector.DiveContactFrames) {
                ctx.ContactWindow = true;
            }
            if (timer == frames) {
                VDVfx.DiveShock(ctx.Npc.Center);
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.7f);
                ctx.RimFlash = 1f;
                ctx.WingPulse = 1f;
                ctx.CoreGlow = 1f;
            }
            return timer >= frames;
        }
        #endregion

        /// <summary>核心出手的通用演出:后坐、能量翼张开、核心亮起、描边爆闪、火花、音效</summary>
        protected static void MuzzleCue(VDStateContext ctx, Vector2 dir, float recoil, string sound, float pitch = 1f, float volume = 1f) {
            ctx.Npc.velocity -= dir * recoil;
            ctx.WingPulse = Math.Max(ctx.WingPulse, 1f);
            ctx.CoreGlow = 1f;
            ctx.RimFlash = 1f;
            if (Main.dedServ) {
                return;
            }
            //粒子系统不分层,一律放在投影后的核心位置(本体在平面时投影 = 世界坐标)
            Vector2 core = ctx.Owner.ProjectedCorePos;
            float depthScale = VDDepth.Scale(ctx.Owner.Depth);
            if (sound != null) {
                CEUtils.PlaySound(sound, pitch, core, 6, volume);
            }
            for (int i = 0; i < 8; i++) {
                Vector2 v = dir.RotatedBy(Main.rand.NextFloat(-0.6f, 0.6f)) * Main.rand.NextFloat(4f, 10f) * depthScale;
                VDVfx.Spark(core, v, VDVfx.VoidPurple, Main.rand.NextFloat(0.5f, 1f) * depthScale, 1f, 20, gravity: true);
            }
        }

        /// <summary>声明:朝目标点平滑飞行(速度上限 + 进入减速带按距离比例减速)</summary>
        protected static void DeclareHoverTo(VDStateContext ctx, Vector2 dest, float maxSpeed, float accel = 0.1f, float slowRadius = 120f) {
            ctx.Mode = VDMoveMode.HoverTo;
            ctx.MoveTarget = dest;
            ctx.MoveSpeed = maxSpeed;
            ctx.Accel = accel;
            ctx.SlowRadius = slowRadius;
        }

        /// <summary>声明:与目标保持相对静止(前馈目标速度 + 偏差回位)</summary>
        protected static void DeclareHoldRelative(VDStateContext ctx, Vector2 offset, float stiffness = 0.12f, float lerp = 0.35f, float maxSpeed = 40f) {
            ctx.Mode = VDMoveMode.HoldRelative;
            ctx.HoldOffset = offset;
            ctx.Stiffness = stiffness;
            ctx.Accel = lerp;
            ctx.MoveSpeed = maxSpeed;
        }

        /// <summary>声明:状态自管速度(冲刺/演出),宿主不再碰 velocity</summary>
        protected static void DeclareDirect(VDStateContext ctx) {
            ctx.Mode = VDMoveMode.Direct;
        }

        /// <summary>声明本帧透明度与绘制缩放(不声明则宿主自动拉回 1)</summary>
        protected static void DeclareAlpha(VDStateContext ctx, float alpha, float scale = 1f) {
            ctx.AlphaDeclared = alpha;
            ctx.DrawScaleDeclared = scale;
        }

        /// <summary>汇聚粒子:从四周向核心收束(蓄力语法的第一层);位置与半径按本体投影与深度缩放</summary>
        protected static void ConvergeSparks(VDStateContext ctx, Color color, float minDist = 80f, float maxDist = 160f, float pull = 0.09f) {
            if (Main.dedServ) {
                return;
            }
            float depthScale = VDDepth.Scale(ctx.Owner.Depth);
            Vector2 core = ctx.Owner.ProjectedCorePos;
            Vector2 from = core + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(minDist, maxDist) * depthScale;
            Vector2 v = (core - from) * pull;
            VDVfx.Spark(from, v, VDDepth.Fog(color, ctx.Owner.Depth), Main.rand.NextFloat(0.5f, 0.9f) * Math.Max(depthScale, 0.4f), 1f, 12);
        }

        /// <summary>决策点同步(权威端)</summary>
        protected static void MarkNetUpdate(VDStateContext ctx) {
            if (IsServer) {
                ctx.Npc.netUpdate = true;
            }
        }
        #endregion
    }
}
