using CalamityEntropy.Assets.Register;
using InnoVault.Rigs2D.Runtime;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth
{
    /// <summary>
    /// 月华之蛾的骨架:InnoVault Rigs2D,定义在 <c>Assets/Rigs/Luminaris.rig.json</c>。
    /// <para>
    /// 本体为根(8 帧竖排图集件,锚在原点 (W/2, 104)),两条尾巴各是一条 9 节 <c>VerletStrand</c>,
    /// 锚在本体下方 (∓14, 32)。原 <c>Utilities.Rope</c> 每帧走 5 个整步(每步重力 0.12、阻尼 1/1.054、30 次约束),
    /// 对应 <c>substeps 5</c> 下的 <c>gravity 3.0</c>(3.0 × 0.2² × 5 = 0.6 = 5 × 0.12)、<c>damping 0.769</c>(0.9488⁵)、<c>iterations 30</c>;
    /// 子步的锚点在上帧位置与本帧位置之间插值,正是原代码「绳根沿 OldPos → Center 推进」的做法。
    /// 纯本地表现量,输入只有已同步的本体位姿
    /// </para>
    /// </summary>
    public partial class Luminaris
    {
        private Rig2DInstance rig;
        [Rig2DPiece("body")]
        private int bodyPiece;

        /// <summary>骨架是否可用</summary>
        public bool BodyRigReady => rig != null && rig.Bound && rig.Built;

        private void EnsureBodyRig() {
            if (rig != null) {
                return;
            }
            Vault2DRig asset = CERigAssets.Luminaris;
            if (asset == null || !asset.IsValid) {
                return;
            }
            rig = asset.CreateInstance(NPC.whoAmI);
            rig.Bind(this, null);
        }

        /// <summary>
        /// 骨架落地(各端都跑)。根取「本帧结束后」的位置(<c>Center + velocity</c>):
        /// 原代码给绳根加的正是 <c>NPC.velocity</c>,这样尾巴与引擎积分后实际绘制的本体重合
        /// </summary>
        private void UpdateBodyRig() {
            EnsureBodyRig();
            if (rig == null || !rig.Bound) {
                return;
            }
            rig.Scale = NPC.scale;
            rig.SetRoot(NPC.Center + NPC.velocity, NPC.rotation);
            rig.Pieces[bodyPiece].Frame = (frameCounter / 4) % Main.npcFrameCount[Type];
            rig.Step();
        }
    }
}
