using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Prophet.Core;
using InnoVault.Rigs2D.Runtime;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    /// <summary>
    /// 先知的骨架:InnoVault Rigs2D,定义在 <c>Assets/Rigs/Prophet.rig.json</c>。
    /// <para>
    /// 本体为根,根朝向取平滑后的绘制朝向 <c>rl</c>。四片翅骨挂在本体上:<c>wing2L/R</c> 偏移 (−20, ∓20)、<c>wing1L/R</c> 偏移 (0, ∓20),
    /// 每帧写局部旋转 <c>∓rotj</c> / <c>∓1 ± rotj</c>(原 DrawFins 的四个角度);右侧两件 <c>mirror</c> 沿骨轴翻面,
    /// 与原来「<c>FlipVertically</c> + 把锚点换到另一条边」的画法逐像素等价。
    /// 原代码里内翅左片的偏移绕的是 <c>NPC.rotation</c>、其余三片绕 <c>rl</c>;骨架只有一个根朝向,四片统一跟 <c>rl</c>
    /// </para>
    /// <para>
    /// 尾巴:<c>tailAnchor</c> 骨在本体后方 26,挂一条 10 节 × 24 的 <c>VerletStrand</c>(静息伸展力沿 −骨轴向后撑直),
    /// 带状件 <c>Tail</c> 整宽 80 铺满,<c>ring</c> 件居中钉在 <c>tail8</c> 骨近端(原来画在第 9 个尾迹点上)。
    /// 带状件的骨序是<b>尖 → 根</b>(<c>tailTip</c> 零长装饰骨 → <c>tail10</c> … <c>tail1</c>,<c>includeTip</c> 关掉):
    /// 原贴图叉尾在 u = 0、宽端在 u = 1,而带状件的 u 固定从首骨走到末骨,倒着列骨序贴图方向才与原实现一致
    /// </para>
    /// <para>
    /// 纯本地表现量:输入只有已同步的本体位姿与本地推导的 <c>rl</c> / 鳍相位,摆动相位取 <c>rig.Time</c> / <c>rig.Seed</c>,
    /// 不再读 <c>Main.GameUpdateCount</c>;不新增任何过线字段。服务端也建也 Step(没有贴图,骨骼照样有效)
    /// </para>
    /// </summary>
    public partial class TheProphet
    {
        private Rig2DInstance rig;
        [Rig2DBone("wing2L", "wing2R", "wing1L", "wing1R")]
        private readonly int[] wingBones = new int[4];
        [Rig2DPiece("body")]
        private int bodyPiece;

        /// <summary>骨架是否可用(资产已加载、句柄全部命中、已完成首次求解)</summary>
        public bool RigReady => rig != null && rig.Bound && rig.Built;

        /// <summary>
        /// 惰性建实例:资产字段在 PostSetupContent 之后才有值,而 ModNPC 模板早于此构造,所以首帧 AI 里建
        /// </summary>
        private void EnsureRig() {
            if (rig != null) {
                return;
            }
            Vault2DRig asset = CERigAssets.Prophet;
            if (asset == null || !asset.IsValid) {
                return;
            }
            rig = asset.CreateInstance(NPC.whoAmI);
            rig.Bind(this, OnRigBound);
        }

        /// <summary>
        /// 天顶世界的本体由旧巡洋舰替身自己画(<c>OlderCruiserAIGNPC.PreDraw</c>),骨架只出翅与尾。
        /// 世界级恒定开关,绑定时写一次即可;热重载重绑会再跑一遍
        /// </summary>
        private void OnRigBound(Rig2DInstance r) {
            r.Pieces[bodyPiece].Visible = !Main.zenithWorld;
        }

        /// <summary>
        /// 骨架落地(各端都跑,出生演出期间也跑:本体件在 <c>spawnAnm &lt; 60</c> 时就要就位)。
        /// 根取「本帧结束后」的位置(<c>Center + velocity</c>),与引擎积分后实际绘制的本体重合;根朝向取 <c>rl</c>
        /// </summary>
        private void UpdateRig() {
            EnsureRig();
            if (rig == null || !rig.Bound) {
                return;
            }
            rig.Scale = NPC.scale;
            rig.SetRoot(NPC.Center + NPC.velocity, rl);

            float rotj = FinSwing();
            rig.SetBoneLocalRotation(wingBones[0], -rotj);
            rig.SetBoneLocalRotation(wingBones[1], rotj);
            rig.SetBoneLocalRotation(wingBones[2], -ProphetDirector.Wing1RestAngle + rotj);
            rig.SetBoneLocalRotation(wingBones[3], ProphetDirector.Wing1RestAngle - rotj);
            rig.Step();
        }

        /// <summary>
        /// 鳍摆角 <c>rotj</c>(0 ~ 1 rad):相位前 40% 余弦缓动升到 1,后 60% 落回 0。
        /// 原 DrawFins 里的算式原样搬来,相位由 <see cref="UpdateFins"/> 推进
        /// </summary>
        private float FinSwing() {
            float rise = ProphetDirector.FinSwingRise;
            return finRotCounter <= rise
                ? CEUtils.GetRepeatedCosFromZeroToOne(finRotCounter / rise, 1)
                : 1 - CEUtils.GetRepeatedCosFromZeroToOne((finRotCounter - rise) / (1 - rise), 1);
        }

        /// <summary>
        /// 瞬移落位后硬重建(原来这里是 <c>tail.Clear()</c>):根先写到新位置,再让尾链从静息姿态直接摆好,
        /// 免得下一帧 Step 把这段位移当成一次真实运动去甩尾。骨架未建好时跳过
        /// </summary>
        private void SnapRig() {
            if (rig == null || !rig.Bound) {
                return;
            }
            rig.SetRoot(NPC.Center, rl);
            rig.Snap();
        }
    }
}
