using CalamityEntropy.Assets.Register;
using InnoVault.Rigs2D.Runtime;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage
{
    /// <summary>
    /// 骨架根取 Center,朝向取 NPC.rotation;贴图朝下,件的轴角是 90 度
    /// 翼骨正角度是朝后掠,R 侧取正、L 侧取负
    /// 只在有画面的端推进,没有玩法读骨骼位置
    /// </summary>
    public partial class StarmoonSage
    {
        private Rig2DInstance rig;
        [Rig2DBone("wingLowL", "wingLowR", "wingUpL", "wingUpR")]
        private readonly int[] wingBones = new int[4];
        //句柄由 Bind 按名填入,没命中时是 -1
        [Rig2DBone("tail6")]
        private int ringBone = -1;
        /// <summary>冲刺残影用的姿态快照,每张快照的 Extra[0] 记下拍摄时的 ghostClock</summary>
        private readonly Rig2DPoseTrail poseTrail = new(5, 1) { MinInterval = 2 };
        /// <summary>只在有画面的端逐帧递增,残影按它淡出</summary>
        private float ghostClock;

        /// <summary>资产已加载、句柄命中、做过首次求解</summary>
        public bool RigReady => rig != null && rig.Bound && rig.Built;

        /// <summary>资产在 PostSetupContent 之后才有,首帧 AI 里建</summary>
        private void EnsureRig() {
            if (rig != null) {
                return;
            }
            Vault2DRig asset = CERigAssets.StarmoonSage;
            if (asset == null || !asset.IsValid) {
                return;
            }
            rig = asset.CreateInstance(NPC.whoAmI);
            rig.Bind(this);
        }

        private void UpdateRig() {
            EnsureRig();
            if (rig == null || !rig.Bound) {
                return;
            }
            rig.Scale = NPC.scale;
            rig.SetRoot(NPC.Center, NPC.rotation);

            float flap = MathF.Sin(wingPhase);
            float lag = MathF.Sin(wingPhase - 0.9f);
            //大展时双翼前探,收拢时后掠,下翼的扇动慢上翼半拍
            float up = (WingSpread >= 0f ? -0.28f * WingSpread : -0.6f * WingSpread) + 0.1f * flap;
            float low = (WingSpread >= 0f ? -0.2f * WingSpread : -0.45f * WingSpread) + 0.14f * lag;
            rig.SetBoneLocalRotation(wingBones[0], -low);
            rig.SetBoneLocalRotation(wingBones[1], low);
            rig.SetBoneLocalRotation(wingBones[2], -up);
            rig.SetBoneLocalRotation(wingBones[3], up);
            rig.Step();

            ghostClock++;
            float speed = NPC.velocity.Length();
            if (rig.Built && Context.BlinkTimer <= 0) {
                Rig2DPoseTrail.Snapshot snapshot = poseTrail.Capture(rig, Utils.GetLerpValue(14f, 30f, speed, true));
                if (snapshot != null && snapshot.Extra.Length > 0) {
                    snapshot.Extra[0] = ghostClock;
                }
            }
        }

        /// <summary>根先写到新位置再 Snap,免得下一帧把瞬移当成甩尾</summary>
        private void SnapRig() {
            if (rig == null || !rig.Bound) {
                return;
            }
            rig.SetRoot(NPC.Center, NPC.rotation);
            rig.Snap();
            poseTrail.Clear();
        }
    }
}