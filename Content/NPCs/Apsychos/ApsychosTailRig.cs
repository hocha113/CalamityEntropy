using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Apsychos.Core;
using InnoVault.Rigs2D.Runtime;
using InnoVault.Rigs2D.Solvers;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    /// <summary>
    /// 尾巴骨架:InnoVault Rigs2D,定义在 <c>Assets/Rigs/Apsychos.rig.json</c>。
    /// <para>
    /// 身体为根,颈骨后退 70,12 节尾骨 + 尾尖骨。两个求解器接管同一串骨,按 <see cref="ApsychosTailStyle"/> 互斥启用:
    /// <c>follow</c>(ChainFollow,0.12 收敘,与迁移前逐节跟随逐帧数学等价)接管 12 节 + 尾尖;
    /// <c>bezier</c>(BezierChain,起端控制柄 230 = 原「本体后 300」减去颈长 70,末端控制柄 160,0.6 软跟随,锚点系)接管前 11 节,
    /// 第 12 节与尾尖都钉在尾尖实体上——原 OnePoint / TwoPoint 的第 12 个采样点就是尾尖。
    /// 三点式 = 运行时把末端控制柄归零(二次曲线),四点式 = 末端切线取尾尖朝向
    /// </para>
    /// <para>
    /// 两种求解器的骨轴约定相反(跟随链 <c>Dir</c> 指向领队,贝塞尔链指向尾端),贴图件按 <c>axisDeg 180</c> 作图,
    /// 贝塞尔模式再给全部尾件加半圈 <c>ExtraRotation</c>。
    /// 骨架是纯本地量,输入只有已同步的本体位姿与尾尖位姿;跟随模式下尾尖实体的位置由骨架写出(原逻辑亦然),各端同算
    /// </para>
    /// </summary>
    public partial class Apsychos
    {
        private Rig2DInstance rig;
        [Rig2DBone("seg{0}", Count = ApsychosDirector.TailSegCount)]
        private readonly int[] segBones = new int[ApsychosDirector.TailSegCount];
        [Rig2DBone("tip")]
        private int tipBone;
        [Rig2DPiece("segA{0}", Count = ApsychosDirector.TailSegCount)]
        private readonly int[] segPiecesA = new int[ApsychosDirector.TailSegCount];
        [Rig2DPiece("segB{0}", Count = ApsychosDirector.TailSegCount)]
        private readonly int[] segPiecesB = new int[ApsychosDirector.TailSegCount];
        [Rig2DPiece("bodyA", "tailA")]
        private readonly int[] phase1Pieces = new int[2];
        [Rig2DPiece("bodyB", "tailB")]
        private readonly int[] phase2Pieces = new int[2];
        [Rig2DSolver("follow")]
        private ChainFollowSolver followSolver;
        [Rig2DSolver("bezier")]
        private BezierChainSolver bezierSolver;

        /// <summary>骨架是否可用(资产已加载且句柄全部命中)</summary>
        public bool TailRigReady => rig != null && rig.Bound && rig.Built;

        /// <summary>尾骨节数(不含尾尖)</summary>
        public int TailSegCount => segBones.Length;

        /// <summary>第 <paramref name="index"/> 节尾骨的中心;骨架未就绪时回落本体坐标</summary>
        public Vector2 TailSegCenter(int index) {
            if (!TailRigReady || index < 0 || index >= segBones.Length) {
                return NPC.Center;
            }
            return rig.Bones[segBones[index]].Pos;
        }

        /// <summary>
        /// 惰性建实例:资产字段在 PostSetupContent 之后才有值,而 ModNPC 模板早于此构造,所以首帧 AI 里建。
        /// 服务端也建(体节落位、尾尖实体写位置都要用),只是没有贴图
        /// </summary>
        private void EnsureTailRig() {
            if (rig != null) {
                return;
            }
            Vault2DRig asset = CERigAssets.Apsychos;
            if (asset == null || !asset.IsValid) {
                return;
            }
            rig = asset.CreateInstance(NPC.whoAmI);
            rig.Bind(this, OnTailRigBound);
        }

        private void OnTailRigBound(Rig2DInstance r) {
            //一次性配置留空:两个求解器的启停与目标都是逐帧写的
        }

        /// <summary>
        /// 尾巴骨架落地。三档数字全部来自 Director。
        /// 根取「本帧结束后」的本体位置(<c>Center + velocity</c>,原逻辑同),这样骨架与引擎积分后实际绘制的本体重合
        /// </summary>
        public void UpdateTail() {
            EnsureTailRig();
            if (rig == null || !rig.Bound || tail == null) {
                return;
            }
            ApsychosTailStyle style = Context?.TailStyle ?? ApsychosTailStyle.Follow;
            bool followMode = style == ApsychosTailStyle.Follow;

            rig.Scale = NPC.scale;
            rig.SetRoot(NPC.Center + NPC.velocity, NPC.rotation);
            followSolver.Enabled = followMode;
            bezierSolver.Enabled = !followMode;

            if (!followMode) {
                //尾尖实体由状态自己摆,这里只读;引擎稍后还会给它积分一次速度,所以取积分后的位置
                Vector2 tipPos = tail.Center + tail.velocity;
                bezierSolver.Target = tipPos;
                if (style == ApsychosTailStyle.TwoPoint) {
                    bezierSolver.HandleB = float.NaN;
                    bezierSolver.TargetDir = tail.rotation.ToRotationVector2();
                }
                else {
                    bezierSolver.HandleB = 0f;
                    bezierSolver.TargetDir = null;
                }
                //贝塞尔链的骨轴指向尾端,尾尖朝向本来就是向后的,直接写
                rig.SetBoneWorld(segBones[^1], tipPos, tail.rotation);
                rig.SetBoneWorld(tipBone, tipPos, tail.rotation);
            }

            rig.Step();

            if (followMode) {
                //跟随模式:尾尖实体挂在链尾(原逻辑就是链写 tail.Center),朝向按尾尖实体「指向后方」的约定翻半圈
                ref Bone2D tb = ref rig.Bones[tipBone];
                tail.Center = tb.Pos;
                tail.rotation = MathHelper.WrapAngle(tb.Dir + MathHelper.Pi);
            }
            else if (style == ApsychosTailStyle.OnePoint) {
                //原三点式把尾尖朝向写成最后一节的朝向:即倒数第二关节指向尾尖的方向
                tail.rotation = rig.Bones[segBones[^2]].Dir;
            }

            //两种求解器骨轴相反,贝塞尔模式全部尾件补半圈
            float extra = followMode ? 0f : MathHelper.Pi;
            for (int i = 0; i < segBones.Length; i++) {
                rig.Pieces[segPiecesA[i]].ExtraRotation = extra;
                rig.Pieces[segPiecesB[i]].ExtraRotation = extra;
            }
            rig.Pieces[phase1Pieces[1]].ExtraRotation = extra;
            rig.Pieces[phase2Pieces[1]].ExtraRotation = extra;
        }

        /// <summary>按阶段切换两套贴图件的可见性(纯绘制)</summary>
        private void SyncTailRigPhase(int phase) {
            bool p2 = phase == 2;
            for (int i = 0; i < segBones.Length; i++) {
                rig.Pieces[segPiecesA[i]].Visible = !p2;
                rig.Pieces[segPiecesB[i]].Visible = p2;
            }
            for (int i = 0; i < phase1Pieces.Length; i++) {
                rig.Pieces[phase1Pieces[i]].Visible = !p2;
                rig.Pieces[phase2Pieces[i]].Visible = p2;
            }
        }
    }
}
