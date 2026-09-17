using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Acropolis.Core;
using InnoVault.Rigs2D.Runtime;
using InnoVault.Rigs2D.Solvers;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 一条两节瞄准臂(炮臂 / 鱼叉臂)的门面。两节各由骨架里的一个 <see cref="PointAtSolver"/> 接管:
    /// 第一节从挂点指向瞄点,第二节从第一节末端指向瞄点,转向速率 0.06(比例)与迁移前的 <c>PointAPos</c> 同义;
    /// 开火后坐走 <see cref="Kick"/>(第一节的角速度,每帧按 0.96 衰减),与原 <c>Seg1RotV</c> 同义。
    /// <para>
    /// 状态与宿主每帧只<b>声明</b>瞄点(<see cref="Aim"/>),骨架 <c>Step</c> 时一次落地;开火在 <c>Step</c> 之后从
    /// <see cref="Muzzle"/> / <see cref="BarrelDir"/> 取值,所以「先转再打」仍在同一帧内成立。
    /// 跳射连瞄两次(转向速率翻倍)的旧写法用 <see cref="AimTimes"/> 表达:把瞄点沿角度预推到「连转 N 次」的等效位置
    /// </para>
    /// <para>两节朝向不再过线:各端瞄的是同一个已同步目标(玩家 / 鱼叉实体),开火帧由已过线的节拍量决定,各端同算</para>
    /// </summary>
    public sealed class AcropolisArm
    {
        private Rig2DInstance rig;
        private PointAtSolver seg1;
        private PointAtSolver seg2;
        private int seg1Bone = -1;
        private int seg2Bone = -1;
        private float turnRate = 0.06f;
        private readonly NPC npc;

        /// <summary>挂点相对本体的设计偏移(朝右作图、未乘 dir)。跳射的对地瞄点沿用它,照搬原代码不乘 dir 的写法</summary>
        public Vector2 MountOffset { get; }

        /// <summary>本帧声明的瞄点;<see langword="null"/> = 保持朝向(只结算后坐)</summary>
        public Vector2? AimTarget { get; private set; }

        /// <summary>本帧连瞄次数(原代码对跳射连调两次 PointAPos)</summary>
        public int AimTimes { get; private set; } = 1;

        /// <summary>未晋升形态时手臂垂向的虚拟目标,纯本地</summary>
        public Vector2 DummyPos;

        public AcropolisArm(NPC npc, Vector2 mountOffset) {
            this.npc = npc;
            MountOffset = mountOffset;
            DummyPos = npc.Center;
        }

        /// <summary>句柄就位(骨架绑定 / 热重载重绑时由宿主调用)</summary>
        internal void Wire(Rig2DInstance rig, PointAtSolver seg1, PointAtSolver seg2, int seg1Bone, int seg2Bone, float turnRate) {
            this.rig = rig;
            this.seg1 = seg1;
            this.seg2 = seg2;
            this.seg1Bone = seg1Bone;
            this.seg2Bone = seg2Bone;
            this.turnRate = MathHelper.Clamp(turnRate, 0.001f, 1f);
        }

        public bool Ready => rig != null && rig.Built && seg1 != null && seg2 != null && seg1Bone >= 0 && seg2Bone >= 0;

        /// <summary>第一节末端(第二节根部)</summary>
        public Vector2 Seg1End => Ready ? rig.Bones[seg1Bone].Tip : npc.Center;

        /// <summary>第一节朝向</summary>
        public float Seg1Dir => Ready ? rig.Bones[seg1Bone].Dir : MathHelper.PiOver2;

        /// <summary>第二节(枪管)朝向,即原 <c>Seg2Rot</c></summary>
        public float BarrelDir => Ready ? rig.Bones[seg2Bone].Dir : MathHelper.PiOver2;

        /// <summary>枪口:第二节骨的尖端(原 <c>TopPos</c>,第二节长 60)</summary>
        public Vector2 Muzzle => Ready ? rig.Bones[seg2Bone].Tip : npc.Center;

        /// <summary>声明本帧瞄点。同帧多次声明以最后一次为准</summary>
        public void Aim(Vector2 target, int times = 1) {
            AimTarget = target;
            AimTimes = Math.Max(1, times);
        }

        /// <summary>注入后坐角冲量(第一节),正负决定甩向;调用方按原代码乘 dir</summary>
        public void Kick(float angularImpulse) => seg1?.Kick(angularImpulse);

        /// <summary>把本帧声明写进两个求解器(骨架 Step 之前由宿主调用),随后清空声明</summary>
        internal void Apply() {
            if (seg1 == null || seg2 == null || rig == null) {
                return;
            }
            if (!rig.Built) {
                //首帧 Snap 前骨骼还没有位姿,先让两节直接指向瞄点或垂下
                Vector2 first = AimTarget ?? (rig.RootPosition + new Vector2(0f, 1000f));
                seg1.Target = first;
                seg2.Target = first;
            }
            else if (AimTarget is Vector2 target) {
                seg1.Target = PreLerp(seg1Bone, rig.RestPosition(seg1Bone), target);
                seg2.Target = PreLerp(seg2Bone, rig.Bones[seg1Bone].Tip, target);
            }
            else {
                seg1.Target = HoldTarget(seg1Bone);
                seg2.Target = HoldTarget(seg2Bone);
            }
            AimTarget = null;
            AimTimes = 1;
        }

        /// <summary>
        /// 连瞄 N 次的等效瞄点:求解器每帧只做一次 <c>lerp(cur, want, r)</c>,
        /// 连做 N 次等于一次 <c>lerp(cur, want', r)</c>,其中 <c>want' = cur + (1 − (1 − r)^N) / r × Δ</c>。
        /// 锚点取上一帧位姿(挂点一帧只动几个像素,角度误差可忽略)
        /// </summary>
        private Vector2 PreLerp(int bone, Vector2 anchor, Vector2 target) {
            if (AimTimes <= 1) {
                return target;
            }
            Vector2 d = target - anchor;
            float dist = d.Length();
            if (dist < 0.5f) {
                return target;
            }
            float k = (1f - MathF.Pow(1f - turnRate, AimTimes)) / turnRate;
            float cur = rig.Bones[bone].Dir;
            float want = MathF.Atan2(d.Y, d.X);
            float adjusted = cur + k * MathHelper.WrapAngle(want - cur);
            return anchor + adjusted.ToRotationVector2() * dist;
        }

        /// <summary>保持朝向:瞄点放在当前朝向的极远处,挂点随本体位移不会带来可见的角度变化</summary>
        private Vector2 HoldTarget(int bone) {
            ref Bone2D b = ref rig.Bones[bone];
            return b.Pos + b.Dir.ToRotationVector2() * 100000f;
        }
    }

    /// <summary>
    /// 卫城机器的骨架:InnoVault Rigs2D,定义在 <c>Assets/Rigs/Acropolis.rig.json</c>。
    /// <para>
    /// 本体为根,根朝向取<b>地形倾角</b>(<c>NPC.rotation</c> 在朝左时多出的半圈剥掉),整副骨架在这个坐标系里朝右作图:
    /// 四条腿是世界锚定的——迁移前腿的挂点从不随朝向镜像(世界左腿永远是世界左腿),所以这里<b>不用</b>骨架级 <c>Mirrored</c>
    /// (它会在转身时交换左右腿的身份,落脚点交叉后被迫补步),而是逐帧把随朝向翻转的量写成局部覆写:
    /// 两条臂的挂点 x 乘 dir、鱼叉枪口的侧向偏移乘 dir、本体件挂在一根 <c>facing</c> 骨上(朝左时局部转半圈 + 件镜像,
    /// 与原来「rotation + π 再 FlipVertically」逐像素等价),第二节臂件与停靠鱼叉件按 dir 切镜像(原 FlipVertically)。
    /// </para>
    /// <para>
    /// 腿:每条腿一个 <see cref="FootPlantGaitSolver"/>(内外腿触及不同,150 / 188,单求解器只有一个 reach 装不下)产出足端目标,
    /// 髋 = 腿根 (±20, 60),休息位指向原 <c>LegMounts</c>;四条腿的节律窗相位各错四分之一周期,同侧两腿的窗口不重叠,
    /// 等价于原来的同侧迈步互锁。四个 <see cref="ThreeBoneLegSolver"/> 按「胫节永远竖直」的活塞模式解膝——迁移前的解析 IK
    /// 正是先把踝点钉在落点正上方 l3 处再解膝,静息姿态两者逐像素相同。
    /// 落地 / 悬空 / 迈步都从步态读(<see cref="LegOnTile"/>),不再有逐腿的落点搜索、迈步冷却与 ExtraAI 过线块。
    /// 步态公式换了(唯一的非无损迁移),手感靠 <c>.rig.json</c> 热重载进游戏调
    /// </para>
    /// <para>
    /// 臂:见 <see cref="AcropolisArm"/>;鱼叉链:一条 <see cref="HangChainSolver"/>(垂度 0,即直线)锚在枪口后 72 的 <c>chainTail</c> 骨,
    /// 末端每帧写鱼叉实体「本帧结束后」的位置,带状件平铺链节贴图,只在鱼叉飞行中可见,由鱼叉实体的绘制路径画出(压盖层次不变)
    /// </para>
    /// <para>各端都建、都 Step:服务端没有贴图但骨骼有效,着地腿数、枪口位置、鱼叉停靠点都从骨骼读</para>
    /// </summary>
    public partial class AcropolisMachine
    {
        /// <summary>腿数,与 rig.json 里的 repeat 4 对齐。骨架内的腿序:0 内左、1 外左、2 内右、3 外右;每腿一个步态求解器,节律窗相位依次错开四分之一周期,同侧两腿的窗口互不重叠</summary>
        public const int LegCount = 4;
        private const int LegPartCount = 3;

        /// <summary>层序带:腿 0~11、鱼叉臂第一节 20、停靠鱼叉 21、发射器 22、本体 30、炮臂 40~41、肩甲 50</summary>
        private const float LayerHarpoonArm = 20f;
        private const float LayerHarpoonDocked = 21f;

        private Rig2DInstance rig;

        [Rig2DBone("facing")]
        private int facingBone = -1;
        [Rig2DBone("cannonMount", "harpoonMount", "harpoonMuzzle", "chainTail")]
        private readonly int[] armAnchorBones = new int[4];
        [Rig2DBone("cannon1", "cannon2", "harpoon1", "harpoon2")]
        private readonly int[] armSegBones = new int[4];
        [Rig2DSolver("cannonAim1", "cannonAim2", "harpoonAim1", "harpoonAim2")]
        private readonly PointAtSolver[] armSolvers = new PointAtSolver[4];
        [Rig2DSolver("gait{0}", Count = LegCount)]
        private readonly FootPlantGaitSolver[] gaits = new FootPlantGaitSolver[LegCount];
        [Rig2DSolver("chain")]
        private HangChainSolver chainSolver;
        [Rig2DRibbon("chain")]
        private int chainRibbon = -1;
        [Rig2DPiece("coxa{0}", "femur{0}", "tibia{0}", Count = LegCount)]
        private readonly int[,] legPieces = new int[LegCount, LegPartCount];
        [Rig2DPiece("body", "shoulder", "cannon", "harpoonLauncher", "harpoonDocked")]
        private readonly int[] facingPieces = new int[5];
        [Rig2DPiece("harpoonArm", "cannonConnect")]
        private readonly int[] armLinkPieces = new int[2];

        /// <summary>炮臂</summary>
        public AcropolisArm Cannon { get; private set; }
        /// <summary>鱼叉臂</summary>
        public AcropolisArm HarpoonArm { get; private set; }

        /// <summary>本帧鱼叉臂的瞄点声明(宿主背景行为写,状态不碰)</summary>
        private Vector2? harpoonAimTarget;

        /// <summary>骨架是否可用(资产已加载、句柄全部命中、已完成首次求解)</summary>
        public bool RigReady => rig != null && rig.Bound && rig.Built;

        private int ArmAnchorBone(int i) => i < armAnchorBones.Length ? armAnchorBones[i] : -1;

        /// <summary>
        /// 惰性建实例:资产字段在 PostSetupContent 之后才有值,而 ModNPC 模板早于此构造,所以首帧 AI 里建。
        /// 两条臂的门面同时建好,状态上下文从此可以拿到它们
        /// </summary>
        private void EnsureRig() {
            Cannon ??= new AcropolisArm(NPC, new Vector2(AcropolisDirector.CannonMountX, AcropolisDirector.CannonMountY));
            HarpoonArm ??= new AcropolisArm(NPC, new Vector2(AcropolisDirector.HarpoonMountX, AcropolisDirector.HarpoonMountY));
            if (rig != null) {
                return;
            }
            Vault2DRig asset = CERigAssets.Acropolis;
            if (asset == null || !asset.IsValid) {
                return;
            }
            rig = asset.CreateInstance(NPC.whoAmI);
            rig.Bind(this, OnRigBound);
        }

        /// <summary>句柄就位后的一次性配置(首次绑定与热重载重绑都会跑)</summary>
        private void OnRigBound(Rig2DInstance r) {
            //脚掌贴图:右腿顺时针 24°,左腿逆时针 24° 并沿骨轴镜像(原 FlipVertically),与迁移前的 Draw 一致
            float footTilt = MathHelper.ToRadians(AcropolisDirector.FootTiltDegrees);
            for (int i = 0; i < LegCount; i++) {
                FootPlantGaitSolver gait = gaits[i];
                gait.Probe = ProbeStandable;
                gait.GroundDir = Vector2.UnitY;
                gait.AutoPhase = true;
                gait.Attached = true;

                ref Piece2DState foot = ref r.PieceRef(legPieces[i, 2]);
                foot.ExtraRotation = LegSide(i) * footTilt;
                foot.Mirror = LegSide(i) < 0;
            }

            float cannonRate = SolverTurnRate(r, "cannonAim1");
            float harpoonRate = SolverTurnRate(r, "harpoonAim1");
            Cannon.Wire(r, armSolvers[0], armSolvers[1], armSegBones[0], armSegBones[1], cannonRate);
            HarpoonArm.Wire(r, armSolvers[2], armSolvers[3], armSegBones[2], armSegBones[3], harpoonRate);
        }

        /// <summary>从定义读 PointAt 的转向速率(JSON 唯一真相),连瞄等效换算要用</summary>
        private static float SolverTurnRate(Rig2DInstance r, string solverName) {
            int idx = r.Definition?.SolverIndex(solverName) ?? -1;
            return idx >= 0 ? r.Definition.Solvers[idx].GetFloat("turnRate", 1f) : 1f;
        }

        /// <summary>腿的世界侧:骨架腿序 0/1 在左(−1),2/3 在右(+1)。腿是世界锚定的,不随朝向变</summary>
        public static float LegSide(int leg) => leg < 2 ? -1f : 1f;

        /// <summary>
        /// 落地探测:按 4 像素步进沿 <paramref name="dir"/> 扫,实心块<b>与平台</b>都算可站(原 <c>CanStandOn = !isAir(pos, true)</c>),
        /// 落点取进入物块前的最后一个采样点
        /// </summary>
        private static bool ProbeStandable(Vector2 from, Vector2 dir, float maxDistance, out Vector2 hit) {
            const float step = 4f;
            Vector2 p = from;
            Vector2 prev = from;
            float travelled = 0f;
            while (travelled <= maxDistance) {
                if (!CEUtils.isAir(p, true)) {
                    hit = prev;
                    return true;
                }
                prev = p;
                p += dir * step;
                travelled += step;
            }
            hit = from + dir * maxDistance;
            return false;
        }

        //==================== 步态查询(gameplay 读它们) ====================

        /// <summary>踩在实体上:足端钉在落点且足下有承托(原 <c>AcropolisLeg.OnTile</c>)。迈步途中不算</summary>
        public bool LegOnTile(int leg) {
            if (!RigReady || leg < 0 || leg >= gaits.Length || gaits[leg] == null) {
                return false;
            }
            FootPlantGaitSolver.LegState ls = gaits[leg].Leg(0);
            return ls.Inited && ls.Planted && ls.Grounded;
        }

        /// <summary>足端位置(原 <c>StandPoint</c>)</summary>
        public Vector2 LegFoot(int leg) => RigReady && leg >= 0 && leg < gaits.Length && gaits[leg] != null ? gaits[leg].Foot(0) : NPC.Center;

        //==================== 鱼叉几何(骨骼读出) ====================

        /// <summary>鱼叉在发射架上时的枪口位置:枪管前伸 150、侧向 10×dir(骨架里的 <c>harpoonMuzzle</c> 骨)</summary>
        public Vector2 HarpoonPos {
            get {
                int b = ArmAnchorBone(2);
                return RigReady && b >= 0 ? rig.Bones[b].Pos : NPC.Center;
            }
        }

        /// <summary>锁链尾端:枪口沿发射方向回退 72(骨架里的 <c>chainTail</c> 骨)。鱼叉实体的朝向与锁链都读它</summary>
        public Vector2 HarpoonChainTail {
            get {
                int b = ArmAnchorBone(3);
                return RigReady && b >= 0 ? rig.Bones[b].Pos : NPC.Center;
            }
        }

        //==================== 每帧落地 ====================

        /// <summary>
        /// 骨架落地,在 AI 末尾、本帧位移与朝向都结算完之后跑(各端都跑)。
        /// 顺序:根位姿与随朝向翻转的局部覆写 → 腿的模式与目标 → 两条臂的瞄点 → 鱼叉链 → <c>Step</c> → 件的朝向镜像与可见性
        /// </summary>
        private void UpdateRig() {
            EnsureRig();
            if (rig == null || !rig.Bound) {
                return;
            }
            bool left = dir < 0;
            rig.Scale = NPC.scale;
            rig.SetRoot(NPC.Center, NPC.rotation - (left ? MathHelper.Pi : 0f));
            rig.SetBoneLocalRotation(facingBone, left ? MathHelper.Pi : 0f);
            rig.SetBoneLocalOffset(armAnchorBones[0], new Vector2(AcropolisDirector.CannonMountX * dir, AcropolisDirector.CannonMountY));
            rig.SetBoneLocalOffset(armAnchorBones[1], new Vector2(AcropolisDirector.HarpoonMountX * dir, AcropolisDirector.HarpoonMountY));
            rig.SetBoneLocalOffset(armAnchorBones[2], new Vector2(0f, AcropolisDirector.HarpoonMuzzleSide * dir));
            rig.SetBoneLocalOffset(armAnchorBones[3], new Vector2(AcropolisDirector.HarpoonMuzzleReach - AcropolisDirector.HarpoonChainTail, AcropolisDirector.HarpoonMuzzleSide * dir));

            UpdateLegTargets();
            UpdateArmTargets();
            UpdateChainTarget();

            rig.Step();
            SyncFacingPieces(left);
        }

        /// <summary>
        /// 腿的模式:未晋升且腾空(<see cref="Dummy"/>)时贴着本体收拢;腾空时收到本体正下方;其余走世界落足步态。
        /// 两种收拢都是原代码的 <c>targetPos</c> 直写 + 0.2 收敛,这里用步态的 Hold 模式(<c>holdRate</c> 0.2)表达
        /// </summary>
        private void UpdateLegTargets() {
            for (int i = 0; i < LegCount; i++) {
                FootPlantGaitSolver gait = gaits[i];
                gait.Velocity = NPC.velocity;
                Vector2 mount = AcropolisDirector.LegMounts[i];
                if (Dummy) {
                    Vector2 hug = NPC.Center + (mount * new Vector2(AcropolisDirector.LegDummySpreadX, AcropolisDirector.LegDummySpreadY))
                        .RotatedBy(NPC.rotation) * NPC.scale;
                    gait.SetLegHold(0, hug);
                }
                else if (Jumping) {
                    Vector2 tuck = NPC.Center + new Vector2(mount.X * AcropolisDirector.LegTuckSideFactor, AcropolisDirector.LegTuckDrop) * NPC.scale;
                    gait.SetLegHold(0, tuck);
                }
                else {
                    gait.SetLegMode(0, null);
                }
            }
        }

        /// <summary>
        /// 臂的瞄点:未晋升形态两臂各自垂向挂点下方(原 <c>DummyPos</c> 0.3 跟随);
        /// 战斗中炮臂吃状态 / 宿主的声明(<see cref="AcropolisStateContext.CannonAim"/>),鱼叉臂吃宿主背景行为的声明;
        /// 没有声明就保持朝向、只结算后坐
        /// </summary>
        private void UpdateArmTargets() {
            if (!NPC.boss) {
                DummyAim(Cannon);
                DummyAim(HarpoonArm);
            }
            else {
                if (Context?.CannonAim is Vector2 aim) {
                    Cannon.Aim(aim, Context.CannonAimTimes);
                }
                if (harpoonAimTarget is Vector2 harpoonAim) {
                    HarpoonArm.Aim(harpoonAim);
                }
            }
            harpoonAimTarget = null;
            Cannon.Apply();
            HarpoonArm.Apply();
        }

        /// <summary>未晋升形态的下垂:虚拟目标向「挂点下方两倍第一节长」0.3 跟随。原式不乘 dir、不随朝向旋转,照搬</summary>
        private void DummyAim(AcropolisArm arm) {
            int seg1 = ReferenceEquals(arm, Cannon) ? armSegBones[0] : armSegBones[2];
            float seg1Length = seg1 >= 0 ? rig.RestLength(seg1) : 0f;
            arm.Aim(arm.DummyPos);
            arm.DummyPos = Vector2.Lerp(arm.DummyPos, NPC.Center + arm.MountOffset + new Vector2(0f, seg1Length * 2f), AcropolisDirector.HandDummyLerp);
        }

        /// <summary>
        /// 鱼叉链:只在鱼叉离架飞行时启用。末端取鱼叉实体「本帧结束后」的位置(<c>Center + velocity</c>):
        /// 鱼叉的 AI 在本体之后跑、位置在 AI 之后积分,直线飞行期这就是它本帧实际绘制的位置,链尾不会拖一帧
        /// </summary>
        private void UpdateChainTarget() {
            NPC hp = HarpoonEntity;
            bool flying = hp != null && hp.active && hp.ModNPC is Harpoon h && !h.OnLauncher;
            chainSolver.Enabled = flying;
            rig.Ribbons[chainRibbon].Visible = flying;
            if (flying) {
                chainSolver.Target = hp.Center + hp.velocity;
            }
        }

        /// <summary>随朝向翻面的件:本体、肩甲、炮管、发射器、停靠鱼叉;两条臂的第一节贴图原来从不翻,保持不翻</summary>
        private void SyncFacingPieces(bool left) {
            for (int i = 0; i < facingPieces.Length; i++) {
                rig.Pieces[facingPieces[i]].Mirror = left;
            }
            for (int i = 0; i < armLinkPieces.Length; i++) {
                rig.Pieces[armLinkPieces[i]].Mirror = false;
            }
            rig.Pieces[facingPieces[4]].Visible = _harpoon < 0 || HarpoonOnLauncher;
        }

        /// <summary>
        /// 鱼叉链绘制。由 <see cref="Harpoon"/> 的绘制路径回调(保持迁移前「链画在鱼叉之下、机体之上」的层次);
        /// 带状件逐顶点吃物块光(原逐节采光),自己切一轮批次再回到 Deferred / AlphaBlend,调用方处在任意已 Begin 的批次内即可
        /// </summary>
        public void DrawHarpoonChain(SpriteBatch spriteBatch) {
            if (!RigReady || chainRibbon < 0 || !rig.Ribbons[chainRibbon].Visible) {
                return;
            }
            Rig2DDrawContext ctx = Rig2DDrawContext.World();
            Rig2DRibbonRenderer.DrawIndices(spriteBatch, rig, rig.Bones, in ctx, [chainRibbon]);
        }
    }
}
